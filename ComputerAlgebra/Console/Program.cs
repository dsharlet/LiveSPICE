using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ComputerAlgebra;

namespace Console
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
                    System.Console.WriteLine();

                    if (s == "exit")
                        break;

                    Expression E = s;

                    System.Console.WriteLine(Arrow.New(E, E.Evaluate()).ToPrettyString());
                    System.Console.WriteLine();
                }
                catch (Exception Ex)
                {
                    System.Console.WriteLine(Ex.Message);
                }
            }
        }
    }
}
