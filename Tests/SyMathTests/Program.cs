using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SyMath;

namespace SyMathTests
{
    class Program
    {
        static void Test(Expression Expr, Expression Result)
        {
            Expression T = Equal.New(Expr, Result);
            Expression TE = T.Evaluate();

            if (!TE.IsTrue())
            {
                System.Console.WriteLine("{0}", Arrow.New(T, TE).ToPrettyString());
            }
        }

        static void Main(string[] args)
        {
            Dictionary<Expression, Expression> Tests = new Dictionary<Expression, Expression>()
            {
                // Basic operations.
                { "IsInteger[2]", "1" },
                { "IsInteger[2.1]", "0" },
                { "Cos[0]", "1" }, 
                { "Sin[0]", "0" }, 
                { "Sqrt[4]", "2" },
                { "-1.0", "-1" },
                { "-1.2e1", "-12" },
                { "-1.2e-1", "-.12" },
                { "Abs[e - 2.7183] < 0.01", "1" },
                { "Abs[Pi - 3.1416] < 0.01", "1" },
                //{ "f'[t]", "D[f[t], t]" },

                { "Sqrt[x] : x->4", "2" },
                { "D[x^2, x] : x->1", "2" },
                { "Sin[x] : x->t", "Sin[t]" },
                { "x + y : {x->1, y->2}", "3" },
                { "D[f[x], x] : x->0", "D[f[x], x] : x->0" },
                { "D[Cos[x], x] : x->0", "0" },
                
                // Basic arithmetic.
                { "x + x", "2*x" },
                { "2*-x", "-2*x" },
                { "2*x + 3*x", "5*x" },
                { "2*x - 3*x", "-x" },
                { "-(2*x + 3*x)", "-5*x" },
                { "20*s - 20*s", "0" },
                { "x*x", "x^2" },
                { "x*x^2", "x^3" },
                { "x^3*x^2", "x^5" },
                { "x/x", "1" },
                { "x/x^2", "1/x" },
                { "x^3/x^2", "x" },
                { "x^-1 - 1/x", "0" },
                { "(x^2)^3", "x^6" },
                { "x/(y/z)", "(z*x)/y" },
                { "(x/y)/z", "x/(y*z)" },
                { "(x/y)/(z/w)", "(x*w)/(y*z)" },

                // Expand.
                { "Expand[2*(x + 2)]", "2*x + 4" },
                { "Expand[x*(3*x + 2) + 4]", "3*x^2 + 2*x + 4" },
                { "Expand[(x + 1)*(x - 1)]", "x^2 - 1" },
                { "Expand[(x + 1)^3]", "x^3 + 3*x^2 + 3*x + 1" },
                { "Expand[(x + 1)^5]", "x^5 + 5*x^4 + 10*x^3 + 10*x^2 + 5*x + 1" },
                { "Expand[a*(b + c)]", "a*b + a*c" },
                { "Expand[(a + b)*(c + d)]", "a*c + a*d + b*c + b*d" },
                { "Expand[1/(s^3 + s), s]", "1/s - s/(s^2 + 1)" },
                
                // Quadratics
                //{ "Factor[A*x^2 + B*x + C]", "(x - ((-B + Sqrt[B^2 - 4*A*C])/(2*A)))*(x - ((-B - Sqrt[B^2 - 4*A*C])/(2*A)))" },
                { "Factor[x^2 - x, x]", "x*(x - 1)" },
                { "Factor[x^4 - x^2, x]", "x^2*(x^2 - 1)" },

                // Hyperbolic functions.
                { "Exp[x] + Exp[-x]", "2*Cosh[x]" },
                { "Exp[2*x] - Exp[-2*x]", "2*Sinh[2*x]" },
                { "(Exp[x] - Exp[-x])/(Exp[x] + Exp[-x])", "Tanh[x]" },
                { "(Exp[2*x] + Exp[-2*x])/(Exp[2*x] - Exp[-2*x])", "Coth[2*x]" },
                { "2/(Exp[x] + Exp[-x])", "Sech[x]" },
                { "3/(Exp[2*x] - Exp[-2*x])", "1.5*Csch[2*x]" },

                // Trig functions.
                { "Sin[x]/Cos[x]", "Tan[x]" },
                { "Cos[x^2]/Sin[x^2]", "Cot[x^2]" },
                { "Tan[x]*Cos[x]", "Sin[x]" },
                { "Sin[x]^2 + Cos[x]^2", "1" },
                { "y*Sin[x]^2 + y*Cos[x]^2", "y" },

                // Derivatives.
                { "D[A*x + B*x^2, x]", "A + 2*B*x" },
                { "D[Sin[x], x]", "Cos[x]" },
                { "D[Cos[x], x]", "-Sin[x]" },
                { "D[Tan[x], x]", "Sec[x]^2" },
                { "D[Ln[x], x]", "1/x" },
                { "D[(x + 1)^2, x]", "2*(x + 1)" },
                { "D[Exp[x^2], x]", "Exp[x^2]*2*x" },

                // Solve.
                { "Solve[y == A*x + B, x]", "x -> (y - B)/A" },
                { "Solve[{2*x + 4*y == 8, x == 2*y + 3}, {x, y}]", "{x -> 7/2, y -> 1/4}" },

                // NSolve.
                { "NSolve[x == Cos[x], x->0.5, 1e-6]", "x->0.739085" },
                { "NSolve[2 == Exp[x] - Exp[-x], x->0.5, 1e-6]", "x->0.881374" },
                { "NSolve[{y == x^2, x^2 + y^2 == 1}, {x->1, y->1}, 1e-6]", "{x->0.786, y->0.618}" },
                { "NSolve[{y == x^2, x^2 + y^2 == 1, z == x + y}, {x->1, y->1, z->1}, 1e-6]", "{x->0.786, y->0.618, z->1.386}" },
                { "NSolve[{z == x^2, x^2 + y^2 == 1, z == y}, {x->1, y->1, z->1}, 1e-6]", "{x->0.786, y->0.618, z->0.618}" },
                { "NSolve[{Exp[x + y] == 1, Exp[x - 2*y] == 2}, {x->0.5, y->0.5}, 1e-6]", "{x->0.1505, y->-0.1505}" },
                { "NSolve[(((0.01*Vo) + (-0.01*1) + (1E-12*Exp[(38.6847*Vo)]) + (-1E-12*Exp[(-38.6847*Vo)]))==0), Vo->0.57, 1e-6]", "Vo->0.573208" },
                { "NSolve[(((0.01*Vo) + (-0.01*1) + (1E-12*Exp[(38.6847*Vo)]) + (-1E-12*Exp[(-38.6847*Vo)]))==0), Vo->0, 1e-6]", "Vo->0.573208" },
                { "NSolve[(Ln[((0.01*Vo) + (1E-12*Exp[(38.6847*Vo)]) + (-1E-12*Exp[(-38.6847*Vo)]))]==Ln[0.01]), Vo->0.01, 1e-6]", "Vo->0.573208" },

                // DSolve.
                { "DSolve[D[y[t], t]==y[t], y[t], y[0]->1, t]", "y[t]->Exp[t]" },
                { "DSolve[D[y[t], t]==t, y[t], y[0]->0, t]", "y[t]->t^2/2" },
                { "DSolve[D[y[t], t]==1, y[t], y[0]->0, t]", "y[t]->t" },
                { "DSolve[D[D[y[t], t], t]==1, y[t], {y[0]->0, (D[y[t], t]:t->0)->0}, t]", "y[t]->t^2/2" },
                { "DSolve[D[y[t], t]==-y[t], y[t], y[0]->1, t]", "y[t]->Exp[-t]" },
                { "DSolve[D[y[t], t]==2*y[t], y[t], y[0]->1, t]", "y[t]->Exp[2*t]" },
                { "DSolve[D[y[t], t]==y[t]/3, y[t], y[0]->1, t]", "y[t]->Exp[t/3]" },
                { "DSolve[D[y[t], t] + y[t]==0, y[t], y[0]->1, t]", "y[t]->Exp[-t]" },
                { "DSolve[D[y[t], t]==Sin[t], y[t], y[0]->0, t]", "y[t]->1 - Cos[t]" },
                
                { "DSolve[I[y[t], t]==y[t], y[t], y[0]->1, t]", "y[t]->Exp[t]" },
                { "DSolve[I[y[t], t]==t, y[t], y[0]->1, t]", "y[t]->1" },
                { "DSolve[I[y[t], t]==t^2/2, y[t], y[0]->1, t]", "y[t]->t" },
                { "DSolve[I[y[t], t]==Sin[t], y[t], y[0]->1, t]", "y[t]->Cos[t]" },
                { "DSolve[I[y[t], t]==Sin[t] + t, y[t], y[0]->1, t]", "y[t]->Cos[t] + 1" },
            };

            System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
            timer.Start();

            foreach (KeyValuePair<Expression, Expression> i in Tests)
                Test(i.Key, i.Value);

            System.Console.WriteLine("{0} ms", timer.ElapsedMilliseconds);
            System.Console.WriteLine("TransformCalls: {0}", TransformSet.TransformCalls);
        }
    }
}
