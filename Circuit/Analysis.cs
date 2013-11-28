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
    public class Analysis
    {
        private List<Equal> equations = new List<Equal>();
        private List<Expression> unknowns = new List<Expression>();
        private Dictionary<Expression, Expression> kcl = new Dictionary<Expression, Expression>();
        private List<Arrow> initialConditions = new List<Arrow>();

        // Ensure unique unknowns among subcircuits.
        private NodeCollection nodes = new NodeCollection();
        private ComponentCollection components = new ComponentCollection();
        
        protected class Context
        {
            public List<Equal> Equations = new List<Equal>();
            public Dictionary<Expression, Expression> Controllers = new Dictionary<Expression, Expression>();
            public Dictionary<Expression, Expression> Kcl = new Dictionary<Expression, Expression>();
        }
        private Stack<Context> contexts = new Stack<Context>();

        public void PushContext(IEnumerable<Node> Nodes, IEnumerable<Component> Components)
        {
            DeclNodes(Nodes);
            DeclComponents(Components);
            PushContext();
        }
        public void PushContext() { contexts.Push(new Context()); }
        public void PopContext()
        {
            Context context = contexts.Pop();
            // Evaluate the controllers from the context for the equations and add the results to the analysis.
            foreach (Equal i in context.Equations)
            {
                Equal ei = (Equal)i.Evaluate(context.Controllers);
                if (!equations.Contains(ei))
                    equations.Add(ei);
            }
            foreach (KeyValuePair<Expression, Expression> i in context.Kcl)
                AddKcl(kcl, i.Key, i.Value != null ? i.Value.Evaluate(context.Controllers) : null);
        }
        public void DeclNodes(IEnumerable<Node> Nodes) { nodes.AddRange(Nodes); }
        public void DeclNodes(params Node[] Nodes) { nodes.AddRange(Nodes); }
        public void DeclComponents(IEnumerable<Component> Components) { components.AddRange(Components); }
        public void DeclComponents(params Component[] Components) { components.AddRange(Components); }
        
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
        public void AddTerminal(Node Terminal, Expression i) { AddKcl(contexts.Peek().Kcl, Terminal.V, i); }

        /// <summary>
        /// Add the current for a passive component with the given terminals.
        /// </summary>
        /// <param name="Anode"></param>
        /// <param name="Cathode"></param>
        /// <param name="i"></param>
        public void AddPassiveComponent(string Name, Node Anode, Node Cathode, Expression i)
        {
            if (Name != "")
            {
                contexts.Peek().Controllers.Add("V[" + Name + "]", Anode.V - Cathode.V);
                contexts.Peek().Controllers.Add("i[" + Name + "]", i);
            }
            AddTerminal(Anode, i);
            AddTerminal(Cathode, -i);
        }

        /// <summary>
        /// Add the current for a passive component with the given terminals.
        /// </summary>
        /// <param name="Anode"></param>
        /// <param name="Cathode"></param>
        /// <param name="i"></param>
        public void AddPassiveComponent(Node Anode, Node Cathode, Expression i) { AddPassiveComponent("", Anode, Cathode, i); }
        
        
        /// <summary>
        /// Add equations to the system.
        /// </summary>
        /// <param name="Eq"></param>
        public void AddEquations(IEnumerable<Equal> Eq) 
        { 
            foreach (Equal i in Eq) 
                if (!equations.Contains(i))
                    contexts.Peek().Equations.Add(i); 
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
            Expression x = Component.DependentVariable(Name, Component.t); 
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
            Expression x = AddNewUnknown(Name);
            AddEquation(x, Eq);
            return x;
        }

        private int anon = 0;
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

        public string AnonymousName() { return "_x" + (++anon).ToString(); }
    }
}
