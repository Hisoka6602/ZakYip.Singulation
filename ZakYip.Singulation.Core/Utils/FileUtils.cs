using System.IO.Compression;
using System.Text.RegularExpressions;

namespace ZakYip.Singulation.Core.Utils {

    public static class FileUtils {

        public static IEnumerable<string> GetLogFiles(string folderPath, Regex regex) {
            // 获取文件夹中的所有文件和子文件夹
            var files = Directory.GetFiles(folderPath);
            var subFolders = Directory.GetDirectories(folderPath);

            // 处理文件夹中的文件
            var logFiles = files?.Where(fileName => regex.IsMatch(Path.GetFileName(fileName)))?.ToList() ?? new List<string>();

            // 递归处理子文件夹
            foreach (var subFolder in subFolders) {
                var subFolderLogFiles = GetLogFiles(subFolder, regex);
                logFiles.AddRange(subFolderLogFiles);
            }

            // 返回完整路径的日志文件列表
            return logFiles;
        }

        /// <summary>
        /// 提取 URL 的路径部分并确保包含指定前缀
        /// </summary>
        /// <param name="url">完整的 URL 地址</param>
        /// <param name="prefix">静态资源请求前缀（例如 /scr）</param>
        /// <returns>标准化后的路径（带前缀）</returns>
        public static string ExtractStaticPath(string url, string prefix = "/scr") {
            // 参数校验
            if (string.IsNullOrWhiteSpace(url))
                return string.Empty;

            // 提取 URL 的路径部分
            var uri = new Uri(url);
            var path = uri.AbsolutePath;

            // 判断路径是否已包含前缀，若无则添加
            if (!path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) {
                path = prefix + path;
            }

            return path;
        }

        /// <summary>
        /// 批量将指定文件压缩为 Zip 文件
        /// </summary>
        /// <param name="filePaths">要压缩的文件路径列表</param>
        /// <param name="outputZipPath">输出 Zip 文件路径</param>
        /// <param name="overwrite">是否覆盖已有 Zip 文件（默认 true）</param>
        /// <returns>压缩成功返回 true，否则 false</returns>
        public static async Task<bool> CreateZipAsync(
            List<string>? filePaths,
            string outputZipPath,
            bool overwrite = true) {
            try {
                if (filePaths == null || filePaths.Count == 0)
                    return false;

                if (File.Exists(outputZipPath)) {
                    if (overwrite)
                        File.Delete(outputZipPath);
                    else
                        return false;
                }

                // 创建目标目录（如不存在）
                var outputDir = Path.GetDirectoryName(outputZipPath);
                if (!string.IsNullOrWhiteSpace(outputDir) && !Directory.Exists(outputDir))
                    Directory.CreateDirectory(outputDir);

                await using var zipStream = new FileStream(outputZipPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
                using var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, leaveOpen: false);

                foreach (var filePath in filePaths) {
                    if (!File.Exists(filePath))
                        continue;

                    var fileName = Path.GetFileName(filePath);
                    var entry = archive.CreateEntry(fileName, CompressionLevel.Fastest);

                    await using var inputStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                    await using var entryStream = entry.Open();
                    await inputStream.CopyToAsync(entryStream);
                }

                return true;
            }
            catch (Exception e) {
                NLog.LogManager.GetCurrentClassLogger().Error($"{e}");
                // TODO: 可扩展异常日志
                return false;
            }
        }

        /// <summary>
        /// 判断文件是否被占用
        /// </summary>
        /// <param name="filePath">文件完整路径</param>
        /// <returns>返回 true 表示文件已被占用</returns>
        public static bool IsFileLocked(string filePath) {
            try {
                using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.None);
                return false;
            }
            catch (IOException) {
                return true;
            }
        }

        /// <summary>
        /// 尝试复制文件，如果文件被占用则抛出异常
        /// </summary>
        /// <param name="sourcePath">源文件路径</param>
        /// <param name="destinationPath">目标文件路径</param>
        /// <param name="overwrite">是否覆盖已存在目标文件</param>
        public static KeyValuePair<bool, string> CopyFileSafely(string sourcePath, string destinationPath, bool overwrite = true) {
            if (IsFileLocked(sourcePath)) {
                return new KeyValuePair<bool, string>(false, $"文件正在被使用，无法复制: {sourcePath}");
            }

            try {
                File.Copy(sourcePath, destinationPath, overwrite);
            }
            catch (Exception e) {
                return new KeyValuePair<bool, string>(false, $"文件复制异常: {sourcePath},{e}");
            }

            return new KeyValuePair<bool, string>(true, string.Empty);
        }
    }
}