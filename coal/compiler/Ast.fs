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

type Formal =  string * Type

type Expr = 
 | Val of string 
 | Int of int
 | Float of double
 | String of string
 | FuncCall of string * Expr list
 | BinOp of Expr * Binary * Expr
 | UnOp of Unary * Expr


type Stmt = 
 | Assign of string * Expr
 | While of Expr * Stmt 
 | Seq of Stmt list
 | IfThenElse of Expr * Stmt * Stmt option
 | Vardef of Formal * Expr option
 | Funcdef of Formal * Formal list option * Stmt
 | Expr of Expr

type Prog = Stmt list
