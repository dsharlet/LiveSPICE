using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace ComputerAlgebra.LinqCompiler
{
    /// <summary>
    /// Implements standard functions for compiled methods.
    /// </summary>
    public class StandardMath
    {
        public static double Abs(double x) { return x < 0 ? -x : x; }
        public static double Sign(double x) { return x < 0 ? -1 : 1; }

        public static double Min(double x, double y) { return Math.Min(x, y); }
        public static double Max(double x, double y) { return Math.Max(x, y); }

        public static double Sin(double x) { return Math.Sin(x); }
        public static double Cos(double x) { return Math.Cos(x); }
        public static double Tan(double x) { return Math.Tan(x); }
        public static double Sec(double x) { return 1 / Cos(x); }
        public static double Csc(double x) { return 1 / Sin(x); }
        public static double Cot(double x) { return 1 / Tan(x); }

        public static double ArcSin(double x) { return Math.Asin(x); }
        public static double ArcCos(double x) { return Math.Acos(x); }
        public static double ArcTan(double x) { return Math.Atan(x); }
        public static double ArcSec(double x) { return ArcCos(1 / x); }
        public static double ArcCsc(double x) { return ArcSin(1 / x); }
        public static double ArcCot(double x) { return ArcTan(1 / x); }

        public static double Sinh(double x) { return Math.Sinh(x); }
        public static double Cosh(double x) { return Math.Cosh(x); }
        public static double Tanh(double x) { return Math.Tanh(x); }
        public static double Sech(double x) { return 1 / Cosh(x); }
        public static double Csch(double x) { return 1 / Sinh(x); }
        public static double Coth(double x) { return 1 / Tanh(x); }

        public static double ArcSinh(double x) { return Math.Log(x + Math.Sqrt(x * x + 1)); }
        public static double ArcCosh(double x) { return Math.Log(x + Math.Sqrt(x * x - 1)); }
        public static double ArcTanh(double x) { return (Math.Log(1 + x) - Math.Log(1 - x)) / 2; }
        public static double ArcSech(double x) { return ArcCosh(1 / x); }
        public static double ArcCsch(double x) { return ArcSinh(1 / x); }
        public static double ArcCoth(double x) { return ArcTanh(1 / x); }

        public static double Sqrt(double x) { return Math.Sqrt(x); }
        public static double Exp(double x) { return Math.Exp(x); }
        public static double Ln(double x) { return Math.Log(x); }
        public static double Log(double x, double b) { return Math.Log(x, b); }

        public static double Floor(double x) { return Math.Floor(x); }
        public static double Ceiling(double x) { return Math.Ceiling(x); }
        public static double Round(double x) { return Math.Round(x); }

        public static double If(bool x, double t, double f) { return x ? t : f; }
        public static double If(double x, double t, double f) { return If(x != 0, t, f); }

        public static float Abs(float x) { return x < 0.0f ? -x : x; }
        public static float Sign(float x) { return x < 0.0f ? -1.0f : 1.0f; }

        public static float Min(float x, float y) { return Math.Min(x, y); }
        public static float Max(float x, float y) { return Math.Max(x, y); }

        public static float Sin(float x) { return (float)Math.Sin(x); }
        public static float Cos(float x) { return (float)Math.Cos(x); }
        public static float Tan(float x) { return (float)Math.Tan(x); }
        public static float Sec(float x) { return 1.0f / Cos(x); }
        public static float Csc(float x) { return 1.0f / Sin(x); }
        public static float Cot(float x) { return 1.0f / Tan(x); }

        public static float ArcSin(float x) { return (float)Math.Asin(x); }
        public static float ArcCos(float x) { return (float)Math.Acos(x); }
        public static float ArcTan(float x) { return (float)Math.Atan(x); }
        public static float ArcSec(float x) { return ArcCos(1.0f / x); }
        public static float ArcCsc(float x) { return ArcSin(1.0f / x); }
        public static float ArcCot(float x) { return ArcTan(1.0f / x); }

        public static float Sinh(float x) { return (float)Math.Sinh(x); }
        public static float Cosh(float x) { return (float)Math.Cosh(x); }
        public static float Tanh(float x) { return (float)Math.Tanh(x); }
        public static float Sech(float x) { return 1.0f / Cosh(x); }
        public static float Csch(float x) { return 1.0f / Sinh(x); }
        public static float Coth(float x) { return 1.0f / Tanh(x); }

        public static float ArcSinh(float x) { return (float)Math.Log(x + Math.Sqrt(x * x + 1)); }
        public static float ArcCosh(float x) { return (float)Math.Log(x + Math.Sqrt(x * x - 1)); }
        public static float ArcTanh(float x) { return (float)(Math.Log(1 + x) - Math.Log(1 - x)) / 2; }
        public static float ArcSech(float x) { return ArcCosh(1.0f / x); }
        public static float ArcCsch(float x) { return ArcSinh(1.0f / x); }
        public static float ArcCoth(float x) { return ArcTanh(1.0f / x); }

        public static float Sqrt(float x) { return (float)Math.Sqrt(x); }
        public static float Exp(float x) { return (float)Math.Exp(x); }
        public static float Ln(float x) { return (float)Math.Log(x); }
        public static float Log(float x, float b) { return (float)Math.Log(x, b); }

        public static float Floor(float x) { return (float)Math.Floor(x); }
        public static float Ceiling(float x) { return (float)Math.Ceiling(x); }
        public static float Round(float x) { return (float)Math.Round(x); }

        public static float If(bool x, float t, float f) { return x ? t : f; }
        public static float If(float x, float t, float f) { return If(x != 0.0f, t, f); }
    }
}
