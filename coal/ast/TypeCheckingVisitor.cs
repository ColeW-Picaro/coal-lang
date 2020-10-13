using System.Diagnostics;
using Microsoft.FSharp.Core;
using Microsoft.FSharp.Collections;

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
      Visit(a.Item.Lhs);
      // RHS
      Visit(a.Item.Rhs);
      Debug.Assert(a.Item.Lhs.ActualType == a.Item.Lhs.ActualType);
    }
    public void Visit(Ast.Stmt.While w)
    {
      // Cond
      Visit(w.Item.Cond);
      // Body
      Visit(w.Item.Body);
    }
    public void Visit(Ast.Stmt.Seq s) { 
      // Seqs introduce scope
      foreach (var v in s.Item.Body) {
        Visit(v);
      }
    }
    public void Visit(Ast.Stmt.IfThenElse i) { 
      // Cond
      Visit(i.Item.Cond);
      // If body
      Visit(i.Item.Body);
      // Else body
      if (i.Item.ElseBody != null) {
        Visit(i.Item.ElseBody.Value);
      }
    }
    public void Visit(Ast.Stmt.Vardef v) { 
      if (v.Item.Expr != null) {
        Visit(v.Item.Expr.Value);
        Debug.Assert(v.Item.Formal.Type == v.Item.Expr.Value.ActualType);
      }
    }
    public void Visit(Ast.Stmt.Funcdef f) { 
      Visit(f.Item.Body);
    }
    public void Visit(Ast.Stmt.Expr e) {
      Visit(e.Item.Expr);
    }
    public void Visit(Ast.Stmt.Return r) { 
      Visit(r.Item.Expr.Value);
      Ast.Stmt.Funcdef fd = (Ast.Stmt.Funcdef) r.Item.Decl.Value;
      Ast.Expr e = r.Item.Expr.Value;
      Debug.Assert(fd.Item.Formal.Type == e.ActualType);
    }
    // Expr
    public void Visit(Ast.Expr.VarRef vr) { 
      FSharpOption<Ast.Stmt> d = vr.Item.Decl;
      Ast.Stmt.Vardef vd = (Ast.Stmt.Vardef) d.Value;
      vr.ActualType = vd.Item.Formal.Type;
    }
    public void Visit(Ast.Expr.Int i) { 
      i.ActualType = Ast.Type.IntType;
    }
    public void Visit(Ast.Expr.Float f) {
      f.ActualType = Ast.Type.FloatType;
    }
    public void Visit(Ast.Expr.String s) { 
      s.ActualType = Ast.Type.StringType;
    }
    public void Visit(Ast.Expr.Bool b) { 
      b.ActualType = Ast.Type.BoolType;
    }
    public void Visit(Ast.Expr.FuncCall f) {
        foreach (var e in f.Item.ExprList) {
            Visit(e);
        }
        Ast.Stmt.Funcdef fd = (Ast.Stmt.Funcdef) f.Item.Decl.Value;
        // Compare param types to decl types
        var formalList = SeqModule.ToList(fd.Item.FormalList);
        var actualList = SeqModule.ToList(f.Item.ExprList);
        var actualAndFormal = ListModule.Zip(actualList, formalList);
        foreach (var af in actualAndFormal) {
            Debug.Assert(af.Item1.ActualType == af.Item2.Formal.Type);
        }
        f.ActualType = fd.Item.Formal.Type;
    }
    public void Visit(Ast.Expr.BinOp b) { 
      Visit(b.Item.Lhs);
      Visit(b.Item.Rhs);
      if (b.Item.Lhs.ActualType == b.Item.Rhs.ActualType) {
          Ast.Binary op = b.Item.Op;
          // Determine type of b
          // Check if op is defined for operands
          if (op.IsOpMul || op.IsOpDiv || op.IsOpPlus || op.IsOpMinus) {
            b.ActualType = b.Item.Lhs.ActualType;
            Debug.Assert(b.ActualType.IsIntType || b.ActualType.IsFloatType);
          } else {
            b.ActualType = Ast.Type.BoolType;
            Ast.Type type = b.Item.Lhs.ActualType;
            Debug.Assert(type.IsIntType || type.IsFloatType || type.IsBoolType);
          }
      } else {
          System.Console.WriteLine("Type Mismatch " + b.Item.Lhs.ActualType + " " + b.Item.Rhs.ActualType);
      }
    }
    public void Visit(Ast.Expr.UnOp u) { 
      Visit(u.Item.Lhs);
      u.ActualType = u.Item.Lhs.ActualType;
      // TODO check if op is defined for type params
      var op = u.Item.Op;
      if (op.IsOpDecr || op.IsOpIncr || op.IsOpValNegate) {
         Debug.Assert(u.ActualType == Ast.Type.FloatType || u.ActualType == Ast.Type.IntType);
      } else {
         Debug.Assert(u.ActualType == Ast.Type.BoolType);
      }
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
