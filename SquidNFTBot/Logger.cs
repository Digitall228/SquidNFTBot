using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SquidNFTBot
{
    public static class Logger
    {
        public delegate void Log(string text, ConsoleColor color = ConsoleColor.White);
        public static Log logAdd;

        private static object locker { get; set; } = new object();

        static Logger()
        {
            logAdd = LogAdd;
        }
        private static void LogAdd(string text, ConsoleColor color)
        {
            lock (locker)
            {
                Console.ForegroundColor = color;
                Console.WriteLine($"{DateTime.UtcNow}: {text}");
                Console.ForegroundColor = ConsoleColor.White;
            }
        }
    }
}
