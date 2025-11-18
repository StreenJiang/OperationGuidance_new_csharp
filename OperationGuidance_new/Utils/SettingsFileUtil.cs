using System.Runtime.InteropServices;
using log4net; // 假设您已经在项目中引用了 log4net

namespace OperationGuidance_new.Utils {
    // Got from https://stackoverflow.com/questions/217902/reading-writing-an-ini-file
    public class SettingsFileUtil {
        private static readonly ILog log = LogManager.GetLogger(typeof(SettingsFileUtil));

        #region Fileds
        private string _path;
        private string _fileName;
        private string _fileType;
        #endregion

        #region Properties
        public string Path { get => _path; set => _path = value; }
        public string FileName { get => _fileName; set => _fileName = value; }
        public string FileType { get => _fileType; set => _fileType = value; }
        #endregion

        #region Most important part
        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        static extern long WritePrivateProfileString(string Section, string? Key, string? Value, string FilePath);

        // 使用 IntPtr 返回值，以便获取实际读取的字符数
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern int GetPrivateProfileString(string lpAppName, string lpKeyName, string lpDefault, IntPtr lpReturnedString, int nSize, string lpFileName);
        #endregion

        #region Constructors
        public SettingsFileUtil(string FileName, string FileType) : this(MainUtils.GetBaseDirectory(), FileName, FileType) { }
        public SettingsFileUtil(string? IniPath, string FileName, string FileType) {
            if (IniPath == null) {
                IniPath = MainUtils.GetBaseDirectory();
            }
            _path = new FileInfo(IniPath + FileName + FileType).FullName;
            _fileName = FileName;
            _fileType = FileType;
        }
        #endregion

        #region Main methods
        public string Read(string Key, string? Section = null) {
            const int INITIAL_SIZE = 255;
            const int MAX_SIZE = 1024 * 1024; // 1MB, adjust if needed but be mindful of memory
            int size = INITIAL_SIZE;
            IntPtr buffer = IntPtr.Zero;

            try {
                int charsRead;
                do {
                    // Free the previous buffer if it was too small
                    if (buffer != IntPtr.Zero) {
                        Marshal.FreeHGlobal(buffer);
                    }
                    // Allocate a new buffer of the current size
                    buffer = Marshal.AllocHGlobal(size * 2); // *2 for Unicode

                    // Call the API with the allocated buffer
                    charsRead = GetPrivateProfileString(Section ?? _fileName, Key, "", buffer, size, _path);

                    // Check if the buffer was too small (charsRead equals size - 1)
                    if (charsRead == size - 1) {
                        // Buffer was likely too small, double the size and try again
                        size *= 2;
                        if (size > MAX_SIZE) {
                            log.Warn($"INI value for key '{Key}' in section '{Section ?? _fileName}' might be truncated as it exceeds MAX_SIZE ({MAX_SIZE}).");
                            break; // Stop growing to avoid excessive memory allocation
                        }
                    } else {
                        // Buffer was sufficient, break the loop
                        break;
                    }
                } while (true);

                // Convert the buffer to a string up to the number of characters read
                string result = Marshal.PtrToStringUni(buffer, charsRead);

                // --- 核心修复逻辑：针对 Z64 编码进行转义字符恢复 ---
                // 检查是否可能包含 Z64 编码 (可以根据 Key 或内容判断)
                // 这里可以根据您的具体 Key 名称进行判断，或者更复杂的判断逻辑
                if (IsKeyLikelyToContainZ64(Key)) {
                    result = FixZ64EncodingIfTruncated(result);
                }
                // --- 修复逻辑结束 ---

                return result;
            } finally {
                // Always free the allocated buffer
                if (buffer != IntPtr.Zero) {
                    Marshal.FreeHGlobal(buffer);
                }
            }
        }

        /// <summary>
        /// 判断 Key 名称是否可能包含 Z64 编码数据
        /// </summary>
        /// <param name="keyName">INI 键名</param>
        /// <returns>如果是可能包含 Z64 的键名，返回 true</returns>
        private static bool IsKeyLikelyToContainZ64(string keyName) {
            // 根据您的实际情况调整判断逻辑
            return keyName.Equals("location_y", StringComparison.OrdinalIgnoreCase);
            // 如果有多个可能包含 Z64 的键，可以用 Contains 或 switch 语句
            // return keyName.Contains("location") || keyName.Contains("image");
        }

        /// <summary>
        /// 尝试修复 Z64 编码字符串中可能因转义字符导致的问题。
        /// 主要尝试将被错误转换的 \ 换回 /。
        /// 注意：此方法基于 Z64 编码规范，假设 Z64 数据段内原始不应包含 \。
        /// </summary>
        /// <param name="input">从 INI 读取的原始字符串</param>
        /// <returns>尝试修复后的字符串</returns>
        private static string FixZ64EncodingIfTruncated(string input) {
            if (string.IsNullOrEmpty(input)) {
                return input;
            }

            // Z64 编码格式为 :Z64:...:8CAB
            int z64StartIndex = input.IndexOf(":Z64:", StringComparison.Ordinal);
            int z64EndIndex = input.IndexOf(":8CAB", z64StartIndex, StringComparison.Ordinal);

            // 如果找不到 Z64 标记，则不处理
            if (z64StartIndex == -1 || z64EndIndex == -1) {
                // log.Debug($"Key does not contain Z64 markers, skipping fix.");
                return input;
            }

            // 检查截断标志：字符串以 :8CAB 结尾吗？如果不是，可能被截断了。
            // 但更重要的是，检查 Z64 数据段本身是否包含 \。
            // 如果包含，很可能是被错误转义了。
            // 提取 Z64 数据段（不包含 :Z64: 和 :8CAB）
            int z64DataStart = z64StartIndex + 5; // ":Z64:".Length
            int z64DataLength = z64EndIndex - z64DataStart;

            if (z64DataLength <= 0) {
                // log.Debug("Z64 data segment has invalid length, skipping fix.");
                return input; // 或者抛出异常？
            }

            string z64Data = input.Substring(z64DataStart, z64DataLength);

            // 检查 Z64 数据段是否包含反斜杠 \
            // Z64 标准编码字符包括 A-Z, a-z, 0-9, +, /, =。不应包含原始的 \
            if (z64Data.Contains('\\')) {
                log.Info("Detected backslashes in Z64 data segment, attempting to fix by replacing with forward slashes.");
                // Z64 编码规范中包含 /, +, =, A-Z, a-z, 0-9 等字符
                // Windows INI API 可能将 Z64 中的 / 错误地转义为 \，我们需要将其换回来
                string fixedZ64Data = z64Data.Replace('\\', '/');

                // 重新组合字符串
                string prefix = input.Substring(0, z64DataStart);
                string suffix = input.Substring(z64EndIndex);

                string fixedResult = prefix + fixedZ64Data + suffix;
                // log.Debug($"Fixed Z64 string length: {fixedResult.Length}");
                return fixedResult;
            } else {
                // log.Debug("Z64 data segment does not contain backslashes, no fix needed based on this check.");
                // 即使没有 \，也不能完全排除截断，因为截断可能发生在 \Z 之前
                // 但没有 \，我们无法直接修复，只能返回当前结果
                // 可以进一步检查 :8CAB 是否存在且在末尾
                if (!input.EndsWith(":8CAB", StringComparison.Ordinal)) {
                    log.Warn("Z64 data might be truncated as it does not end with ':8CAB'.");
                    // 这种情况下，我们无法知道原始数据应该是什么，只能返回当前读取到的部分
                    // 但上面的 \ -> / 修复可能已经解决了问题
                }
                return input;
            }
        }


        public void Write(string? Key, string? Value, string? Section = null) => WritePrivateProfileString(Section ?? _fileName, Key, Value, _path);
        public void DeleteKey(string Key, string? Section = null) => Write(Key, null, Section ?? _fileName);
        public void DeleteSection(string? Section = null) => Write(null, null, Section ?? _fileName);
        public bool KeyExists(string Key, string? Section = null) => Read(Key, Section).Length > 0;

        /// <summary>
        /// 写入一条注释（以 ; 开头）
        /// </summary>
        /// <param name="comment">注释内容</param>
        /// <param name="section">要写入的 section，null 表示默认 section</param>
        public void WriteComment(string comment, string? section = null) {
            if (string.IsNullOrWhiteSpace(comment))
                return;

            var sec = section ?? _fileName;
            var lines = comment.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines) {
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed))
                    continue;

                var commentLine = trimmed.StartsWith(";") || trimmed.StartsWith("#")
                    ? trimmed
                    : "# " + trimmed;

                WritePrivateProfileString(sec, commentLine, "", _path);
            }
        }
        #endregion
    }
}
