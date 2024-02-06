using System.Runtime.InteropServices;
using System.Text;

namespace OperationGuidance_new {
    // Got from https://stackoverflow.com/questions/217902/reading-writing-an-ini-file
    public class IniFile {
        #region Fileds
        private string _path;
        private string _fileName = "Settings";
        #endregion 

        #region Most important part
        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        static extern long WritePrivateProfileString(string Section, string? Key, string? Value, string FilePath);
        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        static extern int GetPrivateProfileString(string Section, string Key, string Default, StringBuilder RetVal, int Size, string FilePath);
        #endregion

        #region Constructors
        public IniFile(string? IniPath = null) => _path = new FileInfo(IniPath ?? _fileName + ".ini").FullName;
        #endregion

        #region Main methods
        public string Read(string Key, string? Section = null) {
            var RetVal = new StringBuilder(255);
            GetPrivateProfileString(Section ?? _fileName, Key, "", RetVal, 255, _path);
            return RetVal.ToString();
        }
        public void Write(string? Key, string? Value, string? Section = null) => WritePrivateProfileString(Section ?? _fileName, Key, Value, _path);
        public void DeleteKey(string Key, string? Section = null) => Write(Key, null, Section ?? _fileName);
        public void DeleteSection(string? Section = null) => Write(null, null, Section ?? _fileName);
        public bool KeyExists(string Key, string? Section = null) => Read(Key, Section).Length > 0;
        #endregion
    }
}
