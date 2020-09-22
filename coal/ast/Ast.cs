using System;
using System.Collections.Generic;

using Microsoft.FSharp.Core;

namespace CoalLang {
    public class AstPrinter {
       Ast.Prog m_prog;
        public AstPrinter (Ast.Prog prog) {
            this.m_prog = prog;              
            foreach (var s in m_prog.Item) {
                Console.WriteLine(s);
            }                        
        }
    }

    public class SymbolTable {
        Ast.Prog m_prog;
        List<Dictionary<String, Ast.Stmt.Vardef>> m_symbolTable;
        public SymbolTable (Ast.Prog prog) {
            // Create the symbol table and push a scope onto it
            this.m_symbolTable = new List<Dictionary<string, Ast.Stmt.Vardef>>();
            this.PushNewScope();
            this.m_prog = prog;
        }

        // Adds a new Key Value Pair to the current scope
        public KeyValuePair<String, Ast.Stmt.Vardef> Insert(String name, Ast.Stmt.Vardef vardef) {
            KeyValuePair<String, Ast.Stmt.Vardef> ins = new KeyValuePair<String, Ast.Stmt.Vardef>(name, vardef);
            this.m_symbolTable[m_symbolTable.Count - 1].Add(name, vardef);
            return ins;
        }
        
        // Pushes a new scope onto the symbol table
        public void PushNewScope () {
            this.m_symbolTable.Add(new Dictionary<string, Ast.Stmt.Vardef>());
        }

        public void PopScope () {
            this.m_symbolTable.RemoveAt(this.m_symbolTable.Count - 1);
        }
    }
}