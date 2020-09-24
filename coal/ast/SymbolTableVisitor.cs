using System.Collections.Generic;
using Microsoft.FSharp.Core;

namespace CoalLang
{
  public class SymbolTableVisitor : IVisitor
  {
    public SymbolTable m_symbolTable;
    private List<Ast.Stmt.Vardef> m_vardefs;
    public SymbolTableVisitor(SymbolTable st) {
      this.m_symbolTable = st;
      this.m_vardefs = new List<Ast.Stmt.Vardef>();
      // Visits
      Visit();
    }

    public void Visit() {
      Visit(this.m_symbolTable.m_prog);
    }
    // Visit methods for every relevant Ast type
    // Looking for Vardefs and VarRefs to insert into the symbol table
    public void Visit(Ast.Prog prog)
    {
      foreach (var stmt in prog.Item)
      {
        Visit(stmt);
      }
    }

    // Stmt
    public void Visit(Ast.Stmt stmt)
    {
      switch (stmt)
      {
        case Ast.Stmt.Assign a:
          Visit(a);
          break;
        case Ast.Stmt.While w:
          Visit(w);
          break;
        case Ast.Stmt.Seq s:
          Visit(s);
          break;
        case Ast.Stmt.IfThenElse i:
          Visit(i);
          break;
        case Ast.Stmt.Vardef v:
          Visit(v);
          break;
        case Ast.Stmt.Funcdef f:
          Visit(f);
          break;
        case Ast.Stmt.Expr e:
          Visit(e);
          break;
        case Ast.Stmt.Return r:
          Visit(r);
          break;
      }
    }
    public void Visit(Ast.Stmt.Assign a)
    {
      Visit(a.Item2);
    }
    public void Visit(Ast.Stmt.While w)
    {
      // Cond
      Visit(w.Item1);
      // Body
      Visit(w.Item2);
    }
    public void Visit(Ast.Stmt.Seq s) { 
      // Seqs introduce scope
      this.m_symbolTable.PushNewScope();
      foreach (var v in s.Item) {
        Visit(v);
      }
      this.m_symbolTable.PopScope();
    }
    public void Visit(Ast.Stmt.IfThenElse i) { 
      // Cond
      Visit(i.Item1);
      // If body
      Visit(i.Item2);
      // Else body
      if (i.Item3 != null) {
        Visit(i.Item3.Value);
      }
    }
    public void Visit(Ast.Stmt.Vardef v) { 
      if (v.Item.Expr != null){
        Visit(v.Item.Expr.Value);
      }
      this.m_vardefs.Add(v);
    }
    public void Visit(Ast.Stmt.Funcdef f) { 
      Visit(f.Item.Body);
    }
    public void Visit(Ast.Stmt.Expr e)
    {
      Visit(e.Item);
    }
    public void Visit(Ast.Stmt.Return r) { 
      Visit(r.Item.Value);
    }
    // Expr
    public void Visit(Ast.Expr.VarRef vr) { 
      // Find the corresponding vardef in symbol table
      // Insert in symbol table
      foreach (var vardef in m_vardefs) {
        if (vardef.Item.Formal.Name == vr.Item) {
          this.m_symbolTable.Insert(vr.Item, vardef);
          return;
        }
      }
      // Compiler Error???
    }
    public void Visit(Ast.Expr.Int i) { 
      
    }
    public void Visit(Ast.Expr.Float f) {
    
    }
    public void Visit(Ast.Expr.String s) { 

    }
    public void Visit(Ast.Expr.Bool b) { 

    }
    public void Visit(Ast.Expr.FuncCall f) { 
      // Insert in symbol table
    }
    public void Visit(Ast.Expr.BinOp b) { 
      Visit(b.Item1);
      Visit(b.Item3);
    }
    public void Visit(Ast.Expr.UnOp u) { 
      Visit(u.Item2);
    }
    private void Visit(Ast.Expr e)
    {
      switch (e)
      {
        case Ast.Expr.VarRef v:
          Visit(v);
          break;
        case Ast.Expr.Int i:
          Visit(i);
          break;
        case Ast.Expr.Float f:
          Visit(f);
          break;
        case Ast.Expr.String s:
          Visit(s);
          break;
        case Ast.Expr.Bool b:
          Visit(b);
          break;
        case Ast.Expr.FuncCall f:
          Visit(f);
          break;
        case Ast.Expr.BinOp b:
          Visit(b);
          break;
        case Ast.Expr.UnOp u:
          Visit(u);
          break;
      }
    }
  }
}
