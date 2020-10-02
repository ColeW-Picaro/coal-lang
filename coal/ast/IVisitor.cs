namespace CoalLang {
    public interface IVisitor {
        void Visit(Ast.Prog prog);

        // Stmt
        void Visit(Ast.Stmt.Assign a);
        void Visit(Ast.Stmt.While w);
        void Visit(Ast.Stmt.Seq s);
        void Visit(Ast.Stmt.IfThenElse i);
        void Visit(Ast.Stmt.Vardef v);
        void Visit(Ast.Stmt.Funcdef f);
        void Visit(Ast.Stmt.Expr e);
        void Visit(Ast.Stmt.Return r);

        // Expr
        void Visit(Ast.Expr.VarRef v);
        void Visit(Ast.Expr.Int i);
        void Visit(Ast.Expr.Float f);
        void Visit(Ast.Expr.String s);
        void Visit(Ast.Expr.Bool b);
        void Visit(Ast.Expr.FuncCall f);
        void Visit(Ast.Expr.BinOp b);
        void Visit(Ast.Expr.UnOp u);
    }
}