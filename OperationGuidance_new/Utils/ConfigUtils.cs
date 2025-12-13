using log4net;
using OperationGuidance_new.Attributes;
using OperationGuidance_new.Configs;
using System.ComponentModel;
using System.Reflection;

namespace OperationGuidance_new.Utils {
    public static class ConfigUtils {
        private static ILog log = LogManager.GetLogger(typeof(ConfigUtils));

        public static T LoadConfig<T>() where T : ConfigBase, new() {
            T config = new();
            Type type = config.GetType();
            var iniFile = new SettingsFileUtil(type.Name, ".ini");

            PropertyInfo[] propertyInfos = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (PropertyInfo proInfo in propertyInfos) {
                string configKey = proInfo.Name;
                IEnumerable<Attribute> attributes = proInfo.GetCustomAttributes();
                bool shouldIgnore = false;
                foreach (Attribute attr in attributes) {
                    if (attr is ConfigIgnore ignore) {
                        shouldIgnore = ignore.IsIgnored;
                        break;
                    }
                }
                if (shouldIgnore) {
                    continue;
                }

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

            config.File = iniFile;
            return config;
        }

        public static void SaveConfig<T>(T config) where T : ConfigBase {
            Type type = config.GetType();
            var iniFile = config.File;

            PropertyInfo[] propertyInfos = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (PropertyInfo proInfo in propertyInfos) {
                string configKey = proInfo.Name;
                IEnumerable<Attribute> attributes = proInfo.GetCustomAttributes();
                bool shouldIgnore = false;
                foreach (Attribute attr in attributes) {
                    if (attr is ConfigIgnore ignore) {
                        shouldIgnore = ignore.IsIgnored;
                        break;
                    }
                }
                if (shouldIgnore) {
                    continue;
                }

                object? obj = proInfo.GetValue(config);
                if (obj != null) {
                    string? value = obj.ToString();
                    if (!string.IsNullOrEmpty(value)) {
                        iniFile.Write(configKey, value);
                    }
                }
            }

        }

        public static T GetDefault<T>() where T : ConfigBase, new() {
            return new T();
        }
    }
}
