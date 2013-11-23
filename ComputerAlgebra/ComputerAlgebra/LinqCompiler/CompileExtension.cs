using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace ComputerAlgebra.LinqCompiler
{
    public static class CompileExtension
    {
        /// <summary>
        /// Compile function to a lambda.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static Delegate Compile(this Function This, LinqCompiler.Module Module)
        {
            // Compile the function.
            CodeGen code = new LinqCompiler.CodeGen(Module);
            foreach (Variable i in This.Parameters)
                code.Decl(Scope.Parameter, i);
            code.Return<double>(code.Compile(This.Call(This.Parameters)));
            return code.Build().Compile();
        }

        public static Delegate Compile(this Function This) { return Compile(This, null); }

        /// <summary>
        /// Compile function to a lambda.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T Compile<T>(this Function This, LinqCompiler.Module Module)
        {
            // Validate the function types match.
            MethodInfo invoke = typeof(T).GetMethod("Invoke");
            if (invoke.GetParameters().Count() != This.Parameters.Count())
                throw new InvalidOperationException("Different parameters for Function '" + This.Name + "' and '" + typeof(T).ToString() + "'");

            // Compile the function.
            CodeGen code = new LinqCompiler.CodeGen(Module);
            foreach (Variable i in This.Parameters)
                code.Decl(Scope.Parameter, i);
            code.Return<double>(code.Compile(This.Call(This.Parameters)));
            return code.Build<T>().Compile();
        }

        public static T Compile<T>(this Function This) { return Compile<T>(This, null); }
    }
}
