
namespace CoalLang


open System


module rec Ast =

  type Type =
  | IntType
  | BoolType
  | FloatType
  | StringType
  | NilType

  type Binary =
  | OpNotEqual
  | OpGreaterEqual
  | OpLessEqual
  | OpEqual
  | OpAnd
  | OpOr
  | OpLess
  | OpGreater
  | OpMul
  | OpDiv
  | OpPlus
  | OpMinus

  type Unary =
  | OpBoolNegate
  | OpValNegate
  | OpIncr
  | OpDecr

  type Formal(name : string, t : Type) =
    member this.Name = name
    member this.Type = t

  type Expr = 
  | VarRef of VarRefType
  | Int of int
  | Float of double
  | String of string
  | Bool of bool
  | FuncCall of FuncCallType 
  | BinOp of Expr * Binary * Expr
  | UnOp of Unary * Expr

  type VardefType(arg : Formal * Expr option) =
    member this.Formal = let (f, _) = arg in f
    member this.Expr = let (_, e) = arg in e

  type VarRefType(s : string) =
    member this.Name = s
    member this.Var : VardefType option = None 

  type FuncdefType(arg : Formal * VardefType list * Stmt) =
    member this.Formal = let (f, _, _) = arg in f
    member this.FormalList = let (_, fl, _) = arg in fl
    member this.Body = let (_, _, s) = arg in s 

  type FuncCallType(arg : string * Expr list) =
    member this.Name = let (n, _) = arg in n 
    member this.ExprList = let (_, fl) = arg in fl
    member this.Fun : FuncdefType option = None

  type Stmt = 
  | Assign of string * Expr
  | While of Expr * Stmt 
  | Seq of Stmt list
  | IfThenElse of Expr * Stmt * Stmt option
  | Vardef of VardefType
  | Funcdef of FuncdefType
  | Expr of Expr
  | Return of Expr option

  let MakeVardef t = 
    Vardef(VardefType t)

  let MakeFuncdef t =
    Funcdef(FuncdefType t)

  let MakeFuncCall t =
    FuncCall(FuncCallType t)

  let MakeVarRef t =
    VarRef(VarRefType t)

  type Prog =
  | Prog of Stmt list
