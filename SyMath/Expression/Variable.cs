using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SyMath
{
    /// <summary>
    /// Variable expression.
    /// </summary>
    public class Variable : NamedAtom
    {
        protected Variable(string Name) : base(Name) { }

        public static Variable New(string Name) { return new Variable(Name); }

        public override bool Matches(Expression E, MatchContext Matched)
        {
            return Matched.Matches(this, E);
        }

        // object interface.
        public override bool Equals(Expression E)
        {
            if (ReferenceEquals(this, E))
                return true;

            Variable VE = E as Variable;
            if (!ReferenceEquals(VE, null))
                return Name.Equals(VE.Name);

            return false;
        }
        public override int GetHashCode() { return Name.GetHashCode(); }
    }

    /// <summary>
    /// Variable the checks a predicate before matching an expression.
    /// </summary>
    class PatternVariable : Variable
    {
        protected Func<Expression, bool> condition;

        private PatternVariable(string Name, Func<Expression, bool> Condition) : base(Name) { condition = Condition; }

        /// <summary>
        /// Create a new variable with a condition callback for matching.
        /// </summary>
        /// <param name="Name"></param>
        /// <param name="Condition">A function that should return true if the variable is allowed to match the given Expression.</param>
        /// <returns></returns>
        public static PatternVariable New(string Name, Func<Expression, bool> Condition) { return new PatternVariable(Name, Condition); }

        public override bool Matches(Expression E, MatchContext Matched)
        {
            if (condition(E))
                return base.Matches(E, Matched);
            else
                return false;
        }
    }
}
