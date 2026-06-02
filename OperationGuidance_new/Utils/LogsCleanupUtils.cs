using log4net;

namespace OperationGuidance_new.Utils {
    public static class LogsCleanupUtils {
        private static readonly ILog logger = LogManager.GetLogger(typeof(LogsCleanupUtils));

        public static void CleanOldLogs(int retentionDays) {
            if (retentionDays <= 0) return;

            string logsDir = AppDomain.CurrentDomain.BaseDirectory + "logs\\";

            string[] logFiles;
            try {
                logFiles = Directory.GetFiles(logsDir, "*.log");
            } catch (DirectoryNotFoundException) {
                return;
            }

            DateTime cutoff = DateTime.Now.AddDays(-retentionDays);

            foreach (string filePath in logFiles) {
                try {
                    string fileName = Path.GetFileNameWithoutExtension(filePath);
                    if (!DateTime.TryParseExact(fileName, MainUtils.DATETIME_FORMAT_YYYY_MM_DD, null,
                            System.Globalization.DateTimeStyles.None, out DateTime fileDate)) continue;
                    if (fileDate >= cutoff) continue;
                    File.Delete(filePath);
                } catch (Exception ex) {
                    logger.Warn($"Failed to delete old log file [{filePath}]: {ex.Message}");
                }
            }
        }
    }
}
