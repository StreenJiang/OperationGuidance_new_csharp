namespace OperationGuidance_new.Utils {
    // Got from https://stackoverflow.com/questions/217902/reading-writing-an-ini-file
    public class IniFileUtil: SettingsFileUtil {
        public IniFileUtil(string? IniPath = null) : base(IniPath, "Settings", ".ini") { }
    }
}
