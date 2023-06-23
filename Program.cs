namespace TgMediaSizeGroup
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("输入文件名");
            var customName = Console.ReadLine();
            customName ??= "";

            var inPath = new DirectoryInfo(args[0]);
            var outPath = new DirectoryInfo(args[0]+"_NGZ");
            if(!outPath.Exists ) { outPath.Create(); }
            var groups = FileGrouper.GetFileGroups(inPath.GetFiles("*", SearchOption.AllDirectories));

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
    }
}