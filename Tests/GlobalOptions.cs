using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Binding;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Util;

namespace LiveSPICE.CLI
{
    internal static class GlobalOptions
    {
        public static Option<MessageType> Verbosity { get; } = new Option<MessageType>(new[] { "-v", "--verbosity" },() => MessageType.Info, "Output verbosity");
    }
}
