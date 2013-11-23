using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using LinqExprs = System.Linq.Expressions;
using LinqExpr = System.Linq.Expressions.Expression;
using ParamExpr = System.Linq.Expressions.ParameterExpression;

namespace ComputerAlgebra.LinqCompiler
{
    /// <summary>
    /// Represents the code generation for a function.
    /// </summary>
    public class CodeGen : DeclContext
    {
        private class ScopeDeclContext : DeclContext
        {
            public void Map(Expression Expr, LinqExpr To) { map[Expr] = To; }

            public ScopeDeclContext(DeclContext Parent) : base(Parent) { }
        }

        public Module Module { get { return (Module)Parent; } }

        private LinqExprs.LabelTarget ret = null;

        private List<LinqExpr> code = new List<LinqExpr>();
        private List<ParamExpr> parameters = new List<ParamExpr>();
        private ScopeDeclContext scope;

        private int anonymous = 1;
        private Dictionary<Expression, LinqExpr> intermediates = new Dictionary<Expression, LinqExpr>();

        private IEnumerable<Type> libraries = new Type[] { typeof(StandardMath) };
        public IEnumerable<Type> Libraries { get { return libraries; } set { libraries = value; } }
        private CompileVisitor compiler;

        public CodeGen() : this(null) { }

        public CodeGen(Module Module) : base(Module != null ? Module : new Module())
        {
            compiler = new CompileVisitor(this);
            scope = new ScopeDeclContext(this);
        }

        /// <summary>
        /// Add some instructions to the codegen.
        /// </summary>
        /// <param name="Instructions"></param>
        public void Add(params LinqExpr[] Instructions) { code.AddRange(Instructions); }

        /// <summary>
        /// Create a lambda with the generated code.
        /// </summary>
        /// <param name="Parameters"></param>
        /// <returns></returns>
        public LinqExprs.LambdaExpression Build()
        {
            // If the return target has been created, append it to the body.
            IEnumerable<LinqExpr> body = code;
            if (ret != null)
                body = body.Append(LinqExpr.Label(ret, ret.Type.IsValueType ? LinqExpr.Constant(Activator.CreateInstance(ret.Type)) : null));

            return LinqExpr.Lambda(LinqExpr.Block(Decls, body), parameters);
        }

        public LinqExprs.Expression<T> Build<T>()
        {
            // If the return target has been created, append it to the body.
            IEnumerable<LinqExpr> body = code;
            if (ret != null)
                body = body.Append(LinqExpr.Label(ret, ret.Type.IsValueType ? LinqExpr.Constant(Activator.CreateInstance(ret.Type)) : null));

            return LinqExpr.Lambda<T>(LinqExpr.Block(Decls, body), parameters);
        }

        /// <summary>
        /// Generate a return statement.
        /// </summary>
        /// <param name="Value"></param>
        public void Return<T>(LinqExpr Value)
        {
            // Create the return label if it doesn't already exist.
            if (ret == null)
                ret = LinqExpr.Label(typeof(T), "ret");
            code.Add(LinqExpr.Return(ret, Value));
        }

        public void Return()
        {
            // Create the return label if it doesn't already exist.
            if (ret == null)
                ret = LinqExpr.Label("ret");
            code.Add(LinqExpr.Return(ret));
        }

        /// <summary>
        /// Push a new scope for live variables.
        /// </summary>
        public void PushScope() { scope = new ScopeDeclContext(scope); }
        /// <summary>
        /// Pop the current live variables.
        /// </summary>
        public void PopScope() { scope = (ScopeDeclContext)scope.Parent; }

        /// <summary>
        /// Intermediate expressions are reset at synchronization points. For example,
        /// 
        ///    LinqExpr x = code.Compile("Exp[t]*Exp[t]");
        ///    
        /// will generate one call to Exp[t], stored in an intermediate scope variable _int,
        /// x will be stored in a second intermediate _int2 = _int * _int.
        /// 
        /// However, in this example,
        /// 
        ///    LinqExpr x = code.Compile("Exp[t]*Exp[t]");
        ///    code.SyncPoint();
        ///    LinqExpr y = code.Compile("Exp[t]*Exp[t]");
        ///    
        /// the intermediate for Exp[t] from compiling x will not be re-used when compiling y.
        /// </summary>
        public void SyncPoint() { intermediates.Clear(); }

        /// <summary>
        /// Look up an existing expression in the map and intermediates.
        /// </summary>
        /// <param name="Expr"></param>
        /// <returns>null if the expression was not found.</returns>
        public new LinqExpr LookUp(Expression Expr)
        {
            LinqExpr ret;
            if (intermediates.TryGetValue(Expr, out ret))
                return ret;
            return scope.LookUp(Expr);
        }
        public void Map(Scope Scope, Expression Expr, LinqExpr To)
        {
            switch (Scope)
            {
                case Scope.Local: scope.Map(Expr, To); break;
                case Scope.Intermediate: intermediates[Expr] = To; break;
                case Scope.Parameter: map[Expr] = To; break;
                default: throw new ArgumentException("Scope is not valid for mapping.");
            }
        }

        public LinqExpr this[Expression Expr]
        {
            get 
            {
                LinqExpr expr = LookUp(Expr);
                if (expr != null)
                    return expr;
                throw new KeyNotFoundException();
            }
            set { Map(Scope.Local, Expr, value); }
        }

        /// <summary>
        /// Compile the expression, returning the result expression.
        /// </summary>
        /// <param name="Expr"></param>
        /// <returns></returns>
        public LinqExpr Compile(Expression Expr) { return compiler.Visit(Expr); }

        /// <summary>
        /// Add variables to the specified scope.
        /// </summary>
        /// <param name="Scope"></param>
        /// <param name="Vars"></param>
        public void Declare(Scope Scope, params ParamExpr[] Vars)
        {
            switch (Scope)
            {
                case Scope.Parameter: parameters.AddRange(Vars); break;
                case Scope.Local: decls.AddRange(Vars); scope.Declare(Vars); break;
                case Scope.Intermediate: decls.AddRange(Vars); break;
                default: throw new InvalidOperationException("Unknown variable scope.");
            }
        }
        
        /// <summary>
        /// Add variables to the local scope.
        /// </summary>
        /// <param name="Vars"></param>
        public void Declare(params ParamExpr[] Vars) { Declare(Scope.Local, Vars); }
        
        /// <summary>
        /// Declare a new variable.
        /// </summary>
        /// <param name="Scope"></param>
        /// <param name="T"></param>
        /// <param name="Name"></param>
        /// <returns></returns>
        public ParamExpr Decl(Scope Scope, Type T, string Name)
        {
            ParamExpr p = LinqExpr.Parameter(T, Name);
            Declare(Scope, p);
            return p;
        }
        public ParamExpr Decl(Scope Scope, Type T) { return Decl(Scope, T, AnonymousName()); }
        public ParamExpr Decl<T>(Scope Scope, string Name) { return Decl(Scope, typeof(T), Name); }
        public ParamExpr Decl<T>(Scope Scope) { return Decl(Scope, typeof(T), AnonymousName()); }

        // Default local.
        public ParamExpr Decl(Type T, string Name) { return Decl(Scope.Local, T, Name); }
        public ParamExpr Decl(Type T) { return Decl(Scope.Local, T); }
        public ParamExpr Decl<T>(string Name) { return Decl<T>(Scope.Local, Name); }
        public ParamExpr Decl<T>() { return Decl<T>(Scope.Local, AnonymousName()); }

        /// <summary>
        /// Declare and initialize a new variable.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="Name"></param>
        /// <param name="Init"></param>
        /// <returns></returns>
        public ParamExpr DeclInit<T>(Scope Scope, string Name, LinqExpr Init)
        {
            ParamExpr p = Decl(Scope, typeof(T), Name);
            code.Add(LinqExpr.Assign(p, Init));
            return p;
        }

        public ParamExpr DeclInit<T>(Scope Scope, string Name, T Init)
        {
            ParamExpr p = Decl(Scope.Local, typeof(T), Name);
            code.Add(LinqExpr.Assign(p, LinqExpr.Constant(Init)));
            return p;
        }

        // Default to local declaration.
        public ParamExpr DeclInit<T>(string Name, LinqExpr Init) { return DeclInit<T>(Scope.Local, Name, Init); }
        public ParamExpr DeclInit<T>(string Name, T Init) { return DeclInit<T>(Scope.Local, Name, Init); }

        public LinqExpr ReDecl(Type T, string Name)
        {
            LinqExpr d = scope.LookUp(Name);
            if (d == null)
                d = Decl(Scope.Local, T, Name);
            return d;
        }
        public LinqExpr ReDecl<T>(string Name) { return ReDecl(typeof(T), Name); }

        public LinqExpr ReDeclInit<T>(string Name, LinqExpr Init)
        {
            LinqExpr p = ReDecl(typeof(T), Name);
            code.Add(LinqExpr.Assign(p, Init));
            return p;
        }

        public LinqExpr ReDeclInit<T>(string Name, T Init)
        {
            LinqExpr p = ReDecl(typeof(T), Name);
            code.Add(LinqExpr.Assign(p, LinqExpr.Constant(Init)));
            return p;
        }

        /// <summary>
        /// Declare a variable mapped to an expression.
        /// </summary>
        /// <param name="Scope"></param>
        /// <param name="Name"></param>
        /// <param name="Expr"></param>
        /// <returns></returns>
        public ParamExpr Decl(Scope Scope, string Name, Expression Expr)
        {
            ParamExpr p = LinqExpr.Parameter(typeof(double), Name);
            Declare(Scope, p);
            Map(Scope, Expr, p);
            return p;
        }
        public ParamExpr Decl(Scope Scope, Expression Expr) { return Decl(Scope, "_" + AnonymousName(), Expr); }

        public ParamExpr DeclInit(Scope Scope, Expression Expr, LinqExpr Init)
        {
            ParamExpr d = Decl(Scope, Expr);
            Add(LinqExpr.Assign(d, Init));
            return d;
        }

        public ParamExpr DeclInit(Scope Scope, Expression Expr, Expression Init)
        {
            ParamExpr d = Decl(Scope, Expr);
            Add(LinqExpr.Assign(d, Compile(Init)));
            return d;
        }

        // Default to local declaration.
        public ParamExpr Decl(string Name, Expression Expr) { return Decl(Scope.Local, Name, Expr); }
        public ParamExpr Decl(Expression Expr) { return Decl(Scope.Local, "_" + AnonymousName(), Expr); }
        public ParamExpr DeclInit(Expression Expr, LinqExpr Init) { return DeclInit(Scope.Local, Expr, Init); }
        public ParamExpr DeclInit(Expression Expr, Expression Init) { return DeclInit(Scope.Local, Expr, Init); }
        
        /// <summary>
        /// Generate a for loop with the given code generator functions.
        /// </summary>
        /// <param name="Init"></param>
        /// <param name="Condition"></param>
        /// <param name="Step"></param>
        /// <param name="Body"></param>
        public void For(
            Action Init,
            LinqExpr Condition,
            Action Step,
            Action<LinqExpr, LinqExpr> Body)
        {
            PushScope();

            // Generate the loop header code.
            Init();

            string name = LabelName();
            LinqExprs.LabelTarget begin = LinqExpr.Label("for_" + name + "_begin");
            LinqExprs.LabelTarget end = LinqExpr.Label("for_" + name + "_end");

            // Check the condition, exit if necessary.
            code.Add(LinqExpr.Label(begin));
            code.Add(LinqExpr.IfThen(LinqExpr.Not(Condition), LinqExpr.Goto(end)));

            // Generate the body code.
            Body(LinqExpr.Goto(end), LinqExpr.Goto(begin));

            // Generate the step code.
            Step();
            code.Add(LinqExpr.Goto(begin));

            // Exit point.
            code.Add(LinqExpr.Label(end));

            PopScope();
        }
        
        public void For(Action Init, LinqExpr Condition, Action Step, Action<LinqExpr> Body) { For(Init, Condition, Step, (end, y) => Body(end)); }
        public void For(Action Init, LinqExpr Condition, Action Step, Action Body) { For(Init, Condition, Step, (x, y) => Body()); }

        /// <summary>
        /// Generate a while loop with the given code generator functions.
        /// </summary>
        /// <param name="Condition"></param>
        /// <param name="Body"></param>
        public void While(
            LinqExpr Condition,
            Action<LinqExpr, LinqExpr> Body)
        {
            string name = LabelName();
            LinqExprs.LabelTarget begin = LinqExpr.Label("while_" + name + "_begin");
            LinqExprs.LabelTarget end = LinqExpr.Label("while_" + name + "_end");

            PushScope();

            // Check the condition, exit if necessary.
            code.Add(LinqExpr.Label(begin));
            code.Add(LinqExpr.IfThen(LinqExpr.Not(Condition), LinqExpr.Goto(end)));

            // Generate the body code.
            Body(LinqExpr.Goto(end), LinqExpr.Goto(begin));

            // Loop.
            code.Add(LinqExpr.Goto(begin));

            // Exit label.
            code.Add(LinqExpr.Label(end));

            PopScope();
        }

        public void While(LinqExpr Condition, Action<LinqExpr> Body) { While(Condition, (end, y) => Body(end)); }
        public void While(LinqExpr Condition, Action Body) { While(Condition, (x, y) => Body()); }

        /// <summary>
        /// Generate an infinite loop with the given code generator functions.
        /// </summary>
        /// <param name="Condition"></param>
        /// <param name="Body"></param>
        public void Loop(Action<LinqExpr, LinqExpr> Body)
        {
            string name = LabelName();
            LinqExprs.LabelTarget begin = LinqExpr.Label("while_" + name + "_begin");
            LinqExprs.LabelTarget end = LinqExpr.Label("while_" + name + "_end");

            PushScope();

            code.Add(LinqExpr.Label(begin));

            // Generate the body code.
            Body(LinqExpr.Goto(end), LinqExpr.Goto(begin));

            // Loop.
            code.Add(LinqExpr.Goto(begin));

            // Exit label.
            code.Add(LinqExpr.Label(end));

            PopScope();
        }

        public void Loop(Action<LinqExpr> Body) { Loop((end, y) => Body(end)); }

        /// <summary>
        /// Generate a do-while loop with the given code generator functions.
        /// </summary>
        /// <param name="Body"></param>
        /// <param name="Condition"></param>
        public void DoWhile(Action<LinqExpr, LinqExpr> Body, LinqExpr Condition)
        {
            string name = LabelName();
            LinqExprs.LabelTarget begin = LinqExpr.Label("do_" + name + "_begin");
            LinqExprs.LabelTarget end = LinqExpr.Label("do_" + name + "_end");

            PushScope();

            // Check the condition, exit if necessary.
            code.Add(LinqExpr.Label(begin));

            // Generate the body code.
            Body(LinqExpr.Goto(end), LinqExpr.Goto(begin));

            // Loop.
            code.Add(LinqExpr.IfThen(Condition, LinqExpr.Goto(begin)));

            // Exit label.
            code.Add(LinqExpr.Label(end));

            PopScope();
        }

        public void DoWhile(Action<LinqExpr> Body, LinqExpr Condition) { DoWhile((end, y) => Body(end), Condition); }
        public void DoWhile(Action Body, LinqExpr Condition) { DoWhile((x, y) => Body(), Condition); }

        private string LabelName() { return (++anonymous).ToString(); }
        private string AnonymousName() { return "!" + (++anonymous).ToString(); }
    }
}
