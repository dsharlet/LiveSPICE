using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ComputerAlgebra
{
    /// <summary>
    /// Enumerates a set of equivalent algebraic pattern transformations via inverses of basic algebraic operations. 
    /// TODO: Adding to target is a workaround for bug https://connect.microsoft.com/VisualStudio/feedback/details/677532/an-attempt-was-made-to-load-a-program-with-an-incorrect-format-exception-from-hresult-0x8007000b#details
    /// </summary>
    class AlgebraTransformGenerator : ExpressionVisitor<object>
    {
        protected Stack<Expression> equal = new Stack<Expression>();
        protected Stack<Expression> conditions;

        protected TransformSet target;
        
        public AlgebraTransformGenerator(Expression EqualTo, IEnumerable<Expression> Conditions, TransformSet Target)
        {
            equal.Push(EqualTo);
            conditions = new Stack<Expression>(Conditions);
            target = Target;
        }

        public override object Visit(Expression E)
        {
            target.AddRange(new SubstituteTransform(E, equal.Peek(), conditions));
            return base.Visit(E);
        }

        protected override object VisitUnknown(Expression E)
        {
            return null;
        }

        protected override object VisitSum(Sum A)
        {
            foreach (Expression i in A.Terms)
            {
                equal.Push(equal.Peek() - i);
                Visit(Sum.New(A.Terms.ExceptUnique(i)));
                equal.Pop();
            }
            return null;
        }

        protected override object VisitProduct(Product M)
        {
            foreach (Expression i in M.Terms)
            {
                conditions.Push(Binary.NotEqual(i, 0));
                equal.Push(equal.Peek() / i);
                Visit(Product.New(M.Terms.ExceptUnique(i)));
                equal.Pop();
                conditions.Pop();
            }
            return null;
        }

        //protected override object VisitPower(Power P)
        //{
        //    equal.Push(Power.New(equal.Peek(), 1 / P.Right));
        //    Visit(P.Left);
        //    equal.Pop();
        //    return null;
        //}
    }

    /// <summary>
    /// Transform that 
    /// </summary>
    public class AlgebraTransform : TransformSet
    {
        protected void Init(Expression x, Expression y, IEnumerable<Expression> PreConditions)
        {
            new AlgebraTransformGenerator(x, PreConditions, this).Visit(y);
            new AlgebraTransformGenerator(y, PreConditions, this).Visit(x);
        }

        /// <summary>
        /// Generate a set of transforms for the relationship x = y via basic algebraic inverses.
        /// For example, given transform Sin[x]/Cos[x] = Tan[x], also generates a transform Sin[x] = Tan[x]*Cos[x].
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="PreConditions"></param>
        public AlgebraTransform(Expression x, Expression y, IEnumerable<Expression> PreConditions)
        {
            Init(x, y, PreConditions);
        }
        public AlgebraTransform(Expression x, Expression y, params Expression[] PreConditions)
        {
            Init(x, y, PreConditions);
        }
    }
}
