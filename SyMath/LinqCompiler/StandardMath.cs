using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace SyMath.LinqCompiler
{
    /// <summary>
    /// Implements standard functions for compiled methods.
    /// </summary>
    public class StandardMath
    {
        public static double Abs(double x) { return x < 0 ? -x : x; }
        public static double Sign(double x) { return x < 0 ? -1 : 1; }

        public static double Min(double x, double y) { return System.Math.Min(x, y); }
        public static double Max(double x, double y) { return System.Math.Max(x, y); }

        public static double Sin(double x) { return System.Math.Sin(x); }
        public static double Cos(double x) { return System.Math.Cos(x); }
        public static double Tan(double x) { return System.Math.Tan(x); }
        public static double Sec(double x) { return 1 / Cos(x); }
        public static double Csc(double x) { return 1 / Sin(x); }
        public static double Cot(double x) { return 1 / Tan(x); }

        public static double ArcSin(double x) { return System.Math.Asin(x); }
        public static double ArcCos(double x) { return System.Math.Acos(x); }
        public static double ArcTan(double x) { return System.Math.Atan(x); }
        public static double ArcSec(double x) { return ArcCos(1 / x); }
        public static double ArcCsc(double x) { return ArcSin(1 / x); }
        public static double ArcCot(double x) { return ArcTan(1 / x); }

        public static double Sinh(double x) { return System.Math.Sinh(x); }
        public static double Cosh(double x) { return System.Math.Cosh(x); }
        public static double Tanh(double x) { return System.Math.Tanh(x); }
        public static double Sech(double x) { return 1 / Cosh(x); }
        public static double Csch(double x) { return 1 / Sinh(x); }
        public static double Coth(double x) { return 1 / Tanh(x); }

        public static double ArcSinh(double x) { throw new NotImplementedException("ArcSinh"); }
        public static double ArcCosh(double x) { throw new NotImplementedException("ArcCosh"); }
        public static double ArcTanh(double x) { throw new NotImplementedException("ArcTanh"); }
        public static double ArcSech(double x) { return ArcCosh(1 / x); }
        public static double ArcCsch(double x) { return ArcSinh(1 / x); }
        public static double ArcCoth(double x) { return ArcTanh(1 / x); }

        public static double Sqrt(double x) { return System.Math.Sqrt(x); }
        public static double Exp(double x) { return System.Math.Exp(x); }
        public static double Ln(double x) { return System.Math.Log(x); }
        public static double Log(double x, double b) { return System.Math.Log(x, b); }

        public static double Floor(double x) { return System.Math.Floor(x); }
        public static double Ceiling(double x) { return System.Math.Ceiling(x); }
        public static double Round(double x) { return System.Math.Round(x); }

        public static double If(bool x, double t, double f) { return x ? t : f; }
        public static double If(double x, double t, double f) { return If(x != 0, t, f); }

        public static float Abs(float x) { return x < 0.0f ? -x : x; }
        public static float Sign(float x) { return x < 0.0f ? -1.0f : 1.0f; }

        public static float Min(float x, float y) { return System.Math.Min(x, y); }
        public static float Max(float x, float y) { return System.Math.Max(x, y); }

        public static float Sin(float x) { return (float)System.Math.Sin(x); }
        public static float Cos(float x) { return (float)System.Math.Cos(x); }
        public static float Tan(float x) { return (float)System.Math.Tan(x); }
        public static float Sec(float x) { return 1.0f / Cos(x); }
        public static float Csc(float x) { return 1.0f / Sin(x); }
        public static float Cot(float x) { return 1.0f / Tan(x); }

        public static float ArcSin(float x) { return (float)System.Math.Asin(x); }
        public static float ArcCos(float x) { return (float)System.Math.Acos(x); }
        public static float ArcTan(float x) { return (float)System.Math.Atan(x); }
        public static float ArcSec(float x) { return ArcCos(1.0f / x); }
        public static float ArcCsc(float x) { return ArcSin(1.0f / x); }
        public static float ArcCot(float x) { return ArcTan(1.0f / x); }

        public static float Sinh(float x) { return (float)System.Math.Sinh(x); }
        public static float Cosh(float x) { return (float)System.Math.Cosh(x); }
        public static float Tanh(float x) { return (float)System.Math.Tanh(x); }
        public static float Sech(float x) { return 1.0f / Cosh(x); }
        public static float Csch(float x) { return 1.0f / Sinh(x); }
        public static float Coth(float x) { return 1.0f / Tanh(x); }

        public static float ArcSinh(float x) { throw new NotImplementedException("ArcSinh"); }
        public static float ArcCosh(float x) { throw new NotImplementedException("ArcCosh"); }
        public static float ArcTanh(float x) { throw new NotImplementedException("ArcTanh"); }
        public static float ArcSech(float x) { return ArcCosh(1.0f / x); }
        public static float ArcCsch(float x) { return ArcSinh(1.0f / x); }
        public static float ArcCoth(float x) { return ArcTanh(1.0f / x); }

        public static float Sqrt(float x) { return (float)System.Math.Sqrt(x); }
        public static float Exp(float x) { return (float)System.Math.Exp(x); }
        public static float Ln(float x) { return (float)System.Math.Log(x); }
        public static float Log(float x, float b) { return (float)System.Math.Log(x, b); }

        public static float Floor(float x) { return (float)System.Math.Floor(x); }
        public static float Ceiling(float x) { return (float)System.Math.Ceiling(x); }
        public static float Round(float x) { return (float)System.Math.Round(x); }

        public static float If(bool x, float t, float f) { return x ? t : f; }
        public static float If(float x, float t, float f) { return If(x != 0.0f, t, f); }
    }
}
