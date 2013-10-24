using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SyMath;

namespace Circuit
{
    public class ModifiedNodalAnalysis
    {
        private HashSet<Equal> equations = new HashSet<Equal>();
        private List<Expression> unknowns = new List<Expression>();
        
        public IEnumerable<Equal> Equations { get { return equations; } }
        public IEnumerable<Expression> Unknowns { get { return unknowns; } }

        public void AddNode(Expression i) { equations.Add(Equal.New(i, Constant.Zero)); }

        public void AddEquations(IEnumerable<Equal> Eq) { foreach (Equal i in Eq) equations.Add(i); }
        public void AddEquations(params Equal[] Eq) { AddEquations(Eq.AsEnumerable()); }
        public void AddEquation(Expression a, Expression b) { equations.Add(Equal.New(a, b)); }

        public void AddUnknowns(IEnumerable<Expression> Unknowns) { unknowns.AddRange(Unknowns); }
        public void AddUnknowns(params Expression[] Unknowns) { unknowns.AddRange(Unknowns); }

        public Expression AddNewUnknown(string Name) 
        { 
            Expression x = Component.DependentVariable(Name, Component.t); 
            AddUnknowns(x);
            return x;
        }
        public Expression AddNewUnknownEqualTo(string Name, Expression Eq)
        {
            Expression x = AddNewUnknown(Name);
            AddEquation(x, Eq);
            return x;
        }
    }
}
