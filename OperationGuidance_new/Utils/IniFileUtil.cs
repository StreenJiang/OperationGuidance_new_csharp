namespace OperationGuidance_new.Utils {
    // Got from https://stackoverflow.com/questions/217902/reading-writing-an-ini-file
    public class IniFileUtil: ASettingsFileUtil {
        public IniFileUtil(string? IniPath = null) : base(IniPath ?? MainUtils.GetBaseDirectory(), "Settings", ".ini") { }
    }
}
