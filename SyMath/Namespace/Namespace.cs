using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Numerics;

namespace SyMath
{
    /// <summary>
    /// Contains functions and values.
    /// </summary>
    public class Namespace
    {
        private Dictionary<string, List<Expression>> members = new Dictionary<string, List<Expression>>();

        public IEnumerable<Expression> LookupName(string Name)
        {
            List<Expression> lookup;
            if (members.TryGetValue(Name, out lookup))
                return lookup;
            return new Expression[] { };
        }

        /// <summary>
        /// Resolve a name to an expression.
        /// </summary>
        /// <param name="Name"></param>
        /// <returns></returns>
        public Expression Resolve(string Name) 
        {
            return LookupName(Name).SingleOrDefault(); 
        }
        /// <summary>
        /// Resolve a name with arguments to a function.
        /// </summary>
        /// <param name="Name"></param>
        /// <param name="Params"></param>
        /// <returns></returns>
        public Function Resolve(string Name, IEnumerable<Expression> Params)
        {
            IEnumerable<Function> candidates = LookupName(Name).OfType<Function>();
            return candidates.SingleOrDefault(i => i.CanCall(Params));
        }

        /// <summary>
        /// Add a member to the namespace.
        /// </summary>
        /// <param name="f"></param>
        public void Add(string Name, Expression x) 
        {
            List<Expression> values;
            if (!members.TryGetValue(Name, out values))
            {
                values = new List<Expression>();
                members[Name] = values;
            }
            values.Add(x); 
        }

        public Namespace() { }

        protected Namespace(Type T)
        {
            foreach (MethodInfo i in T.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance))
                Add(i.Name, NativeFunction.New(i));
            foreach (FieldInfo i in T.GetFields(BindingFlags.Public | BindingFlags.Static))
                Add(i.Name, (Expression)i.GetValue(null));
        }

        private static Namespace global = new Namespace(typeof(GlobalNamespace));
        /// <summary>
        /// Get the global namespace.
        /// </summary>
        public static Namespace Global { get { return global; } }
    }
}
