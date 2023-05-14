using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests.Genetic
{
    internal static class ArrayExtensions
    {
        static readonly Random R = new Random();

        public static int[] Permutate(this int[] source, int mutations)
        {
            var res = source[..];

            for (int i = 0; i < mutations; i++)
            {
                var srcIndex = R.Next(0, source.Length);
                var targetIdx = R.Next(0, source.Length);

                var tmp = res[targetIdx];
                res[targetIdx] = res[srcIndex];
                res[srcIndex] = tmp;
            }

            return res;
        }

        public static int[] Crossover(this int[] parent1, int[] parent2)
        {
            var length = parent1.Length;

            var a = R.Next(0, length);
            var b = R.Next(0, length);
            if (a > b)
            {
                var tmp = a;
                a = b; 
                b = tmp;
            }

            var child = Enumerable.Repeat(-1, length).ToArray();

            parent1[a..b].CopyTo(child, a);

            for (int i = a; i < b; i++)
            {
                var m = parent2[i];

                if (child.Contains(m))
                    continue;
                
                var newSpot = Array.IndexOf(parent2, parent1[i]);
                if (child[newSpot] != -1)
                {
                    newSpot = Array.IndexOf(parent2, child[newSpot]);
                }
                child[newSpot] = m;
            }

            var notCopied = parent2.Except(child).ToArray();

            var idx = 0;

            for (int i = 0; i < length; i++)
            {
                if (child[i] == -1)
                {
                    child[i] = notCopied[idx++];
                }
            }

            return child;

        }

        public static void Print(this int[] array)
        {
            Console.WriteLine($"[{string.Join(", ", array)}]");
        }
    }
}

