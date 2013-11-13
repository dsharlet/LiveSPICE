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

namespace SyMath
{
    // Helper for generating sequences of expressions.
    public class LinqCodeGen : Dictionary<Expression, LinqExpr>
    {
        public enum Scope
        {
            Parameter,
            Local,
        }

        private LinqExprs.LabelTarget ret = null;

        private List<LinqExpr> target = new List<LinqExpr>();
        private List<ParamExpr> locals = new List<ParamExpr>();
        private List<ParamExpr> parameters = new List<ParamExpr>();
        private Stack<List<ParamExpr>> live = new Stack<List<ParamExpr>>();

        public void PushScope() { live.Push(new List<ParamExpr>()); }
        public void PopScope() { live.Pop(); }

        public LinqCodeGen() { PushScope(); }

        /// <summary>
        /// Create a lambda with the given parameters and generated code.
        /// </summary>
        /// <param name="Parameters"></param>
        /// <returns></returns>
        public LinqExprs.LambdaExpression Compile()
        {
            // If the return target has been created, append it to the body.
            IEnumerable<LinqExpr> body = target;
            if (ret != null)
                body = body.Append(LinqExpr.Label(ret));

            return LinqExpr.Lambda(LinqExpr.Block(locals, body), parameters);
        }

        public LinqExprs.Expression<T> Compile<T>()
        {
            // If the return target has been created, append it to the body.
            IEnumerable<LinqExpr> body = target;
            if (ret != null)
                body = body.Append(LinqExpr.Label(ret, ret.Type.IsValueType ? LinqExpr.Constant(Activator.CreateInstance(ret.Type)) : null));

            return LinqExpr.Lambda<T>(LinqExpr.Block(locals, body), parameters);
        }
        
        /// <summary>
        /// Add some instructions to the codegen.
        /// </summary>
        /// <param name="Instructions"></param>
        public void Add(params LinqExpr[] Instructions) { target.AddRange(Instructions); }

        /// <summary>
        /// Generate a return statement.
        /// </summary>
        /// <param name="Value"></param>
        public void Return<T>(LinqExpr Value)
        {
            if (ret == null) ret = LinqExpr.Label(typeof(T), "ret");
            target.Add(LinqExpr.Return(ret, Value));
        }

        public void Return()
        {
            if (ret == null) ret = LinqExpr.Label("ret");
            target.Add(LinqExpr.Return(ret));
        }

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

            // Generate the init code.
            Init();

            string name = LoopName();
            LinqExprs.LabelTarget begin = LinqExpr.Label("for_" + name + "_begin");
            LinqExprs.LabelTarget end = LinqExpr.Label("for_" + name + "_end");

            // Check the condition, exit if necessary.
            target.Add(LinqExpr.Label(begin));
            target.Add(LinqExpr.IfThen(LinqExpr.Not(Condition), LinqExpr.Goto(end)));

            // Generate the body code.
            Body(LinqExpr.Goto(end), LinqExpr.Goto(begin));

            // Generate the step code.
            Step();
            target.Add(LinqExpr.Goto(begin));

            // Exit point.
            target.Add(LinqExpr.Label(end));

            PopScope();
        }

        public void For(
            Action Init,
            LinqExpr Condition,
            Action Step,
            Action<LinqExpr> Body)
        {
            For(Init, Condition, Step, (end, y) => Body(end));
        }

        public void For(
            Action Init,
            LinqExpr Condition,
            Action Step,
            Action Body)
        {
            For(Init, Condition, Step, (x, y) => Body());
        }

        /// <summary>
        /// Generate a while loop with the given code generator functions.
        /// </summary>
        /// <param name="Condition"></param>
        /// <param name="Body"></param>
        public void While(
            LinqExpr Condition,
            Action<LinqExpr, LinqExpr> Body)
        {
            string name = LoopName();
            LinqExprs.LabelTarget begin = LinqExpr.Label("while_" + name + "_begin");
            LinqExprs.LabelTarget end = LinqExpr.Label("while_" + name + "_end");

            PushScope();

            // Check the condition, exit if necessary.
            target.Add(LinqExpr.Label(begin));
            target.Add(LinqExpr.IfThen(LinqExpr.Not(Condition), LinqExpr.Goto(end)));

            // Generate the body code.
            Body(LinqExpr.Goto(end), LinqExpr.Goto(begin));

            // Loop.
            target.Add(LinqExpr.Goto(begin));

            // Exit label.
            target.Add(LinqExpr.Label(end));

            PopScope();
        }

        public void While(
            LinqExpr Condition,
            Action<LinqExpr> Body)
        {
            While(Condition, (end, y) => Body(end));
        }

        public void While(
            LinqExpr Condition,
            Action Body)
        {
            While(Condition, (x, y) => Body());
        }

        /// <summary>
        /// Generate an infinite loop with the given code generator functions.
        /// </summary>
        /// <param name="Condition"></param>
        /// <param name="Body"></param>
        public void Loop(
            Action<LinqExpr, LinqExpr> Body)
        {
            string name = LoopName();
            LinqExprs.LabelTarget begin = LinqExpr.Label("while_" + name + "_begin");
            LinqExprs.LabelTarget end = LinqExpr.Label("while_" + name + "_end");

            PushScope();

            target.Add(LinqExpr.Label(begin));

            // Generate the body code.
            Body(LinqExpr.Goto(end), LinqExpr.Goto(begin));

            // Loop.
            target.Add(LinqExpr.Goto(begin));

            // Exit label.
            target.Add(LinqExpr.Label(end));

            PopScope();
        }

        public void Loop(
            Action<LinqExpr> Body)
        {
            Loop((end, y) => Body(end));
        }

        /// <summary>
        /// Generate a do-while loop with the given code generator functions.
        /// </summary>
        /// <param name="Body"></param>
        /// <param name="Condition"></param>
        public void DoWhile(
            Action<LinqExpr, LinqExpr> Body,
            LinqExpr Condition)
        {
            string name = LoopName();
            LinqExprs.LabelTarget begin = LinqExpr.Label("do_" + name + "_begin");
            LinqExprs.LabelTarget end = LinqExpr.Label("do_" + name + "_end");

            PushScope();

            // Check the condition, exit if necessary.
            target.Add(LinqExpr.Label(begin));

            // Generate the body code.
            Body(LinqExpr.Goto(end), LinqExpr.Goto(begin));

            // Loop.
            target.Add(LinqExpr.IfThen(Condition, LinqExpr.Goto(begin)));

            // Exit label.
            target.Add(LinqExpr.Label(end));

            PopScope();
        }

        // Generate a do-while loop given the condition and body expressions.
        public void DoWhile(
            Action<LinqExpr> Body,
            LinqExpr Condition)
        {
            DoWhile((end, y) => Body(end), Condition);
        }

        // Generate a do-while loop given the condition and body expressions.
        public void DoWhile(
            Action Body,
            LinqExpr Condition)
        {
            DoWhile((x, y) => Body(), Condition);
        }

        /// <summary>
        /// Add variables to the codegen scope.
        /// </summary>
        /// <param name="Vars"></param>
        public void Declared(Scope Scope, params ParamExpr[] Vars)
        {
            switch (Scope)
            {
                case LinqCodeGen.Scope.Local: locals.AddRange(Vars); live.Peek().AddRange(Vars); break;
                case LinqCodeGen.Scope.Parameter: parameters.AddRange(Vars); break;
                default: throw new InvalidOperationException("Unknown variable scope.");
            }
        }
        public void Declared(params ParamExpr[] Vars) { Declared(Scope.Local, Vars); }

        public void Declared(Scope Scope, ParamExpr Var, Expression Expr)
        {
            Declared(Scope, Var);
            this[Expr] = Var;
        }
        public void Declared(ParamExpr Var, Expression Expr) { Declared(Scope.Local, Var, Expr); }


        public ParamExpr Decl(Scope Scope, Type T, string Name)
        {
            ParamExpr p = LinqExpr.Parameter(T, Name);
            Declared(Scope, p);
            return p;
        }
        public ParamExpr Decl<T>(Scope Scope, string Name) { return Decl(Scope, typeof(T), Name); }
        public ParamExpr Decl(Type T, string Name) { return Decl(Scope.Local, T, Name); }

        public ParamExpr Decl<T>(string Name, LinqExpr Init)
        {
            ParamExpr p = Decl(Scope.Local, typeof(T), Name);
            target.Add(LinqExpr.Assign(p, Init));
            return p;
        }

        public ParamExpr Decl<T>(string Name, T Init)
        {
            ParamExpr p = Decl(Scope.Local, typeof(T), Name);
            target.Add(LinqExpr.Assign(p, LinqExpr.Constant(Init)));
            return p;
        }

        public ParamExpr ReDecl(Type T, string Name)
        {
            ParamExpr d = live.SelectMany(i => i).SingleOrDefault(i => i.Name == Name);
            if (d == null)
                d = Decl(T, Name);
            return d;
        }
        public ParamExpr ReDecl<T>(string Name) { return ReDecl(typeof(T), Name); }

        public ParamExpr ReDecl<T>(string Name, LinqExpr Init)
        {
            ParamExpr p = ReDecl(typeof(T), Name);
            target.Add(LinqExpr.Assign(p, Init));
            return p;
        }

        public ParamExpr ReDecl<T>(string Name, T Init)
        {
            ParamExpr p = ReDecl(typeof(T), Name);
            target.Add(LinqExpr.Assign(p, LinqExpr.Constant(Init)));
            return p;
        }


        public ParamExpr Decl<T>(Scope Scope) { return Decl<T>(Scope, "anon" + locals.Count.ToString()); }

        public ParamExpr Decl<T>(string Name) { return Decl<T>(Scope.Local, Name); }
        public ParamExpr Decl<T>() { return Decl<T>(Scope.Local); }

        public ParamExpr DeclParameter<T>(string Name) { return Decl<T>(Scope.Parameter, Name); }
        public ParamExpr DeclParameter<T>() { return Decl<T>(Scope.Parameter); }

        /// <summary>
        /// Decl a variable mapped to an expression.
        /// </summary>
        /// <param name="Scope"></param>
        /// <param name="Name"></param>
        /// <param name="Expr"></param>
        /// <returns></returns>
        public ParamExpr Decl(Scope Scope, string Name, Expression Expr)
        {
            ParamExpr p = LinqExpr.Parameter(typeof(double), Name);
            Declared(Scope, p);
            Add(Expr, p);
            return p;
        }
        public ParamExpr Decl(Scope Scope, Expression Expr) { return Decl(Scope, "anon" + locals.Count.ToString(), Expr); }

        public ParamExpr Decl(string Name, Expression Expr) { return Decl(Scope.Local, Name, Expr); }
        public ParamExpr Decl(Expression Expr) { return Decl(Scope.Local, Expr); }
        public ParamExpr Decl(Expression Expr, LinqExpr Init)
        {
            ParamExpr d = Decl(Expr);
            Add(LinqExpr.Assign(d, Init));
            return d;
        }
        public ParamExpr Decl(Expression Expr, Expression Init) { return Decl(Expr, Init.Compile(this)); }

        public ParamExpr DeclParameter(string Name, Expression Expr) { return Decl(Scope.Parameter, Name, Expr); }
        public ParamExpr DeclParameter(Expression Expr) { return Decl(Scope.Parameter, Expr); }

        
        private string LoopName()
        {
            if (target.Any())
                return target.Last().ToString();
            return "loop";
        }
    }
}
