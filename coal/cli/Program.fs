module Program
open FSharp.Text.Lexing
open CoalLang
open System.IO

let parse (filename:string) =
    use textReader = new StreamReader(filename)
    let lexbuf = LexBuffer<char>.FromTextReader textReader
    let res = Parser.start Lexer.read lexbuf in
    res

[<EntryPoint>]
let main argv =
    let tree = parse argv.[0] in
    let st = SymbolTable tree in 
    let stv = SymbolTableVisitor st in
    let tcv = TypeCheckingVisitor tree in
    let cgv = CodeGenVisitor tree in
    stv.printErrorList();
    tcv.printErrorList();
    cgv.Gen();
    0
