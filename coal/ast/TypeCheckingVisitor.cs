using System;
using System.Reflection.Metadata;
using System.Runtime.InteropServices.ComTypes;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.FSharp.Core;
using Microsoft.FSharp.Collections;

using Optional;

namespace CoalLang
{
  public class TypeCheckingVisitor : IVisitor
  {
    Ast.Prog m_prog;
    public TypeCheckingVisitor(Ast.Prog prog) {
      this.m_prog = prog;
      // Visits
      Visit();
    }

    public void Visit() {
      Visit(this.m_prog);
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
      // LHS
      Visit(a.Item1);
      // RHS
      Visit(a.Item2);
      Debug.Assert(a.Item1.ActualType == a.Item2.ActualType);

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
      foreach (var v in s.Item) {
        Visit(v);
      }
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
      if (v.Item.Expr != null) {
        Visit(v.Item.Expr.Value);
      }
    }
    public void Visit(Ast.Stmt.Funcdef f) { 
      Visit(f.Item.Body);
    }
    public void Visit(Ast.Stmt.Expr e) {
      Visit(e.Item);
    }
    public void Visit(Ast.Stmt.Return r) { 
      Visit(r.Item.Value);
    }
    // Expr
    public void Visit(Ast.Expr.VarRef vr) { 
      FSharpOption<Ast.Stmt> d = vr.Item.Decl;
      Ast.Stmt.Vardef vd = (Ast.Stmt.Vardef) d.Value;
      vr.ActualType = vd.Item.Formal.Type;
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
        Ast.Stmt.Funcdef fd = (Ast.Stmt.Funcdef) f.Item.Decl.Value;
        foreach (var e in f.Item.ExprList) {
            Visit(e);
        }
        // Compare param types to decl types
        var formalList = SeqModule.ToList(fd.Item.FormalList);
        var actualList = SeqModule.ToList(f.Item.ExprList);
        var actualAndFormal = ListModule.Zip(actualList, formalList);
        foreach (var af in actualAndFormal) {
            Debug.Assert(af.Item1.ActualType == af.Item2.Formal.Type);
        }

    }
    public void Visit(Ast.Expr.BinOp b) { 
      Visit(b.Item.Lhs);
      Visit(b.Item.Rhs);
      if (b.Item.Lhs.ActualType == b.Item.Rhs.ActualType) {
          b.ActualType = b.Item.Lhs.ActualType;
          // TODO propagate type or return new type for each of
          // TODO check if op is defined for type params
          var op = b.Item.Op;
          Debug.Assert(b.ActualType.IsIntType || b.ActualType.IsFloatType);
      } else {
          System.Console.WriteLine("Type Mismatch " + b.Item.Lhs.ActualType + " " + b.Item.Rhs.ActualType);
      }
    }
    public void Visit(Ast.Expr.UnOp u) { 
      Visit(u.Item.Lhs);
      u.ActualType = u.Item.Lhs.ActualType;
      // TODO check if op is defined for type params
      Debug.Assert(u.ActualType.IsFloatType || u.ActualType.IsIntType);

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
