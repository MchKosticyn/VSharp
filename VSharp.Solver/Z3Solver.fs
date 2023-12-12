namespace VSharp.Solver

open System
open ISolver
open Microsoft.Z3

module internal Z3Solver =
    type Z3Solver(ctx : Context) =
        interface ISolverCommon<Expr, BoolExpr, BitVecExpr, FPExpr, ArrayExpr, BitVecNum, FPNum, FuncDecl, Sort, Model, Solver> with
            // Creation of constant expressions
            member t.MkTrue() = ctx.MkTrue()
            member t.MkFalse() = ctx.MkFalse()
            member t.MkConst (x : string, y : Sort) = ctx.MkConst (x, y)
            member t.MkConstDecl (x : string, y : Sort) = ctx.MkConstDecl (x, y)
            member t.MkNumeral (x : string, y : Sort) = ctx.MkNumeral (x, y)
            member t.MkNumeral (x : int, y : Sort) =  ctx.MkNumeral (x, y)
            member t.MkNumeral (x : uint, y : Sort) =  ctx.MkNumeral (x, y)
            member t.MkFPNumeral (x : float32, y : Sort) =  ctx.MkFPNumeral (x , y :?> FPSort)
            member t.MkFPDoubleNumeral (x : float, y :  Sort) = ctx.MkFPNumeral (x, y :?> FPSort)
            member t.MkFPRoundNearestTiesToEven() = ctx.MkFPRoundNearestTiesToEven()

            //Creating sorts
            member t.MkBoolSort() = ctx.MkBoolSort()
            member t.MkBitVecSort (x :uint)= ctx.MkBitVecSort x
            member t.MkFPSort32() = ctx.MkFPSort32()
            member t.MkFPSort64() = ctx.MkFPSort64()
            member t.MkArraySort (x : Sort, y : Sort) = ctx.MkArraySort (x, y)
            member t.MkRangeArraySort (x: Sort array, y : Sort) = ctx.MkArraySort (x, y)
            member t.BoolSort() = ctx.BoolSort

            //Creating terms
            member t.MkBV (x : uint32, y : uint32) = ctx.MkBV (x, y)
            member t.MkBVInt (x : int, y : uint32) = ctx.MkBV (x, y)
            member t.MkBool(x : bool) = ctx.MkBool x
            member t.MkFPToFP (x : Expr, y : FPExpr, z : Sort) = ctx.MkFPToFP (x :?> FPRMExpr, y, z :?> FPSort)
            member t.MkFPToFP (x : Expr, y : BitVecExpr, z : Sort, sign : Boolean) =
                ctx.MkFPToFP (x :?> FPRMExpr, y, z :?> FPSort, sign)
            member t.MkApp (x : FuncDecl, [<ParamArray>] y : Expr array) = ctx.MkApp (x, y)
            member t.MkFuncDecl(x : string, y :Sort array, z : Sort) = ctx.MkFuncDecl(x, y, z)
            member t.MkFP(x :BitVecExpr, y : BitVecExpr, z : BitVecExpr) = ctx.MkFP(x, y, z)

            //Array operations
            member t.MkSelect (x : ArrayExpr, y : Expr) = ctx.MkSelect (x , y)
            member t.MkRangeSelect (x : ArrayExpr, y : Expr array) = ctx.MkSelect (x, y)

            //Solver creating
            member t.MkSolver(timeoutMs : uint option) =
                let solver = ctx.MkSolver()
                match timeoutMs with
                | Some timeoutMs ->
                    let parameters = ctx.MkParams().Add("timeout", timeoutMs);
                    solver.Parameters <- parameters
                | None -> ()
                solver

            //Terms conversion
            member t.MkFPToIEEEBV (x : FPExpr) = ctx.MkFPToIEEEBV x
            member t.MkFPToBV(x : Expr, y : FPExpr, z : uint, signed : bool) =
                ctx.MkFPToBV(x :?> FPRMExpr, y, z, signed)

            //down casts from expr
            member t.MkEToBE(x : Expr) = x :?> BoolExpr
            member t.MkEToBVE(x : Expr) = x :?> BitVecExpr
            member t.MkEToFPE(x : Expr) = x :?> FPExpr
            member t.MkEToAE(x : Expr) = x :?> ArrayExpr
            //up casts to expr
            member t.MkBEToE(x : BoolExpr) = x :> Expr
            member t.MkBVEToE(x : BitVecExpr) = x :> Expr
            member t.MkFPEToE(x : FPExpr) = x :> Expr
            member t.MkAEToE(x : ArrayExpr) = x :> Expr
            //down casts to nums
            member t.MkEToBVNum(x : Expr) = x :?> BitVecNum
            member t.MkEToFPNum(x : Expr) = x :?> FPNum
            member t.MkBVNumToBVE(x : BitVecNum) = x :> BitVecExpr

//Terms conversion check
            //down casts from expr
            member t.MkCheckEToBE(x : Expr) = x :? BoolExpr
            member t.MkCheckEToBVE(x : Expr) = x :? BitVecExpr
            member t.MkCheckEToFPE(x : Expr) = x :? FPExpr
            member t.MkCheckEToAE(x : Expr) = x :? ArrayExpr
            //down casts to nums
            member t.MkCheckBVEToBVNum(x : BitVecExpr) = x :? BitVecNum
            member t.MkCheckFPEToFPNum(x : FPExpr) = x :? FPNum
            member t.MkCheckEToBVNum(x : Expr) = x :? BitVecNum
            member t.MkCheckEToFPNum(x : Expr) = x :? FPNum
            member T.MkCheckEToINum(x : Expr) = x :? IntNum
            member T.MkCheckEToRNum(x : Expr) = x :? IntNum

            //Common logic
            member t.MkNot (x : BoolExpr) = ctx.MkNot x
            member t.MkAnd (x : BoolExpr, y : BoolExpr) = ctx.MkAnd (x , y)
            member t.MkAndArray ([<ParamArray>] y : BoolExpr array) = ctx.MkAnd y
            member t.MkAndSeq (x : seq<BoolExpr>) = ctx.MkAnd x
            member t.MkOr (x : BoolExpr, y : BoolExpr) = ctx.MkOr (x, y)
            member t.MkXor(x : Expr seq) = ctx.MkXor (Seq.cast<BoolExpr> x)
            member t.MkOrArray ([<ParamArray>] x : BoolExpr array) = ctx.MkOr x
            member t.MkOrSeq (x : seq<BoolExpr>) = ctx.MkOr x
            member t.MkEq (x : Expr, y : Expr) = ctx.MkEq (x, y)
            member t.MkITE(x : BoolExpr, y : Expr, z: Expr) = ctx.MkITE(x, y, z)

            //BitVec logic
            member t.MkBVAnd (x : Expr, y : Expr) = ctx.MkBVAND (x :?> BitVecExpr, y :?> BitVecExpr)
            member t.MkBVOr (x : Expr, y : Expr) = ctx.MkBVOR (x :?> BitVecExpr, y :?> BitVecExpr)
            member t.MkBVXor (x : Expr, y : Expr) = ctx.MkBVXOR (x :?> BitVecExpr, y :?> BitVecExpr)
            member t.MkBVNot (x : Expr) = ctx.MkBVNot (x :?> BitVecExpr)
            member t.MkBVULT (x : Expr, y : Expr) = ctx.MkBVULT (x :?> BitVecExpr, y :?> BitVecExpr)
            member t.MkBVULEq (x : Expr, y : Expr) = ctx.MkBVULE (x :?> BitVecExpr, y :?> BitVecExpr)
            member t.MkBVUGT (x : Expr, y : Expr) = ctx.MkBVUGT (x :?> BitVecExpr, y :?> BitVecExpr)
            member t.MkBVUGEq (x : Expr, y : Expr) = ctx.MkBVUGE (x :?> BitVecExpr, y :?> BitVecExpr)
            member t.MkBVSLT (x : Expr, y : Expr) = ctx.MkBVSLT (x :?> BitVecExpr, y :?> BitVecExpr)
            member t.MkBVSLEq (x : Expr, y : Expr) = ctx.MkBVSLE (x :?> BitVecExpr, y :?> BitVecExpr)
            member t.MkBVSGT (x : Expr, y : Expr) = ctx.MkBVSGT (x :?> BitVecExpr, y :?> BitVecExpr)
            member t.MkBVSGEq (x : Expr, y : Expr) = ctx.MkBVSGE (x :?> BitVecExpr, y :?> BitVecExpr)

            //BitVec shifts
            member t.MkBVSHL (x : Expr, y : Expr) = ctx.MkBVSHL (x :?> BitVecExpr, y :?> BitVecExpr)
            member t.MkBVASHR (x : Expr, y : Expr) = ctx.MkBVASHR (x :?> BitVecExpr, y :?> BitVecExpr)
            member t.MkBVLSHR (x : Expr, y : Expr) = ctx.MkBVLSHR (x :?> BitVecExpr, y :?> BitVecExpr)

             //BitVec conversions
            member t.MkBVNeg (x : Expr) = ctx.MkBVNeg (x :?> BitVecExpr)
            member t.MkSignExt (x : uint, y : BitVecExpr) = ctx.MkSignExt (x, y)
            member t.MkZeroExt (x : uint, y : BitVecExpr) = ctx.MkZeroExt (x, y )
            member t.MkExtract (x : uint, y : uint, z : BitVecExpr) = ctx.MkExtract (x, y, z)
            member t.MkConcat (x : BitVecExpr, y : BitVecExpr) = ctx.MkConcat (x, y)

            //BitVec arithmetics
            member t.MkBVAdd (x : Expr, y : Expr) = ctx.MkBVAdd (x :?> BitVecExpr, y :?> BitVecExpr)
            member t.MkBVSub (x : Expr, y : Expr) = ctx.MkBVSub (x :?> BitVecExpr, y :?> BitVecExpr)
            member t.MkBVMul (x : Expr, y : Expr) = ctx.MkBVMul (x :?> BitVecExpr, y :?> BitVecExpr)
            member t.MkBVUDiv (x : Expr, y : Expr) = ctx.MkBVUDiv (x :?> BitVecExpr, y :?> BitVecExpr)
            member t.MkBVSDiv (x : Expr, y : Expr) = ctx.MkBVSDiv (x :?> BitVecExpr, y :?> BitVecExpr)
            member t.MkBVURem (x : Expr, y : Expr) = ctx.MkBVURem (x :?> BitVecExpr, y :?> BitVecExpr)
            member t.MkBVSRem (x : Expr, y : Expr) = ctx.MkBVSRem (x :?> BitVecExpr, y :?> BitVecExpr)

             //BitVec arithmetics without overflow/underflow
            member t.MkBVAddNoUnderflow (x : Expr, y : Expr) = ctx.MkBVAddNoUnderflow (x :?> BitVecExpr, y :?> BitVecExpr)
            member t.MkBVAddNoOverflow (x : Expr, y : Expr, sign : Boolean) =
                ctx.MkBVAddNoOverflow (x :?> BitVecExpr, y :?> BitVecExpr , sign)
            member t.MkBVMulNoUnderflow (x : Expr, y : Expr) = ctx.MkBVMulNoUnderflow (x :?> BitVecExpr, y :?> BitVecExpr)
            member t.MkBVMulNoOverflow (x : Expr, y : Expr, sign : Boolean) =
                ctx.MkBVMulNoOverflow (x :?> BitVecExpr, y :?> BitVecExpr, sign)

            //FP Logic
            member t.MkFPEq(x: FPExpr, y : FPExpr) = ctx.MkFPEq(x, y)
            member t.MkFPLT (x : Expr, y : Expr) = ctx.MkFPLt (x :?> FPExpr, y :?> FPExpr)
            member t.MkFPLEq (x : Expr, y : Expr) = ctx.MkFPLEq (x :?> FPExpr, y :?> FPExpr)
            member t.MkFPGT (x : Expr, y : Expr) = ctx.MkFPGt (x :?> FPExpr, y :?> FPExpr)
            member t.MkFPGEq (x : Expr, y : Expr) = ctx.MkFPGEq (x :?> FPExpr, y :?> FPExpr)

            //FP arithmetics
            member t.MkFPAdd (x : Expr, y : Expr, z : Expr) = ctx.MkFPAdd (x :?> FPRMExpr, y :?> FPExpr, z :?> FPExpr)
            member t.MkFPMul (x : Expr, y : Expr, z : Expr) = ctx.MkFPMul (x :?> FPRMExpr, y :?> FPExpr, z :?> FPExpr)
            member t.MkFPSub (x : Expr, y : Expr, z : Expr) = ctx.MkFPSub (x :?> FPRMExpr, y :?> FPExpr, z :?> FPExpr)
            member t.MkFPDiv (x : Expr, y : Expr, z : Expr) = ctx.MkFPDiv (x :?> FPRMExpr, y :?> FPExpr, z :?> FPExpr)
            member t.MkFPRem (x : Expr, y : Expr) = ctx.MkFPRem (x :?> FPExpr, y :?> FPExpr)
            member t.MkFPNeg (x : Expr) = ctx.MkFPNeg (x :?> FPExpr)

//Expression methods
            //Common methods
            //properties
            member t.String(x : Expr) = x.String
            member t.GetArgs(x : Expr) = x.Args
            member t.GetSort(x : Expr) = x.Sort
            member t.GetExprType(x : Expr) = x.GetType()
            //Check
            member t.IsConst(x : Expr) = x.IsConst
            member t.IsConstantArray(x : Expr) = x.IsConstantArray
            member t.IsDefaultArray(x : Expr) = x.IsConstantArray
            member t.IsStore(x : Expr) = x.IsStore
            member t.IsQuantifier(x : Expr) = x.IsQuantifier
            member t.IsApp(x : Expr) = x.IsApp
            member t.IsTrue(x : Expr) = x.IsTrue
            member t.IsFalse(x : Expr) = x.IsFalse
            member t.IsNot(x : Expr) = x.IsNot
            member t.IsAnd(x : Expr) = x.IsAnd
            member t.IsOr(x : Expr) = x.IsOr
            member t.IsEq(x : Expr) = x.IsEq
            member t.IsBVSGT(x : Expr) = x.IsBVSGT
            member t.IsBVUGT(x : Expr) = x.IsBVUGT
            member t.IsBVSGE(x : Expr) = x.IsBVSGE
            member t.IsBVUGE(x : Expr) = x.IsBVUGE
            member t.IsBVSLT(x : Expr) = x.IsBVSLT
            member t.IsBVULT(x : Expr) = x.IsBVULT
            member t.IsBVSLE(x : Expr) = x.IsBVSLE
            member t.IsBVULE(x : Expr) = x.IsBVULE
            member t.IsFPGT(x : Expr) = x.IsFPGt
            member t.IsFPGE(x : Expr) = x.IsFPGe
            member t.IsFPLT(x : Expr) = x.IsFPLt
            member t.IsFPLE(x : Expr) = x.IsFPLe

            //BitVecNum properties
            member t.SortSize(x : BitVecExpr) = x.SortSize
            member t.Int64(x: BitVecNum) = x.Int64
            member t.Int(x: BitVecNum) = x.Int
            member t.BigInteger(x: BitVecNum) = x.BigInteger

            //FPNum Properties
            member t.FPEBits(x : FPNum) = x.EBits
            member t.IsNaN(x: FPNum) = x.IsNaN
            member t.IsFPPlusInfinity(x : FPNum) = x.IsFPPlusInfinity
            member t.IsFPMinusInfinity(x : FPNum) = x.IsFPMinusInfinity
            member t.ExponentInt64(x : FPNum) = x.ExponentInt64()
            member t.SignificandUInt64(x : FPNum) = x.SignificandUInt64
            member t.Sign(x : FPNum) = x.Sign
            //IntNum properties
            member t.GetIntFromIntNum(x : Expr) = (x :?> IntNum).Int
            //RatNum properties
            member t.GetValueFromRatNum(x: Expr) =
                let r = x :?> RatNum
                double(r.Numerator.Int) * 1.0 / double(r.Denominator.Int)

            member t.GetQuantifierBody(x : Expr) = (x :?> Quantifier).Body

            member t.MkSModel (x : Solver) = x.Model
            member t.CheckSat (x : Solver, y : Expr array) =
                match x.Check y with
                |Status.SATISFIABLE -> IStatus.SATISFIABLE
                |Status.UNSATISFIABLE -> IStatus.UNSATISFIABLE
                |Status.UNKNOWN -> IStatus.UNKNOWN
                |_ -> failwith "Unexpected status at check in Z3"

            member t.Assert (x : Solver, [<ParamArray>] y : BoolExpr array) = x.Assert y
            member t.GetReasonUnknown (x : Solver) = x.ReasonUnknown

            //Model operations
            member t.Eval (x : Model, y : Expr, z : bool) = x.Eval(y, z)
