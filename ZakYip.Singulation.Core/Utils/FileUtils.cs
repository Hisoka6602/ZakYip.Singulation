using System.Text.RegularExpressions;

namespace ZakYip.Singulation.Core.Utils {

    /// <summary>
    /// 提供文件系统操作的实用工具方法。
    /// </summary>
    public static class FileUtils {

        /// <summary>
        /// 递归获取指定文件夹及其子文件夹中匹配正则表达式的所有日志文件。
        /// </summary>
        /// <param name="folderPath">要搜索的文件夹路径。</param>
        /// <param name="regex">用于匹配文件名的正则表达式。</param>
        /// <returns>返回匹配的文件完整路径集合。</returns>
        /// <remarks>
        /// 此方法执行递归搜索，遍历所有子文件夹。
        /// 正则表达式仅应用于文件名（不包括路径），但返回的是完整路径。
        /// </remarks>
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
    }
}