using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ComputerAlgebra;

namespace Circuit
{
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
        protected class Subcircuit
        {
            private Subcircuit parent = null;
            public Subcircuit Parent { get { return parent; } }

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
            public string AnonymousName() { return "_x" + (++anon).ToString(); }

            public Dictionary<Expression, Expression> Definitions = new Dictionary<Expression, Expression>();
            public List<Equal> Equations = new List<Equal>();
            public Dictionary<Expression, Expression> Kcl = new Dictionary<Expression, Expression>();
            public NodeCollection Nodes = new NodeCollection();

            public Subcircuit() { }
            public Subcircuit(Subcircuit Parent) { parent = Parent; }
            public Subcircuit(Subcircuit Parent, string Name) : this(Parent) { name = Name; }
        }

        private Subcircuit context = new Subcircuit();

        public void PushContext(string Name, IEnumerable<Node> Nodes)
        {
            PushContext(Name);
            DeclNodes(Nodes);
        }
        public void PushContext(string Name, params Node[] Nodes) { PushContext(Name, Nodes.AsEnumerable()); }

        public void PushContext(string Name) { context = new Subcircuit(context, Name); }
        public void PopContext()
        {
            // Evaluate the definitions from the context for the equations and add the results to the analysis.
            foreach (Equal i in context.Equations)
            {
                Equal ei = (Equal)Evaluate(i, context.Definitions);
                if (!equations.Contains(ei))
                    equations.Add(ei);
            }
            foreach (KeyValuePair<Expression, Expression> i in context.Kcl)
                AddKcl(kcl, i.Key.Evaluate(context.Definitions), Evaluate(i.Value, context.Definitions));

            foreach (Node i in context.Nodes)
                i.EndAnalysis();

            context = context.Parent;
        }

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
        public IEnumerable<KeyValuePair<Expression, Expression>> Kcl { get { return kcl.Where(i => !ReferenceEquals(i.Value, null)); } }

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
        public void AddDefinition(Expression Key, Expression Value) { context.Definitions.Add(Key, Value); }

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
        public Expression AddNewUnknown(string Name) 
        {
            Expression x = Component.DependentVariable(context.Prefix + Name, Component.t); 
            AddUnknowns(x);
            return x;
        }

        /// <summary>
        /// Add a new named unknown to the system with a known equation.
        /// </summary>
        /// <param name="Name"></param>
        /// <param name="Eq"></param>
        /// <returns></returns>
        public Expression AddNewUnknownEqualTo(string Name, Expression Eq)
        {
            IEnumerable<Equal> eqs = equations.Concat(context.Equations);
            Equal eq = eqs.FirstOrDefault(i => Component.IsDependentVariable(i.Left, Component.t) && i.Right.Equals(Eq));
            if (ReferenceEquals(eq, null))
            {
                Expression x = AddNewUnknown(Name);
                AddEquation(x, Eq);
                return x;
            }
            else
            {
                return eq.Left;
            }
        }

        /// <summary>
        /// Add an anonymous unknown to the system.
        /// </summary>
        /// <returns></returns>
        public Expression AddNewUnknown() { return AddNewUnknown(AnonymousName()); }
        /// <summary>
        /// Add an anonymous unknown to the system with a known equation.
        /// </summary>
        /// <param name="Eq"></param>
        /// <returns></returns>
        public Expression AddNewUnknownEqualTo(Expression Eq) { return AddNewUnknownEqualTo(AnonymousName(), Eq); }

        /// <summary>
        /// Add initial conditions to the system.
        /// </summary>
        /// <param name="InitialCondition"></param>
        public void AddInitialConditions(IEnumerable<Arrow> InitialConditions) { initialConditions.AddRange(InitialConditions); }
        public void AddInitialConditions(params Arrow[] InitialConditions) { initialConditions.AddRange(InitialConditions); }

        /// <summary>
        /// Get an anonymous variable name. It will be uniqued later.
        /// </summary>
        /// <returns></returns>
        public string AnonymousName() { return context.AnonymousName(); }
        
        private void AddKcl(Dictionary<Expression, Expression> Kcl, Expression V, Expression i)
        {
            Expression sumi;
            if (Kcl.TryGetValue(V, out sumi))
            {
                // preserve null (arbitrary current).
                if (ReferenceEquals(i, null))
                    Kcl[V] = null;
                else if (!ReferenceEquals(sumi, null))
                    Kcl[V] = sumi + i;
            }
            else
            {
                Kcl[V] = i;
            }

        }

        // Helper for evaluating typical analysis expressions.
        private static Expression Evaluate(Expression x, IDictionary<Expression, Expression> At)
        {
            if (ReferenceEquals(x, null))
                return null;
            else if (At.Any())
                return x.Evaluate(At);
            else
                return x;
        }
    }
}
