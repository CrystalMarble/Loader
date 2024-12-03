using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Loader
{
    public class Logging
    {
        public static void Warn(string message)
        {
            Console.WriteLine($"[!] [CrystalMarble] {message}");
        }

        public static void Error(string message)
        {
            Console.WriteLine($"[x] [CrystalMarble] {message}");
        }

        public static void Info(string message)
        {
            Console.WriteLine($"[i] [CrystalMarble] {message}");
        }
    }
}
