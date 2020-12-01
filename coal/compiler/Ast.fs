
namespace CoalLang


open System


module rec Ast =

  type Type =
  | IntType
  | BoolType
  | FloatType
  | StringType
  | NilType
  | Unresolved

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

  // Class for each type of expr
  type Formal(name : string, t : Type) =
    member this.Name = name
    member this.Type = t

  type VarRefType(s : string) =
    member this.Name = s
    member val Decl : Stmt option = None with get, set
    member val ActualType : Type = Unresolved with get, set

  type IntLit(i : int) =
    member this.Value = i

  type FloatLit(f : double) =
    member this.Value = f

  type StringLit(s : string) =
    member this.Value = s

  type BoolLit(b : bool) =
    member this.Value = b

  type FuncCallType(arg : string * Expr list) =
    member this.Name = let (n, _) = arg in n
    member this.ExprList = let (_, fl) = arg in fl
    member val Decl : Stmt option = None with get, set
    member val ActualType : Type = Unresolved with get, set

  type BinOpType(lhs: Expr, op: Binary, rhs: Expr) =
    member this.Lhs = lhs
    member this.Op = op
    member this.Rhs = rhs
    member val ActualType : Type = Unresolved with get, set

  type UnOpType(op: Unary, lhs: Expr) =
    member this.Lhs = lhs
    member this.Op = op
    member val ActualType : Type = Unresolved with get, set

  type ITypeResolvable =
    abstract GetActualType: unit -> Type
    abstract SetActualType: Type -> unit

  // Expr union, contains get and set functions for types
  type Expr = 
    | VarRef of VarRefType
    | Int of IntLit
    | Float of FloatLit
    | String of StringLit
    | Bool of BoolLit
    | FuncCall of FuncCallType
    | BinOp of BinOpType
    | UnOp of UnOpType

    member this.Size with get () =
        match this with
            | String s -> s.Value.Length
            | _ -> 1
    member this.ActualType with get () =
        match this with
          | VarRef v -> v.ActualType
          | Int i -> IntType
          | Float f -> FloatType
          | String s -> StringType
          | Bool b -> BoolType
          | FuncCall f -> f.ActualType
          | BinOp b -> b.ActualType
          | UnOp u -> u.ActualType
    member this.ActualType with set t =
        match this with
          | VarRef v -> v.ActualType <- t
          | FuncCall f -> f.ActualType <- t
          | BinOp b -> b.ActualType <- t
          | UnOp u -> u.ActualType <- t
          | _ -> ()

  // Stmt union
  type Stmt =
  | Assign of AssignType
  | While of WhileType
  | Seq of SeqType
  | IfThenElse of IfThenElseType
  | Vardef of VardefType
  | Funcdef of FuncdefType
  | Expr of ExprType
  | Return of ReturnType

  // Classes for every type of Stmt
  type AssignType(lhs: Expr, rhs: Expr) =
    member this.Lhs = lhs
    member this.Rhs = rhs

  type WhileType(cond: Expr, body: Stmt) =
    member this.Cond = cond
    member this.Body = body

  type SeqType(body: Stmt list) =
    member this.Body = body

  type IfThenElseType(cond: Expr, body: Stmt, elseBody: Stmt option) =
    member this.Cond = cond
    member this.Body = body
    member this.ElseBody = elseBody

  type VardefType(arg : Formal * Expr option) =
    member this.Formal = let (f, _) = arg in f
    member this.Expr = let (_, e) = arg in e

  type FuncdefType(arg : Formal * VardefType list * Stmt) =
    member this.Formal = let (f, _, _) = arg in f
    member this.FormalList = let (_, fl, _) = arg in fl
    member this.Body = let (_, _, s) = arg in s 

  type ExprType(expr: Expr) =
    member this.Expr = expr

  type ReturnType(expr: Expr option) =
    member this.Expr = expr
    member val Decl: Stmt option = None with get, set

  // Used by the parser to build ast types
  let MakeAssign t =
    Assign(AssignType t)

  let MakeWhile t =
    While(WhileType t)

  let MakeSeq t =
    Seq(SeqType t)

  let MakeIfThenElse t =
    IfThenElse(IfThenElseType t)

  let MakeVardef t =
    Vardef(VardefType t)

  let MakeFuncdef t =
    Funcdef(FuncdefType t)

  let MakeExpr t =
    Expr(ExprType t)

  let MakeReturn t =
    Return(ReturnType t)

  let MakeFuncCall t =
    FuncCall(FuncCallType t)

  let MakeVarRef t =
    VarRef(VarRefType t)

  let MakeBinOp t =
    BinOp(BinOpType t)

  let MakeUnOp t =
    UnOp(UnOpType t)

  type Prog =
  | Prog of Stmt list
