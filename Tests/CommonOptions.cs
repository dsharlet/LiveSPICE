using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Circuit;

namespace LiveSPICE.Cli
{
    internal static class CommonOptions
    {
        public static Option<int> SampleRate { get; } = new Option<int>("--sampleRate", () => 48000, "Sample Rate");

        public static Option<int> Oversample { get; } = new Option<int>("--oversample", () => 8, "Oversample");

        public static Option<int> Iterations { get; } = new Option<int>("--iterations", () => 8, "Iterations");

        public static Option<Quantity> Amplitude { get; } = new(
            name: "--amplitude",
            parseArgument: arg => 
            {
                return arg.Tokens.SingleOrDefault()?.Value is string v ? Quantity.Parse(v, Units.V) : new Quantity(.5d, Units.V);
            },
            isDefault: true, 
            description: "Test signal amplitude");

        public static Option<Dictionary<string, double>> Parameters { get; } = new(
            name: "--parameters", 
            parseArgument: arg =>
            {
                return arg.Tokens.Select(t => t.Value.Split('=')).ToDictionary(kv => kv[0], kv => double.Parse(kv[1]), StringComparer.OrdinalIgnoreCase);
            }, 
            isDefault: true, 
            description: "Variable parameters e.g. Potentiometers")
            { 
                AllowMultipleArgumentsPerToken = true 
            };
    }
}
