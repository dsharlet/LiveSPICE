using System;
using Util;

namespace LiveSPICE.Utils
{
    public static class ILogExtensions
    {
        public static void Error(this ILog log, string message) => log.WriteLine(MessageType.Error, message);

        public static void Warning(this ILog log, string message) => log.WriteLine(MessageType.Warning, message);

        public static void Info(this ILog log, string message) => log.WriteLine(MessageType.Info, message);

        public static void Verbose(this ILog log, string message) => log.WriteLine(MessageType.Verbose, message);

        public static void Exception(this ILog log, Exception e) => log.WriteLine(MessageType.Error, "Exception: " + e.Message);
    }
}
