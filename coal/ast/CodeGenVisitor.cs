using System.Collections.Generic;
using System.Text;
using LLVMSharp.Interop;



// Var dec (int, float, bool), string
// Assignment
// Bool expr
// Int arithmetic
// Float arithmetic
namespace CoalLang
{
    public class CodeGenVisitor : IVisitor
    {
        Ast.Prog m_prog;
        LLVMModuleRef m_module;
        LLVMBuilderRef m_builder;
        Dictionary<string, LLVMValueRef> m_namedValues;
        Stack<LLVMValueRef> m_valueStack;

        public CodeGenVisitor(Ast.Prog prog)
        {
            this.m_prog = prog;
            this.m_namedValues = new Dictionary<string, LLVMValueRef>();
            this.m_valueStack = new Stack<LLVMValueRef>();
            unsafe
            {
                this.m_builder = LLVM.CreateBuilder();
            }
            this.Visit();
        }

        public void Visit()
        {
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
        }
        public void Visit(Ast.Stmt.While w)
        {
            // Cond
            Visit(w.Item.Cond);
            // Body
            Visit(w.Item.Body);
        }
        public void Visit(Ast.Stmt.Seq s)
        {
            foreach (var v in s.Item.Body)
            {
                Visit(v);
            }
        }
        public void Visit(Ast.Stmt.IfThenElse i)
        {
            // Cond
            Visit(i.Item.Cond);
            // If body
            Visit(i.Item.Body);
            // Else body
            if (i.Item.ElseBody != null)
            {
                Visit(i.Item.ElseBody.Value);
            }
        }
        public void Visit(Ast.Stmt.Vardef v)
        {
            if (v.Item.Expr != null)
            {
                Visit(v.Item.Expr.Value);
            }
        }
        public void Visit(Ast.Stmt.Funcdef f)
        {
            Visit(f.Item.Body);
        }
        public void Visit(Ast.Stmt.Expr e)
        {
            Visit(e.Item.Expr);
        }
        public void Visit(Ast.Stmt.Return r)
        {
            Visit(r.Item.Expr.Value);
        }

        // Expr
        public void Visit(Ast.Expr.VarRef vr)
        {
            LLVMValueRef value;
            if (this.m_namedValues.TryGetValue(vr.Item.Name, out value))
            {
                this.m_valueStack.Push(value);
            }
            else
            {
                // Error
            }
        }
        public void Visit(Ast.Expr.Int i)
        {
            unsafe
            {
                this.m_valueStack.Push(LLVM.ConstReal(LLVM.Int32Type(), i.Item.Value));
            }
        }
        public void Visit(Ast.Expr.Float f)
        {
            unsafe
            {
                this.m_valueStack.Push(LLVM.ConstReal(LLVM.FloatType(), f.Item.Value));
            }
        }
        public void Visit(Ast.Expr.String s)
        {
            unsafe
            {
                // https://stackoverflow.com/questions/5666073/c-converting-string-to-sbyte
                byte[] bytes = Encoding.ASCII.GetBytes(s.Item.Value);
                fixed (byte* b = bytes)
                {
                    sbyte* sb = (sbyte*)b;
                    this.m_valueStack.Push(LLVM.ConstString(sb, (uint)s.Item.Value.Length, 0));
                }
            }
        }
        public void Visit(Ast.Expr.Bool b)
        {
            unsafe
            {
                if (b.Item.Value) {
                    this.m_valueStack.Push(LLVM.ConstInt(LLVM.Int8Type(), (ulong) 1, 0));
                } else {
                    this.m_valueStack.Push(LLVM.ConstInt(LLVM.Int8Type(), (ulong) 1, 0));
                }
            }
        }
        public void Visit(Ast.Expr.FuncCall f)
        {
            foreach (var e in f.Item.ExprList)
            {
                Visit(e);
            }
        }
        public void Visit(Ast.Expr.BinOp b)
        {
            Visit(b.Item.Lhs);
            Visit(b.Item.Rhs);

            LLVMValueRef rhs = this.m_valueStack.Pop();
            LLVMValueRef lhs = this.m_valueStack.Pop();

            LLVMValueRef expr;
            unsafe
            {
                // Arithmetic operations
                if (b.ActualType.IsIntType || b.ActualType.IsFloatType)
                {
                    if (b.Item.Op.IsOpPlus)
                    {
                        expr = this.m_builder.BuildFAdd(lhs, rhs);
                    }
                    else if (b.Item.Op.IsOpMinus)
                    {
                        expr = this.m_builder.BuildFSub(lhs, rhs);
                    }
                    else if (b.Item.Op.IsOpMul)
                    {
                        expr = this.m_builder.BuildFMul(lhs, rhs);
                    }
                    else
                    {  // if (b.Item.Op.IsOpDiv) {
                        expr = this.m_builder.BuildFDiv(lhs, rhs);
                    }
                    this.m_valueStack.Push(expr);
                }
                // Boolean Operations
                else if (b.ActualType.IsBoolType)
                {
                    if (b.Item.Op.IsOpAnd)
                    {
                        expr = this.m_builder.BuildAnd(lhs, rhs);
                    }
                   else if (b.Item.Op.IsOpOr)
                    {
                        expr = this.m_builder.BuildOr(lhs, rhs);
                    }
                    else
                    {
                        LLVMRealPredicate pred;
                        if (b.Item.Op.IsOpLess)
                        {
                            pred = LLVMRealPredicate.LLVMRealULT;
                        }
                        else if (b.Item.Op.IsOpGreater)
                        {
                            pred = LLVMRealPredicate.LLVMRealUGT;
                        }
                        else if (b.Item.Op.IsOpGreaterEqual)
                        {
                            pred = LLVMRealPredicate.LLVMRealUGE;
                        }
                        else if (b.Item.Op.IsOpLessEqual)
                        {
                            pred = LLVMRealPredicate.LLVMRealULE;
                        }
                        else if (b.Item.Op.IsOpEqual)
                        {
                            pred = LLVMRealPredicate.LLVMRealUEQ;
                        }
                        else
                        {
                            pred = LLVMRealPredicate.LLVMRealUNE;
                        }
                        expr = this.m_builder.BuildFCmp(pred, lhs, rhs);
                    }
                    this.m_valueStack.Push(expr);
                    System.Console.WriteLine(expr);
                }
                // Type is string, nil, or unresolved
            }
        }
        public void Visit(Ast.Expr.UnOp u)
        {
            Visit(u.Item.Lhs);
            LLVMValueRef operand = this.m_valueStack.Pop();

            LLVMValueRef expr;
            unsafe
            {
                if (u.Item.Op.IsOpBoolNegate)
                {
                    expr = this.m_builder.BuildNot(operand);
                }
                else if (u.Item.Op.IsOpValNegate)
                {
                    expr = this.m_builder.BuildNeg(operand);
                }
                else if (u.Item.Op.IsOpIncr)
                {
                    expr = this.m_builder.BuildFAdd(operand, LLVM.ConstReal(LLVM.FloatType(), 1));
                }
                else
                {  // if (u.Item.Op.IsOpDecr) {
                    expr = this.m_builder.BuildFAdd(operand, LLVM.ConstReal(LLVM.FloatType(), -1));
                }
                this.m_valueStack.Push(expr);
                System.Console.WriteLine(expr);
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
