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
        
        /// <summary>
        /// Enumerates the equations in the system.
        /// </summary>
        public IEnumerable<Equal> Equations { get { return equations; } }
        /// <summary>
        /// Enumerates the unknowns in the system.
        /// </summary>
        public IEnumerable<Expression> Unknowns { get { return unknowns; } }

        /// <summary>
        /// Adds a new node equation to the system.
        /// </summary>
        /// <param name="i"></param>
        public void AddNode(Expression i) { equations.Add(Equal.New(i, Constant.Zero)); }

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
