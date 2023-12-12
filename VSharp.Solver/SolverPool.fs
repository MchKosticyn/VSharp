namespace VSharp.Solver
open Microsoft.Z3
open VSharp.Core.SolverInteraction
open VSharp.Solver.Encoding
open VSharp.Solver.ISolver
open VSharp

type public SupportedSolvers =
    | Yices
    | Z3

module public SolverPool =

    let mutable private currentSolverType = Z3

    let mutable private currentSolver : ISolver option = None

    let switchSolver (solver : SupportedSolvers) =
        currentSolverType <- solver

    let mkSolver (timeoutMs : int) : ISolver =
        let timeoutOpt = if timeoutMs > 0 then Some(uint timeoutMs) else None
        match currentSolverType with
        | Z3 ->
            let ctx = new Context()
            let solver = Z3Solver.Z3Solver(ctx) :> ISolverCommon<Expr, BoolExpr, BitVecExpr, FPExpr, ArrayExpr, BitVecNum, FPNum, FuncDecl, Sort, Model, Solver>
            let builder = SolverBuilder(solver)
            let commonSolver = CommonSolver(builder, timeoutOpt) :> ISolver
            currentSolver <- Some commonSolver
            commonSolver
        | Yices -> internalfail "Yices not implemented yet"

    let reset() =
        assert(Option.isSome currentSolver)
        currentSolver.Value.Reset()
