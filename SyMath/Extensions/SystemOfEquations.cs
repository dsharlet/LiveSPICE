using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyMath
{
    /// <summary>
    /// Represents an MxN matrix.
    /// </summary>
    public class SystemOfEquations
    {
        protected List<Expression> unknowns;
        protected List<LinearCombination> eqs;

        public IEnumerable<LinearCombination> Equations { get { return eqs; } }
        public IEnumerable<Expression> Unknowns { get { return unknowns; } }

        private LinearCombination InTermsOf(Equal F) { return LinearCombination.New(unknowns, F.Left - F.Right); }

        public SystemOfEquations(IEnumerable<Equal> F, IEnumerable<Expression> x)
        {
            unknowns = x.ToList();
            eqs = F.Select(i => InTermsOf(i)).ToList();
        }
        public SystemOfEquations(IEnumerable<LinearCombination> F, IEnumerable<Expression> x)
        {
            unknowns = x.ToList();
            eqs = F.ToList();
        }

        public void Add(Equal F) { eqs.Add(InTermsOf(F)); }
        public void AddRange(IEnumerable<Equal> F) { eqs.AddRange(F.Select(i => InTermsOf(i))); }
        
        /// <summary>
        /// Compute the row-echelon form of the system S in terms of x. This function modifies S in place.
        /// </summary>
        /// <param name="S"></param>
        /// <param name="x"></param>
        private void RowReduce(IEnumerable<Expression> x)
        {
            int r = 0;
            foreach (Expression j in x)
            {
                int i = FindPivotRow(r, j);
                // If there is no pivot row, all of the entries in this column are already eliminated.
                if (i == -1)
                    continue;
                // If the pivot row isn't the current row, swap them.
                if (i != r)
                    Swap(i, r);

                Expression scale = -eqs[r][j];

                // Cancel the pivot variable from other rows.
                for (i = r + 1; i < eqs.Count; ++i)
                {
                    Expression fij = eqs[i][j];
                    if (!fij.EqualsZero())
                    {
                        eqs[i] = ScaleAdd(eqs[i], eqs[r], fij / scale);
                        Debug.Assert(eqs[i][j].EqualsZero());
                    }
                }

                ++r;
            }
        }
        
        /// <summary>
        /// Solve a system for the variables in x.
        /// </summary>
        /// <param name="S"></param>
        /// <param name="x"></param>
        /// <returns></returns>
        public List<Arrow> Solve(IEnumerable<Expression> x)
        {
            RowReduce(x);

            x = x.Reverse().ToArray();

            int r = x.Count() - 1;
            // Solve for the variables in x.
            List<Arrow> result = new List<Arrow>();
            foreach (Expression j in x)
            {
                LinearCombination i = eqs[r];

                // If there is no pivot in this position, find any row with a non-zero coefficient of j.
                if (!i[j].EqualsZero())
                    --r;
                else
                    i = eqs.FirstOrDefault(a => !a[j].EqualsZero());

                if (!ReferenceEquals(i, null))
                    result.Add(Arrow.New(j, i.Solve(j)));
            }
            return result;
        }
        public List<Arrow> Solve() { return Solve(unknowns.ToArray()); }
                
        private int FindPivotRow(int i, Expression x)
        {
            int r = -1;
            Real p = 0;

            for (; i < eqs.Count; ++i)
            {
                Expression ix = eqs[i][x];
                // If we don't already have a pivot row, use this non-zero pivot position row.
                if (!ix.EqualsZero() && r == -1)
                {
                    r = i;
                    p = 0.0;
                }

                // Check if this is the largest pivot position term.
                if (ix is Constant)
                {
                    Real cix = Real.Abs((Real)ix);
                    if (cix > p)
                    {
                        r = i;
                        p = cix;
                    }
                }
            }
            return r;
        }

        private LinearCombination Scale(LinearCombination L, Expression R)
        {
            return LinearCombination.New(unknowns.Append(1).Select(i => new KeyValuePair<Expression, Expression>(i, L[i] * R)));
        }

        private LinearCombination ScaleAdd(LinearCombination L, LinearCombination R, Expression s)
        {
            return LinearCombination.New(unknowns.Append(1).Select(i => new KeyValuePair<Expression, Expression>(i, L[i] + Binary.Multiply(R[i], s))));
        }

        private void Swap(int a, int b)
        {
            LinearCombination t = eqs[a];
            eqs[a] = eqs[b];
            eqs[b] = t;
        }
    }
}
