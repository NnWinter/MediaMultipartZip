using Ionic.Zip;
using MediaToolkit.Model;
using MediaToolkit;
using Newtonsoft.Json;
using System.Text;
using System.Xml.Linq;
using static TgMediaSizeGroup.Program;
using MetadataExtractor;

namespace TgMediaSizeGroup
{
    internal class FileGrouper
    {
        public static long GROUP_SIZE = 3932160000L;
        public static List<FileGroup> GetFileGroups(FileInfo[] files, EPackStyle packStyle)
        {
            List<FileInfo> sorted = GetFileInfo(files, packStyle);
            return GetGroups(sorted, packStyle);

            // 根据打包方式获取文件列表
            List<FileInfo> GetFileInfo(FileInfo[] x, EPackStyle y)
            {
                switch (y)
                {
                    // 文件尺寸是从大到小
                    case EPackStyle.ByFileSize: return x.OrderByDescending(n => n.Length).ToList();
                    // 文件名是从小到大
                    case EPackStyle.ByFileName: return x.OrderBy(n => n.Name).ToList();
                    // 文件修改日期从小到大
                    case EPackStyle.ByModify: return x.OrderBy(n => n.LastWriteTime).ToList();
                    // 无效的排序方式应该抛出异常
                    default: throw new Exception($"错误: 无效的排序方式 - {y}");
                }
            }

            // 分组
            List<FileGroup> GetGroups(List<FileInfo> sort, EPackStyle packStyle)
            {
                var groups = new List<FileGroup>();
                int id = 1;
                while (sort.Count() > 0)
                {
                    var group = new List<FileInfo>();

                    GetSizePack(ref group, ref sort);

                    // 从列表中删除已匹配的项
                    foreach (var file in group)
                    {
                        if (!sort.Remove(file)) { IO.LogError($"错误: 从列表中删除匹配项失败 - {file.FullName}", true); };
                    }

                    // 添加组
                    groups.Add(new FileGroup(group, id++));
                }
                return groups;

                // 添加到组
                void GetSizePack(ref List<FileInfo> group, ref List<FileInfo> sort)
                {
                    // 取出列表最大文件
                    var head = sort.First();
                    group.Add(head);

                    // 寻找能塞入的下一个文件
                    var sum = head.Length;
                    foreach (var file in sort.Except(group))
                    {
                        if ((sum + file.Length) <= GROUP_SIZE)
                        {
                            sum += file.Length;
                            group.Add(file);
                        }
                    }
                }
            }
        }
        [Obsolete]
        public static XElement GroupsToXml(List<FileGroup> groups)
        {
            XElement root = new XElement("root");
            foreach (var group in groups)
            {
                XElement zip = new XElement("Zip",
                    new XAttribute("ID", group.GroupId),
                    new XAttribute("Length", group.Length),
                    new XAttribute("VolumeCount", group.VolumeCount)
                    );
                foreach (var file in group.Group)
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
        public static string GroupsToJson(List<FileGroup> groups)
        {
            var groups_array = groups.Select(group => new FileGroups(group)).ToArray();
            return JsonConvert.SerializeObject(groups_array);
        }
        public static void SetGroupSize(long bytes)
        {
            GROUP_SIZE = bytes;
        }
    }
    [Obsolete]
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
                zip.AlternateEncoding = Encoding.UTF8;
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
                        string progress = $"{arg.BytesTransferred * 100 / arg.TotalBytesToTransfer}% = {arg.BytesTransferred} / {arg.TotalBytesToTransfer} | ";
                        output = $"文件 \"{filename}\" : {progress}{arg.CurrentEntry.FileName.Split('/').Last()}";
                    }
                    else if (arg.EventType == ZipProgressEventType.Saving_Completed)
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
                    count++; total += file.Length;
                }
                zip.Save();

                string output = $"文件 \"{filename}\" : 已压缩 {count} 个文件，总大小 {total}";
                IO.LogLines(new string[] { output });
                IO.ConsoleReplaceLast(output);
            }
            catch (Exception ex)
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
    struct FileGroups
    {
        /// <summary>
        /// 压缩包的ID
        /// </summary>
        public int GroupId { get; init; }
        /// <summary>
        /// 压缩包的总大小(含分卷)
        /// </summary>
        public long Length { get; init; }
        /// <summary>
        /// 压缩包使用的分卷数
        /// </summary>
        public long VolumeCount { get; init; }
        /// <summary>
        /// 压缩包是单独的(未分卷)
        /// </summary>
        public bool IsSingle { get; init; }
        /// <summary>
        /// 压缩包内包含的文件信息<br/>
        /// 含基本信息 和 图片或视频的元数据
        /// </summary>
        public FileMeta[] FileMetas { get; init; }
        public FileGroups(FileGroup group)
        {
            GroupId = group.GroupId;
            Length = group.Length;
            VolumeCount = group.VolumeCount;
            IsSingle = group.IsSingle;

            FileMetas = group.Group.Select(x => new FileMeta(x)).ToArray();
        }

        /// <summary>
        /// 文件的元数据
        /// </summary>
        public struct FileMeta
        {
            public string FullName { get; init; }
            public long Length { get; init; }
            public DateTime WriteTime { get; init; }
            public DateTime CreationTime { get; init; }
            public DateTime AccessTime { get; init; }
            /// <summary>
            /// MetadataExtractor 读取的图片元数据
            /// </summary>
            public Dictionary<string, string>? ImageExif { get; init; }
            /// <summary>
            /// FFMPEG 读取的视频元数据
            /// </summary>
            public Metadata? VideoMeta { get; init; }
            public FileMeta(FileInfo fileinfo)
            {
                // 读取基本数据
                FullName = fileinfo.FullName;
                Length = fileinfo.Length;
                WriteTime = fileinfo.LastWriteTime;
                CreationTime = fileinfo.CreationTime;
                AccessTime = fileinfo.LastAccessTime;

                // 读取图片元数据
                ImageExif = GetImageExif(fileinfo);
                // 读取视频元数据
                VideoMeta = GetVideoMeta(fileinfo);
            }
            /// <summary>
            /// 读取图片元数据
            /// </summary>
            /// <param name="fileinfo"></param>
            /// <returns></returns>
            private static Dictionary<string, string>? GetImageExif(FileInfo fileinfo)
            {
                var exifData = new Dictionary<string, string>();

                try
                {
                    var directories = ImageMetadataReader.ReadMetadata(fileinfo.FullName);

                    foreach (var directory in directories)
                    {
                        foreach (var tag in directory.Tags)
                        {
                            if (tag.Description == null) continue;
                            exifData[tag.Name] = tag.Description;
                        }
                    }
                }
                catch { return null; }

                return exifData;
            }
            /// <summary>
            /// 读取视频元数据
            /// </summary>
            /// <param name="fileinfo"></param>
            /// <returns></returns>
            private static Metadata? GetVideoMeta(FileInfo fileinfo)
            {
                try
                {
                    var mediaFile = new MediaFile { Filename = fileinfo.FullName };

                    using (var engine = new Engine())
                    {
                        engine.GetMetadata(mediaFile);
                    }
                    return mediaFile.Metadata;
                }catch { return null; }
            }
        }
    }
}
