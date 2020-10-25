using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace Util
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
        void WriteLines(MessageType Type, IEnumerable<string> Lines);
    }

    public abstract class Log : ILog
    {
        private DateTime t0 = DateTime.Now;

        private string tag = "";
        /// <summary>
        /// Message tag format string:
        /// 
        /// {Type} - MessageType of the message.
        /// {ThreadId} - Name or id of the thread writing the message.
        /// {Time} - Time the message is being written.
        /// 
        /// </summary>
        public string Tag
        {
            get { return tag; }
            set
            {
                tag = value
                    .Replace("{Type}", "{0}")
                    .Replace("{ThreadId}", "{1}")
                    .Replace("{Time}", "{2}");
            }
        }

        private MessageType verbosity = MessageType.Verbose;
        /// <summary>
        /// Threshold for message types to be written.
        /// </summary>
        public MessageType Verbosity { get { return verbosity; } set { verbosity = value; } }

        protected abstract void WriteLine(string Text);

        public void WriteLine(MessageType Type, string Format, params object[] Args)
        {
            if (verbosity >= Type)
                WriteLine(
                    String.Format(tag,
                        Type,
                        Thread.CurrentThread.Name != null ? Thread.CurrentThread.Name : Thread.CurrentThread.ManagedThreadId.ToString(),
                        DateTime.Now - t0) +
                    String.Format(
                        Format,
                        Args));
        }
        public void WriteLines(MessageType Type, IEnumerable<string> Lines)
        {
            if (verbosity >= Type)
                foreach (string i in Lines)
                    WriteLine(Type, i);
        }
        public void WriteLine(string Format, params object[] Args) { WriteLine(MessageType.Info, Format, Args); }

        private static Log global = null;
        public static Log Global
        {
            get
            {
                if (global == null)
                {
                    string name = Path.GetFileNameWithoutExtension(Process.GetCurrentProcess().ProcessName);
                    string path = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        name,
                        DateTime.Now.ToString("yyyy dd M HH mm ss") + ".txt");
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                    global = new FileLog(path);
                }
                return global;
            }
            set { global = value; }
        }
    }

    /// <summary>
    /// No-op Log implementation 
    /// </summary>
    public class NullLog : ILog
    {
        void ILog.WriteLine(MessageType Type, string Text, params object[] Format) { }
        void ILog.WriteLines(MessageType Type, IEnumerable<string> Lines) { }
    }

    /// <summary>
    /// Log implementation targeting a StringBuilder.
    /// </summary>
    public class StringLog : Log
    {
        private StringBuilder s = new StringBuilder();
        public StringBuilder String { get { return s; } set { s = value; } }

        protected override void WriteLine(string Text) { s.AppendLine(Text); }
    }

    /// <summary>
    /// Log implementation targeting a file.
    /// </summary>
    public class FileLog : Log
    {
        private FileStream file;
        private StreamWriter writer;

        public FileLog(string FileName)
        {
            Tag = "[{ThreadId}]";

            file = new FileStream(FileName, FileMode.Create);
            writer = new StreamWriter(file);
        }

        protected override void WriteLine(string Text)
        {
            lock (file)
            {
                writer.WriteLine(Text);
                writer.Flush();
                file.Flush();
            }
        }
    }

    /// <summary>
    /// System.Console Log implementation.
    /// </summary>
    public class ConsoleLog : Log
    {
        protected override void WriteLine(string Text)
        {
            Console.WriteLine(Text);
        }
    }
}
