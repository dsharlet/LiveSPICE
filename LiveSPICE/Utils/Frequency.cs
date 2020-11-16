using MathNet.Numerics.IntegralTransforms;
using System;
using System.Numerics;
using System.Text;

namespace LiveSPICE
{
    public class Frequency
    {
        private static string[] Notes = { "C", "C\u266f", "D", "D\u266f", "E", "F", "F\u266f", "G", "G\u266f", "A", "A\u266f", "B" };
        public static string ToNote(double f, double A4)
        {
            // Halfsteps above C0
            double halfsteps = (Math.Log(f / A4, 2.0) + 5.0) * 12.0 - 3.0;
            if (halfsteps < 0 || double.IsNaN(halfsteps) || double.IsInfinity(halfsteps))
                return "";

            int note = (int)Math.Round(halfsteps) % 12;
            int octave = (int)Math.Round(halfsteps) / 12;
            int cents = (int)Math.Round((halfsteps - Math.Round(halfsteps)) * 100);

            StringBuilder sb = new StringBuilder(Notes[note]);
            sb.Append(IntToSubscript(octave));
            sb.Append(' ');
            if (cents >= 0)
                sb.Append('+');
            sb.Append(cents);
            sb.Append('\u00A2');
            return sb.ToString();
        }

        private static string IntToSubscript(int x)
        {
            string chars = x.ToString();

            StringBuilder ret = new StringBuilder();
            foreach (char i in chars)
            {
                if (i == '-')
                    ret.Append((char)0x208B);
                else
                    ret.Append((char)(0x2080 + i - '0'));
            }
            return ret.ToString();
        }

        public static double Estimate(double[] Samples, int Decimate, out double Phase)
        {
            Complex[] data = DecimateSignal(Samples, Decimate);
            int N = data.Length;
            Fourier.Forward(data);
            // Zero the DC bin.
            data[0] = 0.0;

            double f = 0.0;
            double max = 0.0;
            Phase = 0.0;

            // Find largest frequency in FFT.
            for (int i = 1; i < N / 2 - 1; ++i)
            {
                double x;
                Complex m = LogParabolaMax(data[i - 1], data[i], data[i + 1], out x);

                if (m.Magnitude > max)
                {
                    max = m.Magnitude;
                    f = i + x;
                    Phase = m.Phase;
                }
            }

            // Check if this is a harmonic of another frequency (the fundamental frequency).
            double f0 = f;
            for (int h = 2; h < 5; ++h)
            {
                int i = (int)Math.Round(f / h);
                if (i >= 1)
                {
                    double x;
                    Complex m = LogParabolaMax(data[i - 1], data[i], data[i + 1], out x);

                    if (m.Magnitude * 5.0 > max)
                    {
                        f0 = f / h;
                        Phase = m.Phase;
                    }
                }
            }

            return f0;
        }

        private static double Hann(int i, int N) { return 0.5 * (1.0 - Math.Cos((2.0 * Math.PI * i) / (N - 1))); }

        // Fit parabola to 3 bins and find the maximum.
        private static Complex LogParabolaMax(Complex A, Complex B, Complex C, out double x)
        {
            double a = A.Magnitude;
            double b = B.Magnitude;
            double c = C.Magnitude;

            if (b > a && b > c)
            {
                // Parabola fitting is more accurate in log magnitude.
                a = Math.Log(a);
                b = Math.Log(b);
                c = Math.Log(c);

                // Maximum location.
                x = (a - c) / (2.0 * (a - 2.0 * b + c));

                // Maximum value.
                return Complex.FromPolarCoordinates(
                    Math.Exp(b - x * (a - c) / 4.0),
                    (B - x * (A - C) / 4.0).Phase);
            }
            else
            {
                x = 0.0;
                return B;
            }
        }

        private static Complex[] DecimateSignal(double[] Block, int Decimate)
        {
            int N = Block.Length / Decimate;
            Complex[] data = new Complex[N];

            // Decimate input audio with low pass filter.
            for (int i = 0; i < N; ++i)
            {
                double v = 0.0;
                for (int j = 0; j < Decimate; ++j)
                    v += Block[i * Decimate + j];
                data[i] = new Complex(v * Hann(i, N), 0.0);
            }
            return data;
        }

        /// <summary>
        /// Get the parameter for a first-order IIR filter.
        /// </summary>
        /// <param name="timestep">The time between steps.</param>
        /// <param name="halflife">The time to decay by half.</param>
        public static double DecayRate(double timestep, double halflife)
        {
            return Math.Exp(timestep / halflife * Math.Log(0.5));
        }
    }
}