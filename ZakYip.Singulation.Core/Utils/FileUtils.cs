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
    }
}