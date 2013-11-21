using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace SyMath
{
    /// <summary>
    /// Extensions for solving equations.
    /// </summary>
    public static class SolveExtension
    {
        /// <summary>
        /// Compute the linear combinations of x that describe f.
        /// </summary>
        /// <param name="f"></param>
        /// <param name="x"></param>
        /// <returns></returns>
        public static List<LinearCombination> InTermsOf(this IEnumerable<Equal> f, IEnumerable<Expression> x)
        {
            // Convert f to a system of linear equations.
            return f.Select(i => LinearCombination.New(x, i.Left - i.Right)).ToList();
        }


        private static LinearCombination Scale(LinearCombination L, Expression R)
        {
            return LinearCombination.New(L.Basis.Select(i => new KeyValuePair<Expression, Expression>(i, L[i] * R)));
        }

        private static LinearCombination AddScale(LinearCombination L, LinearCombination R, Expression s)
        {
            return LinearCombination.New(L.Basis.Union(R.Basis).Select(i => new KeyValuePair<Expression, Expression>(i, L[i] + Binary.Multiply(R[i], s))));
        }

        public static LinearCombination FindPivot(this IList<LinearCombination> S, Expression x)
        {
            IEnumerable<LinearCombination> candidates = S.Where(i => x.Equals(i.PivotVariable));
            if (candidates.Empty())
                return null;

            return candidates.ArgMax(i =>
            {
                if (i.PivotCoefficient is Constant && i.Basis.All(j => !i[j].DependsOn(j)))
                    return Math.Abs((double)((Constant)i.PivotCoefficient).Value);
                return -1.0;
            });
        }

        public static int FindPivotRow(this IList<LinearCombination> S, int i, Expression x)
        {
            int r = -1;
            double p = 0.0;

            for (; i < S.Count; ++i)
            {
                Expression Six = S[i][x];
                // If we don't already have a pivot row, use this non-zero pivot position row.
                if (!Six.EqualsZero() && r == -1)
                {
                    r = i;
                    p = 0.0;
                }

                // Check if this is the largest pivot position term.
                if (Six is Constant)
                {
                    double cSix = (double)Six;
                    if (Math.Abs(cSix) > Math.Abs(p))
                    {
                        r = i;
                        p = cSix;
                    }
                }
            }
            return r;
        }

        /// <summary>
        /// Compute the row-echelon form of the system S in terms of x. This function modifies S in place.
        /// </summary>
        /// <param name="S"></param>
        /// <param name="x"></param>
        public static void RowReduce(this IList<LinearCombination> S, IEnumerable<Expression> x)
        {
            int r = 0;
            foreach (Expression j in x)
            {
                int i = FindPivotRow(S, r, j);
                // If there is no pivot row, all of the entries in this column are already eliminated.
                if (i == -1)
                    continue;
                // If the pivot row isn't the current row, swap them.
                if (i != r)
                    Swap(S, i, r);

                Expression scale = -S[r][j];

                // Cancel the pivot variable from other rows.
                for (i = r + 1; i < S.Count; ++i)
                {
                    Expression Sij = S[i][j];
                    if (!Sij.EqualsZero())
                    {
                        S[i] = AddScale(S[i], S[r], Sij / scale);
                        Debug.Assert(S[i][j].EqualsZero());
                    }
                }

                ++r;
            }
        }

        private static void Swap(IList<LinearCombination> S, int a, int b)
        {
            LinearCombination t = S[a];
            S[a] = S[b];
            S[b] = t;
        }

        /// <summary>
        /// Given a row-echelon form system of x, eliminate the upper triangular terms.
        /// </summary>
        /// <param name="S"></param>
        /// <returns></returns>
        public static void BackSubstitute(this IList<LinearCombination> S, IEnumerable<Expression> x)
        {
            int r = S.Count - 1;
            foreach (Expression j in x.Reverse())
            {
                // Check if we have a pivot for j, if not skip it.
                if (S[r][j].EqualsZero())
                    continue;
                
                // Eliminate non-pivot variables from other rows.
                Expression scale = -S[r][j];
                for (int i = r - 1; i >= 0; --i)
                    S[i] = AddScale(S[i], S[r], S[i][j] / scale);

                // Move to the next row.
                --r;
            }
        }

        /// <summary>
        /// Solve a system for the variables in x.
        /// </summary>
        /// <param name="S"></param>
        /// <param name="x"></param>
        /// <returns></returns>
        public static List<Arrow> Solve(this IList<LinearCombination> S, IEnumerable<Expression> x, bool Remove)
        {
            int r = S.Count - 1;
            // Solve for the variables in x.
            List<Arrow> result = new List<Arrow>();
            foreach (Expression j in x.Reverse())
            {
                LinearCombination i = S[r];

                // If there is no pivot in this position, find any row with a non-zero coefficient of j.
                if (!i[j].EqualsZero())
                    --r;
                else
                    i = S.FirstOrDefault(a => !a[j].EqualsZero());

                if (!ReferenceEquals(i, null))
                    result.Add(Arrow.New(j, i.Solve(j)));
            }
            return result;
        }

        /// <summary>
        /// Solve a linear equation or system of linear equations.
        /// </summary>
        /// <param name="f">Equation or set of equations to solve.</param>
        /// <param name="x">Variable of set of variables to solve for.</param>
        /// <returns>The solved values of x, including non-independent solutions.</returns>
        public static List<Arrow> Solve(this IEnumerable<Equal> f, IEnumerable<Expression> x)
        {
            // Convert f to a system of linear equations.
            List<LinearCombination> S = f.InTermsOf(x);

            // Get row-echelon form of S.
            S.RowReduce(x);

            // Back substitution.
            S.BackSubstitute(x);

            // Solve for the variables.
            return S.Solve(x);
        }

        /// <summary>
        /// Partially solve a linear equation or system of linear equations. Back substitution is not performed, equations that are solved
        /// are removed from the system.
        /// </summary>
        /// <param name="f">Equation or set of equations to solve.</param>
        /// <param name="x">Variable of set of variables to solve for.</param>
        /// <returns>The solved values of x, including non-independent solutions.</returns>
        public static List<Arrow> PartialSolve(this IList<LinearCombination> S, IEnumerable<Expression> x)
        {
            // Get row-echelon form of S.
            S.RowReduce(x);

            return S.Solve(x);
        }
    }
}
