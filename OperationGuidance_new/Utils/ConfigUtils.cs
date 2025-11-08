using log4net;
using OperationGuidance_new.Utils.IIPSC;
using System.ComponentModel;
using System.Reflection;

namespace OperationGuidance_new.Utils {
    public static class ConfigUtils {
        private static ILog log = LogManager.GetLogger(typeof(ConfigUtils));
        public static SciiXtPrinterConfig SciiXtPrinterConfig { get; set; }

        public static T LoadConfig<T>() where T : new() {
            T config = new();
            Type type = config.GetType();
            var iniFile = new SettingsFileUtil(type.Name, ".ini");

            PropertyInfo[] propertyInfos = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (PropertyInfo proInfo in propertyInfos) {
                string configKey = proInfo.Name;

                string value = iniFile.Read(configKey);
                if (string.IsNullOrEmpty(value)) {
                    object? val = proInfo.GetValue(config);
                    if (val is not null) {
                        iniFile.Write(configKey, val.ToString());
                    } else {
                        iniFile.Write(configKey, "");
                    }
                } else {
                    var converter = TypeDescriptor.GetConverter(proInfo.PropertyType);
                    if (converter != null && converter.CanConvertFrom(typeof(string))) {
                        try {
                            proInfo.SetValue(config, converter.ConvertFromInvariantString(value));
                        } catch (Exception ex) {
                            log.Error($"Error converting config file [{type.Name}], property name = [{proInfo.Name}]", ex);
                        }
                    }
                }
            }

            return config;
        }
    }
}
