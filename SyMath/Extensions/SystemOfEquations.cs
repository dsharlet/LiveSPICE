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
        /// Transform the system to row-echelon form.
        /// </summary>
        /// <param name="S"></param>
        /// <param name="x"></param>
        public void RowReduce(IEnumerable<Expression> Columns)
        {
            int r = 0;
            foreach (Expression j in Columns)
            {
                int i = SelectPivotRow(r, j);
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
        public void RowReduce() { RowReduce(unknowns); }

        /// <summary>
        /// Assuming the system is in reduced row-echelon form, back substitute the solutions.
        /// </summary>
        /// <param name="Columns"></param>
        public void BackSubstitute(IEnumerable<Expression> Columns)
        {
            int i = Math.Min(Columns.Count(), eqs.Count) - 1;
            foreach (Expression j in Columns.Reverse())
            {
                if (eqs[i][j].EqualsZero())
                    continue;

                for (int i2 = i - 1; i2 >= 0; --i2)
                {
                    if (!eqs[i2][j].EqualsZero())
                    {
                        eqs[i2] = ScaleAdd(eqs[i2], eqs[i], -eqs[i2][j] / eqs[i][j]);
                        Debug.Assert(eqs[i2][j].EqualsZero());
                    }
                }

                --i;
            }
        }
        public void BackSubstitute() { BackSubstitute(unknowns); }
        
        /// <summary>
        /// Solve a system for the variables in x.
        /// </summary>
        /// <param name="S"></param>
        /// <param name="x"></param>
        /// <returns></returns>
        public List<Arrow> Solve(IEnumerable<Expression> For)
        {
            List<Expression> x = For.ToList();

            int r = Math.Min(x.Count(), eqs.Count) - 1;
            // Solve for the variables in x.
            List<Arrow> result = new List<Arrow>();
            for (int j = x.Count - 1; j >= 0; --j)
            {
                int i = FindPivotRow(r, x[j]);
                if (i != -1)
                {
                    Expression s = eqs[i].Solve(x[j]);
                    if (!s.DependsOn(x))
                    {
                        result.Add(Arrow.New(x[j], s));
                        eqs.RemoveAt(i);
                        unknowns.Remove(x[j]);
                        x.RemoveAt(j);
                    }
                    r = i;
                }
                --r;
            }
            return result;
        }
        public List<Arrow> Solve() { return Solve(unknowns); }

        public void Evaluate(List<Arrow> x0)
        {
            eqs = eqs.Select(i => LinearCombination.New(unknowns, i.Evaluate(x0))).ToList();
        }

        private int FindPivotRow(int i, Expression j)
        {
            for (; i >= 0; --i)
                if (!eqs[i][j].EqualsZero())
                    return i;
            return -1;
        }

        private int SelectPivotRow(int i, Expression j)
        {
            int r = -1;
            Real p = 0;

            for (; i < eqs.Count; ++i)
            {
                Expression ix = eqs[i][j];
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
