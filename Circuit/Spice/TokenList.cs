using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Circuit.Spice
{
    public class TokenList : List<string>
    {
        private string text;
        public string Text { get { return text; } }

        private int lineCount = 0;
        public int LineCount { get { return lineCount; } }

        // Whitespace characters.
        private static char[] Whitespace = new char[] { ' ', '\t', '(', ')', ',', '=' };

        public TokenList(string Line)
        {
            text = Line.ToString().TrimEnd();

            foreach (string i in text.Split(Whitespace))
            {
                string tok = i.Trim().ToUpper();
                if (tok.Length == 0)
                    continue;

                // * at the beginning of the line is a comment.
                if (tok.StartsWith("*") && Count == 0)
                    return;

                // Truncate tokens at semicolon comments.
                int semi = tok.IndexOf(';');
                if (semi > 0)
                {
                    Add(tok.Substring(0, semi));
                    return;
                }
                else if (semi == 0)
                    return;

                Add(tok);
            }
        }

        public static TokenList ReadLine(StreamReader Stream)
        {
            int count = 0;
            StringBuilder line = new StringBuilder("");
            do
            {
                string l = Stream.ReadLine().TrimEnd(Whitespace);
                ++count;
                if (l.StartsWith("+"))
                    l = l.Substring(1);
                line.Append(l + " ");
            } while (Stream.Peek() == '+');

            return new TokenList(line.ToString()) { lineCount = count };
        }
    }
}
