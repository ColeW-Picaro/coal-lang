namespace CoalLang
{
  public class SymbolTableVisitor : IVisitor
  {
    public void Visit(Ast.Prog prog)
    {
      foreach (var stmt in prog.Item)
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
    }

    // Stmt
    public void Visit(Ast.Stmt.Assign a) { }
    public void Visit(Ast.Stmt.While w) { 
      Visit(w.Item1);
    }
    public void Visit(Ast.Stmt.Seq s) { }
    public void Visit(Ast.Stmt.IfThenElse i) { }
    public void Visit(Ast.Stmt.Vardef v) { }
    public void Visit(Ast.Stmt.Funcdef f) { }
    public void Visit(Ast.Stmt.Expr e)
    {
      Visit(e.Item);
    }
    public void Visit(Ast.Stmt.Return r) { }
    // Expr
    public void Visit(Ast.Expr.VarRef v) { }
    public void Visit(Ast.Expr.Int i) { }
    public void Visit(Ast.Expr.Float f) { }
    public void Visit(Ast.Expr.String s) { }
    public void Visit(Ast.Expr.Bool b) { }
    public void Visit(Ast.Expr.FuncCall f) { }
    public void Visit(Ast.Expr.BinOp b) { }
    public void Visit(Ast.Expr.UnOp u) { }
    private void Visit(Ast.Expr e) {
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
