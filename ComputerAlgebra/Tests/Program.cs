using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ComputerAlgebra;

namespace Tests
{
    class Program
    {
        static bool RunTest(Expression Expr, Expression Result)
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
                Console.WriteLine("{0}", Arrow.New(T, TE).ToPrettyString());
            return passed;
        }

        static bool RunTest(Expression fx, Func<double, double> Result)
        {
            try
            {
                int N = 1000;
                for (int i = 0; i < N; ++i)
                {
                    double x = (((double)i / N) * 2 - 1) * Math.PI;

                    if (Math.Abs((double)fx.Evaluate("x", x) - Result(x)) > 1e-6)
                    {
                        Console.WriteLine("{0} -> {1} != {2}", fx, fx.Evaluate("x", x), Result(x));
                        return false;
                    }
                }
                return true;
            }
            catch (Exception Ex)
            {
                Console.WriteLine(Ex.Message);
                return false;
            }
        }

        static KeyValuePair<Expression, Expression> Test(Expression K, Expression V) { return new KeyValuePair<Expression, Expression>(K, V); }
        static KeyValuePair<Expression, Func<double, double>> Test(Expression K, Func<double, double> V) { return new KeyValuePair<Expression, Func<double, double>>(K, V); }

        static void Main(string[] args)
        {
            List<KeyValuePair<Expression, Func<double, double>>> functions = new List<KeyValuePair<Expression, Func<double, double>>>()
            {
                Test("Abs[x]", Math.Abs),
                Test("Sign[x]", x => x > 0 ? 1 : 0),

                Test("Sin[x]", Math.Sin),
                Test("Cos[x]", Math.Cos),
                Test("Tan[x]", Math.Tan),
                Test("Sec[x]", x => 1 / Math.Cos(x)),
                Test("Csc[x]", x => 1 / Math.Sin(x)),
                Test("Cot[x]", x => 1 / Math.Tan(x)),

                Test("ArcSin[x]", Math.Asin),
                Test("ArcCos[x]", Math.Acos),
                Test("ArcTan[x]", Math.Atan),
                Test("ArcSec[x]", x => Math.Acos(1 / x)),
                Test("ArcCsc[x]", x => Math.Asin(1 / x)),
                Test("ArcCot[x]", x => Math.Atan(1 / x)),

                Test("Sinh[x]", Math.Sinh),
                Test("Cosh[x]", Math.Cosh),
                Test("Tanh[x]", Math.Tanh),
                Test("Sech[x]", x => 1 / Math.Cosh(x)),
                Test("Csch[x]", x => 1 / Math.Sinh(x)),
                Test("Coth[x]", x => 1 / Math.Tanh(x)),
                
                Test("Sqrt[x]", Math.Sqrt),
                Test("Exp[x]", Math.Exp),
                Test("Ln[x]", Math.Log),
                Test("Log[x, 2]", x => Math.Log(x, 2)),
                Test("Log[x, 6]", x => Math.Log(x, 6)),

                Test("Floor[x]", Math.Floor),
                Test("Ceiling[x]", Math.Ceiling),
                Test("Round[x]", Math.Round),
            };
                        
            List<KeyValuePair<Expression, Expression>> tests = new List<KeyValuePair<Expression, Expression>>()
            {
                // Basic operations.
                Test("IsInteger[2]", "1"),
                Test("IsInteger[2.1]", "0"),
                Test("-1.0", "-1"),
                Test("-1.2e1", "-12"),
                Test("Abs[e - 2.7183] < 0.01", "1"),
                Test("Abs[Pi - 3.1416] < 0.01", "1"),
                //KV("f'[t]", "D[f[t], t]"),

                Test("Sqrt[x] : x->4", "2"),
                Test("D[x^2, x] : x->1", "2"),
                Test("Sin[x] : x->t", "Sin[t]"),
                Test("x + y : {x->1, y->2}", "3"),
                Test("D[f[x], x] : x->0", "D[f[x], x] : x->0"),
                Test("D[Cos[x], x] : x->0", "0"),

                // Functional equivalents.
                Test("Ln[x]", "Log[x, e]"),
                Test("Sqrt[x]", "x^(1/2)"),
                Test("Exp[x]", "e^x"),
                
                // Basic arithmetic.
                Test("x + x", "2*x"),
                Test("2*-x", "-2*x"),
                Test("2*x + 3*x", "5*x"),
                Test("2*x - 3*x", "-x"),
                Test("-(2*x + 3*x)", "-5*x"),
                Test("20*s - 20*s", "0"),
                Test("x*x", "x^2"),
                Test("x*x^2", "x^3"),
                Test("x^3*x^2", "x^5"),
                Test("x/x", "1"),
                Test("x/x^2", "1/x"),
                Test("x^3/x^2", "x"),
                Test("x^-1 - 1/x", "0"),
                Test("(x^2)^3", "x^6"),
                Test("x/(y/z)", "(z*x)/y"),
                Test("(x/y)/z", "x/(y*z)"),
                Test("(x/y)/(z/w)", "(x*w)/(y*z)"),

                // Expand.
                Test("Expand[2*(x + 2)]", "2*x + 4"),
                Test("Expand[x*(3*x + 2) + 4]", "3*x^2 + 2*x + 4"),
                Test("Expand[(x + 1)*(x - 1)]", "x^2 - 1"),
                Test("Expand[(x + 1)^3]", "x^3 + 3*x^2 + 3*x + 1"),
                Test("Expand[(x + 1)^5]", "x^5 + 5*x^4 + 10*x^3 + 10*x^2 + 5*x + 1"),
                Test("Expand[(x + 1)^7]", "x^7 + 7*x^6 + 21*x^5 + 35*x^4 + 35*x^3 + 21*x^2 + 7*x + 1"),
                Test("Expand[a*(b + c)]", "a*b + a*c"),
                Test("Expand[(a + b)*(c + d)]", "a*c + a*d + b*c + b*d"),
                Test("Expand[(a + b)*(c + d)*(f + g)]", "a*c*f + a*d*f + b*c*f + b*d*f + a*c*g + a*d*g + b*c*g + b*d*g"),
                Test("Expand[1/(s^3 + s), s]", "1/s - s/(s^2 + 1)"),
                
                // Factor.
                //KV("Factor[A*x^2 + B*x + C]", "(x - ((-B + Sqrt[B^2 - 4*A*C])/(2*A)))*(x - ((-B - Sqrt[B^2 - 4*A*C])/(2*A)))"),
                Test("Factor[x^2 - x, x]", "x*(x - 1)"),
                Test("Factor[x^4 - x^2, x]", "x^2*(x^2 - 1)"),
                Test("Factor[A*Exp[x] + B*Exp[x] + C*Sin[x] + D*Sin[x]]", "(A + B)*Exp[x] + (C + D)*Sin[x]"),
                Test("Factor[A*Exp[x] + B*Exp[x] + A*Sin[x] + B*Sin[x]]", "(A + B)*(Exp[x] + Sin[x])"),

                // Exponential functions.
                Test("Ln[Exp[2]]", "2"),
                Test("Log[10^3, 10]", "3"),
                Test("Ln[a^b]/b", "Ln[a]"),
                Test("Ln[Exp[x]]", "x"),
                Test("Log[b^x, b]", "x"),
                Test("Log[x, 3]", "Ln[x]/Ln[3]"),
                Test("Ln[x*y]", "Ln[x] + Ln[y]"),
                Test("Ln[x/y]", "Ln[x] - Ln[y]"),
                Test("Ln[x*y^2]", "Ln[x] - 2*Ln[y]"),

                //// Hyperbolic functions.
                Test("ArcSinh[Sinh[Pi/6]]", "Pi/6"),
                Test("ArcCosh[Cosh[Pi/6]]", "Pi/6"),
                Test("ArcTanh[Tanh[Pi/6]]", "Pi/6"),
                Test("ArcSech[Sech[Pi/6]]", "Pi/6"),
                Test("ArcCsch[Csch[Pi/6]]", "Pi/6"),
                Test("ArcCoth[Coth[Pi/6]]", "Pi/6"),
                //KV("Exp[x] + Exp[-x]", "2*Cosh[x]"),
                //KV("Exp[2*x] - Exp[-2*x]", "2*Sinh[2*x]"),
                //KV("(Exp[x] - Exp[-x])/(Exp[x] + Exp[-x])", "Tanh[x]"),
                //KV("(Exp[2*x] + Exp[-2*x])/(Exp[2*x] - Exp[-2*x])", "Coth[2*x]"),
                //KV("2/(Exp[x] + Exp[-x])", "Sech[x]"),
                //KV("3/(Exp[2*x] - Exp[-2*x])", "1.5*Csch[2*x]"),

                //// Trig functions.
                Test("ArcSin[Sin[Pi/6]]", "Pi/6"),
                Test("ArcCos[Cos[Pi/6]]", "Pi/6"),
                Test("ArcTan[Tan[Pi/6]]", "Pi/6"),
                Test("ArcSec[Sec[Pi/6]]", "Pi/6"),
                Test("ArcCsc[Csc[Pi/6]]", "Pi/6"),
                Test("ArcCot[Cot[Pi/6]]", "Pi/6"),
                //KV("Sin[x]/Cos[x]", "Tan[x]"),
                //KV("Cos[x^2]/Sin[x^2]", "Cot[x^2]"),
                //KV("Tan[x]*Cos[x]", "Sin[x]"),
                //KV("Sin[x]^2 + Cos[x]^2", "1"),
                //KV("y*Sin[x]^2 + y*Cos[x]^2", "y"),

                // Derivatives.
                Test("D[Sin[x], x]", "Cos[x]"),
                Test("D[Cos[x], x]", "-Sin[x]"),
                Test("D[Tan[x], x]", "Sec[x]^2"),
                Test("D[Sec[x], x]", "Sec[x]*Tan[x]"),
                Test("D[Csc[x], x]", "-Csc[x]*Cot[x]"),
                Test("D[Cot[x], x]", "-Csc[x]^2"),

                Test("D[Sinh[A*x], x]", "A*Cosh[A*x]"),
                Test("D[Cosh[A*x + B], x]", "A*Sinh[A*x + B]"),
                Test("D[Tanh[A*x^2 + B*x + C], x]", "(2*A*x + B)*Sech[A*x^2 + B*x + C]^2"),
                Test("D[Sech[x], x]", "-Sech[x]*Tanh[x]"),
                Test("D[Csch[x], x]", "-Csch[x]*Coth[x]"),
                Test("D[Coth[x], x]", "-Csch[x]^2"),
                
                Test("D[A*x + B*x^2, x]", "A + 2*B*x"),
                Test("D[(x^2 + 1)^5, x]", "5*(2*x)*(x^2 + 1)^4"),
                Test("D[Ln[x], x]", "1/x"),
                Test("D[Ln[Abs[x]], x]", "Abs[x]/x^2"),
                Test("D[(x + 1)^2, x]", "2*(x + 1)"),
                Test("D[Exp[x^2], x]", "Exp[x^2]*2*x"),
                Test("D[Exp[x^5], x]", "Exp[x^5]*5*x^4"),

                // Solve.
                Test("Solve[y == A*x + B, x]", "x -> (y - B)/A"),
                Test("Solve[{2*x + 4*y == 8, x == 2*y + 3}, {x, y}]", "{x -> 7/2, y -> 1/4}"),
                
                // NSolve.
                Test("NSolve[x == Cos[x], x->0.5]", "x->0.739085"),
                Test("NSolve[2 == Exp[x] - Exp[-x], x->0.5]", "x->0.881374"),
                Test("NSolve[{y == x^2, x^2 + y^2 == 1}, {x->1, y->1}]", "{x->0.786, y->0.618}"),
                Test("NSolve[{y == x^2, x^2 + y^2 == 1, z == x + y}, {x->1, y->1, z->1}]", "{x->0.786151, y->0.618034, z->1.40419}"),
                Test("NSolve[{z == x^2, x^2 + y^2 == 1, z == y}, {x->1, y->1, z->1}]", "{x->0.786151, y->0.618034, z->0.618034}"),
                Test("NSolve[{Exp[x + y] == 1, Exp[x - 2*y] == 2}, {x->0.5, y->0.5}]", "{x->0.231049, y->-0.231049}"),
                Test("NSolve[(((0.01*Vo) + (-0.01*1) + (1E-12*Exp[(38.6847*Vo)]) + (-1E-12*Exp[(-38.6847*Vo)]))==0), Vo->0.57]", "Vo->0.573208"),
                Test("NSolve[(((0.01*Vo) + (-0.01*1) + (1E-12*Exp[(38.6847*Vo)]) + (-1E-12*Exp[(-38.6847*Vo)]))==0), Vo->0]", "Vo->0.573208"),
                Test("NSolve[(Ln[((0.01*Vo) + (1E-12*Exp[(38.6847*Vo)]) + (-1E-12*Exp[(-38.6847*Vo)]))]==Ln[0.01]), Vo->0.01]", "Vo->0.573208"),

                // DSolve.
                Test("DSolve[D[y[t], t]==y[t], y[t], y[0]->1, t]", "y[t]->Exp[t]"),
                Test("DSolve[D[y[t], t]==y[t], y[t], y[0]->C, t]", "y[t]->C*Exp[t]"),
                Test("DSolve[D[y[t], t]==t, y[t], y[0]->0, t]", "y[t]->t^2/2"),
                Test("DSolve[D[y[t], t]==1, y[t], y[0]->0, t]", "y[t]->t"),
                Test("DSolve[D[D[y[t], t], t]==1, y[t], {y[0]->0, (D[y[t], t]:t->0)->0}, t]", "y[t]->t^2/2"),
                Test("DSolve[D[y[t], t]==-y[t], y[t], y[0]->1, t]", "y[t]->Exp[-t]"),
                Test("DSolve[D[y[t], t]==2*y[t], y[t], y[0]->1, t]", "y[t]->Exp[2*t]"),
                Test("DSolve[D[y[t], t]==y[t]/3, y[t], y[0]->C, t]", "y[t]->C*Exp[t/3]"),
                Test("DSolve[D[y[t], t] + y[t]==0, y[t], y[0]->1, t]", "y[t]->Exp[-t]"),
                Test("DSolve[D[y[t], t]==Sin[t], y[t], y[0]->0, t]", "y[t]->1 - Cos[t]"),
                
                Test("DSolve[I[y[t], t]==y[t], y[t], y[0]->1, t]", "y[t]->Exp[t]"),
                Test("DSolve[I[y[t], t]==t, y[t], y[0]->1, t]", "y[t]->1"),
                Test("DSolve[I[y[t], t]==t^2/2, y[t], y[0]->1, t]", "y[t]->t"),
                Test("DSolve[I[y[t], t]==Sin[t], y[t], y[0]->1, t]", "y[t]->Cos[t]"),
                Test("DSolve[I[y[t], t]==Sin[t] + t, y[t], y[0]->1, t]", "y[t]->Cos[t] + 1"),
            };

            System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
            timer.Start();

            int passed = 0;
            int total = 0;
            foreach (KeyValuePair<Expression, Expression> i in tests)
            {
                passed += RunTest(i.Key, i.Value) ? 1 : 0;
                total += 1;
            }

            Console.WriteLine("{0} of {1} passed", passed, total);

            Console.WriteLine("{0} ms", timer.ElapsedMilliseconds);
            Console.WriteLine("TransformCalls: {0}", TransformSet.TransformCalls);
            
            foreach (KeyValuePair<Expression, Func<double, double>> i in functions)
                RunTest(i.Key, i.Value);
        }
    }
}
