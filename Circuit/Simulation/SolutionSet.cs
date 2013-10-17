using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using SyMath;

namespace Circuit
{
    /// <summary>
    /// Represents a set of solutions for a system of equations.
    /// </summary>
    public abstract class SolutionSet
    {
        /// <summary>
        /// Enumerate the unknowns solved by this solution set.
        /// </summary>
        public abstract IEnumerable<Expression> Unknowns { get; }

        /// <summary>
        /// Check if any of the solutions of this system depend on x.
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public abstract bool DependsOn(Expression x);
    }

    /// <summary>
    /// A simple linear system solution set. The system directly gives solutions.
    /// </summary>
    public class LinearSolutions : SolutionSet
    {
        private List<Arrow> solutions;
        /// <summary>
        /// Enumerate the solutions to this system.
        /// </summary>
        public IEnumerable<Arrow> Solutions { get { return solutions; } }

        public override IEnumerable<Expression> Unknowns { get { return solutions.Select(i => i.Left); } }

        public LinearSolutions(IEnumerable<Arrow> Solutions) { solutions = Solutions.ToList(); }

        public override bool DependsOn(Expression x) { return solutions.Any(i => i.Right.DependsOn(x)); }
    }

    /// <summary>
    /// A solution set described by an iteration of newton's method, partially solved.
    /// </summary>
    public class NewtonRhapsonIteration : SolutionSet
    {
        private List<Arrow> solved;
        /// <summary>
        /// Enumerate the solved solutions.
        /// </summary>
        public IEnumerable<Arrow> Solved { get { return solved; } }

        private List<LinearCombination> equations;
        /// <summary>
        /// Enumerate the equations describing the unsolved part of this system.
        /// </summary>
        public IEnumerable<LinearCombination> Equations { get { return equations; } }

        private List<Expression> updates;
        /// <summary>
        /// Enumerate the variables that have an update delta described by the Equations member.
        /// </summary>
        public IEnumerable<Expression> Updates { get { return updates; } }

        public override IEnumerable<Expression> Unknowns { get { return solved != null ? solved.Select(i => i.Left).Concat(updates) : updates; } }

        public NewtonRhapsonIteration(List<Arrow> Solved, List<LinearCombination> Equations, List<Expression> Updates)
        {
            solved = Solved;
            equations = Equations;
            updates = Updates;
        }

        public override bool DependsOn(Expression x) 
        { 
            return solved != null ? solved.Any(i => i.Right.DependsOn(x)) : false ||
                equations.Any(i => (i.Basis.Contains(x) && !i[x].IsZero()) || i[Constant.One].DependsOn(x)); 
        }
    }
}
