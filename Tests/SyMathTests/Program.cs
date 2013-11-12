using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SyMath;

namespace SyMathTests
{
    class Program
    {
        static bool Test(Expression Expr, Expression Result)
        {
            Expression T = Equal.New(Expr, Result);
            Expression TE = T.Evaluate();

            bool passed = TE.IsTrue();
            if (!passed)
            {
                // Special case for NSolve since it does not produce exact results.
                Expression pattern = "NSolve[x, y]";
                MatchContext m = pattern.Matches(Expr);
                if (m != null)
                {
                    IEnumerable<Equal> f = Set.MembersOf(m["x"]).Cast<Equal>();
                    passed = f.All(i => Call.Abs((i.Left - i.Right).Evaluate(Set.MembersOf(Result).Cast<Arrow>())) < 1e-3);
                }
            }

            if (!passed)
                System.Console.WriteLine("{0}", Arrow.New(T, TE).ToPrettyString());
            return TE.IsTrue();
        }

        static void Main(string[] args)
        {
            Dictionary<Expression, Expression> Tests = new Dictionary<Expression, Expression>()
            {
                // Basic operations.
                { "IsInteger[2]", "1" },
                { "IsInteger[2.1]", "0" },
                { "Floor[1]", "1" },
                { "Floor[1.1]", "1" },
                { "Floor[0.9]", "0" },
                { "Floor[0.1]", "0" },
                { "Floor[0]", "0" },
                { "Floor[-0.1]", "-1" },
                { "Floor[-0.9]", "-1" },
                { "Floor[-1]", "-1" },
                { "Floor[-1.1]", "-2" },
                { "Ceiling[1]", "1" },
                { "Ceiling[1.1]", "2" },
                { "Ceiling[0.9]", "1" },
                { "Ceiling[0.1]", "1" },
                { "Ceiling[0]", "0" },
                { "Ceiling[-0.1]", "0" },
                { "Ceiling[-0.9]", "0" },
                { "Ceiling[-1]", "-1" },
                { "Ceiling[-1.1]", "-1" },
                { "Cos[0]", "1" }, 
                { "Sin[0]", "0" }, 
                { "Sqrt[4]", "2" },
                { "-1.0", "-1" },
                { "-1.2e1", "-12" },
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
                { "Expand[(x + 1)^7]", "x^7 + 7*x^6 + 21*x^5 + 35*x^4 + 35*x^3 + 21*x^2 + 7*x + 1" },
                { "Expand[a*(b + c)]", "a*b + a*c" },
                { "Expand[(a + b)*(c + d)]", "a*c + a*d + b*c + b*d" },
                { "Expand[(a + b)*(c + d)*(f + g)]", "a*c*f + a*d*f + b*c*f + b*d*f + a*c*g + a*d*g + b*c*g + b*d*g" },
                { "Expand[1/(s^3 + s), s]", "1/s - s/(s^2 + 1)" },
                
                // Factor.
                //{ "Factor[A*x^2 + B*x + C]", "(x - ((-B + Sqrt[B^2 - 4*A*C])/(2*A)))*(x - ((-B - Sqrt[B^2 - 4*A*C])/(2*A)))" },
                { "Factor[x^2 - x, x]", "x*(x - 1)" },
                { "Factor[x^4 - x^2, x]", "x^2*(x^2 - 1)" },
                { "Factor[A*Exp[x] + B*Exp[x] + C*Sin[x] + D*Sin[x]]", "(A + B)*Exp[x] + (C + D)*Sin[x]" },
                { "Factor[A*Exp[x] + B*Exp[x] + A*Sin[x] + B*Sin[x]]", "(A + B)*(Exp[x] + Sin[x])" },

                //// Hyperbolic functions.
                //{ "Exp[x] + Exp[-x]", "2*Cosh[x]" },
                //{ "Exp[2*x] - Exp[-2*x]", "2*Sinh[2*x]" },
                //{ "(Exp[x] - Exp[-x])/(Exp[x] + Exp[-x])", "Tanh[x]" },
                //{ "(Exp[2*x] + Exp[-2*x])/(Exp[2*x] - Exp[-2*x])", "Coth[2*x]" },
                //{ "2/(Exp[x] + Exp[-x])", "Sech[x]" },
                //{ "3/(Exp[2*x] - Exp[-2*x])", "1.5*Csch[2*x]" },

                //// Trig functions.
                //{ "Sin[x]/Cos[x]", "Tan[x]" },
                //{ "Cos[x^2]/Sin[x^2]", "Cot[x^2]" },
                //{ "Tan[x]*Cos[x]", "Sin[x]" },
                //{ "Sin[x]^2 + Cos[x]^2", "1" },
                //{ "y*Sin[x]^2 + y*Cos[x]^2", "y" },

                // Derivatives.
                { "D[Sin[x], x]", "Cos[x]" },
                { "D[Cos[x], x]", "-Sin[x]" },
                { "D[Tan[x], x]", "Sec[x]^2" },
                { "D[Sec[x], x]", "Sec[x]*Tan[x]" },
                { "D[Csc[x], x]", "-Csc[x]*Cot[x]" },
                { "D[Cot[x], x]", "-Csc[x]^2" },

                { "D[Sinh[A*x], x]", "A*Cosh[A*x]" },
                { "D[Cosh[A*x + B], x]", "A*Sinh[A*x + B]" },
                { "D[Tanh[A*x^2 + B*x + C], x]", "(2*A*x + B)*Sech[A*x^2 + B*x + C]^2" },
                { "D[Sech[x], x]", "-Sech[x]*Tanh[x]" },
                { "D[Csch[x], x]", "-Csch[x]*Coth[x]" },
                { "D[Coth[x], x]", "-Csch[x]^2" },
                
                { "D[A*x + B*x^2, x]", "A + 2*B*x" },
                { "D[(x^2 + 1)^5, x]", "5*(2*x)*(x^2 + 1)^4" },
                { "D[Ln[x], x]", "1/x" },
                { "D[Ln[Abs[x]], x]", "Abs[x]/x^2" },
                { "D[(x + 1)^2, x]", "2*(x + 1)" },
                { "D[Exp[x^2], x]", "Exp[x^2]*2*x" },
                { "D[Exp[x^5], x]", "Exp[x^5]*5*x^4" },

                // Solve.
                { "Solve[y == A*x + B, x]", "x -> (y - B)/A" },
                { "Solve[{2*x + 4*y == 8, x == 2*y + 3}, {x, y}]", "{x -> 7/2, y -> 1/4}" },
                
                // NSolve.
                { "NSolve[x == Cos[x], x->0.5]", "x->0.739085" },
                { "NSolve[2 == Exp[x] - Exp[-x], x->0.5]", "x->0.881374" },
                { "NSolve[{y == x^2, x^2 + y^2 == 1}, {x->1, y->1}]", "{x->0.786, y->0.618}" },
                { "NSolve[{y == x^2, x^2 + y^2 == 1, z == x + y}, {x->1, y->1, z->1}]", "{x->0.786151, y->0.618034, z->1.40419}" },
                { "NSolve[{z == x^2, x^2 + y^2 == 1, z == y}, {x->1, y->1, z->1}]", "{x->0.786151, y->0.618034, z->0.618034}" },
                { "NSolve[{Exp[x + y] == 1, Exp[x - 2*y] == 2}, {x->0.5, y->0.5}]", "{x->0.231049, y->-0.231049}" },
                { "NSolve[(((0.01*Vo) + (-0.01*1) + (1E-12*Exp[(38.6847*Vo)]) + (-1E-12*Exp[(-38.6847*Vo)]))==0), Vo->0.57]", "Vo->0.573208" },
                { "NSolve[(((0.01*Vo) + (-0.01*1) + (1E-12*Exp[(38.6847*Vo)]) + (-1E-12*Exp[(-38.6847*Vo)]))==0), Vo->0]", "Vo->0.573208" },
                { "NSolve[(Ln[((0.01*Vo) + (1E-12*Exp[(38.6847*Vo)]) + (-1E-12*Exp[(-38.6847*Vo)]))]==Ln[0.01]), Vo->0.01]", "Vo->0.573208" },

                // DSolve.
                { "DSolve[D[y[t], t]==y[t], y[t], y[0]->1, t]", "y[t]->Exp[t]" },
                { "DSolve[D[y[t], t]==y[t], y[t], y[0]->C, t]", "y[t]->C*Exp[t]" },
                { "DSolve[D[y[t], t]==t, y[t], y[0]->0, t]", "y[t]->t^2/2" },
                { "DSolve[D[y[t], t]==1, y[t], y[0]->0, t]", "y[t]->t" },
                { "DSolve[D[D[y[t], t], t]==1, y[t], {y[0]->0, (D[y[t], t]:t->0)->0}, t]", "y[t]->t^2/2" },
                { "DSolve[D[y[t], t]==-y[t], y[t], y[0]->1, t]", "y[t]->Exp[-t]" },
                { "DSolve[D[y[t], t]==2*y[t], y[t], y[0]->1, t]", "y[t]->Exp[2*t]" },
                { "DSolve[D[y[t], t]==y[t]/3, y[t], y[0]->C, t]", "y[t]->C*Exp[t/3]" },
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

            int passed = 0;
            int total = 0;
            foreach (KeyValuePair<Expression, Expression> i in Tests)
            {
                passed += Test(i.Key, i.Value) ? 1 : 0;
                total += 1;
            }

            System.Console.WriteLine("{0} of {1} passed", passed, total);

            System.Console.WriteLine("{0} ms", timer.ElapsedMilliseconds);
            System.Console.WriteLine("TransformCalls: {0}", TransformSet.TransformCalls);
        }
    }
}
