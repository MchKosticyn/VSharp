namespace VSharp.Solver
 
open System.Collections.Generic
open FSharpx.Collections
open VSharp
open VSharp.Core.SolverInteraction
open VSharp.Core
 
module public Simplification =

    let private (|Connective|_|) = function
        | Conjunction ts -> Some ts
        | Disjunction ts -> Some ts
        | _ -> None
 
    let private (|Leaf|_|) (t: term) =
        match t with
        | Connective _ -> None
        | _ -> Some t
 
    type private Redundancy =
        | NonConstraining
        | NonRelaxing
        | NotRedundant
 
    type private SATResult =
        | SAT
        | UNSAT
 
    exception UnknownSolverResult
    exception AssertionFailed
 
    let private toSATResult r =
        match r with
        | SmtSat _ -> SAT
        | SmtUnsat _ -> UNSAT
        | SmtUnknown _ -> raise UnknownSolverResult
 
    let private simplify
        (assumptions: term)
        (formula: term)
        (encCtx: encodingContext)
        (solver: IIncrementalSolver): term =
 
        let mkConjunction ts = Expression (Operator OperationType.LogicalAnd) (Seq.toList ts) Bool
        let mkDisjunction ts = Expression (Operator OperationType.LogicalOr) (Seq.toList ts) Bool
        let mkNegation t = Expression (Operator OperationType.LogicalNot) [t] Bool
 
        let rec transformXor (t: term): term =
 
            let unwrapXor x y =
                let x' = transformXor x
                let y' = transformXor y
                mkDisjunction [
                        mkConjunction [mkNegation x'; y'];
                        mkConjunction [x'; mkNegation y']
                ]
 
            match t with
            | Xor(x, y) -> unwrapXor x y
            | Conjunction xs -> mkConjunction <| List.map transformXor xs
            | Disjunction xs -> mkDisjunction <| List.map transformXor xs
            | Negation x -> mkNegation <| transformXor x
            | _ -> t
 
        let rec toNNF (t: term) =
            match t with
            | Negation t' ->
                match t' with
                | Conjunction ts -> mkDisjunction <| List.map (mkNegation << toNNF) ts
                | Disjunction ts -> mkDisjunction <| List.map (mkNegation << toNNF) ts
                | Negation nt -> nt
                | _ -> t
            | Conjunction ts -> mkConjunction <| List.map toNNF ts
            | Disjunction ts -> mkDisjunction <| List.map toNNF ts  
            | _ -> t
 
        let check() = toSATResult <| solver.Check()
        let push() = solver.Push()
        let pop() = solver.Pop()
        let solverAssert (formula: term) = if solver.Assert formula encCtx then () else raise AssertionFailed

        let rec simplify' formula = 

            let checkRedundancy (leaf: term) =
                push()
                solverAssert <| mkNegation leaf
                match check() with
                | UNSAT ->
                    pop()
                    NonConstraining
                | SAT ->
                    pop()
                    push()
                    solverAssert <| leaf
                    match check() with
                    | UNSAT ->
                        pop()
                        NonRelaxing
                    | SAT ->
                        pop()
                        NotRedundant
 
            let handleLeaf t  =
                match checkRedundancy t with
                | NonConstraining -> True
                | NonRelaxing -> False
                | NotRedundant -> t
 
 
            let handleConnective t ts =
                let rec updateChildren (oldChildren : term[]) (newChildren : term[]) op =
                        Array.iteri (fun i oldChild ->
                            push()
                            solverAssert << mkConjunction <| seq {
                                for j in 0 .. i - 1 ->
                                    match op with
                                    | Conjunction _ -> newChildren[j]
                                    | Disjunction _ -> mkNegation newChildren[j]
                                    | _ -> __unreachable__()
                                for j in i + 1 .. oldChildren.Length - 1 ->
                                    match op with
                                    | Conjunction _ -> oldChildren[j]
                                    | Disjunction _ -> mkNegation oldChildren[j]
                                    | _ -> __unreachable__()
                            }
                            newChildren[i] <- simplify' oldChild
                            pop()
                        ) oldChildren
                        if Array.forall2 (=) oldChildren newChildren then
                            match op with
                            | Conjunction _ -> mkConjunction newChildren
                            | Disjunction _ -> mkDisjunction newChildren
                            | _ -> __unreachable__()
                        else
                            updateChildren newChildren oldChildren op
                updateChildren (List.toArray ts) (List.toArray ts) t
 
            match formula with
            | Connective ts -> handleConnective formula ts
            | Leaf t -> handleLeaf t
            | _ -> __unreachable__()
 
        let rec syntaxSimplify formula =
 
            let isTrue t =
                match t with
                | True -> true
                | _ -> false
 
            let isFalse t =
                match t with
                | False -> true
                | _ -> false
 
            let hasTrue ts = (List.filter isTrue ts).Length > 0
            let hasFalse ts = (List.filter isFalse ts).Length > 0
 
            match formula with
            | Conjunction ts ->
                let simplifiedTerms = List.map syntaxSimplify ts
                if hasFalse simplifiedTerms then False
                else
                    match List.filter (not << isTrue) simplifiedTerms with
                    | [] -> True
                    | [t] -> t
                    | ts -> mkConjunction ts 
            | Disjunction ts ->
                let simplifiedTerms = List.map syntaxSimplify ts
                if hasTrue simplifiedTerms then True
                else
                    match List.filter (not << isFalse) simplifiedTerms with
                    | [] -> False
                    | [t] -> t
                    | ts -> mkDisjunction ts
            | _ -> formula
 
        try
            match formula with
            | Leaf t -> t
            | _ ->
                push()
                assumptions |> solverAssert
                let x = transformXor formula |> toNNF |> simplify'
                Logger.error $"@ %O{x}"
                pop()
                x |> syntaxSimplify
        with
            | :? UnknownSolverResult -> formula
            | :? AssertionFailed -> formula
    
    type Simplifier() =
        let mutable cache = Dictionary<term * term, term>()
        interface ISimplifier with
            member x.Simplify assumptions condition ctx solver = Dict.getValueOrUpdate cache (assumptions, condition) (
                fun () -> simplify assumptions condition ctx solver
            )
