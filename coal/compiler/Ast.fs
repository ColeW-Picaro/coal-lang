module Ast

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

type Formal =
  | Formal of string * Type

type Expr = 
  | VarRef of string
  | Int of int
  | Float of double
  | String of string
  | Bool of bool
  | FuncCall of string * Expr list
  | BinOp of Expr * Binary * Expr
  | UnOp of Unary * Expr

type Stmt = 
  | Assign of string * Expr
  | While of Expr * Stmt 
  | Seq of Stmt list
  | IfThenElse of Expr * Stmt * Stmt option
  | Vardef of Formal * Expr option
  | Funcdef of Formal * Formal list * Stmt
  | Expr of Expr
  | Return of Expr option

type Prog =
  | Prog of Stmt list
