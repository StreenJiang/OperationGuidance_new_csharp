using System.Runtime.InteropServices;
using System.Text;

namespace OperationGuidance_new.Utils {
    // Got from https://stackoverflow.com/questions/217902/reading-writing-an-ini-file
    public class SettingsFileUtil {
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
        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        static extern int GetPrivateProfileString(string Section, string Key, string Default, StringBuilder RetVal, int Size, string FilePath);
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
            var RetVal = new StringBuilder(10240);
            GetPrivateProfileString(Section ?? _fileName, Key, "", RetVal, 10240, _path);
            return RetVal.ToString();
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
