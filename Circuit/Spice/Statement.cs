using ComputerAlgebra;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Circuit.Spice
{
    public class Statement
    {
        // Parsing quantities.
        private static readonly Dictionary<string, double> Prefixes = new Dictionary<string, double>()
        {
            { "F", 1e-15 },
            { "P", 1e-12 },
            { "N", 1e-9 },
            { "U", 1e-6 },
            { "M", 1e-3 },
            { "K", 1e+3 },
            { "MEG", 1e+6 },
            { "G", 1e+9 },
            { "T", 1e+12 },
        };

        private static readonly Regex Quantity = new Regex(@"([-+]?[0-9]*\.?[0-9]+([eE][-+]?[0-9]+)?)(F|P|N|U|M|K|MEG|G|T)?.*", RegexOptions.IgnoreCase);
        public static Expression ParseValue(string s)
        {
            Match m = Quantity.Match(s);
            if (m.Success)
            {
                double v = double.Parse(m.Groups[1].Value);
                double p = 1;
                if (m.Groups[3].Success)
                    p = Prefixes[m.Groups[3].Value.ToUpper()];
                return v * p;
            }
            else
            {
                throw new Exception("Unable to parse quantity '" + s + "'.");
            }
        }
    }
}
