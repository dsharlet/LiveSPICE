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
            Expression T = Binary.ApproxEqual(Expr, Result);
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
            return passed;
        }

        static KeyValuePair<Expression, Expression> KV(Expression K, Expression V) { return new KeyValuePair<Expression, Expression>(K, V); }
            
        static void Main(string[] args)
        {
            List<KeyValuePair<Expression, Expression>> Tests = new List<KeyValuePair<Expression, Expression>>()
            {
                // Basic operations.
                KV("IsInteger[2]", "1"),
                KV("IsInteger[2.1]", "0"),
                KV("Floor[1]", "1"),
                KV("Floor[1.1]", "1"),
                KV("Floor[0.9]", "0"),
                KV("Floor[0.1]", "0"),
                KV("Floor[0]", "0"),
                KV("Floor[-0.1]", "-1"),
                KV("Floor[-0.9]", "-1"),
                KV("Floor[-1]", "-1"),
                KV("Floor[-1.1]", "-2"),
                KV("Ceiling[1]", "1"),
                KV("Ceiling[1.1]", "2"),
                KV("Ceiling[0.9]", "1"),
                KV("Ceiling[0.1]", "1"),
                KV("Ceiling[0]", "0"),
                KV("Ceiling[-0.1]", "0"),
                KV("Ceiling[-0.9]", "0"),
                KV("Ceiling[-1]", "-1"),
                KV("Ceiling[-1.1]", "-1"),
                KV("Cos[0]", "1"), 
                KV("Sin[0]", "0"), 
                KV("Sqrt[4]", "2"),
                KV("-1.0", "-1"),
                KV("-1.2e1", "-12"),
                KV("Abs[e - 2.7183] < 0.01", "1"),
                KV("Abs[Pi - 3.1416] < 0.01", "1"),
                //KV("f'[t]", "D[f[t], t]"),

                KV("Sqrt[x] : x->4", "2"),
                KV("D[x^2, x] : x->1", "2"),
                KV("Sin[x] : x->t", "Sin[t]"),
                KV("x + y : {x->1, y->2}", "3"),
                KV("D[f[x], x] : x->0", "D[f[x], x] : x->0"),
                KV("D[Cos[x], x] : x->0", "0"),

                // Functional equivalents.
                KV("Ln[x]", "Log[x, e]"),
                KV("Sqrt[x]", "x^(1/2)"),
                KV("Exp[x]", "e^x"),


                // Basic arithmetic.
                KV("x + x", "2*x"),
                KV("2*-x", "-2*x"),
                KV("2*x + 3*x", "5*x"),
                KV("2*x - 3*x", "-x"),
                KV("-(2*x + 3*x)", "-5*x"),
                KV("20*s - 20*s", "0"),
                KV("x*x", "x^2"),
                KV("x*x^2", "x^3"),
                KV("x^3*x^2", "x^5"),
                KV("x/x", "1"),
                KV("x/x^2", "1/x"),
                KV("x^3/x^2", "x"),
                KV("x^-1 - 1/x", "0"),
                KV("(x^2)^3", "x^6"),
                KV("x/(y/z)", "(z*x)/y"),
                KV("(x/y)/z", "x/(y*z)"),
                KV("(x/y)/(z/w)", "(x*w)/(y*z)"),

                // Expand.
                KV("Expand[2*(x + 2)]", "2*x + 4"),
                KV("Expand[x*(3*x + 2) + 4]", "3*x^2 + 2*x + 4"),
                KV("Expand[(x + 1)*(x - 1)]", "x^2 - 1"),
                KV("Expand[(x + 1)^3]", "x^3 + 3*x^2 + 3*x + 1"),
                KV("Expand[(x + 1)^5]", "x^5 + 5*x^4 + 10*x^3 + 10*x^2 + 5*x + 1"),
                KV("Expand[(x + 1)^7]", "x^7 + 7*x^6 + 21*x^5 + 35*x^4 + 35*x^3 + 21*x^2 + 7*x + 1"),
                KV("Expand[a*(b + c)]", "a*b + a*c"),
                KV("Expand[(a + b)*(c + d)]", "a*c + a*d + b*c + b*d"),
                KV("Expand[(a + b)*(c + d)*(f + g)]", "a*c*f + a*d*f + b*c*f + b*d*f + a*c*g + a*d*g + b*c*g + b*d*g"),
                KV("Expand[1/(s^3 + s), s]", "1/s - s/(s^2 + 1)"),
                
                // Factor.
                //KV("Factor[A*x^2 + B*x + C]", "(x - ((-B + Sqrt[B^2 - 4*A*C])/(2*A)))*(x - ((-B - Sqrt[B^2 - 4*A*C])/(2*A)))"),
                KV("Factor[x^2 - x, x]", "x*(x - 1)"),
                KV("Factor[x^4 - x^2, x]", "x^2*(x^2 - 1)"),
                KV("Factor[A*Exp[x] + B*Exp[x] + C*Sin[x] + D*Sin[x]]", "(A + B)*Exp[x] + (C + D)*Sin[x]"),
                KV("Factor[A*Exp[x] + B*Exp[x] + A*Sin[x] + B*Sin[x]]", "(A + B)*(Exp[x] + Sin[x])"),

                // Exponential functions.
                KV("Ln[Exp[2]]", "2"),
                KV("Log[10^3, 10]", "3"),
                KV("Ln[a^b]/b", "Ln[a]"),
                KV("Ln[Exp[x]]", "x"),
                KV("Log[b^x, b]", "x"),
                KV("Log[x, 3]", "Ln[x]/Ln[3]"),
                KV("Ln[x*y]", "Ln[x] + Ln[y]"),
                KV("Ln[x/y]", "Ln[x] - Ln[y]"),
                KV("Ln[x*y^2]", "Ln[x] - 2*Ln[y]"),

                //// Hyperbolic functions.
                KV("ArcSinh[Sinh[Pi/6]]", "Pi/6"),
                KV("ArcCosh[Cosh[Pi/6]]", "Pi/6"),
                KV("ArcTanh[Tanh[Pi/6]]", "Pi/6"),
                KV("ArcSech[Sech[Pi/6]]", "Pi/6"),
                KV("ArcCsch[Csch[Pi/6]]", "Pi/6"),
                KV("ArcCoth[Coth[Pi/6]]", "Pi/6"),
                //KV("Exp[x] + Exp[-x]", "2*Cosh[x]"),
                //KV("Exp[2*x] - Exp[-2*x]", "2*Sinh[2*x]"),
                //KV("(Exp[x] - Exp[-x])/(Exp[x] + Exp[-x])", "Tanh[x]"),
                //KV("(Exp[2*x] + Exp[-2*x])/(Exp[2*x] - Exp[-2*x])", "Coth[2*x]"),
                //KV("2/(Exp[x] + Exp[-x])", "Sech[x]"),
                //KV("3/(Exp[2*x] - Exp[-2*x])", "1.5*Csch[2*x]"),

                //// Trig functions.
                KV("ArcSin[Sin[Pi/6]]", "Pi/6"),
                KV("ArcCos[Cos[Pi/6]]", "Pi/6"),
                KV("ArcTan[Tan[Pi/6]]", "Pi/6"),
                KV("ArcSec[Sec[Pi/6]]", "Pi/6"),
                KV("ArcCsc[Csc[Pi/6]]", "Pi/6"),
                KV("ArcCot[Cot[Pi/6]]", "Pi/6"),
                //KV("Sin[x]/Cos[x]", "Tan[x]"),
                //KV("Cos[x^2]/Sin[x^2]", "Cot[x^2]"),
                //KV("Tan[x]*Cos[x]", "Sin[x]"),
                //KV("Sin[x]^2 + Cos[x]^2", "1"),
                //KV("y*Sin[x]^2 + y*Cos[x]^2", "y"),

                // Derivatives.
                KV("D[Sin[x], x]", "Cos[x]"),
                KV("D[Cos[x], x]", "-Sin[x]"),
                KV("D[Tan[x], x]", "Sec[x]^2"),
                KV("D[Sec[x], x]", "Sec[x]*Tan[x]"),
                KV("D[Csc[x], x]", "-Csc[x]*Cot[x]"),
                KV("D[Cot[x], x]", "-Csc[x]^2"),

                KV("D[Sinh[A*x], x]", "A*Cosh[A*x]"),
                KV("D[Cosh[A*x + B], x]", "A*Sinh[A*x + B]"),
                KV("D[Tanh[A*x^2 + B*x + C], x]", "(2*A*x + B)*Sech[A*x^2 + B*x + C]^2"),
                KV("D[Sech[x], x]", "-Sech[x]*Tanh[x]"),
                KV("D[Csch[x], x]", "-Csch[x]*Coth[x]"),
                KV("D[Coth[x], x]", "-Csch[x]^2"),
                
                KV("D[A*x + B*x^2, x]", "A + 2*B*x"),
                KV("D[(x^2 + 1)^5, x]", "5*(2*x)*(x^2 + 1)^4"),
                KV("D[Ln[x], x]", "1/x"),
                KV("D[Ln[Abs[x]], x]", "Abs[x]/x^2"),
                KV("D[(x + 1)^2, x]", "2*(x + 1)"),
                KV("D[Exp[x^2], x]", "Exp[x^2]*2*x"),
                KV("D[Exp[x^5], x]", "Exp[x^5]*5*x^4"),

                // Solve.
                KV("Solve[y == A*x + B, x]", "x -> (y - B)/A"),
                KV("Solve[{2*x + 4*y == 8, x == 2*y + 3}, {x, y}]", "{x -> 7/2, y -> 1/4}"),
                
                // NSolve.
                KV("NSolve[x == Cos[x], x->0.5]", "x->0.739085"),
                KV("NSolve[2 == Exp[x] - Exp[-x], x->0.5]", "x->0.881374"),
                KV("NSolve[{y == x^2, x^2 + y^2 == 1}, {x->1, y->1}]", "{x->0.786, y->0.618}"),
                KV("NSolve[{y == x^2, x^2 + y^2 == 1, z == x + y}, {x->1, y->1, z->1}]", "{x->0.786151, y->0.618034, z->1.40419}"),
                KV("NSolve[{z == x^2, x^2 + y^2 == 1, z == y}, {x->1, y->1, z->1}]", "{x->0.786151, y->0.618034, z->0.618034}"),
                KV("NSolve[{Exp[x + y] == 1, Exp[x - 2*y] == 2}, {x->0.5, y->0.5}]", "{x->0.231049, y->-0.231049}"),
                KV("NSolve[(((0.01*Vo) + (-0.01*1) + (1E-12*Exp[(38.6847*Vo)]) + (-1E-12*Exp[(-38.6847*Vo)]))==0), Vo->0.57]", "Vo->0.573208"),
                KV("NSolve[(((0.01*Vo) + (-0.01*1) + (1E-12*Exp[(38.6847*Vo)]) + (-1E-12*Exp[(-38.6847*Vo)]))==0), Vo->0]", "Vo->0.573208"),
                KV("NSolve[(Ln[((0.01*Vo) + (1E-12*Exp[(38.6847*Vo)]) + (-1E-12*Exp[(-38.6847*Vo)]))]==Ln[0.01]), Vo->0.01]", "Vo->0.573208"),

                // DSolve.
                KV("DSolve[D[y[t], t]==y[t], y[t], y[0]->1, t]", "y[t]->Exp[t]"),
                KV("DSolve[D[y[t], t]==y[t], y[t], y[0]->C, t]", "y[t]->C*Exp[t]"),
                KV("DSolve[D[y[t], t]==t, y[t], y[0]->0, t]", "y[t]->t^2/2"),
                KV("DSolve[D[y[t], t]==1, y[t], y[0]->0, t]", "y[t]->t"),
                KV("DSolve[D[D[y[t], t], t]==1, y[t], {y[0]->0, (D[y[t], t]:t->0)->0}, t]", "y[t]->t^2/2"),
                KV("DSolve[D[y[t], t]==-y[t], y[t], y[0]->1, t]", "y[t]->Exp[-t]"),
                KV("DSolve[D[y[t], t]==2*y[t], y[t], y[0]->1, t]", "y[t]->Exp[2*t]"),
                KV("DSolve[D[y[t], t]==y[t]/3, y[t], y[0]->C, t]", "y[t]->C*Exp[t/3]"),
                KV("DSolve[D[y[t], t] + y[t]==0, y[t], y[0]->1, t]", "y[t]->Exp[-t]"),
                KV("DSolve[D[y[t], t]==Sin[t], y[t], y[0]->0, t]", "y[t]->1 - Cos[t]"),
                
                KV("DSolve[I[y[t], t]==y[t], y[t], y[0]->1, t]", "y[t]->Exp[t]"),
                KV("DSolve[I[y[t], t]==t, y[t], y[0]->1, t]", "y[t]->1"),
                KV("DSolve[I[y[t], t]==t^2/2, y[t], y[0]->1, t]", "y[t]->t"),
                KV("DSolve[I[y[t], t]==Sin[t], y[t], y[0]->1, t]", "y[t]->Cos[t]"),
                KV("DSolve[I[y[t], t]==Sin[t] + t, y[t], y[0]->1, t]", "y[t]->Cos[t] + 1"),
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
