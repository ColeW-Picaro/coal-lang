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
    public class CodeGenVisitor
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
        public LLVMBasicBlockRef Visit(Ast.Stmt stmt)
        {
            switch (stmt)
            {
                case Ast.Stmt.Assign a:
                    return Visit(a);
                case Ast.Stmt.While w:
                    return Visit(w);
                case Ast.Stmt.Seq s:
                    return Visit(s);
                case Ast.Stmt.IfThenElse i:
                    return Visit(i);
                case Ast.Stmt.Vardef v:
                    return Visit(v);
                case Ast.Stmt.Funcdef f:
                    return Visit(f);
                case Ast.Stmt.Expr e:
                    LLVMBasicBlockRef bb = Visit(e);
                    System.Console.WriteLine(bb);
                    return bb;
                case Ast.Stmt.Return r:
                    return Visit(r);
                default:
                    return null;
            }
        }
        public LLVMBasicBlockRef Visit(Ast.Stmt.Assign a)
        {
            // RHS
            LLVMValueRef rhs = Visit(a.Item.Rhs);
            // LHS
            LLVMValueRef lhs = Visit(a.Item.Lhs);
            LLVMValueRef assign = this.m_builder.BuildStore(lhs, rhs);
            unsafe
            {
                LLVMBasicBlockRef bb = LLVM.ValueAsBasicBlock(rhs);
                LLVM.AppendExistingBasicBlock(lhs, bb);
                LLVM.AppendExistingBasicBlock(assign, bb);
                return bb;
            }
        }
        public LLVMBasicBlockRef Visit(Ast.Stmt.While w)
        {
            // Cond
            Visit(w.Item.Cond);
            // Body
            Visit(w.Item.Body);
            LLVMBasicBlockRef bb = null;
            return bb;
        }
        public LLVMBasicBlockRef Visit(Ast.Stmt.Seq s)
        {
            unsafe
            {
                LLVMBasicBlockRef bb = null;
                bool first = true;
                foreach (var v in s.Item.Body)
                {
                    if (first) {
                        first = false;
                        bb = LLVM.ValueAsBasicBlock(Visit(v));
                    }
                    else
                    {
                        LLVM.AppendExistingBasicBlock(Visit(v), bb);
                    }
                }
                return bb;
            }

        }
        public LLVMBasicBlockRef Visit(Ast.Stmt.IfThenElse i)
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
            return null;
        }
        public LLVMBasicBlockRef Visit(Ast.Stmt.Vardef v)
        {
            LLVMValueRef expr;
            if (v.Item.Expr != null)
            {
                expr = Visit(v.Item.Expr.Value);
            }
            else
            {
                expr = null;
            }
            LLVMValueRef def = null;
            unsafe
            {
                if (v.Item.Formal.Type.IsIntType)
                {
                    def = this.m_builder.BuildAlloca(LLVM.Int32Type(), v.Item.Formal.Name);
                    this.m_namedValues.Add(v.Item.Formal.Name, def);
                }
                else if (v.Item.Formal.Type.IsFloatType)
                {
                    def = this.m_builder.BuildAlloca(LLVM.FloatType(), v.Item.Formal.Name);
                }
                else if (v.Item.Formal.Type.IsBoolType)
                {
                    def = this.m_builder.BuildAlloca(LLVM.Int1Type(), v.Item.Formal.Name);
                }
                else if (v.Item.Expr.Value.IsString)
                {
                    //def = this.m_builder.BuildGlobalString((string) v.Item.Expr.Value);

                }
            }
            unsafe
            {
                LLVMBasicBlockRef bb = LLVM.ValueAsBasicBlock(def);
                if (expr != null)
                {
                    LLVMValueRef init = this.m_builder.BuildStore(def, expr);
                    LLVM.AppendExistingBasicBlock(init, bb);
                }
                return bb;
            }

        }
        public LLVMBasicBlockRef Visit(Ast.Stmt.Funcdef f)
        {
            Visit(f.Item.Body);
            return null;
        }
        public LLVMBasicBlockRef Visit(Ast.Stmt.Expr e)
        {
            unsafe
            {
                LLVMValueRef bb = Visit(e.Item.Expr);
                System.Console.WriteLine(bb);
                return LLVM.ValueAsBasicBlock(bb);
            }
        }
        public LLVMBasicBlockRef Visit(Ast.Stmt.Return r)
        {
            Visit(r.Item.Expr.Value);
            return null;
        }
        // Expr
        public LLVMValueRef Visit(Ast.Expr.VarRef vr)
        {
            LLVMValueRef value;
            this.m_namedValues.TryGetValue(vr.Item.Name, out value);
            this.m_valueStack.Push(value);
            return value;
        }
        public LLVMValueRef Visit(Ast.Expr.Int i)
        {
            LLVMValueRef expr;
            unsafe
            {
                expr = LLVM.ConstInt(LLVM.Int32Type(), (ulong) i.Item.Value, 0);
                this.m_valueStack.Push(expr);
            }
            return expr;
        }
        public LLVMValueRef Visit(Ast.Expr.Float f)
        {
            LLVMValueRef expr;
            unsafe
            {
                expr = LLVM.ConstReal(LLVM.FloatType(), f.Item.Value);
                this.m_valueStack.Push(expr);
            }
            return expr;
        }
        public LLVMValueRef Visit(Ast.Expr.String s)
        {
            LLVMValueRef expr;
            unsafe
            {
                // https://stackoverflow.com/questions/5666073/c-converting-string-to-sbyte
                byte[] bytes = Encoding.ASCII.GetBytes(s.Item.Value);
                fixed (byte* b = bytes)
                {
                    sbyte* sb = (sbyte*)b;
                    expr = LLVM.ConstString(sb, (uint)s.Item.Value.Length, 0);
                    this.m_valueStack.Push(expr);
                }
            }
            return expr;
        }
        public LLVMValueRef Visit(Ast.Expr.Bool b)
        {
            LLVMValueRef expr;
            unsafe
            {
                if (b.Item.Value) {
                    expr = LLVM.ConstInt(LLVM.Int1Type(), (ulong) 1, 0);
                } else {
                    expr = LLVM.ConstInt(LLVM.Int1Type(), (ulong) 0, 0);
                }
                this.m_valueStack.Push(expr);
            }
            return expr;
        }
        public LLVMValueRef Visit(Ast.Expr.FuncCall f)
        {
            foreach (var e in f.Item.ExprList)
            {
                Visit(e);
            }
            LLVMValueRef expr = null;
            return expr;
        }
        public LLVMValueRef Visit(Ast.Expr.BinOp b)
        {
            Visit(b.Item.Lhs);
            Visit(b.Item.Rhs);

            LLVMValueRef rhs = this.m_valueStack.Pop();
            LLVMValueRef lhs = this.m_valueStack.Pop();

            LLVMValueRef expr;
            unsafe
            {
                // Arithmetic operations
                if (b.ActualType.IsFloatType)
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
                else if (b.ActualType.IsIntType)
                {
                    if (b.Item.Op.IsOpPlus)
                    {
                        expr = this.m_builder.BuildAdd(lhs, rhs);
                    }
                    else if (b.Item.Op.IsOpMinus)
                    {
                        expr = this.m_builder.BuildSub(lhs, rhs);
                    }
                    else if (b.Item.Op.IsOpMul)
                    {
                        expr = this.m_builder.BuildNSWMul(lhs, rhs);
                    }
                    else
                    {  // if (b.Item.Op.IsOpDiv) {
                        expr = this.m_builder.BuildExactSDiv(lhs, rhs);
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
                }
                else
                {
                    expr = null;
                }
                // Type is string, nil, or unresolved
                System.Console.WriteLine(expr);
            }
            return expr;
        }
        public LLVMValueRef Visit(Ast.Expr.UnOp u)
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
                else if (u.Item.Op.IsOpIncr && u.Item.ActualType.IsIntType)
                {
                    expr = this.m_builder.BuildAdd(operand, LLVM.ConstReal(LLVM.Int32Type(), 1));
                }
                else if (u.Item.Op.IsOpIncr && u.Item.ActualType.IsFloatType)
                {
                    expr = this.m_builder.BuildFAdd(operand, LLVM.ConstReal(LLVM.Int32Type(), 1));
                }
                else if (u.Item.Op.IsOpDecr && u.Item.ActualType.IsIntType)
                {
                    expr = this.m_builder.BuildAdd(operand, LLVM.ConstReal(LLVM.Int32Type(), -1));
                }
                else if (u.Item.Op.IsOpDecr && u.Item.ActualType.IsFloatType)
                {
                    expr = this.m_builder.BuildFAdd(operand, LLVM.ConstReal(LLVM.Int32Type(), -1));
                }
                else
                {
                    expr = null;
                }
                this.m_valueStack.Push(expr);
                System.Console.WriteLine(expr);
            }
            return expr;
        }
        private LLVMValueRef Visit(Ast.Expr e)
        {
            switch (e)
            {
                case Ast.Expr.VarRef v:
                    return Visit(v);
                case Ast.Expr.Int i:
                    return Visit(i);
                case Ast.Expr.Float f:
                    return Visit(f);
                case Ast.Expr.String s:
                    return Visit(s);
                case Ast.Expr.Bool b:
                    return Visit(b);
                case Ast.Expr.FuncCall f:
                    return Visit(f);
                case Ast.Expr.BinOp b:
                    return Visit(b);
                case Ast.Expr.UnOp u:
                    return Visit(u);
                default:
                    return null;
            }

        }
    }
}
