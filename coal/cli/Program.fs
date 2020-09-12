module Program
open FSharp.Text.Lexing
open Lexer
open Parser
open Ast

let parse (filename:string) = 
    use textReader = new System.IO.StreamReader(filename)
    let lexbuf = LexBuffer<char>.FromTextReader textReader
    let res = Parser.start Lexer.read lexbuf in
    res

[<EntryPoint>]
let main argv =
    let tree = parse argv.[0] in
    tree |> string |> System.Console.WriteLine
    0
