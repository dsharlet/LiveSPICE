using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace ComputerAlgebra
{
    /// <summary>
    /// Transform containing a set of pattern transforms.
    /// </summary>
    public class TransformSet : ITransform, IEnumerable<PatternTransform>
    {
        private static Expression ChildPattern(ref int unique)
        {
            unique++;
            return Variable.New("_" + unique.ToString());
        }

        private static Expression Split(Expression P)
        {
            int child = 0;

            if (P is Sum)
                return ComputerAlgebra.Sum.New(((ComputerAlgebra.Sum)P).Terms.Select(i => ChildPattern(ref child)));
            if (P is Product)
                return Product.New(((Product)P).Terms.Select(i => ChildPattern(ref child)));
            if (P is Binary)
                return Binary.New(((Binary)P).Operator, ChildPattern(ref child), ChildPattern(ref child));
            if (P is Unary)
                return Unary.New(((Unary)P).Operator, ChildPattern(ref child));
            if (P is Call)
                return Call.New(((Call)P).Target, ((Call)P).Arguments.Select(i => ChildPattern(ref child)));
            return null;
        }

        private Expression pattern;
        private List<TransformSet> children = new List<TransformSet>();
        private List<PatternTransform> transforms = new List<PatternTransform>();

        private bool IsChild(Expression P)
        {
            return pattern == null || pattern.Matches(P) != null;
        }

        private TransformSet(Expression Pattern, PatternTransform Transform) { pattern = Pattern; Add(Transform); }

        public TransformSet(IEnumerable<PatternTransform> Transforms) { AddRange(Transforms); }
        public TransformSet(params PatternTransform[] Transforms) { AddRange(Transforms.AsEnumerable()); }

        /// <summary>
        /// Add a transform to the set.
        /// </summary>
        /// <param name="T"></param>
        public void Add(PatternTransform T)
        {
            Debug.Assert(IsChild(T.Pattern));

            // Try to add this transform to a child node.
            foreach (TransformSet i in children)
            {
                if (i.IsChild(T.Pattern))
                {
                    i.Add(T);
                    return;
                }
            }

            // If the pattern can be split, create a new child.
            Expression parent = Split(T.Pattern);
            if (parent != null && !parent.Equals(pattern))
            {
                children.Add(new TransformSet(parent, T));
                return;
            }

            // Can't add or create a child. Add the transform to this node.
            transforms.Add(T);
        }

        /// <summary>
        /// Add transforms to the set.
        /// </summary>
        /// <param name="T"></param>
        public void AddRange(IEnumerable<PatternTransform> T) 
        {
            foreach (PatternTransform i in T)
                Add(i);
        }
        public void AddRange(params PatternTransform[] T) { AddRange(T.AsEnumerable()); }

        public static int TransformCalls = 0;

        /// <summary>
        /// Transform expression with the first successful transform in the set.
        /// </summary>
        /// <param name="E"></param>
        /// <returns></returns>
        public Expression Transform(Expression x, Func<Expression, bool> Validate)
        {
            // If the expression doesn't match the base pattern, it won't match any of the transforms here.
            if (pattern != null && pattern.Matches(x) == null)
                return x;
            
            // Try child nodes.
            foreach (TransformSet i in children)
            {
                Expression xi = i.Transform(x, Validate);
                if (!ReferenceEquals(xi, x))
                    return xi;
            }

            // Try transforms at this node.
            foreach (PatternTransform i in transforms)
            {
                ++TransformCalls;
                Expression xi = i.Transform(x);
                if (!ReferenceEquals(xi, x) && Validate(xi))
                    return xi;
            }

            return x;
        }

        /// <summary>
        /// Transform expression with the first successful transform in the set.
        /// </summary>
        /// <param name="E"></param>
        /// <returns></returns>
        public Expression Transform(Expression x)
        {
            return Transform(x, y => true);
        }

        public IEnumerator<PatternTransform> GetEnumerator() { return transforms.GetEnumerator(); }
        IEnumerator IEnumerable.GetEnumerator() { return this.GetEnumerator(); }
    }
}
