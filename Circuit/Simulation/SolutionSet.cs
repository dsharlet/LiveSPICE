using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using ComputerAlgebra;

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
    public class NewtonIteration : SolutionSet
    {
        private List<Arrow> solved;
        /// <summary>
        /// Enumerate the solved Newton deltas.
        /// </summary>
        public IEnumerable<Arrow> Solved { get { return solved; } }

        private List<LinearCombination> equations;
        /// <summary>
        /// Enumerate the equations describing the unsolved part of this system.
        /// </summary>
        public IEnumerable<LinearCombination> Equations { get { return equations; } }

        private List<Expression> updates;
        /// <summary>
        /// Enumerate the Newton deltas of the Equations system.
        /// </summary>
        public IEnumerable<Expression> Updates { get { return updates; } }

        /// <summary>
        /// Enumerate the Newton update deltas in this solution set.
        /// </summary>
        public IEnumerable<Expression> Deltas { get { return solved != null ? solved.Select(i => i.Left).Concat(updates) : updates; } }

        private List<Arrow> guesses;
        /// <summary>
        /// Initial guesses for the first iteration.
        /// </summary>
        public IEnumerable<Arrow> Guesses { get { return guesses; } }

        public override IEnumerable<Expression> Unknowns { get { return Deltas.Select(i => DeltaOf(i)); } }

        public NewtonIteration(IEnumerable<Arrow> Solved, IEnumerable<LinearCombination> Equations, IEnumerable<Expression> Updates, IEnumerable<Arrow> Guesses)
        {
            solved = Solved.ToList();
            equations = Equations.ToList();
            updates = Updates.ToList();
            guesses = Guesses.ToList();
        }

        public override bool DependsOn(Expression x) 
        {
            if (solved != null && solved.Any(i => i.Right.DependsOn(x)))
                return true;
            if (guesses != null && guesses.Any(i => i.Right.DependsOn(x)))
                return true;
            return equations.Any(i => i.DependsOn(x)); 
        }

        private static Function d = ExprFunction.New("d", Variable.New("x"));
        public static Expression Delta(Expression x) { return Call.New(d, x); }
        public static Expression DeltaOf(Expression x)
        {
            Call c = (Call)x;
            if (c.Target == d)
                return c.Arguments.First();
            throw new InvalidCastException("Expression is not a Newton Delta");
        }
    }
}
