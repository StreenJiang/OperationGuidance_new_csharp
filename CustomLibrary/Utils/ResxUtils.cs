using log4net;
using System.Reflection;
using System.Resources;

namespace CustomLibrary.Utils {
    /// <summary>
    /// RESX 图片资源提取工具类
    /// </summary>
    public static class ResxUtils {
        private static ILog log = LogManager.GetLogger(typeof(ResxUtils));
        private static string ResourceDir = WidgetUtils.GetBaseDirectory() + "\\WidgetIcons";


        // 启动时调用一次
        public static void Init() {
            if (Directory.Exists(ResourceDir))
                return;

            DirectoryInfo directoryInfo = Directory.CreateDirectory(ResourceDir);
            directoryInfo.Attributes = FileAttributes.Directory | FileAttributes.Hidden;

            var asm = Assembly.GetExecutingAssembly();
            foreach (string name in asm.GetManifestResourceNames()) {
                var resourceManager = new ResourceManager(name, Assembly.GetExecutingAssembly());
                ExtractAllResourcesFromResourceManager(resourceManager, name.Replace(".resources", ""));
            }
        }

        private static void ExtractAllResourcesFromResourceManager(ResourceManager rm, string sourceType) {
            // 尝试通过反射获取 Resources 类中的所有属性
            var assembly = Assembly.GetExecutingAssembly();
            var resourcesType = assembly.GetType(sourceType);

            if (resourcesType != null) {
                var properties = resourcesType.GetProperties(
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static
                );

                foreach (var prop in properties) {
                    if (prop.PropertyType == typeof(Image) ||
                        prop.PropertyType == typeof(Bitmap) ||
                        prop.PropertyType == typeof(Icon)) {
                        try {
                            var image = prop.GetValue(null) as Image;
                            if (image != null) {
                                string fileName = $"{prop.Name}.png";
                                SaveImageToFile(image, Path.Combine(ResourceDir, fileName));
                                Console.WriteLine($"提取: {prop.Name}");
                            }
                        } catch (Exception ex) {
                            Console.WriteLine($"跳过 {prop.Name}: {ex.Message}");
                        }
                    }
                }
            } else {
                Console.WriteLine("未找到 CustomResources 类型");
            }
        }

        private static void SaveImageToFile(Image image, string filePath) {
            try {
                image.Save(filePath, image.RawFormat);
            } catch {
                image.Save(filePath, System.Drawing.Imaging.ImageFormat.Png);
            }
        }

        // 使用时调用
        public static Image Load(string fileName) {
            byte[] data = File.ReadAllBytes(Path.Combine(ResourceDir, $"{fileName}.png"));
            using (var ms = new MemoryStream(data))
                return Image.FromStream(ms);
        }
    }
}
