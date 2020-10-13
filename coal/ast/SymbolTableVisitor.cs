using Microsoft.FSharp.Core;
using Microsoft.FSharp.Collections;
using System.Collections.Generic;

using Optional;

namespace CoalLang
{
  public class SymbolTableVisitor : IVisitor
  {
    public Stack<Ast.Stmt.Funcdef> m_FunctionStack;
    public SymbolTable m_symbolTable;
    public SymbolTableVisitor(SymbolTable st) {
      this.m_FunctionStack = new Stack<Ast.Stmt.Funcdef>();
      // this.m_FunctionStack.Push((Ast.Stmt.Funcdef) Ast.MakeFuncdef(new Ast.Formal("Name", Ast.Type.IntType), null, null));
      this.m_symbolTable = st;
      // Visits
      Visit();
    }

    public void Visit() {
      foreach(var s in this.m_symbolTable.m_prog.Item) {
        switch (s) {
          case Ast.Stmt.Vardef v:
            this.m_symbolTable.Insert(v.Item.Formal.Name, v);
            break;
          case Ast.Stmt.Funcdef f:
            this.m_symbolTable.Insert(f.Item.Formal.Name, f);
            break;
        }
      }
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
      Visit(a.Item.Lhs);
      Visit(a.Item.Rhs);
    }
    public void Visit(Ast.Stmt.While w)
    {
      Visit(w.Item.Cond);
      Visit(w.Item.Body);
    }
    public void Visit(Ast.Stmt.Seq s) { 
      // Seqs introduce scope
      this.m_symbolTable.PushNewScope();
      foreach (var v in s.Item.Body) {
        Visit(v);
      }
      this.m_symbolTable.PopScope();
    }
    public void Visit(Ast.Stmt.IfThenElse i) { 
      // Cond
      Visit(i.Item.Body);
      // If body
      Visit(i.Item.Cond);
      // Else body
      if (i.Item.ElseBody != null) {
        Visit(i.Item.ElseBody.Value);
      }
    }
    public void Visit(Ast.Stmt.Vardef v) { 
      this.m_symbolTable.Insert(v.Item.Formal.Name, v);
      if (v.Item.Expr != null) {
        Visit(v.Item.Expr.Value);
      }
    }
    public void Visit(Ast.Stmt.Funcdef f) {
      this.m_FunctionStack.Push(f);
      System.Console.WriteLine("Push " + f);
      this.m_symbolTable.Insert(f.Item.Formal.Name, f);
      // Add formal params to defs
      this.m_symbolTable.PushNewScope();
      foreach (var formal in f.Item.FormalList) {
        Ast.Stmt.Vardef vd = (Ast.Stmt.Vardef) Ast.Stmt.Vardef.NewVardef(new Ast.VardefType(new System.Tuple<Ast.Formal, FSharpOption<Ast.Expr>>(formal.Formal, new FSharpOption<Ast.Expr>(null))));
        this.m_symbolTable.Insert(vd.Item.Formal.Name, vd);
      }
      Visit(f.Item.Body);
      this.m_FunctionStack.Pop();
      System.Console.WriteLine("Pop " + f);
      this.m_symbolTable.PopScope();
    }
    public void Visit(Ast.Stmt.Expr e) {
      Visit(e.Item.Expr);
    }
    public void Visit(Ast.Stmt.Return r) { 
      Visit(r.Item.Expr.Value);
      if (this.m_FunctionStack.Count == 0) {
        r.Item.Decl = (Ast.Stmt.Funcdef) Ast.Stmt.Funcdef.NewFuncdef(new Ast.FuncdefType(new System.Tuple<Ast.Formal, FSharpList<Ast.VardefType>, Ast.Stmt>(new Ast.Formal("Main", Ast.Type.IntType), null, null)));
      } else {
        r.Item.Decl = this.m_FunctionStack.Peek();
      }
      Ast.Stmt.Funcdef fd = (Ast.Stmt.Funcdef) r.Item.Decl.Value;
      System.Console.WriteLine("Return " + fd.Item.Formal.Name);
    }
    // Expr
    public void Visit(Ast.Expr.VarRef vr) { 
      // Find the corresponding vardef in symbol table
      Option<Ast.Stmt> vd = this.m_symbolTable.Find(vr.Item.Name);
      vd.MatchSome(v => vr.Item.Decl = v);
      System.Console.WriteLine("Reference to " + vr.Item.Name + " " + vr.Item.Decl);
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
      Option<Ast.Stmt> fd = this.m_symbolTable.Find(f.Item.Name);
      fd.MatchSome(funcdef => f.Item.Decl = funcdef);
      System.Console.WriteLine("Reference to function " + f.Item.Name + " " + f.Item.Decl);
      foreach (var e in f.Item.ExprList) {
        Visit(e);
      }
    }
    public void Visit(Ast.Expr.BinOp b) { 
      Visit(b.Item.Lhs);
      Visit(b.Item.Rhs);
    }
    public void Visit(Ast.Expr.UnOp u) { 
      Visit(u.Item.Lhs);
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
