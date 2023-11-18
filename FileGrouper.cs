using Ionic.Zip;
using System.Text;
using System.Xml.Linq;
using static TgMediaSizeGroup.Program;

namespace TgMediaSizeGroup
{
    internal class FileGrouper
    {
        public static readonly long GROUP_SIZE = 3932160000L;
        public static List<FileGroup> GetFileGroups(FileInfo[] files, EPackStyle packStyle)
        {
            // 根据打包方式获取文件列表
            List<FileInfo> GetFileInfo(FileInfo[] x, EPackStyle y)
            {
                switch (y)
                {
                    // 文件尺寸是从大到小
                    case EPackStyle.ByFileSize: return x.OrderByDescending(n => n.Length).ToList();
                    // 文件名是从小到大
                    case EPackStyle.ByFileName: return x.OrderBy(n => n.Name).ToList();
                    // 无效的排序方式应该抛出异常
                    default: throw new Exception($"错误: 无效的排序方式 - {y}");
                }
            }
            var sort = GetFileInfo(files, packStyle);

            // 开始分组
            var groups = new List<FileGroup>();
            int id = 1;
            while (sort.Count() > 0)
            {
                var group = new List<FileInfo>();

                // 取出列表最大文件
                var head = sort.First();
                if (!sort.Remove(head)) { IO.LogError($"错误: 从列表中移除最大文件失败 - {head.FullName}", true); }
                group.Add(head);

                // 寻找能塞入的下一个文件
                var matches = new List<FileInfo>();
                var sum = head.Length;
                foreach(var file in sort)
                {
                    if((sum + file.Length) <= GROUP_SIZE)
                    {
                        matches.Add(file);
                        sum += file.Length;
                        group.Add(file);
                    }
                }
                // 从列表中删除已匹配的项
                foreach(var file in matches)
                {
                    if (!sort.Remove(file)) { IO.LogError($"错误: 从列表中删除匹配项失败 - {file.FullName}", true); };
                }

                // 添加组
                groups.Add(new FileGroup(group, id++));
            }

            return groups;
        }
        public static XElement GroupsToXml(List<FileGroup> groups)
        {
            XElement root = new XElement("root");
            foreach(var group in groups)
            {
                XElement zip = new XElement("Zip", 
                    new XAttribute("ID", group.GroupId), 
                    new XAttribute("Length", group.Length),
                    new XAttribute("VolumeCount", group.VolumeCount)
                    );
                foreach(var file in group.Group)
                {
                    zip.Add(new XElement("File",
                        new XAttribute("FileName", file.FullName),
                        new XAttribute("FileSize", file.Length),
                        new XAttribute("Write", file.LastWriteTime),
                        new XAttribute("Creation", file.CreationTime),
                        new XAttribute("Access", file.LastAccessTime)
                        ));
                }
                root.Add(zip);
            }
            return root;
        }
    }
    class FileGroup
    {
        public List<FileInfo> Group { get; init; }
        public long Length { get; init; }
        public bool IsSingle { get; init; }
        public int VolumeCount { get; init; }
        public int GroupId { get; init; }
        public FileGroup(List<FileInfo> group, int id) 
        {
            Group = group;
            Length = group.Sum(x => x.Length);
            IsSingle = group.Count == 1;
            VolumeCount = DivideLongCeil(Length, FileGrouper.GROUP_SIZE);
            GroupId = id;
        }
        public void Compress(DirectoryInfo outdir, int totalZips, DirectoryInfo indir, string customName)
        {
            var filename = customName + "." + GroupId.ToString().PadLeft(GetDp(totalZips), '0') + (VolumeCount > 1 ? ("x" + VolumeCount) : "") + ".zip";
            try
            {
                var path = Path.Combine(outdir.FullName, filename);
                var zip = new ZipFile(path);
                zip.AlternateEncoding= Encoding.UTF8;
                zip.UseZip64WhenSaving = Zip64Option.Always;
                zip.CompressionMethod = CompressionMethod.Deflate;

                // 分卷尺寸
                if (IsSingle) { zip.MaxOutputSegmentSize64 = FileGrouper.GROUP_SIZE; }
                IO.ConsoleAddLine($"文件 \"{filename}\" : ");

                IO.LogLines(new string[] { $"文件 \"{filename}\" : 处理中 {Group.Count} 个文件, 总大小 {Length}" });

                // 进度显示
                zip.SaveProgress += (obj, arg) =>
                {
                    string output = $"文件 \"{filename}\" :";
                    if (arg.EventType == ZipProgressEventType.Saving_EntryBytesRead)
                    {
                        string progress = $"{arg.BytesTransferred*100 / arg.TotalBytesToTransfer}% = {arg.BytesTransferred} / {arg.TotalBytesToTransfer} | ";
                        output = $"文件 \"{filename}\" : {progress}{arg.CurrentEntry.FileName.Split('/').Last()}";
                    }
                    else if(arg.EventType == ZipProgressEventType.Saving_Completed)
                    {
                        output = $"文件 \"{filename}\" : [已保存]";
                    }
                    else
                    {
                        output = $"文件 \"{filename}\" : []";
                    }
                    IO.ConsoleReplaceLast(output);
                };

                // 添加文件
                long count = 0L, total = 0L;
                foreach (var file in Group)
                {
                    string entryDir = file.DirectoryName;
                    entryDir = entryDir.Replace(indir.FullName, "");
                    zip.AddFile(file.FullName, entryDir);
                    count++; total+= file.Length;
                }
                zip.Save();

                string output = $"文件 \"{filename}\" : 已压缩 {count} 个文件，总大小 {total}";
                IO.LogLines(new string[] { output });
                IO.ConsoleReplaceLast(output);
            }
            catch(Exception ex)
            {
                IO.LogError($"错误: 压缩文件时发生了一个错误 - \"{filename}\" - {ex.Message}", true);
            }
        }
        static int DivideLongCeil(long numerator, long denominator)
        {
            if (numerator == 0 || denominator == 0) { return 0; }
            int count = 1;
            while (numerator > denominator)
            {
                count++;
                numerator -= denominator;
            }
            return count;
        }
        static int GetDp(int max)
        {
            return (int)Math.Log10(max + 1) + 1;
        }
    }
}
