using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ComputerAlgebra
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
            return f.Select(i => new LinearCombination(x, i.Left - i.Right)).ToList();
        }

        public static LinearCombination FindPivot(this IEnumerable<LinearCombination> S, Expression x)
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

        /// <summary>
        /// Compute the row-echelon form of the system S in terms of x. This function modifies S in place.
        /// </summary>
        /// <param name="S"></param>
        /// <param name="x"></param>
        public static void RowReduce(this IEnumerable<LinearCombination> S, IEnumerable<Expression> x)
        {
            foreach (Expression j in x)
            {
                LinearCombination i1 = FindPivot(S, j);
                if (i1 == null)
                    continue;
                Expression scale = -i1.PivotCoefficient;

                // Cancel the pivot variable from other rows.
                foreach (LinearCombination i2 in S.Except(i1).Where(i => j.Equals(i.PivotVariable)))
                {
                    i2.AddScaled(i2.PivotCoefficient / scale, i1);
                    // This really should be 0 already, but due to numerical/symbolic issues, it might not be.
                    i2[j] = 0;
                }
            }
        }

        /// <summary>
        /// Given a row-echelon form 
        /// </summary>
        /// <param name="S"></param>
        /// <returns></returns>
        public static void BackSubstitute(this IEnumerable<LinearCombination> S, IEnumerable<Expression> x)
        {
            foreach (LinearCombination i in S.Where(i => x.Contains(i.PivotVariable)))
            {
                Expression pivot = i.PivotVariable;
                Expression scale = -i.PivotCoefficient;

                // Eliminate non-pivot variables from other rows.
                foreach (LinearCombination r in S.Except(i).Where(r => !r[pivot].EqualsZero()))
                    r.AddScaled(r[pivot] / scale, i);
            }
        }

        /// <summary>
        /// Return the pivot variables of the system S.
        /// </summary>
        /// <param name="S"></param>
        /// <returns></returns>
        public static List<Expression> Pivots(this List<LinearCombination> S)
        {
            return S.Where(i => !ReferenceEquals(i.PivotVariable, null)).Select(i => i.PivotVariable).Distinct().ToList();
        }

        /// <summary>
        /// Solve a system for the variables in x.
        /// </summary>
        /// <param name="S"></param>
        /// <param name="x"></param>
        /// <returns></returns>
        public static List<Arrow> Solve(this IEnumerable<LinearCombination> S, IEnumerable<Expression> x)
        {
            // Solve for the variables in x.
            List<Arrow> result = new List<Arrow>();
            foreach (Expression j in x.Reverse())
            {
                // Find the row with the pivot variable in this position.
                LinearCombination i = S.FindPivot(j);

                // If there is no pivot in this position, find any row with a non-zero coefficient of j.
                if (i == null)
                    i = S.FirstOrDefault(s => !s[j].EqualsZero());

                // Solve the row for i.
                if (i != null)
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
        /// Solve a linear equation or system of linear equations.
        /// </summary>
        /// <param name="f">Equation or set of equations to solve.</param>
        /// <param name="x">Variable of set of variables to solve for.</param>
        /// <returns>The solved values of x, including non-independent solutions.</returns>
        public static List<Arrow> Solve(this IEnumerable<Equal> f, params Expression[] x) { return f.Solve(x.AsEnumerable()); }
        

        /// <summary>
        /// Partially solve a linear equation or system of linear equations. Back substitution is not performed.
        /// </summary>
        /// <param name="f">Equation or set of equations to solve.</param>
        /// <param name="x">Variable of set of variables to solve for.</param>
        /// <returns>The solved values of x, including non-independent solutions.</returns>
        public static List<Arrow> PartialSolve(this IEnumerable<Equal> f, IEnumerable<Expression> x)
        {
            // Convert f to a system of linear equations.
            List<LinearCombination> S = f.InTermsOf(x);

            // Get row-echelon form of S.
            S.RowReduce(x);
            
            // Solve for the variables.
            return S.Solve(x);
        }

        /// <summary>
        /// Partially solve a linear equation or system of linear equations. Back substitution is not performed.
        /// </summary>
        /// <param name="f">Equation or set of equations to solve.</param>
        /// <param name="x">Variable of set of variables to solve for.</param>
        /// <returns>The solved values of x, including non-independent solutions.</returns>
        public static List<Arrow> PartialSolve(this IEnumerable<Equal> f, params Expression[] x) { return f.PartialSolve(x.AsEnumerable()); }
    }
}
