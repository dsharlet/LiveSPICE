using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace ComputerAlgebra
{
    public static class AlebraicEquivalentsExtension
    {
        private static List<ITransform> rules = new List<ITransform>()
        {
            // Functional equivalents.
            new SubstituteTransform("Sqrt[x]", "x^(1/2)"),
            new SubstituteTransform("Log[x, e]", "Ln[x]"),

            // Log.
            new SubstituteTransform("Log[1, x]", "0"),
            new SubstituteTransform("Log[x, x]", "1"),
            new SubstituteTransform("Log[x^y, b]", "y*Log[x, b]"),
            new SubstituteTransform("Log[x*y, b]", "Log[x, b] + Log[y, b]"),

            // Exp.
            new SubstituteTransform("Sinh[-x]", "-Sinh[x]"),
            new SubstituteTransform("Cosh[-x]", "Cosh[x]"),
            new SubstituteTransform("Tanh[-x]", "-Tanh[x]"),
            new SubstituteTransform("Csch[-x]", "-Csch[x]"),
            new SubstituteTransform("Sech[-x]", "Sech[x]"),
            new SubstituteTransform("Coth[-x]", "-Coth[x]"),
            
            new SubstituteTransform("1/Cosh[x]", "Sech[x]"),
            new SubstituteTransform("1/Sinh[x]", "Csch[x]"),
            new SubstituteTransform("1/Tanh[x]", "Coth[x]"),
            new SubstituteTransform("Sinh[x]/Cosh[x]", "Tanh[x]"),
            new SubstituteTransform("Sinh[x]*Sech[x]", "Tanh[x]"),
            new SubstituteTransform("Cosh[x]*Csch[x]", "Coth[x]"),

            new SubstituteTransform("Cosh[x]^2 - Sinh[x]^2", "1"),
            new SubstituteTransform("Sech[x]^2", "1 - Tanh[x]^2"),
            new SubstituteTransform("Coth[x]^2", "1 + Csch[x]^2"),
            
            new SubstituteTransform("Exp[x] - Exp[-x]", "2*Sinh[x]"),
            new SubstituteTransform("Exp[x] + Exp[-x]", "2*Cosh[x]"),
            new SubstituteTransform("Cosh[x] + Sinh[x]", "Exp[x]"),
            new SubstituteTransform("Cosh[x] - Sinh[x]", "Exp[-x]"),

            // Trig.
            new SubstituteTransform("Sin[-x]", "-Sin[x]"),
            new SubstituteTransform("Cos[-x]", "Cos[x]"),
            new SubstituteTransform("Tan[-x]", "-Tan[x]"),
            new SubstituteTransform("Csc[-x]", "-Csc[x]"),
            new SubstituteTransform("Sec[-x]", "Sec[x]"),
            new SubstituteTransform("Cot[-x]", "-Cot[x]"),

            new SubstituteTransform("1/Cos[x]", "Sec[x]"),
            new SubstituteTransform("1/Sin[x]", "Csc[x]"),
            new SubstituteTransform("1/Tan[x]", "Cot[x]"),
            new SubstituteTransform("Sin[x]/Cos[x]", "Tan[x]"),
            new SubstituteTransform("Sin[x]*Sec[x]", "Tan[x]"),
            new SubstituteTransform("Cos[x]*Csc[x]", "Cot[x]"),
            
            new SubstituteTransform("Sin[x]^2 + Cos[x]^2", "1"),
            new SubstituteTransform("1 + Tan[x]^2", "Sec[x]^2"),
            new SubstituteTransform("1 + Cot[x]^2", "Csc[x]^2"),

            //new SubstituteTransform("Exp[i*x]", "Cos[x] + i*Sin[x]"),
            //new SubstituteTransform("(Exp[i*x] - Exp[-i*x])/(2*i)", "Sin[x]"),
            //new SubstituteTransform("(Exp[i*x] + Exp[-i*x])/2", "Cos[x]"),
        };


        /// <summary>
        /// Enumerate the algebraic equivalents of x.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="Recursion">How many times to apply the algebraic transformation rules.</param>
        /// <returns></returns>
        public static IEnumerable<Expression> AlgebraicEquivalents(this Expression x, int Recursion = 3)
        {
            return AlgebraicEquivalents(x, Recursion, new HashSet<Expression>());
        }

        private static IEnumerable<Expression> AlgebraicEquivalents(Expression x, int Recursion, HashSet<Expression> Enumerated)
        {
            // Don't enumerate expressions more than once.
            if (!Enumerated.Contains(x))
            {
                // Enumerate self.
                Enumerated.Add(x);
                yield return x;

                if (Recursion > 0)
                {
                    foreach (ITransform T in rules)
                    {
                        Expression Tx = T.Transform(x);
                        if (!ReferenceEquals(Tx, x))
                        {
                            foreach (Expression i in AlgebraicEquivalents(Tx, Recursion - 1, Enumerated))
                                yield return i;
                        }
                    }
                }
            }
        }
    }
}
