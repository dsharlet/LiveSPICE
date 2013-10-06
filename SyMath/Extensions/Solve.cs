using System;
using System.Collections.Generic;
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
        public static List<LinearCombination> TermsOf(this IEnumerable<Equal> f, IEnumerable<Expression> x)
        {
            // Convert f to a system of linear equations.
            return f.Select(i => new LinearCombination(x, i.Left - i.Right)).ToList();
        }

        /// <summary>
        /// Compute the row-echelon form of the system S in terms of x. This function modifies S in place.
        /// </summary>
        /// <param name="S"></param>
        /// <param name="x"></param>
        public static void ToRowEchelon(this List<LinearCombination> S, IEnumerable<Expression> x)
        {
            foreach (Expression PivotVariable in x)
            {
                // Find the first row with i as a pivot variable.
                LinearCombination PivotRow = S.FirstOrDefault(j => PivotVariable.Equals(j.PivotVariable));
                if (PivotRow == null) continue;
                Expression PivotCoefficient = PivotRow.PivotCoefficient;

                // Cancel the pivot variable from other rows.
                foreach (LinearCombination j in S.Except(PivotRow).Where(i => PivotVariable.Equals(i.PivotVariable)))
                    j.AddScaled(-j.PivotCoefficient / PivotCoefficient, PivotRow);
            }
        }

        /// <summary>
        /// Given a row-echelon form 
        /// </summary>
        /// <param name="S"></param>
        /// <returns></returns>
        public static void BackSubstitute(this List<LinearCombination> S)
        {
            foreach (LinearCombination PivotRow in S.Where(i => !ReferenceEquals(i.PivotVariable, null)))
            {
                Expression PivotVariable = PivotRow.PivotVariable;
                Expression PivotCoefficient = PivotRow.PivotCoefficient;

                // Eliminate non-pivot variables from other rows.
                foreach (LinearCombination r in S.Except(PivotRow).Where(i => !i[PivotVariable].IsZero()))
                    r.AddScaled(-r[PivotVariable] / PivotCoefficient, PivotRow);
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
        public static List<Arrow> Solve(this List<LinearCombination> S, IEnumerable<Expression> x)
        {
            // Solve for the variables in x.
            List<Arrow> result = new List<Arrow>();
            foreach (Expression i in x)
            {
                // Find the row with the pivot variable in this position.
                LinearCombination row = S.Find(s => i.Equals(s.PivotVariable));

                // If there is no pivot in this position, find any row with a non-zero coefficient of i.
                if (row == null)
                    row = S.Find(s => !s[i].IsZero());

                // Solve the row for i.
                if (row != null)
                    result.Add(Arrow.New(i, row.Solve(i)));
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
            List<LinearCombination> S = f.TermsOf(x);

            // Get row-echelon form of S.
            S.ToRowEchelon(x);

            // Back substitution.
            S.BackSubstitute();

            // Solve for the variables.
            return S.Solve(x);
        }
    }
}
