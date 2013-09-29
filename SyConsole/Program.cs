using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SyMath;

namespace SyMathTests
{
    class Program
    {        
        static void Main(string[] args)
        {
            while (true)
            {
                try
                {
                    System.Console.Write("> ");
                    string s = System.Console.ReadLine();

                    if (s == "exit")
                        break;

                    Expression E = s;

                    System.Console.WriteLine();
                    System.Console.WriteLine(Equal.New(E, E.Evaluate()).ToPrettyString());
                }
                catch (System.Exception Ex)
                {
                    System.Console.WriteLine(Ex.Message);
                }
            }
        }
    }
}
