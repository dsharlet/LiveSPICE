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
        /// Solve a linear equation or system of linear equations.
        /// </summary>
        /// <param name="f">Equation or set of equations to solve.</param>
        /// <param name="x">Variable of set of variables to solve for.</param>
        /// <returns></returns>
        public static List<Arrow> Solve(this IEnumerable<Equal> f, IEnumerable<Expression> x)
        {
            // Convert f to a system of linear equations.
            List<LinearCombination> S = f.Select(i => new LinearCombination(x, i.Left - i.Right)).ToList();

            // Find row-echelon form of the system S.
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

            // Back substitution.
            foreach (LinearCombination PivotRow in S.Where(i => !ReferenceEquals(i.PivotVariable, null)))
            {
                Expression PivotVariable = PivotRow.PivotVariable;
                Expression PivotCoefficient = PivotRow.PivotCoefficient;

                // Eliminate non-pivot variables from other rows.
                foreach (LinearCombination r in S.Except(PivotRow).Where(i => !i[PivotVariable].IsZero()))
                    r.AddScaled(-r[PivotVariable] / PivotCoefficient, PivotRow);
            }

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
    }
}
