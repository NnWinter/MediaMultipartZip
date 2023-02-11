namespace TgMediaSizeGroup
{
    internal class Program
    {
        
        static void Main(string[] args)
        {
            var inPath = new DirectoryInfo(@"");
            var outPath = new DirectoryInfo(@"");
            if(!outPath.Exists ) { outPath.Create(); }
            var groups = FileGrouper.GetFileGroups(inPath.GetFiles("*", SearchOption.AllDirectories));

            IO.ConsoleBeginUpdate();

            var xml = FileGrouper.GroupsToXml(groups);
            var xmlpath = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + "_" + inPath.Name + ".xml";
            xml.Save(xmlpath);

            foreach (var group in groups) {
                group.Compress(outPath, groups.Count, inPath);
            }

            Console.WriteLine("运行结束，按回车退出");
            IO.ConsoleStopUpdate();
            Console.ReadLine();
        }
    }
}