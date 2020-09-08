type Type =
  | Int of int
  | Bool of bool
  | Float of double
  | String of String

type Formal =  Formal of string * type

type Expr = 
 | Val of string 
 | Int of int
 | Float of double
 | String of string
 | FuncCall of string * Expr list
 | BinOp of Expr * string * Expr
 | UnOp of string * Expr


type Stmt = 
 | Assign of string * Expr
 | While of Expr * Stmt 
 | Seq of Stmt list
 | IfThenElse of Expr * Stmt * Stmt
 | Vardef of Formal * Expr 
 | Funcdef of string * Formal list * Type * Stmt list

  
type Prog = Prog of Stmt list