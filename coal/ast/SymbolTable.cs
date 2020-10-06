using System;
using System.Collections.Generic;

using Optional;

namespace CoalLang
{
  public class SymbolTable
  {
    public Ast.Prog m_prog;
    public List<Dictionary<String, Ast.Stmt>> m_symbolTable;
    public SymbolTable(Ast.Prog prog)
    {
      // Create the symbol table and push a scope onto it
      this.m_symbolTable = new List<Dictionary<string, Ast.Stmt>>();
      this.PushNewScope();
      this.m_prog = prog;
    }

    // Adds a new Key Value Pair to the current scope
    public bool Insert(String name, Ast.Stmt def)
    {
      KeyValuePair<String, Ast.Stmt> ins = new KeyValuePair<String, Ast.Stmt>(name, def);
      this.m_symbolTable[m_symbolTable.Count - 1].TryAdd(name, def);
      Console.WriteLine(ins);
      return true;
    }

    // Pushes a new scope onto the symbol table
    public void PushNewScope()
    {
      this.m_symbolTable.Add(new Dictionary<string, Ast.Stmt>());
    }

    public void PopScope()
    {
      this.m_symbolTable.RemoveAt(this.m_symbolTable.Count - 1);
    }

    public Option<Ast.Stmt> Find(String name)
    {
        for (var i = this.m_symbolTable.Count - 1; i >= 0; i--)
        {
            var st = this.m_symbolTable[i];
            if (st.ContainsKey(name))
            {
                return Option.Some(st[name]);
            }
        }
        return Option.None<Ast.Stmt>();
    }
  }
}