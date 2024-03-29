﻿namespace TgMediaSizeGroup
{
    internal class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("未找到拖入的文件夹路径，手动输入文件夹路径：");
                string str = Console.ReadLine();
                args = new string[] { str };
            }

            Console.WriteLine("输入文件名");
            var customName = Console.ReadLine();
            customName ??= "";
            var packStyle = GetPackStyle();
            SetGroupSize();

            var inPath = new DirectoryInfo(args[0]);
            var outPath = new DirectoryInfo(args[0]+"_NGZ");
            if(!outPath.Exists ) { outPath.Create(); }
            var groups = FileGrouper.GetFileGroups(inPath.GetFiles("*", SearchOption.AllDirectories), packStyle);

            IO.ConsoleBeginUpdate();

            var xml = FileGrouper.GroupsToXml(groups);
            var xmlpath = customName + ".xml";
            xml.Save(xmlpath);
            var json = FileGrouper.GroupsToJson(groups);
            var jsonpath = customName + ".json";
            File.WriteAllText(jsonpath, json);

            foreach (var group in groups) {
                group.Compress(outPath, groups.Count, inPath, customName);
            }

            Console.WriteLine("运行结束，按回车退出");
            IO.ConsoleStopUpdate();
            Console.ReadLine();
        }
        /// <summary>
        /// 使用Console让用户选择不同的分卷方式
        /// </summary>
        /// <returns>分卷方式</returns>
        static EPackStyle GetPackStyle()
        {
            while (true)
            {
                Console.WriteLine("选择分卷方式\n[1]文件尺寸优先(适合有预览的随机存储)\n[2]文件名称优先(适合相片等顺序存储)\n[3]文件修改日期优先");
                Console.Write(">");
                string? input = Console.ReadLine();
                if (input == null) { Console.WriteLine("输入不可为空"); continue; }
                bool valid = int.TryParse(input, out int choice);
                if (!valid) { Console.WriteLine("输入不是有效的数字"); continue; }
                switch (choice)
                {
                    case 1: return EPackStyle.ByFileSize;
                    case 2: return EPackStyle.ByFileName;
                    case 3: return EPackStyle.ByModify;
                    default: Console.WriteLine("无效的选项"); continue;
                }
            }
        }
        static void SetGroupSize()
        {
            while (true)
            {
                Console.WriteLine("设置分卷尺寸(byte)");
                Console.WriteLine("TG会员: 3932160000  TG非会员: 1992294400");
                Console.Write(">");
                string? input = Console.ReadLine();
                if (input == null) { Console.WriteLine("输入不可为空"); continue; }
                bool valid = long.TryParse(input, out long bytes);
                if (!valid|| bytes < 1024 * 1024 * 10) { Console.WriteLine("输入不是有效的数字"); continue; }
                var mib = bytes / 1024.0 / 1024.0;
                var mb = bytes / 1000.0 / 1000.0;
                Console.WriteLine($"分卷大小为 {mib}MiB = {mb}MB = {bytes}字节");
                Console.WriteLine("输入y确认, 其他键重新输入");
                var key = Console.ReadKey();
                if (!key.Key.Equals(ConsoleKey.Y)) { continue; }
                FileGrouper.SetGroupSize(bytes);
                break;
            }
        }
        public enum EPackStyle {
            ByFileSize,
            ByFileName,
            ByModify
        }
    }
}