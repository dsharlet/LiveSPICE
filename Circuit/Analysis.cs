using ComputerAlgebra;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Circuit
{
    /// <summary>
    /// Exception for problems analyzing a component.
    /// </summary>
    public class AnalysisException : Exception
    {
        public AnalysisException(string Message) : base(Message) { }
    }

    /// <summary>
    /// Helper class for building a system of MNA equations and unknowns.
    /// </summary>
    public class Analysis : DynamicNamespace
    {
        private List<Equal> equations = new List<Equal>();
        private List<Expression> unknowns = new List<Expression>();
        private Dictionary<Expression, Expression> kcl = new Dictionary<Expression, Expression>();
        private List<Arrow> initialConditions = new List<Arrow>();

        // Describes the analysis of a subcircuit.
        protected class Circuit
        {
            private Circuit parent = null;
            public Circuit Parent { get { return parent; } }

            private string name = null;
            public string Name { get { return name; } }

            public string Prefix
            {
                get
                {
                    string prefix = "";
                    if (parent != null)
                        prefix = parent.Prefix;
                    if (name != null)
                        prefix = prefix + name + ".";
                    return prefix;
                }
            }

            private int anon = 0;
            public string AnonymousName() { return "_" + (++anon).ToString(); }

            public Dictionary<Expression, Expression> Definitions = new Dictionary<Expression, Expression>();
            public List<Equal> Equations = new List<Equal>();
            public Dictionary<Expression, Expression> Kcl = new Dictionary<Expression, Expression>();
            public NodeCollection Nodes = new NodeCollection();
            public List<Arrow> InitialConditions = new List<Arrow>();

            public Circuit() { }
            public Circuit(Circuit Parent, string Name) { parent = Parent; name = Name; }
        }

        private Circuit context = new Circuit();

        /// <summary>
        /// Begin analysis of a new context with the given nodes.
        /// </summary>
        /// <param name="Name"></param>
        /// <param name="Nodes"></param>
        public void PushContext(string Name, IEnumerable<Node> Nodes)
        {
            PushContext(Name);
            DeclNodes(Nodes);
        }
        public void PushContext(string Name, params Node[] Nodes) { PushContext(Name, Nodes.AsEnumerable()); }

        /// <summary>
        /// Begin analysis of a new context.
        /// </summary>
        /// <param name="Name"></param>
        public void PushContext(string Name) { context = new Circuit(context, Name); }
        /// <summary>
        /// End analysis of the current context.
        /// </summary>
        public void PopContext()
        {
            // Evaluate the definitions from the context for the equations and add the results to the analysis.
            foreach (Equal i in context.Equations)
            {
                Equal ei = (Equal)Evaluate(i, context.Definitions);
                if (!equations.Contains(ei))
                    equations.Add(ei);
            }
            // And the KCL equations.
            foreach (KeyValuePair<Expression, Expression> i in context.Kcl)
                AddKcl(kcl, i.Key, Evaluate(i.Value, context.Definitions));
            // And the initial conditions.
            initialConditions.AddRange(context.InitialConditions.Evaluate(context.Definitions).OfType<Arrow>());

            foreach (Node i in context.Nodes)
                i.EndAnalysis();

            context = context.Parent;
        }

        /// <summary>
        /// Add Nodes to the current context.
        /// </summary>
        /// <param name="Nodes"></param>
        public void DeclNodes(IEnumerable<Node> Nodes)
        {
            context.Nodes.AddRange(Nodes);

            string prefix = context.Prefix;
            foreach (Node i in Nodes)
                i.BeginAnalysis(prefix);
        }
        public void DeclNodes(params Node[] Nodes) { DeclNodes(Nodes.AsEnumerable()); }

        /// <summary>
        /// Get the KCL expressions for this analysis.
        /// </summary>
        public IEnumerable<KeyValuePair<Expression, Expression>> Kcl { get { return kcl.Where(i => i.Value is object); } }

        /// <summary>
        /// Enumerates the equations in the system.
        /// </summary>
        public IEnumerable<Equal> Equations { get { return equations.Concat(Kcl.Select(i => Equal.New(i.Value, 0))); } }
        /// <summary>
        /// Enumerates the unknowns in the system.
        /// </summary>
        public IEnumerable<Expression> Unknowns { get { return kcl.Keys.Concat(unknowns); } }

        /// <summary>
        /// Enumerates the inputs 
        /// </summary>
        public IEnumerable<Arrow> InitialConditions { get { return initialConditions; } }

        /// <summary>
        /// Add a current to the given node.
        /// </summary>
        /// <param name="Node"></param>
        /// <param name="i"></param>
        public void AddTerminal(Node Terminal, Expression i) { AddKcl(context.Kcl, Terminal.V, i); }

        /// <summary>
        /// Add the current for a passive component with the given terminals.
        /// </summary>
        /// <param name="Anode"></param>
        /// <param name="Cathode"></param>
        /// <param name="i"></param>
        public void AddPassiveComponent(Node Anode, Node Cathode, Expression i)
        {
            AddTerminal(Anode, i);
            AddTerminal(Cathode, -i);
        }

        /// <summary>
        /// Define the value of an expression in the current context. 
        /// </summary>
        /// <param name="Key"></param>
        /// <param name="Value"></param>
        public void Define(Expression Key, Expression Value)
        {
            Expression value;
            if (!context.Definitions.TryGetValue(Key, out value))
                context.Definitions.Add(Key, Value);
            else if (!value.Equals(Value))
                throw new ArgumentException("Redefinition of '" + Key.ToString() + "'.");
        }
        public void Define(Arrow x) { Define(x.Left, x.Right); }

        /// <summary>
        /// Add equations to the system.
        /// </summary>
        /// <param name="Eq"></param>
        public void AddEquations(IEnumerable<Equal> Eq)
        {
            foreach (Equal i in Eq)
                if (!equations.Contains(i))
                    context.Equations.Add(i);
        }
        public void AddEquations(params Equal[] Eq) { AddEquations(Eq.AsEnumerable()); }
        public void AddEquation(Expression a, Expression b) { AddEquations(Equal.New(a, b)); }

        /// <summary>
        /// Add Unknowns to the system.
        /// </summary>
        /// <param name="Unknowns"></param>
        public void AddUnknowns(IEnumerable<Expression> Unknowns)
        {
            foreach (Expression i in Unknowns)
                if (!unknowns.Contains(i))
                    unknowns.Add(i);
        }
        public void AddUnknowns(params Expression[] Unknowns) { AddUnknowns(Unknowns.AsEnumerable()); }

        /// <summary>
        /// Add a new named unknown to the system.
        /// </summary>
        /// <param name="Name"></param>
        /// <returns></returns>
        public Expression AddUnknown(string Name)
        {
            Expression x = Component.DependentVariable(context.Prefix + Name, Component.t);
            AddUnknowns(x);
            return x;
        }
        /// <summary>
        /// Add an anonymous unknown to the system.
        /// </summary>
        /// <returns></returns>
        public Expression AddUnknown() { return AddUnknown(AnonymousName()); }

        /// <summary>
        /// Add a new named unknown to the system with a known equation.
        /// </summary>
        /// <param name="Name"></param>
        /// <param name="Eq"></param>
        /// <returns></returns>
        public Expression AddUnknownEqualTo(string Name, Expression Eq)
        {
            // Find an existing unknown that may just be a constant factor of this one.
            IEnumerable<Equal> eqs = equations.Concat(context.Equations);
            foreach (Equal i in eqs.Where(j => !j.Right.EqualsZero() && Component.IsDependentVariable(j.Left, Component.t)))
            {
                Expression factor = Eq / i.Right;
                if (factor is Constant)
                {
                    // Existing unknown is a constant factor of this new unknown.
                    return i.Left * factor;
                }
            }
            Expression x = AddUnknown(Name);
            AddEquation(x, Eq);
            return x;
        }
        /// <summary>
        /// Add an anonymous unknown to the system with a known equation.
        /// </summary>
        /// <param name="Eq"></param>
        /// <returns></returns>
        public Expression AddUnknownEqualTo(Expression Eq) { return AddUnknownEqualTo(AnonymousName(), Eq); }

        /// <summary>
        /// Add initial conditions to the system.
        /// </summary>
        /// <param name="InitialCondition"></param>
        public void AddInitialConditions(IEnumerable<Arrow> InitialConditions) { context.InitialConditions.AddRange(InitialConditions); }
        public void AddInitialConditions(params Arrow[] InitialConditions) { context.InitialConditions.AddRange(InitialConditions); }

        /// <summary>
        /// Get an anonymous variable name. It will be uniqued later.
        /// </summary>
        /// <returns></returns>
        public string AnonymousName() { return context.AnonymousName(); }

        private void AddKcl(Dictionary<Expression, Expression> kcl, Expression V, Expression i)
        {
            if (kcl.TryGetValue(V, out var sumi))
            {
                // preserve null (arbitrary current).
                if (i is null)
                    kcl[V] = null;
                else if (sumi != null)
                    kcl[V] = sumi + i;
            }
            else
            {
                kcl[V] = i;
            }
        }

        // Helper for evaluating typical analysis expressions.
        private static Expression Evaluate(Expression x, IDictionary<Expression, Expression> At)
        {
            if (x is null)
                return null;
            else if (At.Any())
                return x.Evaluate(At);
            else
                return x;
        }
    }
}
