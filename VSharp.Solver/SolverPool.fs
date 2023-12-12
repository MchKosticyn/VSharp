namespace VSharp.Solver
open Microsoft.Z3
open VSharp.Core.SolverInteraction
open VSharp.Solver.Encoding
open VSharp.Solver.ISolver
type public SupportedSolvers =
    | Yices
    | Z3

module public SolverPool =

    let mutable private currentSolver = Z3

    let switchSolver (solver : SupportedSolvers) =
        currentSolver <- solver

    let mkSolver (timeoutMs : int, solver : SupportedSolvers) : ISolver =
        let timeoutOpt = if timeoutMs > 0 then Some(uint timeoutMs) else None
        match currentSolver with
        | Z3 ->
            let ctx = new Context()
            let builder = Z3Solver.Z3Solver(ctx) :> ISolverCommon<Expr, BoolExpr, BitVecExpr, FPExpr, ArrayExpr, BitVecNum, FPNum, FuncDecl, Sort, Model, Solver, Params>
            Encoding.CommonSolver(builder, timeoutOpt) :> ISolver
        | Yices -> failwith "Yices not implemented yet"
    let reset() =
        Z3Solver.reset()
