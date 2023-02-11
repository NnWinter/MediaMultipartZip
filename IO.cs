using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TgMediaSizeGroup
{
    internal class IO
    {
        public static System.Timers.Timer consoleUpdater = new System.Timers.Timer(1000);
        public static void ConsoleBeginUpdate()
        {
            consoleUpdater.AutoReset = true;
            consoleUpdater.Elapsed += (obj, arg) => { IO.ConsoleUpdate(); };
            consoleUpdater.Enabled = true;
            consoleUpdater.Start();
        }
        public static void ConsoleStopUpdate()
        {
            consoleUpdater.Enabled = false;
            consoleUpdater.Stop();
        }
        public static List<string> ConsoleLines = new List<string>();
        public static void ConsoleAddLine(string line)
        {
            ConsoleLines.Add(line);
        }
        public static void ConsoleUpdate()
        {
            Console.Clear();
            Console.CursorLeft = 0;
            Console.CursorTop = 0;
            foreach (var str in ConsoleLines)
            {
                Console.WriteLine(str);
            }
        }
        public static void ConsoleReplaceLast(string line)
        {
            ConsoleLines.RemoveAt(ConsoleLines.Count - 1);
            ConsoleAddLine(line);
        }
        public static void LogLines(string[] lines)
        {
            try
            {
                for (int i = 0; i < lines.Length; i++)
                {
                    lines[i] = DateTime.Now.ToString() + " => " + lines[i];
                }
                File.AppendAllLines("log.ini", lines);
            }
            catch (Exception ex) { Console.WriteLine("错误: 写入日志文件失败 - " + ex.ToString()); Console.WriteLine("按回车退出"); Console.ReadLine(); Environment.Exit(1); }
        }
        public static void LogError(string message, bool isFatal)
        {
            IO.ConsoleStopUpdate();
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(message);
            Console.ResetColor();
            LogLines(new string[] { message });
            Console.WriteLine("查看 log.ini 获取更多信息");
            if (isFatal) { Console.WriteLine("由于遇到致命错误，按回车退出程序..."); Console.ReadLine(); Environment.Exit(1); }
        }
    }
}
