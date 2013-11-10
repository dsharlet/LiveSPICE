using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SyMath;

namespace Circuit
{
    /// <summary>
    /// Helper class for building a system of MNA equations and unknowns.
    /// </summary>
    public class ModifiedNodalAnalysis
    {
        private HashSet<Equal> equations = new HashSet<Equal>();
        private HashSet<Expression> unknowns = new HashSet<Expression>();
        private Dictionary<Expression, Expression> kcl = new Dictionary<Expression, Expression>();
        private List<Arrow> initialConditions = new List<Arrow>();

        private NodeCollection nodes = new NodeCollection();
        public void BeginAnalysis(Circuit C) { nodes.AddRange(C.Nodes); }

        /// <summary>
        /// Get the KCL expressions for this analysis.
        /// </summary>
        public IEnumerable<KeyValuePair<Expression, Expression>> Kcl { get { return kcl.Where(i => !ReferenceEquals(i.Value, null)); } }

        /// <summary>
        /// Enumerates the equations in the system.
        /// </summary>
        public IEnumerable<Equal> Equations { get { return equations.Concat(Kcl.Select(i => Equal.New(i.Value, Constant.Zero))); } }
        /// <summary>
        /// Enumerates the unknowns in the system.
        /// </summary>
        public IEnumerable<Expression> Unknowns { get { return unknowns; } }

        /// <summary>
        /// Enumerates the inputs 
        /// </summary>
        public IEnumerable<Arrow> InitialConditions { get { return initialConditions; } }

        /// <summary>
        /// Add a current to the given node.
        /// </summary>
        /// <param name="Node"></param>
        /// <param name="i"></param>
        public void AddTerminal(Terminal Terminal, Expression i)
        {
            Expression v = Terminal.V;
            Expression sumi;
            if (kcl.TryGetValue(v, out sumi))
            {
                // preserve null (arbitrary current).
                if (ReferenceEquals(i, null))
                    kcl[v] = null;
                else if (!ReferenceEquals(sumi, null))
                    kcl[v] = sumi + i;
            }
            else
            {
                kcl[v] = i;
                AddUnknowns(v);
            }
        }

        /// <summary>
        /// Add the current for a passive component with the given terminals.
        /// </summary>
        /// <param name="Anode"></param>
        /// <param name="Cathode"></param>
        /// <param name="i"></param>
        public void AddPassiveComponent(Terminal Anode, Terminal Cathode, Expression i)
        {
            AddTerminal(Anode, i);
            AddTerminal(Cathode, -i);
        }
        
        /// <summary>
        /// Add equations to the system.
        /// </summary>
        /// <param name="Eq"></param>
        public void AddEquations(IEnumerable<Equal> Eq) { foreach (Equal i in Eq) equations.Add(i); }
        public void AddEquations(params Equal[] Eq) { AddEquations(Eq.AsEnumerable()); }
        public void AddEquation(Expression a, Expression b) { equations.Add(Equal.New(a, b)); }
        
        /// <summary>
        /// Add Unknowns to the system.
        /// </summary>
        /// <param name="Unknowns"></param>
        public void AddUnknowns(IEnumerable<Expression> Unknowns) { foreach (Expression i in Unknowns) unknowns.Add(i); }
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
        public Expression AddNewUnknown() { return AddNewUnknown("_x" + (++anon).ToString()); }
        /// <summary>
        /// Add an anonymous unknown to the system with a known equation.
        /// </summary>
        /// <param name="Eq"></param>
        /// <returns></returns>
        public Expression AddNewUnknownEqualTo(Expression Eq) { return AddNewUnknownEqualTo("_x" + (++anon).ToString(), Eq); }

        /// <summary>
        /// Add initial conditions to the system.
        /// </summary>
        /// <param name="InitialCondition"></param>
        public void AddInitialConditions(IEnumerable<Arrow> InitialConditions) { initialConditions.AddRange(InitialConditions); }
        public void AddInitialConditions(params Arrow[] InitialConditions) { initialConditions.AddRange(InitialConditions); }
    }
}
