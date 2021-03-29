using System.Collections.Generic;
using System.Text;
using LLVMSharp.Interop;
using System.Linq;


namespace CoalLang
{
    public static class Extensions
    {
        public static IEnumerable<(T item, int index)> WithIndex<T>(this IEnumerable<T> self)
            => self.Select((item, index) => (item, index));

        public unsafe static sbyte* ToCString(this string str)
        {
            if (str == "")
            {
                fixed (sbyte* ptr = new[] { (sbyte)'\0' })
                    return ptr;
            }

            fixed (byte* ptr = &Encoding.Default.GetBytes(str)[0])
                return (sbyte*)ptr;
        }
    }

    public class CodeGenVisitor
    {
        Ast.Prog m_prog;
        LLVMModuleRef m_module;
        LLVMBuilderRef m_builder;
        List<Dictionary<string, LLVMValueRef>> m_namedValues;
        Stack<LLVMValueRef> m_funcStack;

        public CodeGenVisitor(Ast.Prog prog)
        {
            this.m_prog = prog;
            this.m_namedValues = new List<Dictionary<string, LLVMValueRef>>();
            this.m_funcStack = new Stack<LLVMValueRef>();
            unsafe
            {
                fixed (byte* ptr = Encoding.ASCII.GetBytes("mod"))
                    this.m_module = LLVM.ModuleCreateWithName((sbyte*) ptr);
            }
            unsafe
            {
                this.m_builder = LLVM.CreateBuilder();
            }
            this.Visit();
            this.Gen();
        }


        public void Gen() {

            unsafe {
                LLVM.InitializeNativeTarget();
                LLVM.InitializeNativeAsmPrinter();

                sbyte* errorMessage;
                LLVMTarget* target;
                int code = LLVM.GetTargetFromTriple(
                                                 LLVM.GetDefaultTargetTriple(),
                                                 &target,
                                                 &errorMessage
                                                 );
                System.Console.WriteLine(code);
                LLVMTargetMachineRef targetMachine = LLVM.CreateTargetMachine(
                                                                              target,
                                                                              LLVM.GetDefaultTargetTriple(),
                                                                              LLVM.GetHostCPUName(),
                                                                              LLVM.GetHostCPUFeatures(),
                                                                              LLVMCodeGenOptLevel.LLVMCodeGenLevelNone,
                                                                              LLVMRelocMode.LLVMRelocDefault,
                                                                              LLVMCodeModel.LLVMCodeModelDefault
                                                                              );
                this.m_module.Verify(LLVMVerifierFailureAction.LLVMPrintMessageAction);
                targetMachine.EmitToFile(this.m_module, "asm.S", LLVMCodeGenFileType.LLVMAssemblyFile);
            }
        }

        public void Visit()
        {
            this.m_namedValues.Add(new Dictionary<string, LLVMValueRef>());
            LLVMTypeRef[] paramType = { };
            var funcType = LLVMTypeRef.CreateFunction(LLVMTypeRef.Void, paramType);
            var func = m_module.AddFunction("main", funcType);
            var entry = func.AppendBasicBlock("entry");
            var body = func.AppendBasicBlock("body");
            m_builder.PositionAtEnd(body);
            this.m_funcStack.Push(func);
            Visit(this.m_prog);
            m_builder.BuildRetVoid();
            m_builder.PositionAtEnd(entry);
            m_builder.BuildBr(body);
            this.m_namedValues.RemoveAt(this.m_namedValues.Count - 1);
            System.Console.WriteLine(this.m_module);
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
            // RHS
            LLVMValueRef rhs = Visit(a.Item.Rhs);
            // LHS
            LLVMValueRef lhs = VisitRef((Ast.Expr.VarRef) a.Item.Lhs, false);
            LLVMValueRef assign = this.m_builder.BuildStore(rhs, lhs);

        }
        public void Visit(Ast.Stmt.While w)
        {
            // Build basic blocks
            var func = this.m_funcStack.Peek();
            var loopBB = func.AppendBasicBlock("loop");
            var exitBB = func.AppendBasicBlock("exit");
            var bodyBB = func.AppendBasicBlock("body");

            // Loop test
            this.m_builder.BuildBr(loopBB);
            this.m_builder.PositionAtEnd(loopBB);

            // Cond
            LLVMValueRef cond = Visit(w.Item.Cond);
            this.m_builder.BuildCondBr(cond, bodyBB, exitBB);
            this.m_builder.PositionAtEnd(bodyBB);

            //  Body
            Visit(w.Item.Body);
            this.m_builder.BuildBr(loopBB);
            this.m_builder.PositionAtEnd(exitBB);
         
        }
        public void Visit(Ast.Stmt.Seq s)
        {
            this.m_namedValues.Add(new Dictionary<string, LLVMValueRef> ());
            foreach (var v in s.Item.Body)
            {
                Visit(v);
            }
            this.m_namedValues.RemoveAt(this.m_namedValues.Count - 1);
        }
        public void Visit(Ast.Stmt.IfThenElse i)
        {
            unsafe
            {
                // Cond
                LLVMValueRef cond = Visit(i.Item.Cond);
                // Build Basic Blocks and the branch instruction
                var func = this.m_funcStack.Peek();
                var bodyBB = func.AppendBasicBlock("body");
                var elseBB = func.AppendBasicBlock("else");
                var contBB = func.AppendBasicBlock("cont");
                this.m_builder.BuildCondBr(cond, bodyBB, elseBB);

                // Body
                this.m_builder.PositionAtEnd(bodyBB);
                Visit(i.Item.Body);
                this.m_builder.BuildBr(contBB);

                // Else
                this.m_builder.PositionAtEnd(elseBB);
                if (i.Item.ElseBody != null)
                {
                    Visit(i.Item.ElseBody.Value);
                }
                this.m_builder.BuildBr(contBB);

                this.m_builder.PositionAtEnd(contBB);
            }
        }
        public void Visit(Ast.Stmt.Vardef v)
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
            var tmpB = m_module.Context.CreateBuilder();
            tmpB.PositionAtEnd(m_funcStack.Peek().EntryBasicBlock);
            unsafe
            {
                if (v.Item.Formal.Type.IsIntType)
                {
                    def = tmpB.BuildAlloca(LLVMTypeRef.Int32, v.Item.Formal.Name);
                }
                else if (v.Item.Formal.Type.IsFloatType)
                {
                    def = tmpB.BuildAlloca(LLVMTypeRef.Float, v.Item.Formal.Name);
                }
                else if (v.Item.Formal.Type.IsBoolType)
                {
                    def = tmpB.BuildAlloca(LLVMTypeRef.Int1, v.Item.Formal.Name);
                }
                else if (v.Item.Formal.Type.IsStringType)
                {
                    def = tmpB.BuildAlloca(LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0), v.Item.Formal.Name);
                }

                this.m_namedValues[this.m_namedValues.Count - 1].Add(v.Item.Formal.Name, def);

                if (expr != null)
                {
                    this.m_builder.BuildStore(expr, def);
                }
            }
            tmpB.Dispose();

        }
        private LLVMTypeRef typeToLLVMType(Ast.Type t)
        {
            LLVMTypeRef type = null;
            if (t.IsNilType)
                return LLVMTypeRef.Void;
            else if (t.IsBoolType)
                return LLVMTypeRef.Int1;
            else if (t.IsIntType)
                return LLVMTypeRef.Int32;
            else if (t.IsFloatType)
                return LLVMTypeRef.Float;
            else if (t.IsStringType)
                return LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0);
            else
                return LLVMTypeRef.Void;
        }
        public void Visit(Ast.Stmt.Funcdef f)
        {
            // Build the Function Type with return type and param list
            LLVMTypeRef type = typeToLLVMType(f.Item.Formal.Type);
            LLVMTypeRef[] paramTypes = new LLVMTypeRef[f.Item.FormalList.Length];
            for (int i = 0; i < f.Item.FormalList.Length; ++i)
            {
                paramTypes[i] = typeToLLVMType(f.Item.FormalList[i].Formal.Type);
            }
            LLVMTypeRef funcType = LLVMTypeRef.CreateFunction(type, paramTypes, false);
            LLVMValueRef func = this.m_module.AddFunction(f.Item.Formal.Name, funcType);
            this.m_namedValues[this.m_namedValues.Count - 1].Add(f.Item.Formal.Name, func);
            this.m_namedValues.Add(new Dictionary<string, LLVMValueRef>());
            this.m_funcStack.Push(func);
            // Add paramaters to the named values
            for (int i = 0; i < f.Item.FormalList.Length; ++i)
            {
                this.m_namedValues[this.m_namedValues.Count - 1].Add(f.Item.FormalList[i].Formal.Name, func.Params[i]);
            }
            // keep track of previous block
            var insert = this.m_builder.InsertBlock;

            // Build the body
            var body = func.AppendBasicBlock($"body_{f.Item.Formal.Name}");
            this.m_builder.PositionAtEnd(body);
            Visit(f.Item.Body);
            // Cleanup
            this.m_builder.PositionAtEnd(insert);
            this.m_namedValues.RemoveAt(this.m_namedValues.Count - 1);
            // This doesn't work for scope :/
            this.m_funcStack.Pop();
        }
        public void Visit(Ast.Stmt.Expr e)
        {
            Visit(e.Item.Expr);
        }
        public void Visit(Ast.Stmt.Return r)
        {
            var retval = Visit(r.Item.Expr.Value);
            this.m_builder.BuildRet(retval);
        }
        // Expr
        public LLVMValueRef VisitRef(Ast.Expr.VarRef vr, bool isRhs = true)
        {
            LLVMValueRef value;
            for(int i = this.m_namedValues.Count - 1; i >= 0; --i)
            {
                var d = this.m_namedValues[i];
                d.TryGetValue(vr.Item.Name, out value);
                if (value != null) {
                    if (value.IsAAllocaInst != null && isRhs)
                        return this.m_builder.BuildLoad(value);
                    return value;
                }
            }
            return null;
        }
        public LLVMValueRef Visit(Ast.Expr.Int i)
        {
            LLVMValueRef expr;
            unsafe
            {
                expr = LLVM.ConstInt(LLVM.Int32Type(), (ulong) i.Item.Value, 0);
            }
            return expr;
        }
        public LLVMValueRef Visit(Ast.Expr.Float f)
        {
            LLVMValueRef expr;
            unsafe
            {
                expr = LLVM.ConstReal(LLVM.FloatType(), f.Item.Value);
            }
            return expr;
        }
        public LLVMValueRef Visit(Ast.Expr.String s)
        {
            unsafe {
                var str = this.m_builder.BuildGlobalString(s.Item.Value);
                LLVMValueRef[] ind = { LLVM.ConstInt(LLVMTypeRef.Int32, 0, 0), LLVM.ConstInt(LLVMTypeRef.Int32, (ulong) s.Size, 0) };
                return this.m_builder.BuildGEP(str, ind);
            }

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
            }
            return expr;
        }
        public LLVMValueRef Visit(Ast.Expr.FuncCall f)
        {
            LLVMValueRef[] args = new LLVMValueRef[f.Item.ExprList.Length];
            int arg = 0;
            foreach (var e in f.Item.ExprList)
            {
                LLVMValueRef v = Visit(e);
                args[arg] = v;
                ++arg;
            }

            LLVMValueRef fn = null;
            for(int i = this.m_namedValues.Count - 1; i >= 0; --i)
            {
                var d = this.m_namedValues[i];
                d.TryGetValue(f.Item.Name, out fn);
            }
            LLVMValueRef expr = this.m_builder.BuildCall(fn, args);
            return expr;
        }
        // TODO Reorganize logic to not check type of b
        public LLVMValueRef Visit(Ast.Expr.BinOp b)
        {
            LLVMValueRef rhs = Visit(b.Item.Lhs);
            LLVMValueRef lhs = Visit(b.Item.Rhs);

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
                        if (b.Item.Lhs.ActualType.IsFloatType)
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
                                pred = LLVMRealPredicate.LLVMRealOEQ;
                            }
                            else
                            {
                                pred = LLVMRealPredicate.LLVMRealONE;
                            }
                            expr = this.m_builder.BuildFCmp(pred, lhs, rhs);
                        }
                        else
                        {
                            LLVMIntPredicate pred;
                            if (b.Item.Op.IsOpLess)
                            {
                                pred = LLVMIntPredicate.LLVMIntULT;
                            }
                            else if (b.Item.Op.IsOpGreater)
                            {
                                pred = LLVMIntPredicate.LLVMIntUGT;
                            }
                            else if (b.Item.Op.IsOpGreaterEqual)
                            {
                                pred = LLVMIntPredicate.LLVMIntUGE;
                            }
                            else if (b.Item.Op.IsOpLessEqual)
                            {
                                pred = LLVMIntPredicate.LLVMIntULE;
                            }
                            else if (b.Item.Op.IsOpEqual)
                            {
                                pred = LLVMIntPredicate.LLVMIntEQ;
                            }
                            else
                            {
                                pred = LLVMIntPredicate.LLVMIntNE;
                            }
                            expr = this.m_builder.BuildICmp(pred, lhs, rhs);
                        }
                    }
                }
                else
                {
                    expr = null;
                }
                // Type is string, nil, or unresolved
            }
            return expr;
        }
        public LLVMValueRef Visit(Ast.Expr.UnOp u)
        {
            LLVMValueRef operand = VisitRef((Ast.Expr.VarRef) u.Item.Lhs);

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
                    expr = this.m_builder.BuildAdd(operand, LLVM.ConstInt(LLVM.Int32Type(), 1, 0));
                }
                else if (u.Item.Op.IsOpIncr && u.Item.ActualType.IsFloatType)
                {
                    expr = this.m_builder.BuildFAdd(operand, LLVM.ConstReal(LLVM.FloatType(), 1));
                }
                else if (u.Item.Op.IsOpDecr && u.Item.ActualType.IsIntType)
                {
                    expr = this.m_builder.BuildAdd(operand, LLVM.ConstInt(LLVM.Int32Type(), 0xFFFFFFFF, 0));
                }
                else if (u.Item.Op.IsOpDecr && u.Item.ActualType.IsFloatType)
                {
                    expr = this.m_builder.BuildFAdd(operand, LLVM.ConstReal(LLVM.FloatType(), -1));
                }
                else
                {
                    expr = null;
                }
                this.m_builder.BuildStore(expr, operand);
            }
            return expr;
        }
        private LLVMValueRef Visit(Ast.Expr e)
        {
            switch (e)
            {
                case Ast.Expr.VarRef v:
                    return VisitRef(v);
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
