module Program
open FSharp.Text.Lexing
open Lexer

let program = @"
  if (true) do a + b end
"            

let printToken (t : Lexer.token) = match t with
  | INT_LIT e -> printf "int(%d) " e
  | FLOAT_LIT e -> printf "float(%f) " e
  | STRING_LIT e -> printf "'%s' " e
  | BOOL_LIT e -> printf "%s " (if e then "true" else "false")
  | IDENTIFIER e -> printf "id(%s) " e 
  | NIL -> printf "nil " 
  | STRING -> printf "string "
  | INT -> printf "int "
  | FLOAT -> printf "float "
  | BOOL -> printf "bool "
  | LET -> printf "let "
  | DO -> printf "do "
  | END  -> printf "end "
  | IF -> printf "if "
  | FOR -> printf "for "
  | IN -> printf "in "
  | WHILE -> printf "while "
  | UNTIL -> printf "until "
  | UNLESS -> printf "unless "
  | CLASS -> printf "class "
  | SUPER -> printf "super "
  | THIS -> printf "this "
  | BANG_EQUAL -> printf "!= "
  | GREATER_EQUAL -> printf ">= "
  | LESS_EQUAL  -> printf "<= "
  | EQUAL_EQUAL -> printf "== "
  | PLUS_PLUS -> printf "++ "
  | MINUS_MINUS -> printf "-- "
  | AND -> printf "&& "
  | OR -> printf "|| "
  | DOT_DOT -> printf ".. "
  | BANG -> printf "! "
  | GREATER -> printf "> "
  | LESS -> printf "< "
  | EQUAL -> printf "= "
  | LPAREN -> printf "("
  | RPAREN -> printf ") "
  | COLON -> printf ": "
  | COMMA -> printf ", "
  | DOT -> printf ". "
  | SLASH -> printf "/ "
  | STAR -> printf "* "
  | MINUS -> printf "- "
  | PLUS -> printf "+ "
  | SEMICOLON -> printf "; "
  | EOF -> printf "EOF"

let parse program = 
    let lexbuf = LexBuffer<char>.FromString program in
    let rec next lexeme = 
      printToken lexeme;
      match lexeme with
        | EOF -> 0
        | _ -> 1 + next (Lexer.read lexbuf)
    next (Lexer.read lexbuf)
      

[<EntryPoint>]
let main argv =
    match parse program with    
    | _ -> 0
