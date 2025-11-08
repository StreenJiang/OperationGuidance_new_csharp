using OperationGuidance_new.Attributes;
using OperationGuidance_new.Utils;

namespace OperationGuidance_new.Configs {
    public abstract class ConfigBase {
        [ConfigIgnore]
        public SettingsFileUtil File { get; set; }
    }
}

