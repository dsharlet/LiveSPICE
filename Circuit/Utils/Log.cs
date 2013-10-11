using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Circuit
{
    public enum MessageType
    {
        Error,
        Warning,
        Info,
        Verbose,
    }

    public interface ILog
    {
        void WriteLine(MessageType Type, string Text, params object[] Format);
    }

    /// <summary>
    /// No-op ILog implementation 
    /// </summary>
    public class NullLog : ILog
    {
        void ILog.WriteLine(MessageType Type, string Text, params object[] Format) { }
    }

    /// <summary>
    /// System.Console ILog implementation.
    /// </summary>
    public class ConsoleLog : ILog
    {
        void ILog.WriteLine(MessageType Type, string Text, params object[] Format) 
        { 
            if (Type != MessageType.Info)
                System.Console.Write("[" + Type.ToString() + "]"); 
            System.Console.WriteLine(Text, Format);
        }
    }
}
