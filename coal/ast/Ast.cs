using System.Reflection;
using System.Runtime.ExceptionServices;
using System.IO;
using System;
using System.Collections.Generic;

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
        int m_depth;
        List<Dictionary<String, Ast.Stmt.Vardef>> m_symbolTable;
        public SymbolTable (Ast.Prog prog) {
            // Create the symbol table and push a scope onto it
            this.m_symbolTable = new List<Dictionary<string, Ast.Stmt.Vardef>>();
            this.PushNewScope();
            this.m_prog = prog;
            this.m_depth = 0;

            foreach (var s in m_prog.Item) {
                switch(s) {
                    case Ast.Stmt.Vardef v:
                        String name = v.Item1.Item1;
                        this.Insert(name, v);
                        break;
                }
            }
        }

        // Adds a new Key Value Pair to the current scope
        public KeyValuePair<String, Ast.Stmt.Vardef> Insert (String name, Ast.Stmt.Vardef vardef) {
            KeyValuePair<String, Ast.Stmt.Vardef> ins = new KeyValuePair<String, Ast.Stmt.Vardef>(name, vardef);
            this.m_symbolTable[m_symbolTable.Count - 1].Add(name, vardef);
            return ins;
        }
        
        // Pushes a new scope onto the symbol table
        private void PushNewScope () {
            this.m_symbolTable.Add(new Dictionary<string, Ast.Stmt.Vardef>());
        }

    }
}