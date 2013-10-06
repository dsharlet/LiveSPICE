using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SyMath;

namespace Circuit
{
    // Stores a DAE as a linear/differential A and non-linear equation f, A = f.
    class DiffAlgEq
    {
        private static int token = 0;

        public Expression A = Constant.Zero, f = Constant.Zero;

        // Token expressions representing the previous values of A, f.
        public Expression A0 = Constant.Zero;
        public Expression f0 = Constant.Zero;

        public DiffAlgEq(Equal Eq, IEnumerable<Expression> x, IEnumerable<Expression> dxdt)
        {
            Expression eq = Eq.Left - Eq.Right;

            foreach (Expression i in Add.TermsOf((Eq.Left - Eq.Right).Expand()))
            {
                if (!i.IsFunctionOf(x.Concat(dxdt)) || IsLinearFunctionOf(i, x))
                    A += i;
                else if (i.IsFunctionOf(dxdt))
                    A += i;
                else
                    f += -i;
            }

            if (!A.Equals(Constant.Zero))
                A0 = Variable.New("A0_" + Interlocked.Increment(ref token).ToString());
            if (!f.Equals(Constant.Zero))
                f0 = Variable.New("f0_" + Interlocked.Increment(ref token).ToString());
        }

        public override string ToString()
        {
            return Equal.New(A, f).ToString();
        }

        private static bool IsLinearFunctionOf(Expression f, IEnumerable<Expression> x)
        {
            foreach (Expression i in x)
            {
                // TODO: There must be a more efficient way to do this...
                Expression fi = f / i;
                if (!fi.IsFunctionOf(i))
                    return true;

                //if (Add.TermsOf(f).Count(j => Multiply.TermsOf(j).Sum(k => k.Equals(i) ? 1 : k.IsFunctionOf(i) ? 2 : 0) == 1) == 1)
                //    return true;
            }
            return false;
        }
    };
}
