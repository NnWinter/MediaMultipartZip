namespace TgMediaSizeGroup
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("输入文件名");
            var customName = Console.ReadLine();
            customName ??= "";
            var packStyle = GetPackStyle();

            var inPath = new DirectoryInfo(args[0]);
            var outPath = new DirectoryInfo(args[0]+"_NGZ");
            if(!outPath.Exists ) { outPath.Create(); }
            var groups = FileGrouper.GetFileGroups(inPath.GetFiles("*", SearchOption.AllDirectories), packStyle);

            IO.ConsoleBeginUpdate();

            var xml = FileGrouper.GroupsToXml(groups);
            var xmlpath = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + "_" + customName + ".xml";
            xml.Save(xmlpath);

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
                Console.WriteLine("选择分卷方式\n[1]文件尺寸优先(适合有预览的随机存储)\n[2]文件名称优先(适合相片等顺序存储)");
                Console.Write(">");
                string? input = Console.ReadLine();
                if (input == null) { Console.WriteLine("输入不可为空"); continue; }
                bool valid = int.TryParse(input, out int choice);
                if (!valid) { Console.WriteLine("输入不是有效的数字"); continue; }
                switch (choice)
                {
                    case 1: return EPackStyle.ByFileSize;
                    case 2: return EPackStyle.ByFileName;
                    default: Console.WriteLine("无效的选项"); continue;
                }
            }
        }
        public enum EPackStyle {
            ByFileSize,
            ByFileName
        }
    }
}