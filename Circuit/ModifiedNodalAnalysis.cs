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
        private List<Expression> unknowns = new List<Expression>();
        private Dictionary<Expression, Expression> nodes = new Dictionary<Expression, Expression>();

        /// <summary>
        /// Get the KCL expressions for this analysis.
        /// </summary>
        public IEnumerable<KeyValuePair<Expression, Expression>> Kcl { get { return nodes.Where(i => !ReferenceEquals(i.Value, null)); } }

        /// <summary>
        /// Enumerates the equations in the system.
        /// </summary>
        public IEnumerable<Equal> Equations { get { return equations.Concat(Kcl.Select(i => Equal.New(i.Value, Constant.Zero))); } }
        /// <summary>
        /// Enumerates the unknowns in the system.
        /// </summary>
        public IEnumerable<Expression> Unknowns { get { return unknowns; } }

        /// <summary>
        /// Add a current to the given node.
        /// </summary>
        /// <param name="Node"></param>
        /// <param name="i"></param>
        public void AddTerminal(Terminal Terminal, Expression i)
        {
            Expression v = Terminal.V;
            Expression kcl;
            if (nodes.TryGetValue(v, out kcl))
            {
                // null represents a node with undefined current.
                if (ReferenceEquals(i, null))
                    nodes[v] = null;
                else if (!ReferenceEquals(kcl, null))
                    nodes[v] = kcl + i;
            }
            else
            {
                nodes[v] = i;
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
        public void AddUnknowns(IEnumerable<Expression> Unknowns) { unknowns.AddRange(Unknowns); }
        public void AddUnknowns(params Expression[] Unknowns) { unknowns.AddRange(Unknowns); }

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
    }
}
