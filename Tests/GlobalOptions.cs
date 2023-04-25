using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Binding;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiveSPICE.CLI
{
    internal static class GlobalOptions
    {
        public static Option<int> SampleRate { get; } = new Option<int>("--sampleRate", () => 48000, "Sample Rate");
        public static Option<int> Oversample { get; } = new Option<int>("--oversample", () => 8, "Oversample");
        public static Option<int> Iterations { get; } = new Option<int>("--iterations", () => 8, "Iterations");
        public static Option<bool> Verbose { get; } = new Option<bool>(new[] { "-v", "--verbose" }, "Verbose output");
    }
}
