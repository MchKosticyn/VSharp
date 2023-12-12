module VSharp.Solver.ISolver

open System
open System.Numerics
type IStatus =
    SATISFIABLE = 0
    |UNSATISFIABLE = 1
    |UNKNOWN = 2

type ISolverCommon<'IExpr, 'IBoolExpr, 'IBitVecExpr, 'IFPExpr, 'IArrayExpr, 'IBitVecNum, 'IFPNum, 'IFuncDecl, 'ISort, 'IModel, 'ISolver when
    'IExpr : equality and 'IBoolExpr : equality and 'IBitVecExpr : equality and 'IArrayExpr : equality and 'IFPExpr : equality and 'IBitVecNum : equality and 'ISort : equality and 'IArrayExpr : null and 'IFuncDecl : null> =

// Creation of constant expressions
    abstract member MkTrue: unit -> 'IBoolExpr
    abstract member MkFalse: unit -> 'IBoolExpr
    abstract member MkConst: string * 'ISort -> 'IExpr
    abstract member MkConstDecl: string * 'ISort -> 'IFuncDecl
    abstract member MkNumeral: string * 'ISort -> 'IExpr
    abstract member MkNumeral: uint * 'ISort -> 'IExpr
    abstract member MkNumeral: int * 'ISort -> 'IExpr
    abstract member MkFPNumeral: float32 * 'ISort -> 'IExpr
    abstract member MkFPDoubleNumeral: float * 'ISort -> 'IExpr
    abstract member MkFPRoundNearestTiesToEven: unit -> 'IExpr

//Creating sorts
    abstract member MkBoolSort: unit -> 'ISort
    abstract member MkBitVecSort: uint -> 'ISort
    abstract member MkFPSort32: unit -> 'ISort
    abstract member MkFPSort64: unit -> 'ISort
    abstract member MkArraySort: 'ISort * 'ISort -> 'ISort
    abstract member MkRangeArraySort: 'ISort array * 'ISort -> 'ISort
    abstract member BoolSort: unit -> 'ISort

//Creating terms
    abstract member MkBV: uint32 * uint32 -> 'IBitVecExpr
    abstract member MkBVInt: int * uint32 -> 'IBitVecExpr
    abstract member MkBool: bool -> 'IBoolExpr
    abstract member MkFPToFP: 'IExpr * 'IFPExpr * 'ISort -> 'IFPExpr
    abstract member MkFPToFP: 'IExpr * 'IBitVecExpr * 'ISort * bool -> 'IFPExpr
    abstract member MkApp: 'IFuncDecl * [<ParamArray>] parameters : 'IExpr array -> 'IExpr
    abstract member MkFuncDecl: string * 'ISort array * 'ISort -> 'IFuncDecl
    abstract member MkFP: 'IBitVecExpr * 'IBitVecExpr * 'IBitVecExpr -> 'IFPExpr
//Array operations
    abstract member MkSelect: 'IArrayExpr * 'IExpr -> 'IExpr
    abstract member MkRangeSelect: 'IArrayExpr * 'IExpr array -> 'IExpr

//Solver operations
    abstract member MkSolver: uint option -> 'ISolver

//Terms conversion
    //FP to BV and vice versa
    abstract member MkFPToIEEEBV: 'IFPExpr -> 'IBitVecExpr
    abstract member MkFPToBV: 'IExpr * 'IFPExpr * uint * bool -> 'IBitVecExpr
    //down casts from expr
    abstract member MkEToBE: 'IExpr -> 'IBoolExpr
    abstract member MkEToBVE: 'IExpr -> 'IBitVecExpr
    abstract member MkEToFPE: 'IExpr -> 'IFPExpr
    abstract member MkEToAE: 'IExpr -> 'IArrayExpr
    //up casts to expr
    abstract member MkBEToE: 'IBoolExpr -> 'IExpr
    abstract member MkBVEToE: 'IBitVecExpr -> 'IExpr
    abstract member MkFPEToE: 'IFPExpr -> 'IExpr
    abstract member MkAEToE: 'IArrayExpr -> 'IExpr
    //casts with nums
    abstract member MkEToBVNum: 'IExpr -> 'IBitVecNum
    abstract member MkEToFPNum: 'IExpr -> 'IFPNum
    abstract member MkBVNumToBVE: 'IBitVecNum -> 'IBitVecExpr

//Terms conversion check
    //down casts from expr
    abstract member MkCheckEToBE: 'IExpr -> bool
    abstract member MkCheckEToBVE: 'IExpr -> bool
    abstract member MkCheckEToFPE: 'IExpr -> bool
    abstract member MkCheckEToAE: 'IExpr -> bool
    //down casts to nums
    abstract member MkCheckBVEToBVNum: 'IBitVecExpr -> bool
    abstract member MkCheckFPEToFPNum: 'IFPExpr -> bool
    abstract member MkCheckEToBVNum: 'IExpr -> bool
    abstract member MkCheckEToFPNum: 'IExpr -> bool
    abstract member MkCheckEToINum: 'IExpr -> bool
    abstract member MkCheckEToRNum: 'IExpr -> bool

    //Common logic
    abstract member MkNot: 'IBoolExpr -> 'IBoolExpr
    abstract member MkAnd: 'IBoolExpr * 'IBoolExpr -> 'IBoolExpr
    abstract member MkAndArray: [<ParamArray>] x : 'IBoolExpr array -> 'IBoolExpr
    abstract member MkAndSeq: 'IBoolExpr seq -> 'IBoolExpr
    abstract member MkOr: 'IBoolExpr * 'IBoolExpr -> 'IBoolExpr
    abstract member MkXor: 'IExpr seq -> 'IBoolExpr
    abstract member MkOrArray: [<ParamArray>] x : 'IBoolExpr array -> 'IBoolExpr
    abstract member MkOrSeq: 'IBoolExpr seq -> 'IBoolExpr
    abstract member MkEq: 'IExpr * 'IExpr -> 'IBoolExpr
    abstract member MkITE: 'IBoolExpr * 'IExpr * 'IExpr -> 'IExpr

    //BitVec logic
    abstract member MkBVAnd: 'IExpr * 'IExpr -> 'IExpr
    abstract member MkBVOr: 'IExpr * 'IExpr -> 'IExpr
    abstract member MkBVXor: 'IExpr * 'IExpr -> 'IExpr
    abstract member MkBVNot: 'IExpr -> 'IExpr
    abstract member MkBVULT: 'IExpr * 'IExpr -> 'IBoolExpr
    abstract member MkBVULEq: 'IExpr * 'IExpr -> 'IBoolExpr
    abstract member MkBVUGT: 'IExpr * 'IExpr -> 'IBoolExpr
    abstract member MkBVUGEq: 'IExpr * 'IExpr -> 'IBoolExpr
    abstract member MkBVSLT: 'IExpr * 'IExpr -> 'IBoolExpr
    abstract member MkBVSLEq: 'IExpr * 'IExpr -> 'IBoolExpr
    abstract member MkBVSGT: 'IExpr * 'IExpr -> 'IBoolExpr
    abstract member MkBVSGEq: 'IExpr * 'IExpr -> 'IBoolExpr

    //BitVec shifts
    abstract member MkBVSHL: 'IExpr * 'IExpr -> 'IExpr
    abstract member MkBVASHR: 'IExpr * 'IExpr -> 'IExpr
    abstract member MkBVLSHR: 'IExpr * 'IExpr -> 'IExpr

    //BitVec conversions
    abstract member MkBVNeg: 'IExpr -> 'IExpr
    abstract member MkSignExt: uint * 'IBitVecExpr -> 'IBitVecExpr
    abstract member MkZeroExt: uint * 'IBitVecExpr -> 'IBitVecExpr
    abstract member MkExtract: uint * uint * 'IBitVecExpr -> 'IBitVecExpr
    abstract member MkConcat: 'IBitVecExpr * 'IBitVecExpr -> 'IBitVecExpr

    //BitVec arithmetics
    abstract member MkBVAdd: 'IExpr * 'IExpr -> 'IExpr
    abstract member MkBVSub: 'IExpr * 'IExpr -> 'IBitVecExpr
    abstract member MkBVMul: 'IExpr * 'IExpr -> 'IExpr
    abstract member MkBVUDiv: 'IExpr * 'IExpr -> 'IExpr
    abstract member MkBVSDiv: 'IExpr * 'IExpr -> 'IExpr
    abstract member MkBVURem: 'IExpr * 'IExpr -> 'IExpr
    abstract member MkBVSRem: 'IExpr * 'IExpr -> 'IExpr

    //BitVec arithmetics without overflow/underflow
    abstract member MkBVAddNoUnderflow: 'IExpr * 'IExpr -> 'IBoolExpr
    abstract member MkBVAddNoOverflow: 'IExpr * 'IExpr * bool-> 'IBoolExpr
    abstract member MkBVMulNoUnderflow: 'IExpr * 'IExpr -> 'IBoolExpr
    abstract member MkBVMulNoOverflow: 'IExpr * 'IExpr * bool-> 'IBoolExpr

    //FP Logic
    abstract member MkFPEq: 'IFPExpr * 'IFPExpr -> 'IBoolExpr
    abstract member MkFPLT: 'IExpr * 'IExpr -> 'IBoolExpr
    abstract member MkFPLEq: 'IExpr * 'IExpr -> 'IBoolExpr
    abstract member MkFPGT: 'IExpr * 'IExpr -> 'IBoolExpr
    abstract member MkFPGEq: 'IExpr * 'IExpr -> 'IBoolExpr

    //FP arithmetics
    abstract member MkFPAdd: 'IExpr * 'IExpr * 'IExpr -> 'IExpr
    abstract member MkFPMul: 'IExpr * 'IExpr * 'IExpr -> 'IExpr
    abstract member MkFPSub: 'IExpr * 'IExpr * 'IExpr -> 'IExpr
    abstract member MkFPDiv: 'IExpr * 'IExpr * 'IExpr -> 'IExpr
    abstract member MkFPRem: 'IExpr * 'IExpr -> 'IExpr
    abstract member MkFPNeg: 'IExpr -> 'IExpr

//Expression methods
    //Common methods
    //properties
    abstract member GetSort: 'IExpr -> 'ISort
    abstract member GetExprType: 'IExpr -> Type
    abstract member String: 'IExpr -> string
    abstract member GetArgs: 'IExpr -> 'IExpr array
    //Check
    abstract member IsConst: 'IExpr -> bool
    abstract member IsConstantArray: 'IExpr -> bool
    abstract member IsDefaultArray: 'IExpr -> bool
    abstract member IsStore: 'IExpr -> bool
    abstract member IsQuantifier: 'IExpr -> bool
    abstract member IsApp: 'IExpr -> bool
    abstract member IsTrue: 'IExpr -> bool
    abstract member IsFalse: 'IExpr -> bool
    abstract member IsNot: 'IExpr -> bool
    abstract member IsAnd: 'IExpr -> bool
    abstract member IsOr: 'IExpr -> bool
    abstract member IsEq: 'IExpr -> bool
    abstract member IsBVSGT: 'IExpr -> bool
    abstract member IsBVUGT: 'IExpr -> bool
    abstract member IsBVSGE: 'IExpr -> bool
    abstract member IsBVUGE: 'IExpr -> bool
    abstract member IsBVSLT: 'IExpr -> bool
    abstract member IsBVULT: 'IExpr -> bool
    abstract member IsBVSLE: 'IExpr -> bool
    abstract member IsBVULE: 'IExpr -> bool
    abstract member IsFPGT: 'IExpr -> bool
    abstract member IsFPGE: 'IExpr -> bool
    abstract member IsFPLT: 'IExpr -> bool
    abstract member IsFPLE: 'IExpr -> bool

    //BitVecNum properties
    abstract member Int64: 'IBitVecNum -> int64
    abstract member Int: 'IBitVecNum -> int
    abstract member BigInteger: 'IBitVecNum -> BigInteger
    abstract member SortSize: 'IBitVecExpr -> uint
    //FP Properties
    abstract member FPEBits: 'IFPNum -> uint32
    abstract member IsNaN: 'IFPNum -> bool
    abstract member IsFPPlusInfinity: 'IFPNum -> bool
    abstract member IsFPMinusInfinity: 'IFPNum -> bool
    abstract member ExponentInt64: 'IFPNum -> int64
    abstract member SignificandUInt64: 'IFPNum -> uint64
    abstract member Sign: 'IFPNum -> bool
    //IntNum properties
    abstract member GetIntFromIntNum: 'IExpr -> int
    //RatNum properties
    abstract member GetValueFromRatNum: 'IExpr -> double
    //Quantifier properties
    abstract member GetQuantifierBody: 'IExpr -> 'IExpr

    //Solver methods
    abstract member MkSModel: 'ISolver -> 'IModel
    abstract member CheckSat: 'ISolver * 'IExpr array -> IStatus
    abstract member Assert: 'ISolver * [<ParamArray>] x : 'IBoolExpr array  -> unit
    abstract member GetReasonUnknown: 'ISolver -> string

    //Model methods
    abstract member Eval: 'IModel * 'IExpr * bool -> 'IExpr
