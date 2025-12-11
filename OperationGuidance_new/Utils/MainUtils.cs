using CustomLibrary.Constants;
using CustomLibrary.Utils;
using LicenseLib;
using log4net;
using log4net.Config;
using Newtonsoft.Json;
using OperationGuidance_new.Attributes;
using OperationGuidance_new.Configs;
using OperationGuidance_new.Constants;
using OperationGuidance_new.Tasks;
using OperationGuidance_new.Tasks.DeviceTypes;
using OperationGuidance_new.ViewObjects;
using OperationGuidance_new.Views;
using OperationGuidance_service.Constants;
using OperationGuidance_service.Database;
using OperationGuidance_service.Models.DTOs;
using OperationGuidance_service.Utils;
using RJCP.IO.Ports;
using System.Collections.Concurrent;
using System.Data.Common;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using IniFileKeys = OperationGuidance_new.Configs.IniFileKeys;
using Timer = System.Windows.Forms.Timer;

namespace OperationGuidance_new.Utils {
    public static class MainUtils {
        public static MainForm Self;
        public static ILog logger = GetLogger(typeof(MainUtils));

        public static bool AppRunning { get; internal set; } = true;
        public static LoginView LoginView { get; set; }
        public static bool LoginFlag { get; set; } = true;
        public static Action? ActionAfterLogout { get; set; }
        public static string? LastProductBatch { get; set; }
        public static Dictionary<string, Socket> TCPClients { get; } = new();

        public static readonly string DATETIME_FORMAT_YYYY_MM_DD_CHINESE = "yyyy年MM月dd ddd HH:mm:ss";
        public static readonly string DATETIME_FORMAT_FULL_NO_PUNCTUATION = "yyyyMMddHHmmssfff";

        public static readonly string DATETIME_FORMAT_YYYY_MM_DD_HH_MM_SS = "yyyy-MM-dd HH:mm:ss";
        public static readonly string DATETIME_FORMAT_YYYY_MM_DD_HH_MM_SS_FFF = "yyyy-MM-dd HH:mm:ss.fff";

        public static readonly string DATETIME_FORMAT_YYYY_MM = "yyyy-MM";
        public static readonly string DATETIME_FORMAT_YYYY_MM_2 = "yyyy/MM";

        public static readonly string DATETIME_FORMAT_YYYY_MM_DDD = "yyyy-MM_ddd";
        public static readonly string DATETIME_FORMAT_YYYY_MM_DDD_2 = "yyyy/MM_ddd";

        public static readonly string DATETIME_FORMAT_YYYY_MM_DD = "yyyy-MM-dd";
        public static readonly string DATETIME_FORMAT_YYYY_MM_DD_2 = "yyyy/MM/dd";

        public static readonly string DATETIME_FORMAT_YYYY_MM_DD_DDD = "yyyy-MM-dd_ddd";
        public static readonly string DATETIME_FORMAT_YYYY_MM_DD_DDD_2 = "yyyy/MM/dd_ddd";

        /// <summary>
        /// P0级性能优化：图片缓存机制
        /// 使用LRU策略缓存加载的图片，避免重复文件系统I/O操作
        /// </summary>
        internal static class ImageCache {
            private static readonly ConcurrentDictionary<string, CachedImage> _cache = new();
            private static readonly ConcurrentDictionary<string, Task<Image>> _loadingTasks = new();
            private const int MaxCacheSize = 100;
            private static readonly LinkedList<string> _accessOrder = new();
            private static readonly object _lock = new();

            // 缓存命中统计
            private static int _hitCount = 0;
            private static int _missCount = 0;

            public static (int HitCount, int MissCount, double HitRate) Statistics {
                get {
                    int total = _hitCount + _missCount;
                    return (_hitCount, _missCount, total > 0 ? (double)_hitCount / total : 0.0);
                }
            }

            private class CachedImage {
                public Image Image { get; }
                public LinkedListNode<string> AccessNode { get; set; }

                /// <summary>
                /// 修复严重问题 #1: 使用实际缓存键而不是随机GUID
                /// 确保AccessNode存储的值能够正确映射到缓存条目
                /// </summary>
                public CachedImage(Image image, string cacheKey, LinkedList<string> accessOrder) {
                    Image = image;
                    AccessNode = new LinkedListNode<string>(cacheKey);
                    lock (_lock) {
                        accessOrder.AddLast(AccessNode);
                    }
                }
            }

            /// <summary>
            /// 从缓存获取图片，如果不存在则异步加载
            /// 修复缓存混乱问题: 为缓存的图片创建深拷贝，避免引用混乱
            /// </summary>
            public static async Task<Image?> GetProductImageAsync(string? fileName) {
                if (string.IsNullOrEmpty(fileName)) {
                    return null;
                }

                // 尝试从缓存获取
                if (_cache.TryGetValue(fileName, out var cachedImage)) {
                    // 修复警告问题 #7: 使用原子操作增加计数器
                    Interlocked.Increment(ref _hitCount);
                    UpdateAccessOrder(cachedImage.AccessNode);

                    // 修复缓存混乱问题: 返回缓存图片的深拷贝，避免原始图片被修改
                    return MainUtils.DeepCopyImage(cachedImage.Image);
                }

                // 缓存未命中，检查是否正在加载
                if (_loadingTasks.TryGetValue(fileName, out var loadingTask)) {
                    // 等待正在进行的加载任务，不算作 miss
                    return await loadingTask;
                }

                // 启动新的加载任务 - 这里是真正的 miss
                // 修复警告问题 #7: 使用原子操作增加计数器
                Interlocked.Increment(ref _missCount);
                var task = LoadImageAsync(fileName);
                _loadingTasks[fileName] = task;

                try {
                    var image = await task;
                    if (image != null) {
                        await AddToCacheAsync(fileName, image);
                        // 返回原始图片的深拷贝
                        return MainUtils.DeepCopyImage(image);
                    }
                    return image;
                } finally {
                    _loadingTasks.TryRemove(fileName, out _);
                }
            }

            private static async Task<Image?> LoadImageAsync(string fileName) {
                return await Task.Run(() => {
                    try {
                        string imageFilePath = GetProductImagesPath() + "\\" + fileName;
                        logger?.Debug($"[LoadImageAsync] Loading image: {fileName} from {imageFilePath}");

                        if (!File.Exists(imageFilePath)) {
                            logger?.Warn($"[LoadImageAsync] Image file not found: {imageFilePath}");
                            return null;
                        }

                        // 修复核心问题：使用MemoryStream作为中介，确保图片数据完整且独立
                        // 从MemoryStream创建图片，确保是独立的图片对象，不依赖原始文件或Bitmap
                        using (MemoryStream ms = new MemoryStream(File.ReadAllBytes(imageFilePath))) {
                            Image image = Image.FromStream(ms);
                            logger?.Debug($"[LoadImageAsync] Image loaded successfully. Size: {image.Size}, PixelFormat: {image.PixelFormat}");
                            return image;
                        }
                    } catch (Exception ex) {
                        logger?.Error($"[LoadImageAsync] Failed to load image: {fileName}", ex);
                        return null;
                    }
                });
            }

            private static async Task AddToCacheAsync(string fileName, Image image) {
                await Task.Run(() => {
                    lock (_lock) {
                        // 修复优化建议 #4: 双重检查锁，避免重复添加和正确释放
                        if (_cache.ContainsKey(fileName)) {
                            image.Dispose(); // 如果已存在，释放新图片
                            return;
                        }

                        // 如果缓存已满，移除最久未使用的项
                        if (_cache.Count >= MaxCacheSize) {
                            RemoveOldestItem();
                        }

                        // 添加新项 - 修复严重问题 #1: 传入实际缓存键
                        var cachedImage = new CachedImage(image, fileName, _accessOrder);
                        _cache[fileName] = cachedImage;
                    }
                });
            }

            private static void UpdateAccessOrder(LinkedListNode<string> node) {
                lock (_lock) {
                    _accessOrder.Remove(node);
                    _accessOrder.AddLast(node);
                }
            }

            private static void RemoveOldestItem() {
                lock (_lock) {
                    if (_accessOrder.First != null) {
                        var oldest = _accessOrder.First.Value;
                        _accessOrder.RemoveFirst();
                        _cache.TryRemove(oldest, out var cachedImage);
                        cachedImage?.Image?.Dispose();
                    }
                }
            }

            /// <summary>
            /// 清空缓存
            /// </summary>
            public static void Clear() {
                lock (_lock) {
                    foreach (var kvp in _cache.Values) {
                        kvp.Image?.Dispose();
                    }
                    _cache.Clear();
                    _accessOrder.Clear();
                    _loadingTasks.Clear();
                    _hitCount = 0;
                    _missCount = 0;
                }
            }

            /// <summary>
            /// 修复缓存混乱问题: 清除指定图片文件的缓存
            /// </summary>
            /// <param name="fileName">要清除的图片文件名</param>
            public static void ClearImageCache(string? fileName) {
                if (string.IsNullOrEmpty(fileName)) {
                    return;
                }

                lock (_lock) {
                    if (_cache.TryRemove(fileName, out var cachedImage)) {
                        cachedImage.Image?.Dispose();
                        _accessOrder.Remove(fileName);
                        logger?.Debug($"[ImageCache.ClearImageCache] 已清除异步缓存: {fileName}");
                    }
                    _loadingTasks.TryRemove(fileName, out _);
                }
            }

            /// <summary>
            /// 预热缓存 - 预加载指定图片
            /// </summary>
            public static async Task WarmUpAsync(IEnumerable<string> fileNames) {
                var warmUpTasks = fileNames
                    .Where(f => !string.IsNullOrEmpty(f))
                    .Select(f => GetProductImageAsync(f));
                await Task.WhenAll(warmUpTasks);
            }
        }

        static MainUtils() {
            XmlConfigurator.Configure();
        }
        public static ILog GetLogger(Type type) => LogManager.GetLogger(type);

        public static string LicensePath = AppDomain.CurrentDomain.BaseDirectory + "/license.lic";
        public static string Privatkey = @"<RSAKeyValue>
                        <Modulus>xoTNbjRb6NoleIAl/i+9Edvt+9z+8KYUmk+/SHMAGG0Pkjx65TEcW5TzG8sJJkJLu4Ss3aUF1HUTVBSN12aCQbeE7RvB7+99njBsHhikrVggStO294H36vKAMaTsNpARi4MF/yEbQnDqZRNKg5Zz81PU2T0rz374SbWMSYJn1Enrw9axSLOOWGr0UnYs8CepV1Whcj3Y6OrwqUw4bC+ThvIJsZ4y2E60dy5jOfZdBZMy0dSRVxz/oOrLo2/RWBbCXCvC+VjpecJtQtOPZrUbCUZ60HnulD+7LpqRpjzLwV70NyqmE99EbJxvJzSCfZDcfn+JFh2Q9LN0Djj5gh7cgSVed4ABh/A4VQ7YiGQeci29bni3NXSROr8HiWA/XDloZpCjPC8vWfcERFCsWirdcFtcsiAMU+Y/wKknNxT4Xct3CN6vi6XhKh3PWlZBW9e63Vziq4QkwcV0zOIg3uQn9qHChGLBNFTC8tPWxmxSv34WgS3fDepBRxU2cdciXlQATLeGoLx7ON/F8sD42EJBbv2Op0AvGCswvd1MlYSaSHLBKQCR5LJQY+FbxhyrwUumPhwkZrCoChS0aBPilZONCVb2EjiX4QEQ6k79lOjfW7vyqrHgJR/SxpkgYQXhaPSZkFEmCVvzmdQZIWbX0gu52O4oCT0Kfku1RVyqyg8v8AA12uHoLy5vQPpBlnebfzfNGHt8PaChd+zE67Ew6QPNSgx3vd4SKZ1V/lBJfKINzrsSZqaqeCiLBK13tNWQl6zYYVhBt3YuEKBXVVkUHhBHhkEwx3WML3h4BE9fey2np+U1FACCG5U1krTXfr6XSKkUKAcV9Am5XSCf11EIfnScZ1IRjr6gXnvzRtytpfnTBsnjZab/J6F9pZ5X3lrxTbYNc2P8fe9W3xpIWeVehvtnlWqgRUIHEp8SkfwndDPrwiMlia7NragJ+ZZyNhkMlMACPrXcSU/CV8vn6nZwhvQFX4ZfdjEjIs0FuDf4VV9wakVg8IMN6ZPyttdQlhUmevsBjdMohxnZzQcgiTDdb3gZkL8TU6l3WQqHEfA69YzvSlXgPAlb3iPx/XGfDrNK43MADtFp1QXA2gTfob3JQFEvxuXg9W6ATZDisqAZ0uHJert2X0NEdyasjmugBQQ/1q4gQiFhI9XxwnxmoHPsQ25+XONL64rfMVp9wfRS7BzdiKLMcU+ZB3INhs73gmkSO0HB0UsqutRht0G6fenTkrvCsI7Pn28E5vK8kXSrVJZhpIE+NxOpbW7bchF22p5Bk8lDhX2J8sLy51F32ivsOyuMCffqImq5ynwuEbdInXejCQRme7bjmkhlJz1wLCXe6Ghd8IW3cRl8JCbCnponrdlm6JG+iuKWK6bceTv0pCElJGdYYmIBNT1+UwSTXL0bJsZ7rGxfhygC0HBIOHq+qRKB94vDb1aGF3MMvSeyKnYY2YEV//WjNzh0FW+9CsE030AXmclpC9RRmifM/6Xg3SQe0/YWNRPxtuxV55tvTXhOMPRSzfQ366BaNEzHDxtgeTmuWNQJPC0+s2hDW8uAGszx08RRHwX885X2FzIoD8ms1d39Vrlz0fhn02Z21cXsu9jdBGd3guyWd5dE5YXtUtQsdE3rD+sXCgN3Xm3kSTxcizKXDOeij1cJBhI8NfC9j0/9qvMhSGkl8xPYhprwaOIVKUnBSSeo1rNPUgqz7sG92Sk=</Modulus>
                        <Exponent>AQAB</Exponent>
                        <P>4hZI4XGTcFGEKJe1Ty3G/u0uYQmb6S2SzL4cZ164M/YS81COzBeeuOur3+YvvOFw2cNYUhED8G2kpxxu/nthz1Ga89Mj9vRXn6dpksvEjhVPtSO6AK42uh9nUfw1LGOyJSqWoTtRfeqrJjey2/z8B7jYMDoA0Zvxa9G98LgijEtARIi76jmz0x6RHieixDlp3tFWtfHui66Ircuc/libfwKGevCxqpzkzy97wH5qcXPYLAr957Yhdffi+mK66vRa5HRCo07LBUOARxowiiz2y0wK2li7PfHkAlm6Sytu9P7zoI9CZIGmYNzcmswxG5W+KC5K8PG/5tZoP4v2woN0kve0Eyi4q7p2y/t4gF4ZJv1CAz9v95Ro8nsRdZof8B5j9h2RVCWtI2XpKiK4ZfSUX61oTzMbPa/EY04VAFsE5DvZogECETmxMV/GndteZm6RLHN7uv9Yfc2brs+vx2nCaKqm5591FPwdwRQDqWF62QieM5+dWIsYRrMa++8/5SMFbrR+AequS26MerXU2sEra5s8sZfd/D/PxMqRGwIV0VBEVaj/2rmsKhp+eJWcUKzW74j0Imhg2H3fzhDRYnBk3GGR1cNd9pHbQt86xoMn26MqiSOfjsMq5wZ+geEojDI/QVTCSauOXmOagH7bvS+N/8lBf7Sr6C4nflpl3gYsAD9Xe/f5A2F+aLw4WCM3CzXjI1Pj9CDpiDAIIyPcEugX8CcHcaROz8x5ReH/x75AhL12CA4Yudc08aoRRrJ65zYZCL8kBqy4M9lrkLsaIWX81pgZdAmVGTDCnOh5ZRjbtHI52lb8fwpVN4P024AHmYYKxsR7khm5pIHVH99DqcYfAw==</P>
                        <Q>4MjDCjkk9D8BAuSejG0sNyO6+AJ+g0fIuYV9PuBQV7ij1v4C20VULa9tFzM44x0iKVHYmPtL72b8ERrWHeBzlSIA5PGyhF4W3K+Xps6W3Eisn7Agfjr1L0q4/Ac27pHSpHO2WrfMsu2/ntw1CIPvzoOjHBs87yHZ2oDu/9ILESKynVA4kGF/rEctF7om5CJov/AAtZXrfg1EdKaH9xsJ4GjkGOO+UgbzN8HaLXWQZsjSHSI5qsKc/Pisk0AyXcl1Fr3Ftc61BMXkCuSDwBUEaNQnbO7VKkgSmI1c+1VLIxM2+7x1mzV8Xm3XVeZh7XKIiRNxELdOQbt9J7774RsicZx8JyLzgMJSVnDLtV7/BlptCbmQ8Hg4kJwaXxCGZXIvcdL/nX9PNP69DIKsxlmkze6aB0hdPiPPyUQju6GQawKDwzF2XJkF0goLrcnXlKgGY+gL4w10T4jEJElO1tb0tgI6n9JBtLeW6y27yKHzizsP0ymXFaLlt0hvmKC+94y//X9TiRC5ASE4bwd+FPQBgbU4BqgswQPmtFpT5AiRHAwcWZ9M+KB4Q3Wi0VAnyTbGtU/QVvzAhqcVuhMWsjsKDKRMKYjda8NHBw9qX/Vma1d03jpmt/GHockJEtE2ONJbmNOfLYIYJHLaSKaSrC8SLe7OV9P1empAAH5vsf4LhqkyBbRxOc7YL65fBrAFKQJD74xedrbztqde7dEaIVg1mLTqj1o9+M3gYwnUU3UPzRj76k/V7muFHnD9Oc3U+7Ee20AOwraoA6JW0dcPTC1QBAtgFUtJhJJpOtT43/qrRvJW4msG6ufgm7LwYX4CuXlW+L+YLhuTDTHVEa69kRhJYw==</Q>
                        <DP>rrIpfooUMyXJyNPw9U2aBkGfJLwYDQV5+Vqs9/Lowr3RxtDohit6Kclw9YEYQgqw+JNJG6CqOo1+POJroZgU6+1SnjT4BUqoqmTh3tw09NTi0kTY5M242/iIDYGkVLh0XuOZoNwFDBbYSJ+hRPsmg5EA+8LV/yFQWs+mxOqDR4SeFFbTXRlZKjjkSTi3PIhglhuLtOtMOAKU+jXrCV3OSUXaRATYQ80XwEAgj40fEqtAzkdwCithj5YLfQ3tAL/vu7daBnZLybVu2YITH7G+wTfw6ubFSAgw9t/+YzccdZLLDbWkx6SmuxHuJG7DQ11hogqjPaqPbf8ebnvoIEUTPrzIGEXO7GMYiGW3pvkO1mG7MGdETToQHc2aoBSHuTLCatpOAYdbUY7drAFIGv/x5jxH7WrAEdpPFayv7aZnETRt5hCBWG4LzOsEvdVUDDJDWuWhJc1Iw2ysb1drq9q8rcOvVCqSfbSSCS895RO9qRZPp/Qd0N0p1PdwUlt8M8Hr4K727uQ36XFyex8laiL5OhypuVBv5wonsRgVJ5lk4mfzZX1AtfHiZmYyc1qnfm8PZeedPTkKHD6nQMMeB8JxLRj3ZwWfBXeOxQ1YjSurELvzkiREljuYWwtZlWI1wp7Q4dpshiBZ0fWE3OUpRfYI8yy/v74LV0zSl3+iVvzN4yMOpmvlrgcXdjZ6EyCi17a73ABmk60axnDW2NBMrQ9J7/c4eUdM3qRQ0P7gdac8k1USbwb1dCQmdNXEo6wl66KRlYCchcvQScAmrZ7lggkcZXUBtffxjZVvy26W3tdmbPgWW+/7Cu8hCdSczz8OoEJC8XPBSBM4rzTjSc1qqzS7oQ==</DP>
                        <DQ>WbSE8Y2Ai+Cg3LCz/UKMRK0DrnoAVw/MsQzuwKrwJTHQYLoaFbuDLoA1vu54I1q7CVZaZCLVWQL2UTUugdnTBo79YGB8Z4rNAOEqWi1T0zFFgqzdKsMImgjt7dZLO8YCFBMBkQ6MqFNtB07F81ID35x0+YB9Psl5kVOnDXybYglA0rry99uRAgWdnzxwzNZWi1KSVeUwh6tvyEW1OQ4XUPFLJgutJjsT0QqRsVabfAlkoK/J57WmxOXQqSsTbo45Jgwx3K5TW0ZGDXrIgV1h1xvjZ/ugIjGFClBP7RVK2QKQDMJXBMvBiEW5i9RW/FWa22lg09TzbBQQjE4RYvJbOo/ClDPEjXv/M/Prt8PjbnujzB+8EdtIZ52EgK+tksqQ2JHl5Mqrp3CJrXZw0O9xb6Vq3sEoROYxxBZnVDfT1IC6aKlSnP2MbxgHNSG54N91PWWbaM8zvZHNBNYkmmRKYGBfWOylwCMMHWqw6A6JjOTTGegdHUtW9V/4+SYdT9lhvR6VDbwYuLSzOsv4qw/9ke/qHe37fkO6S/tIQ1aP4muK3NFP/GC93d4STgn49rnfbvgdIrnXc2U2rH11r0cUaZsfseumwZy6ubyTLRxX9Tp/rzgLShkfkZcTAelwBBke6Mx7V8P1MoaLSjb/jatzS9Vjj3VNH79LL2dF1/iUTPc0uUYbcYB0kgK6dYbFaGo5Bl40TJgShrAgmDO8g9YZ/YklmnPfC23NwHhpvayX07fGwOl/bJiIFW/t/qV/+7nhoInd2iLexiSBFVkIB9SFfDe17omPG1L6n7niYDA/Lh8EQXOI3TIQVtKAT8fj+nTMGpULfEsQ0lEywM3FZSQWFQ==</DQ>
                        <InverseQ>LRkG1xhmN+WNml8Myf9OUYVCrPSoz7lN4UOtt1/cJUEB+uym47ymRKdVs6XRdE3K4eNEZeD8kMv/QBYkPdzZAtElKEXjd7BlN2jkerls54We3acvBii7LzNQ7X19j3twVvIb+UFOM+F1P1oPTTfA6TYkZ2o8dZTnpwH/X+oSrZaWqkdnXVOUwNNGWu4afDRlmzIVip9tq383LUXnf8+GUpE0XLNObyaTnI4vBjJ3G+SiZ9jO7mIGjlo6M298wtCQ2Lixil86tqC4PEMNn/jG9C2qvzrDxzEbsVqAiB4FPQW8Byj+gYygWXUN19Omh+nr6WIbdHu4DW4kuZIvR3Aob+92OAYVXxW9MnxunROpbZ782ElEvpdOwNg8XSPZeeMJzFhcBFu59FU7aBeE3bpmgQJNY0LJETay7EiM8Cv2E1iZRag6UR68ie2GFO7yukHE2ZvA9i3LrwI5kRqh7JNvTtGVSXVpcvwV7flbUqEbAdZMID038qjRVecFz70bmGCr5zFufKGFUdF14BLfKqBxxYWsEDdMhEDKXeigyKKoxHigWUs6NmUAMTFbDvBiIkftLS6wli1+NivrmKdurFd4OMRQ+rrc9leq2AJv42MiG/rlr35bKkUeXvOsfXrgP569FfmqBJISkkBtdfVFj8kNYhDapLDj1AbayiweHGZYEe+/bXPpn5SKYEz22nTin4HvqR/Ai6ZNkdRKCuIvWOa6YJQKlA07MkmNf/hRj7k0Dpobd9MnLMd5tGkFATSNLs9Y06gEBb2ASs3Q8niaLKMWIvj85seugvpqc9DyrI9AEYjngwIOM8lt3iAOMQLTq0YFQ8nXdXen9ZK6kqYyXBxNww==</InverseQ>
                        <D>wn/HDlx6WE+zOrbDgqGfKupB5uyFU55EvVO44/DYfRYNlYdwGTHeyNPMxMROuI9nx9ebzqUqaxgx0cU5m3sxz9VQhUcW4k/Q0bY7l9kpLzUSnn2D3EgYcLcbZohhbqwEpJ3AuFDldllPLayS6w3zmMnf1uAaFngeJ4maY0NDGzk5p2yn0cUqh/JyYoCqUrlpLsoVHer+pGXbWOP2u/h5IFPvr3iB9HBYXBS030tDpHyt4+vSYnlk83JYBk892oKh9tBhfQ/h4IvfpQPGkiiKzrGhDrUITXOLn4ONhEQ4lcLEn3BHu1yam5dBSDYoS4IspjuqQmLBbR6NbInh290Eyfuk7Iwb8+YmKyAagrclJVUvLhdT2SnWSKp1ZeFI8sDESwkXBZZtXEWqhoxL8SPiRX0d+t9vdJw4hji01UDUfoEkpSoY8Eebrjn6uFWeP6/Dw7KH2ufjY35ki4BaGEeN6BRgknh3NyB8yJFI8qua/0c4IWSiD+4d+tkLuMnBQI4opQGDA+TnDbghJ1Bt/uQbW/VNWQyHl7RpUvyJVkzPJIBaRCPszRbvsIxR8G6S0RWfUWcdOweF/CqA/x/MdiRUoHv1J7EPDUKQIiE2TjlC7Yvdm+Qe6gCd8DqDAMxUJxLPihgXnj/bEjI+VP2AIqmecq4Tx8loiIkAl27NXp82pnxuR3/ijqd+FTmcHSIe1awEZCJIfdDtoqP14759Xy98aTqB7yKuxNsEL+DVvyi/QncC9lQA3S1L109l42UC/7g3YFiqOmethliyeEqIbVQh36XrduDycmgjnoVt1S/L/bGSKYC9+Oxdytkn8WQmsOgHeZhHqAwG8vlN4hXgrpTtzotYP9kVGJ/9ACztV7mH05aXOXbd1WjKMF4bLZ2ktCNpInunbcKvWP7glqdcTPvejJ6KI2l80mWw+LtJjboWJyxHA/EaREfppH31ff4W2ZnRD8AmFPzGajrEZ9bn3tAgun0I+6jC3SFvOVyB9cnm8AdQvw7YLHEIfmxWeaCHCz3NqpcXEV64ZJAZMVpqR/CbxLXB3dOI3PQYM9czEzv3xo45sibmg8Q6ymoUvt2J4ugO3GamjNUwJuL6SuWmBuBVWEhjDqJPZXLjYdcgtv5boVUl13kkNacOezPim/u2TPmv4NdhP4Bre6o6u6VuVidyZRit5hkXU7BLmJEM6R4jz3LnRszqN6yWvV9aJZ0BoxcTTW2GJtUoH9/iAD//Em6mVu2IyZeI7EBAN5lTE1joodUVLXrUlhOu93aVkw4v7uv6NWhz/MV+fhVbpdfVfX4W8/oGPCCgtQQjDIW3Knlt3TOofypQ+UVd8hPQWmx1crymRdQ68nIcZUK6miFIlgfAHs1fd2wflJWsZVfQHtrDCsMjFXWmbk+oHUx29Rbg74H5KamgImRjxfP8DaysNNppu0eFT9bLvo2OH4VyShNOsos98yh53M/TDwNRjzblnqgT/jUk+1HeEwkw6fxbgzEGbkGw6A3GAxZ56jOicMsfaMdNQUq1a5XKVTDGYNbrkurm59OYYZBDYie6B+k6DfWwpURMgbf3xEWjKYm9mSlPZtuRli8rppfJR6evk/iDJpjqlX/pidzP9veAxMzqG2HsJd7ZWpT0hD34zNBCrCtb/JsLaK2SjL1ZdRCutmNweiz4vDfda6pmNNwoAAQdvQuPmSsWAUbUarhcmz0j7e6g6Mk=</D>
                    </RSAKeyValue>";
        public static License License;
        public static List<string> Macs;
        public static bool LicenseMacsOk => Macs.Any(mac => License.Macs.Contains(mac));
        public static bool LicenseExpirationTimeOk => License.ExpirationTime == null || License.ExpirationTime > DateTime.Now;
        public static void CheckLicense() {
            if (File.Exists(LicensePath)) {
                try {
                    using (StreamReader sr = new StreamReader(LicensePath)) {
                        string? line = sr.ReadLine();
                        if (!string.IsNullOrEmpty(line)) {
                            License = LicenseUtils.DecryptLicense(line, Privatkey);
                        }
                    }
                } catch (InvalidDataException ie) {
                    string errorMsg = $"License is not correct, e = {ie}";
                    Error(logger, errorMsg, false);
                } catch (Exception ex) {
                    string errorMsg = $"Error occurred while reading license file, e = {ex}";
                    Error(logger, errorMsg, false);
                }
            } else {
                try {
                    using (StreamWriter sw = File.CreateText(LicensePath)) {
                        sw.WriteLine("");
                    }
                } catch (Exception ex) {
                    string errorMsg = $"Error occurred while creating license file, e = {ex}";
                    Error(logger, errorMsg, false);
                }
            }
            if (License == null || !LicenseMacsOk || !LicenseExpirationTimeOk) {
                if (License != null && !LicenseMacsOk) {
                    WidgetUtils.ShowErrorPopUp("许可证MAC地址不符");
                }
                if (License != null && !LicenseExpirationTimeOk) {
                    WidgetUtils.ShowWarningPopUp("许可证已过期");
                }
                License = new() {
                    AppVersion = AppVersion.STANDARD + "",
                    MenuIds = new() {
                        {200, null},
                        {500, new() { 508, 509 }},
                        {600, new() { 601, 602 }},
                        {700, null},
                    },
                    Macs = new(),
                };
            }

        }

        public static bool CheckDBConnection() {
            Form formPopup = new Form() {
                StartPosition = FormStartPosition.CenterScreen,
                FormBorderStyle = FormBorderStyle.None,
                Size = new(300, 100),
            };
            formPopup.FormClosing += (s, e) => e.Cancel = true;
            string text = "正在连接数据库，请稍后";
            string dotStr = "...";
            Label label = new() {
                Parent = formPopup,
                AutoSize = false,
                Text = text + dotStr,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
            };

            // Pretend to show pop up to ensure handle is created
            formPopup.Opacity = 0;
            formPopup.Show();

            // Start timer
            Timer timer = new();
            formPopup.BeginInvoke(() => {
                timer.Interval = 350;
                timer.Tick += (s, e) => {
                    if (dotStr.Length >= 3) {
                        dotStr = ".";
                    } else {
                        dotStr += ".";
                    }
                    label.Text = text + dotStr;
                    label.Invalidate();
                };
                timer.Start();
            });

            // Begin async task
            DbConnection? dbConnection = null;
            formPopup.BeginInvoke(async () => {
                await Task.Run(() => {
                    try {
                        dbConnection = DbConnector.GetConnection();
                        if (dbConnection == null) {
                            throw new Exception("Cannot connect to DB, throw this error manually...");
                        }
                    } catch (Exception ex) {
                        logger.Error("Failed to connect to DB and getting Error...", ex);
                    }
                });
                timer.Stop();
                formPopup.Dispose();
            });

            // Show pop up (really)
            formPopup.Hide();
            formPopup.Opacity = 1;
            formPopup.ShowDialog();

            // Release connection because this is just for testing
            dbConnection?.Close();

            return dbConnection != null;
        }

        private static IniFileUtil Settings { get; } = new();
        public static ChallengeTaskUtil ChallengeTaskUtil { get; } = new();
        public static MesConfig_TZYX MesConfig_TZYX { get; } = new();
        public static PlcConfig_GLB PlcConfig_GLB { get; } = new();
        public static List<string> InvalidCharacters { get; } = new() {
            "\u0000","\u0001","\u0002","\u0003","\u0004","\u0005","\u0006","\u0007","\u0008",
            "\u000B","\u000C",
            "\u000E","\u000F","\u0010","\u0011","\u0012","\u0013","\u0014","\u0015","\u0016",
            "\u0017","\u0018","\u0019","\u001A","\u001B","\u001C","\u001D","\u001E","\u001F"
        };

        public static string GetBaseDirectory() {
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string visualStudioDebugPath = "\\OperationGuidance_new\\bin\\Debug\\net6.0-windows";
            if (baseDirectory.Contains(visualStudioDebugPath)) {
                baseDirectory = baseDirectory.Replace(visualStudioDebugPath, "");
            }
            string visualStudioDebugPath2 = "\\bin\\Debug\\net6.0-windows";
            if (baseDirectory.Contains(visualStudioDebugPath2)) {
                baseDirectory = baseDirectory.Replace(visualStudioDebugPath2, "");
            }
            return baseDirectory;
        }

        private static string GetProductImagesPath() {
            string productImagesPath = GetBaseDirectory() + "\\ProductImages";
            if (!Directory.Exists(productImagesPath)) {
                DirectoryInfo directoryInfo = Directory.CreateDirectory(productImagesPath);
                directoryInfo.Attributes = FileAttributes.Directory | FileAttributes.Hidden;
            }
            return productImagesPath;
        }
        public static string GenerateProductImageName() {
            return $"ProductSideImage_{DateTime.Now.ToString(DATETIME_FORMAT_FULL_NO_PUNCTUATION)}.png";
        }
        /// <summary>
        /// P0级性能优化：同步版本图片加载（保持向后兼容）
        /// 优先使用异步版本GetProductImageAsync以避免UI阻塞
        /// 修复图片对象生命周期管理问题：确保返回的图片是独立的，不依赖原始Bitmap
        /// 修复缓存混乱问题：为缓存的图片创建深拷贝，避免引用混乱
        /// </summary>
        public static Image? GetProductImage(string? fileName) {
            // 首先尝试从缓存获取（同步检查，避免阻塞）
            if (_syncCache.TryGetValue(fileName ?? "", out var cachedImage)) {
                // 修复警告问题 #10: 使用原子操作增加访问计数
                Interlocked.Increment(ref _syncCacheAccessCount);

                // 修复缓存混乱问题: 返回缓存图片的深拷贝，避免原始图片被修改
                if (cachedImage != null) {
                    return DeepCopyImage(cachedImage);
                }
            }

            // 同步加载（保持原有行为，但已不是最优方案）
            if (string.IsNullOrEmpty(fileName)) {
                return null;
            }

            // 修复警告问题 #10: 定期清理同步缓存
            if (Interlocked.Increment(ref _syncCacheAccessCount) >= 1000) {
                Interlocked.Exchange(ref _syncCacheAccessCount, 0);
                _syncCache.Clear();
            }

            string imageFilePath = GetProductImagesPath() + "\\" + fileName;
            logger?.Info($"[GetProductImage] Loading image: {fileName} from {imageFilePath}");

            if (!File.Exists(imageFilePath)) {
                logger?.Warn($"[GetProductImage] Image file not found: {imageFilePath}");
                return null;
            }

            try {
                // 修复核心问题：使用MemoryStream作为中介，确保图片数据完整且独立
                // 从MemoryStream创建图片，确保是独立的图片对象，不依赖原始文件
                using (MemoryStream ms = new MemoryStream(File.ReadAllBytes(imageFilePath))) {
                    Image image = Image.FromStream(ms);
                    logger?.Info($"[GetProductImage] Image loaded successfully. Size: {image.Size}, PixelFormat: {image.PixelFormat}");

                    // 修复缓存混乱问题: 为缓存的图片创建深拷贝
                    var cachedCopy = DeepCopyImage(image);
                    if (cachedCopy != null) {
                        // 添加到缓存（异步，不阻塞）
                        _ = Task.Run(() => {
                            try {
                                _syncCache[fileName] = cachedCopy;
                            } catch {
                                // 忽略缓存添加失败
                            }
                        });
                    }

                    // 返回原始图片的深拷贝
                    return DeepCopyImage(image);
                }
            } catch (Exception ex) {
                logger?.Error($"[GetProductImage] Failed to load image: {imageFilePath}", ex);
                return null;
            }
        }

        /// <summary>
        /// P0级性能优化：异步图片加载
        /// 使用缓存机制，避免重复文件系统I/O操作
        /// </summary>
        public static async Task<Image?> GetProductImageAsync(string? fileName) {
            var image = await ImageCache.GetProductImageAsync(fileName);

            // 同步更新同步缓存（用于向后兼容）
            if (image != null && !string.IsNullOrEmpty(fileName)) {
                _syncCache[fileName] = image;
            }

            return image;
        }

        // 同步缓存用于向后兼容
        // 修复警告问题 #10: 添加访问计数和定期清理机制
        private static readonly ConcurrentDictionary<string, Image> _syncCache = new();
        private static int _syncCacheAccessCount = 0;
        public static void SaveProductImage(Image? image, string? fileName) {
            if (image == null || string.IsNullOrEmpty(fileName)) {
                return;
            }
            string imageFilePath = GetProductImagesPath() + "\\" + fileName;

            // 如果图片已存在则替换旧图片
            if (File.Exists(imageFilePath)) {
                File.Delete(imageFilePath);
            }
            image.Save(imageFilePath);
        }
        public static Image? DeepCopyImage(Image? image) {
            if (image == null) {
                return null;
            }

            string base64str = CommonUtils.ImageToBase64(image);
            return CommonUtils.ImageBase64ToImage(base64str);
        }

        /// <summary>
        /// 修复缓存混乱问题: 失效指定图片文件的缓存
        /// 在图片被修改或替换后调用，确保后续加载获取最新图片
        /// </summary>
        /// <param name="fileName">要失效缓存的图片文件名</param>
        public static void InvalidateImageCache(string? fileName) {
            if (string.IsNullOrEmpty(fileName)) {
                return;
            }

            logger?.Info($"[InvalidateImageCache] 失效图片缓存: {fileName}");

            try {
                // 清除同步缓存
                if (_syncCache.TryRemove(fileName, out var syncCachedImage)) {
                    syncCachedImage?.Dispose();
                    logger?.Debug($"[InvalidateImageCache] 已清除同步缓存: {fileName}");
                }

                // 清除异步缓存
                ImageCache.ClearImageCache(fileName);

                // 清除旋转图片缓存（需要遍历所有旋转缓存项）
                CustomLibrary.Utils.WidgetUtils.ClearRotatedImageCache();

                logger?.Info($"[InvalidateImageCache] 图片缓存失效完成: {fileName}");
            } catch (Exception ex) {
                logger?.Error($"[InvalidateImageCache] 失效缓存失败: {fileName}", ex);
            }
        }

        // Settings
        // Resolution
        public static Size GetSettingResolution() {
            Size size;
            string resolution = Settings.Read(IniFileKeys.Resolution);
            if (!string.IsNullOrEmpty(resolution)) {
                string[] strings = resolution.Split(",");
                int width = int.Parse(strings[0].Trim());
                int height = int.Parse(strings[1].Trim());
                size = new(width, height);
            } else {
                size = GetDefaultSettingResolution();
                SetSettingResolution(size);
            }
            return size;
        }
        public static Size GetDefaultSettingResolution() => WidgetUtils.GetScreenWorkingArea().Size;
        public static string GetResolution(Size size) => $"{size.Width}, {size.Height}";
        public static void SetSettingResolution(Size newSize) => Settings.Write(IniFileKeys.Resolution, $"{newSize.Width}, {newSize.Height}");
        // Storage file name format
        public static string GetStorageFileName() {
            string nameFormat = Settings.Read(IniFileKeys.DataStorageNameFormat);
            if (string.IsNullOrEmpty(nameFormat)) {
                nameFormat = GetDefaultStorageFileName();
                SetStorageFileName(nameFormat);
            }
            return nameFormat;
        }
        public static string GetDefaultStorageFileName() => DATETIME_FORMAT_YYYY_MM_DD;
        public static void SetStorageFileName(string nameFormat) => Settings.Write(IniFileKeys.DataStorageNameFormat, nameFormat);
        public static string GetStorageFormattedName() {
            DateTime now = DateTime.Now;
            string nameFormatted = GetStorageFileName();
            if (Replace(DATETIME_FORMAT_YYYY_MM_DD_DDD)) { } else if (Replace(DATETIME_FORMAT_YYYY_MM_DD)) { } else if (Replace(DATETIME_FORMAT_YYYY_MM_DDD)) { } else if (Replace(DATETIME_FORMAT_YYYY_MM)) { }
            return nameFormatted;

            bool Replace(string formatPattern) {
                if (nameFormatted.Contains(formatPattern)) {
                    nameFormatted = nameFormatted.Replace(formatPattern, now.ToString(formatPattern)).Replace(" ", "");
                    return true;
                }
                return false;
            }
        }
        // Storage path
        public static string GetStoragePath() {
            string dataStoragePath = Settings.Read(IniFileKeys.DataStoragePath);
            if (string.IsNullOrEmpty(dataStoragePath)) {
                dataStoragePath = GetDefaultStoragePath();
                SetStoragePath(dataStoragePath);
            }
            return dataStoragePath;
        }
        public static string GetDefaultStoragePath() {
            string defaultPath = GetBaseDirectory() + "OperationDataStorage\\";
            // 如果文件夹不存在，则创建文件夹
            if (!Directory.Exists(defaultPath)) {
                Directory.CreateDirectory(defaultPath);
            }
            return defaultPath;
        }
        public static void SetStoragePath(string newPath) => Settings.Write(IniFileKeys.DataStoragePath, newPath);
        // Fields sort config
        public static List<int> GetSortConfig() {
            List<int>? sortConfig = null;
            string dataStorageFields = Settings.Read(IniFileKeys.DataStorageFieldsSort);
            sortConfig = JsonConvert.DeserializeObject<List<int>>(dataStorageFields);
            if (sortConfig == null) {
                sortConfig = GetDefaultSortConfig();
                SetSortConfig(sortConfig);
            }
            return sortConfig;
        }
        public static List<int> GetDefaultSortConfig() => new List<int>() { 33, 44, 14, 20, 18, 17, 15, 24, 22, 21, 16, 13, 11, 10, 47, 48 };
        public static void SetSortConfig(List<int> fieldsSortConfig) => Settings.Write(IniFileKeys.DataStorageFieldsSort, JsonConvert.SerializeObject(fieldsSortConfig));
        // Fields sort config current
        public static List<int>? GetSortConfigCurr() => JsonConvert.DeserializeObject<List<int>>(Settings.Read(IniFileKeys.DataStorageFieldsSortCurr));
        public static void SetSortConfigCurr(List<int>? fieldsSortConfigCurr) => Settings.Write(IniFileKeys.DataStorageFieldsSortCurr, JsonConvert.SerializeObject(fieldsSortConfigCurr));
        public static List<OperationDataField> GetOperationDataFields(List<int>? sortConfig = null) {
            List<PropertyInfo> props = typeof(OperationDataVO).GetProperties(BindingFlags.Public | BindingFlags.Instance).ToList();
            List<OperationDataField> fields = new();
            int index = 1;
            props.ForEach(p => {
                IEnumerable<Attribute> enumerable = p.GetCustomAttributes();
                foreach (Attribute attribute in enumerable) {
                    if (attribute is GridColumnAttribute gridColumn) {
                        string fieldName;
                        if (gridColumn.ColumnName != null && gridColumn.ColumnName != string.Empty) {
                            fieldName = gridColumn.ColumnName;
                        } else {
                            fieldName = p.Name;
                        }
                        string propertyName = p.Name;
                        fields.Add(new(index++, fieldName, propertyName, false));
                    }
                }
            });
            // Get config
            if (sortConfig == null) {
                sortConfig = GetSortConfig();
            }
            fields = fields.OrderBy(f => {
                int indexTemp = sortConfig.IndexOf(f.Id);
                if (indexTemp == -1) {
                    indexTemp = fields.Count;
                }
                return indexTemp;
            }).ToList();
            fields.ForEach(f => {
                if (sortConfig.IndexOf(f.Id) != -1) {
                    f.Visible = true;
                }
            });
            return fields;
        }
        // Store loosening data
        public static bool GetStoreLooseningData() {
            string storeLooseningData = Settings.Read(IniFileKeys.DataStorageStoreLooseningData);
            if (string.IsNullOrEmpty(storeLooseningData)) {
                bool flag = GetDefaultStoreLooseningData();
                SetStoreLooseningData(flag);
                return flag;
            }
            return int.Parse(storeLooseningData) == (int) YesOrNo.YES;
        }
        public static bool GetDefaultStoreLooseningData() => true;
        public static void SetStoreLooseningData(bool flag) {
            if (flag) {
                Settings.Write(IniFileKeys.DataStorageStoreLooseningData, (int) YesOrNo.YES + "");
            } else {
                Settings.Write(IniFileKeys.DataStorageStoreLooseningData, (int) YesOrNo.NO + "");
            }
        }
        // Arm locating enabled
        public static bool IsArmLocatingEnabled() {
            string armLocatingEnabled = Settings.Read(IniFileKeys.MissionArmLocatingEnabled);
            if (string.IsNullOrEmpty(armLocatingEnabled)) {
                bool flag = DefaultIsArmLocatingEnabled();
                SetArmLocatingEnabled(flag);
                return flag;
            }
            return int.Parse(armLocatingEnabled) == (int) YesOrNo.YES;
        }
        public static bool DefaultIsArmLocatingEnabled() => true;
        public static void SetArmLocatingEnabled(bool flag) {
            if (flag) {
                Settings.Write(IniFileKeys.MissionArmLocatingEnabled, (int) YesOrNo.YES + "");
            } else {
                Settings.Write(IniFileKeys.MissionArmLocatingEnabled, (int) YesOrNo.NO + "");
            }
        }
        // Arm locating accuracy
        public static int GetArmLocatingAccuracy() {
            string armLocatingAccuracy = Settings.Read(IniFileKeys.MissionArmLocatingAccuracy);
            if (string.IsNullOrEmpty(armLocatingAccuracy) || armLocatingAccuracy == "0") {
                int accuracy = GetDefaultArmLocatingAccuracy();
                SetArmLocatingAccuracy(accuracy);
                return accuracy;
            }
            return int.Parse(armLocatingAccuracy);
        }
        public static int GetDefaultArmLocatingAccuracy() => 100;
        public static void SetArmLocatingAccuracy(int accuracy) => Settings.Write(IniFileKeys.MissionArmLocatingAccuracy, accuracy + "");
        // Error prompt for arm enabled
        public static bool IsErrorPromptForArmEnabled() {
            string armLocatingEnablederrorPromptForArmEnabled = Settings.Read(IniFileKeys.MissionErrorPromptForArmEnabled);
            if (string.IsNullOrEmpty(armLocatingEnablederrorPromptForArmEnabled)) {
                bool flag = DefaultIsErrorPromptForArmEnabled();
                SetErrorPromptForArmEnabled(flag);
                return flag;
            }
            return int.Parse(armLocatingEnablederrorPromptForArmEnabled) == (int) YesOrNo.YES;
        }
        public static bool DefaultIsErrorPromptForArmEnabled() => false;
        public static void SetErrorPromptForArmEnabled(bool flag) {
            if (flag) {
                Settings.Write(IniFileKeys.MissionErrorPromptForArmEnabled, (int) YesOrNo.YES + "");
            } else {
                Settings.Write(IniFileKeys.MissionErrorPromptForArmEnabled, (int) YesOrNo.NO + "");
            }
        }
        // Mission self looping mode
        public static bool IsMissionSelfLoopingModeEnabled() {
            string missionSelfLoopingModeEnabled = Settings.Read(IniFileKeys.MissionSelfLoopingMode);
            if (string.IsNullOrEmpty(missionSelfLoopingModeEnabled)) {
                bool flag = DefaultMissionSelfLoopingModeEnabled();
                SetMissionSelfLoopingModeEnabled(flag);
                return flag;
            }
            return int.Parse(missionSelfLoopingModeEnabled) == (int) YesOrNo.YES;
        }
        public static bool DefaultMissionSelfLoopingModeEnabled() => false;
        public static void SetMissionSelfLoopingModeEnabled(bool flag) {
            if (flag) {
                Settings.Write(IniFileKeys.MissionSelfLoopingMode, (int) YesOrNo.YES + "");
            } else {
                Settings.Write(IniFileKeys.MissionSelfLoopingMode, (int) YesOrNo.NO + "");
            }
        }
        // Auto lock tool
        public static bool IsAutoLockToolEnabled() {
            string autoLockToolEnabled = Settings.Read(IniFileKeys.AutoLockTool);
            if (string.IsNullOrEmpty(autoLockToolEnabled)) {
                bool flag = DefaultAutoLockToolEnabled();
                SetAutoLockToolEnabled(flag);
                return flag;
            }
            return int.Parse(autoLockToolEnabled) == (int) YesOrNo.YES;
        }
        public static bool DefaultAutoLockToolEnabled() => false;
        public static void SetAutoLockToolEnabled(bool flag) {
            if (flag) {
                Settings.Write(IniFileKeys.AutoLockTool, (int) YesOrNo.YES + "");
            } else {
                Settings.Write(IniFileKeys.AutoLockTool, (int) YesOrNo.NO + "");
            }
        }
        // PLC bar code self looping
        public static bool IsPLCBarCodeSelfLoopingEnabled() {
            string plcBarCodeSelfLoopingEnabled = Settings.Read(IniFileKeys.PLCBarCodeSelfLooping);
            if (string.IsNullOrEmpty(plcBarCodeSelfLoopingEnabled)) {
                bool flag = DefaultPLCBarCodeSelfLoopingModeEnabled();
                SetPLCBarCodeSelfLoopingModeEnabled(flag);
                return flag;
            }
            return int.Parse(plcBarCodeSelfLoopingEnabled) == (int) YesOrNo.YES;
        }
        public static bool DefaultPLCBarCodeSelfLoopingModeEnabled() => false;
        public static void SetPLCBarCodeSelfLoopingModeEnabled(bool flag) {
            if (flag) {
                Settings.Write(IniFileKeys.PLCBarCodeSelfLooping, (int) YesOrNo.YES + "");
            } else {
                Settings.Write(IniFileKeys.PLCBarCodeSelfLooping, (int) YesOrNo.NO + "");
            }
        }
        // PLC model
        public static string GetPLCModel() {
            string model = Settings.Read(IniFileKeys.PLCModel);
            if (string.IsNullOrEmpty(model)) {
                string modelTemp = GetDefaultPLCModel();
                SetPLCModel(modelTemp);
                return modelTemp;
            }
            return model;
        }
        public static string GetDefaultPLCModel() => "";
        public static void SetPLCModel(string model) => Settings.Write(IniFileKeys.PLCModel, model);
        // PLC db address
        public static int GetPLCDBAddress() {
            string dbAddress = Settings.Read(IniFileKeys.PLCDBAddress);
            if (string.IsNullOrEmpty(dbAddress)) {
                int dbAddressTemp = GetDefaultPLCDBAddress();
                SetPLCDBAddress(dbAddressTemp);
                return dbAddressTemp;
            }
            return int.Parse(dbAddress);
        }
        public static int GetDefaultPLCDBAddress() => 0;
        public static void SetPLCDBAddress(int dbAddress) => Settings.Write(IniFileKeys.PLCDBAddress, dbAddress + "");
        // PLC db register no
        public static string GetPLCDBRegisterNo() {
            string registerNo = Settings.Read(IniFileKeys.PLCDBRegisterNo);
            if (string.IsNullOrEmpty(registerNo)) {
                string registerNoTemp = GetDefaultPLCDBRegisterNo();
                SetPLCDBRegisterNo(registerNoTemp);
                return registerNoTemp;
            }
            return registerNo;
        }
        public static string GetDefaultPLCDBRegisterNo() => "";
        public static void SetPLCDBRegisterNo(string registerNo) => Settings.Write(IniFileKeys.PLCDBRegisterNo, registerNo);
        // PLC bit address
        public static int GetPLCDBBitAddress() {
            string bitAddress = Settings.Read(IniFileKeys.PLCDBBitAddress);
            if (string.IsNullOrEmpty(bitAddress)) {
                int bitAddressTemp = GetDefaultPLCDBBitAddress();
                SetPLCDBBitAddress(bitAddressTemp);
                return bitAddressTemp;
            }
            return int.Parse(bitAddress);
        }
        public static int GetDefaultPLCDBBitAddress() => 0;
        public static void SetPLCDBBitAddress(int bitAddress) => Settings.Write(IniFileKeys.PLCDBBitAddress, bitAddress + "");
        // PLC bar code length
        public static int GetPLCBarCodeLength() {
            string startAddress = Settings.Read(IniFileKeys.PLCBarCodeLength);
            if (string.IsNullOrEmpty(startAddress)) {
                int startAddressTemp = GetDefaultPLCBarCodeLength();
                SetPLCBarCodeLength(startAddressTemp);
                return startAddressTemp;
            }
            return int.Parse(startAddress);
        }
        public static int GetDefaultPLCBarCodeLength() => 0;
        public static void SetPLCBarCodeLength(int length) => Settings.Write(IniFileKeys.PLCBarCodeLength, length + "");
        // Get met code from MES
        public static string GetMatCodeApi() {
            string matCodeApi = Settings.Read(IniFileKeys.MatCodeApi);
            if (string.IsNullOrEmpty(matCodeApi)) {
                string matCodeApiTemp = GetDefaultMatCodeApi();
                SetMatCodeApi(matCodeApiTemp);
                return matCodeApiTemp;
            }
            return matCodeApi;
        }
        public static string GetDefaultMatCodeApi() => "";
        public static void SetMatCodeApi(string matCodeApi) => Settings.Write(IniFileKeys.MatCodeApi, matCodeApi);
        // Upload data to MES
        public static string GetUploadDataApi() {
            string uploadDataApi = Settings.Read(IniFileKeys.UploadDataApi);
            if (string.IsNullOrEmpty(uploadDataApi)) {
                string uploadDataApiTemp = GetDefaultUploadDataApi();
                SetUploadDataApi(uploadDataApiTemp);
                return uploadDataApiTemp;
            }
            return uploadDataApi;
        }
        public static string GetDefaultUploadDataApi() => "";
        public static void SetUploadDataApi(string uploadDataApi) => Settings.Write(IniFileKeys.UploadDataApi, uploadDataApi);
        // USB scanner enabled
        public static bool IsUSBScannerEnabled() {
            string usbScannerEnabled = Settings.Read(IniFileKeys.USBScannerEnabled);
            if (string.IsNullOrEmpty(usbScannerEnabled)) {
                bool flag = DefaultUSBScannerEnabled();
                SetUSBScannerEnabled(flag);
                return flag;
            }
            return int.Parse(usbScannerEnabled) == (int) YesOrNo.YES;
        }
        public static bool DefaultUSBScannerEnabled() => false;
        public static void SetUSBScannerEnabled(bool flag) {
            if (flag) {
                Settings.Write(IniFileKeys.USBScannerEnabled, (int) YesOrNo.YES + "");
            } else {
                Settings.Write(IniFileKeys.USBScannerEnabled, (int) YesOrNo.NO + "");
            }
        }
        // Get line text for whyc
        public static string GetLine_WHYC() {
            string line_WHYC = Settings.Read(IniFileKeys.Line_WHYC);
            if (string.IsNullOrEmpty(line_WHYC)) {
                string line_WHYCTemp = GetDefaultLine_WHYC();
                SetLine_WHYC(line_WHYCTemp);
                return line_WHYCTemp;
            }
            return line_WHYC;
        }
        public static string GetDefaultLine_WHYC() => "";
        public static void SetLine_WHYC(string line_WHYC) => Settings.Write(IniFileKeys.Line_WHYC, line_WHYC);
        // Get operator text for whyc
        public static string GetOperator_WHYC() {
            string operator_WHYC = Settings.Read(IniFileKeys.Operator_WHYC);
            if (string.IsNullOrEmpty(operator_WHYC)) {
                string operator_WHYCTemp = GetDefaultOperator_WHYC();
                SetOperator_WHYC(operator_WHYCTemp);
                return operator_WHYCTemp;
            }
            return operator_WHYC;
        }
        public static string GetDefaultOperator_WHYC() => "";
        public static void SetOperator_WHYC(string operator_WHYC) => Settings.Write(IniFileKeys.Operator_WHYC, operator_WHYC);
        // Auto launch enabled
        public static bool IsAutoLaunchEnabled() {
            string autoLaunchEnabled = Settings.Read(IniFileKeys.AutoLaunchEnabled);
            if (string.IsNullOrEmpty(autoLaunchEnabled)) {
                bool flag = DefaultAutoLaunchEnabled();
                SetAutoLaunchEnabled(flag);
                return flag;
            }
            return int.Parse(autoLaunchEnabled) == (int) YesOrNo.YES;
        }
        public static bool DefaultAutoLaunchEnabled() => false;
        public static void SetAutoLaunchEnabled(bool flag) {
            if (flag) {
                Settings.Write(IniFileKeys.AutoLaunchEnabled, (int) YesOrNo.YES + "");
            } else {
                Settings.Write(IniFileKeys.AutoLaunchEnabled, (int) YesOrNo.NO + "");
            }
        }
        // Auto login enabled
        public static bool IsAutoLoginEnabled() {
            string autoLoginEnabled = Settings.Read(IniFileKeys.AutoLoginEnabled);
            if (string.IsNullOrEmpty(autoLoginEnabled)) {
                bool flag = DefaultAutoLoginEnabled();
                SetAutoLoginEnabled(flag);
                return flag;
            }
            return int.Parse(autoLoginEnabled) == (int) YesOrNo.YES;
        }
        public static bool DefaultAutoLoginEnabled() => false;
        public static void SetAutoLoginEnabled(bool flag) {
            if (flag) {
                Settings.Write(IniFileKeys.AutoLoginEnabled, (int) YesOrNo.YES + "");
            } else {
                Settings.Write(IniFileKeys.AutoLoginEnabled, (int) YesOrNo.NO + "");
            }
        }
        // Auto login info
        public static string GetAutoLoginInfo() {
            string autoLoginInfo = Settings.Read(IniFileKeys.AutoLoginInfo);
            if (string.IsNullOrEmpty(autoLoginInfo)) {
                string autoLoginInfoTemp = GetDefaultAutoLoginInfo();
                SetAutoLoginInfo(autoLoginInfoTemp);
                return autoLoginInfoTemp;
            }
            return autoLoginInfo;
        }
        public static string GetDefaultAutoLoginInfo() => "";
        public static void SetAutoLoginInfo(string autoLoginInfo) => Settings.Write(IniFileKeys.AutoLoginInfo, autoLoginInfo);

        // Ping util method
        public static bool PingHost(string nameOrAddress) {
            Ping? pinger = null;
            try {
                pinger = new();
                PingReply pingReply = pinger.Send(IPAddress.Parse(nameOrAddress), 1500);
                bool pingResult = pingReply.Status == IPStatus.Success;
                return pingResult;
            } catch (PingException pe) {
                System.Console.WriteLine($"Ping error while pinging to [{nameOrAddress}]: {pe}");
            } finally {
                if (pinger != null) {
                    pinger.Dispose();
                }
            }
            return false;
        }

        public static string GetTCPClientKey(string ip, int port) => $"{ip}: {port}";
        public static Tuple<string, int> GetHostFromTCPClientKey(string key) {
            string[] strings = key.Split(":");
            return new(strings[0].Trim(), int.Parse(strings[1].Trim()));
        }

        private static ConcurrentDictionary<int, ToolTask> _toolTasks = new();
        public static ConcurrentDictionary<int, ToolTask> ToolTasks => _toolTasks;
        public static void NewToolTask(int toolId, string? toolName, string ip, int port, DeviceTypeTool tool) {
            ToolTask task = new(toolId, toolName, ip, port, tool);
            task.Connect();
            if (IsAutoLockToolEnabled()) {
                task.ForceSendLock();
            }
            _toolTasks[toolId] = task;
        }
        public static async Task<ToolTask> NewToolTaskAsync(int toolId, string? toolName, string ip, int port, DeviceTypeTool tool) {
            ToolTask task = new(toolId, toolName, ip, port, tool);
            await task.ConnectAsync();
            _toolTasks[toolId] = task;
            return task;
        }
        public static ToolTask GetToolTask(int toolId) {
            if (_toolTasks.TryGetValue(toolId, out var task)) {
                return task;
            }
            throw new ArgumentException($"ToolTask for toolId<{toolId}> has not been created.");
        }
        public static ToolTask? TryGetToolTask(int toolId) {
            _toolTasks.TryGetValue(toolId, out var task);
            return task;
        }
        public static void RemoveToolTask(int toolId) {
            _toolTasks.TryRemove(toolId, out _);
        }
        public static void AddToolTask(int toolId, ToolTask task) {
            _toolTasks[toolId] = task;
        }

        private static ConcurrentDictionary<int, SerialPortTask> _serialPortTasks = new();
        public static ConcurrentDictionary<int, SerialPortTask> SerialPortTasks => _serialPortTasks;
        public static void NewSerialPortTask(int serialPortId, string fullName,
                string portName, int baudRate, Parity parity, int dataBits,
                StopBits stopBits, DataTypes dataType, DeviceTypeSerialPort serialPort) {
            SerialPortTask task = new(serialPortId, fullName, portName, baudRate, parity, dataBits, stopBits, dataType, serialPort);
            task.Connect();
            _serialPortTasks[serialPortId] = task;
        }
        public static async Task<SerialPortTask> NewSerialPortTaskAsync(int serialPortId, string fullName,
                string portName, int baudRate, Parity parity, int dataBits,
                StopBits stopBits, DataTypes dataType, DeviceTypeSerialPort serialPort) {
            SerialPortTask task = new(serialPortId, fullName, portName, baudRate, parity, dataBits, stopBits, dataType, serialPort);
            await task.ConnectAsync();
            _serialPortTasks[serialPortId] = task;
            return task;
        }
        public static SerialPortTask GetSerialPortTask(int serialPortId) {
            if (_serialPortTasks.TryGetValue(serialPortId, out var task)) {
                return task;
            }
            throw new ArgumentException($"SerialPortTask for serialPortId<{serialPortId}> has not been created.");
        }
        public static SerialPortTask? TryGetSerialPortTask(int serialPortId) {
            _serialPortTasks.TryGetValue(serialPortId, out var task);
            return task;
        }
        public static void RemoveSerialPortTask(int serialPortId) {
            _serialPortTasks.TryRemove(serialPortId, out _);
        }
        public static void AddSerialPortTask(int serialPortId, SerialPortTask task) {
            _serialPortTasks[serialPortId] = task;
        }

        private static ConcurrentDictionary<int, CommunicationTask> _communicationTasks = new();
        public static ConcurrentDictionary<int, CommunicationTask> CommunicationTasks => _communicationTasks;
        public static void NewCommunicationTask(int communicationId, string? communicationName, string ip, int port, DeviceTypeCommunication communication) {
            CommunicationTask task = new(communicationId, communicationName, ip, port, communication);
            task.Connect();
            _communicationTasks[communicationId] = task;
        }
        public static async Task<CommunicationTask> NewCommunicationTaskAsync(int communicationId, string? communicationName, string ip, int port, DeviceTypeCommunication communication) {
            CommunicationTask task = new(communicationId, communicationName, ip, port, communication);
            await task.ConnectAsync();
            _communicationTasks[communicationId] = task;
            return task;
        }
        public static CommunicationTask GetCommunicationTask(int communicationId) {
            if (_communicationTasks.TryGetValue(communicationId, out var task)) {
                return task;
            }
            throw new ArgumentException($"CommunicationTask for communicationId<{communicationId}> has not been created.");
        }
        public static CommunicationTask? TryGetCommunicationTask(int communicationId) {
            _communicationTasks.TryGetValue(communicationId, out var task);
            return task;
        }
        public static void RemoveCommunicationTask(int communicationId) {
            _communicationTasks.TryRemove(communicationId, out _);
        }
        public static void AddCommunicationTask(int communicationId, CommunicationTask task) {
            _communicationTasks[communicationId] = task;
        }

        private static ConcurrentDictionary<string, IoBoxTask> _ioBoxTasks = new();
        public static ConcurrentDictionary<string, IoBoxTask> IoBoxTasks => _ioBoxTasks;

        // Lock object for synchronizing task creation to prevent race conditions
        private static readonly object _taskCreationLock = new object();

        /// <summary>
        /// 创建IoBoxTask并根据设备类型初始化相应的设备类型实例
        /// </summary>
        /// <param name="ip">IP地址</param>
        /// <param name="port">端口号</param>
        /// <param name="deviceTypeId">设备类型ID（来自DeviceIoDTO.type或DeviceArmDTO.type）</param>
        /// <param name="deviceId">设备ID（来自DeviceIoDTO.id或DeviceArmDTO.id）</param>
        /// <returns>初始化后的IoBoxTask</returns>
        public static IoBoxTask NewIoBoxTask(string ip, int port, int deviceTypeId, bool isArm, int deviceId = -1) {
            string key = GetTCPClientKey(ip, port);

            IoBoxTask task;
            if (!_ioBoxTasks.TryGetValue(key, out IoBoxTask? value)) {
                task = new(ip, port, deviceId);
                InitializeOrUpdateDeviceType(task, deviceTypeId, isArm);
                task.Connect();
                _ioBoxTasks[key] = task;
            } else {
                task = value;
                InitializeOrUpdateDeviceType(task, deviceTypeId, isArm);
            }

            return task;
        }

        /// <summary>
        /// 异步创建IoBoxTask并根据设备类型初始化相应的设备类型实例
        /// </summary>
        /// <param name="ip">IP地址</param>
        /// <param name="port">端口号</param>
        /// <param name="deviceTypeId">设备类型ID（来自DeviceIoDTO.type或DeviceArmDTO.type）</param>
        /// <param name="deviceId">设备ID（来自DeviceIoDTO.id或DeviceArmDTO.id）</param>
        /// <returns>初始化后的IoBoxTask</returns>
        public static async Task<IoBoxTask> NewIoBoxTaskAsync(string ip, int port, int deviceTypeId, bool isArm, int deviceId = -1) {
            string key = GetTCPClientKey(ip, port);

            // Use lock to prevent race condition during task creation
            lock (_taskCreationLock) {
                // Check if task already exists
                if (_ioBoxTasks.TryGetValue(key, out IoBoxTask? existingTask)) {
                    existingTask.AddDeviceId(deviceId);
                    InitializeOrUpdateDeviceType(existingTask, deviceTypeId, isArm);
                    return existingTask;
                }

                // Create new task
                IoBoxTask newTask = new(ip, port, deviceId);
                newTask.AddDeviceId(deviceId);
                InitializeOrUpdateDeviceType(newTask, deviceTypeId, isArm);

                // Add to dictionary BEFORE connecting to prevent race condition
                _ioBoxTasks[key] = newTask;

                // Connect the task outside the lock to avoid holding lock during async operation
                _ = Task.Run(async () => await newTask.ConnectAsync());

                return newTask;
            }
        }

        /// <summary>
        /// 根据设备类型ID初始化IoBoxTask中的设备类型实例
        /// 支持IoBox设备类型（1-4）和Arm设备类型（1-4）
        /// </summary>
        /// <param name="task">要初始化的IoBoxTask</param>
        /// <param name="deviceTypeId">设备类型ID</param>
        private static void InitializeOrUpdateDeviceType(IoBoxTask task, int deviceTypeId, bool isArm) {
            try {
                if (isArm) {
                    // Arm设备类型范围：CF01, CF02, CF03, CF04
                    try {
                        // 作为Arm设备类型处理
                        var armDeviceType = DeviceType_Arm.GetById(deviceTypeId);
                        task.ArmType = new IoBoxTypeArm(armDeviceType, task.DeviceId);
                        return;
                    } catch (NullReferenceException) {
                        // 超出预定义范围，记录警告但不抛出异常
                        Warn(logger, $"未知的设备类型ID: {deviceTypeId}，IoBoxTask将不会被初始化设备类型", false);
                    }
                } else {
                    // IoBox设备类型范围：SetterSelector_4, SetterSelector_8, Arranger, SetterSelector_4_plus
                    try {
                        var ioBoxDeviceType = DeviceType_IoBox.GetById(deviceTypeId);
                        InitializeIoBoxDeviceType(task, ioBoxDeviceType);
                        return;
                    } catch (NullReferenceException) {
                        // 超出预定义范围，记录警告但不抛出异常
                        Warn(logger, $"未知的设备类型ID: {deviceTypeId}，IoBoxTask将不会被初始化设备类型", false);
                    }
                }
            } catch (Exception ex) {
                // 确保设备类型初始化失败时任务创建也失败，而不是创建带有null ArmType的损坏任务
                throw new InvalidOperationException($"初始化设备类型失败 (deviceTypeId={deviceTypeId}): {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 根据IoBox设备类型初始化IoBoxTask中的具体设备类型实例
        /// </summary>
        /// <param name="task">要初始化的IoBoxTask</param>
        /// <param name="deviceType">IoBox设备类型</param>
        private static void InitializeIoBoxDeviceType(IoBoxTask task, DeviceTypeIoBox deviceType) {
            int deviceId = task.DeviceId;
            if (deviceType is IoBoxArranger arrangerType) {
                task.ArrangerType = new IoBoxTypeArranger(task, arrangerType, deviceId);
            } else if (deviceType is IoBoxSetterSelector_4 setterSelector4Type) {
                task.SetterSelectorType = new IoBoxTypeSetterSelector(task, setterSelector4Type, deviceId);
            } else if (deviceType is IoBoxSetterSelector_8 setterSelector8Type) {
                task.SetterSelectorType = new IoBoxTypeSetterSelector(task, setterSelector8Type, deviceId);
            } else if (deviceType is IoBoxSetterSelector_4_plus setterSelector4PlusType) {
                task.SetterSelectorType = new IoBoxTypeSetterSelectorPlus(task, setterSelector4PlusType, deviceId);
            }
        }
        public static IoBoxTask GetIoBoxTask(string ip, int port) => GetIoBoxTask(GetTCPClientKey(ip, port));
        public static IoBoxTask GetIoBoxTask(string key) {
            if (_ioBoxTasks.TryGetValue(key, out var task)) {
                return task;
            }
            throw new ArgumentException($"IoBoxTask for key<{key}> has not been created.");
        }
        public static IoBoxTask? TryGetIoBoxTask(string ip, int port) => TryGetIoBoxTask(GetTCPClientKey(ip, port));
        public static IoBoxTask? TryGetIoBoxTask(string key) {
            _ioBoxTasks.TryGetValue(key, out var task);
            return task;
        }
        public static void RemoveIoBoxTask(string key) {
            _ioBoxTasks.TryRemove(key, out _);
        }
        public static void RemoveIoBoxTask(string ip, int port) {
            _ioBoxTasks.TryRemove(GetTCPClientKey(ip, port), out _);
        }
        public static void AddIoBoxTask(string ip, int port, IoBoxTask task) {
            _ioBoxTasks[GetTCPClientKey(ip, port)] = task;
        }
        public static void AddIoBoxTask(string key, IoBoxTask task) {
            _ioBoxTasks[key] = task;
        }

        public static List<string> LogCache { get; } = new();
        private static TextBox? _textArea = null;
        public static TextBox? EventLogTextArea {
            get => _textArea;
            set {
                _textArea = value;
                if (_textArea != null && LogCache.Count > 0) {
                    WidgetUtils.MainForm.BeginInvoke(() => {
                        LogCache.ForEach(message => {
                            _textArea.AppendText(message + "\r\n");
                        });
                        LogCache.Clear();
                    });
                }
            }
        }

        public static void Log(string message, bool printToView = true) {
            if (printToView) {
                if (_textArea != null) {
                    _textArea.BeginInvoke(() => {
                        _textArea.AppendText(message + "\r\n");
                    });
                } else {
                    LogCache.Add(message);
                }
            }
        }
        public static void Info(ILog logger, string message, bool printToView = true) {
            Log("[INFO]" + message, printToView);
            logger.Info(message);
        }
        public static void Warn(ILog logger, string message, bool printToView = true) {
            Log("[WARN]" + message, printToView);
            logger.Warn(message);
        }
        public static void Error(ILog logger, string message, bool printToView = true) {
            Log("[ERROR]" + message, printToView);
            logger.Error(message);
        }

        /// <summary>
        /// 格式化设备日志消息
        /// 统一格式：[设备类型]{设备标识} - 详细信息
        /// </summary>
        /// <param name="deviceType">设备类型，如TOOL、IOBOX、SERIALPORT等</param>
        /// <param name="identifier">设备标识，如IP地址、设备名等</param>
        /// <param name="details">详细信息，可选</param>
        /// <returns>格式化后的日志消息</returns>
        public static string FormatDeviceLog(string deviceType, string identifier, string details = "") {
            return $"[{deviceType}]{{{identifier}}}" +
                   (string.IsNullOrEmpty(details) ? "" : $" - {details}");
        }

        /// <summary>
        /// Get zooming ratio
        /// </summary>
        /// <param name="imageSize">Size of image.</param>
        /// <param name="size">Size of content.</param>
        /// <returns>Zooming ratio float value.</returns>        
        public static float GetZoomingRatio(Size imageSize, Size size) {
            int newWidth = size.Width;
            float originalRatio = (float) newWidth / imageSize.Width;
            int newHeight = (int) (imageSize.Height * originalRatio);
            if (newHeight > size.Height) {
                newHeight = size.Height;
                originalRatio = (float) newHeight / imageSize.Height;
                newWidth = (int) (imageSize.Width * originalRatio);
            }
            return originalRatio;
        }

        /// <summary>
        /// Resize image by zooming ratio
        /// </summary>
        /// <param name="image">Image that needs to be resized.</param>
        /// <param name="originalRatio">Zooming ratio.</param>
        /// <returns>New Image with new size.</returns>        
        public static Image ResizeImageByZoomingRatio(Image image, float originalRatio) {
            Size newSize = (image.Size * originalRatio).ToSize();
            if (newSize.Width <= 0) {
                newSize.Width = 1;
            }
            if (newSize.Height <= 0) {
                newSize.Height = 1;
            }
            return WidgetUtils.ResizeImage(image, newSize);
        }

        /// <summary>
        /// Crop image
        /// </summary>
        /// <param name="sourceImage">Image that needs to be resized.</param>
        /// <param name="width">Target width.</param>
        /// <param name="height">Target height.</param>
        /// <param name="offsetX">Offset x direction.</param>
        /// <param name="offsetY">Offset y direction.</param>
        /// <returns>New image after cropping.</returns>        
        public static Image CropImage(Image sourceImage, int width, int height, int offsetX, int offsetY) {
            Bitmap resultImage = new(width, height);
            using (Graphics g = Graphics.FromImage(resultImage)) {
                Rectangle resultRect = new(0, 0, width, height);
                Rectangle sourceRect = new(offsetX, offsetY, width, height);
                g.DrawImage(sourceImage, resultRect, sourceRect, GraphicsUnit.Pixel);
            }
            return resultImage;
        }

        /// <summary>
        /// Crop image
        /// </summary>
        /// <param name="sourceImage">Image that needs to be resized.</param>
        /// <param name="size">Target size.</param>
        /// <param name="offsetPoint">Offset point.</param>
        /// <returns>New image after cropping.</returns>        
        public static Image CropImage(Image sourceImage, Size size, Point offsetPoint) {
            return CropImage(sourceImage, size.Width, size.Height, offsetPoint.X, offsetPoint.Y);
        }

        /// <summary>
        /// Crop image
        /// </summary>
        /// <param name="sourceImage">Image that needs to be resized.</param>
        /// <param name="targetRect">Target size and point.</param>
        /// <returns>New image after cropping.</returns>        
        public static Image CropImage(Image sourceImage, Rectangle targetRect) {
            return CropImage(sourceImage, targetRect.Size, targetRect.Location);
        }

        private static void GetMaxSizeOfSizeRatio(out int maxWidthRatio, out int maxHeightRatio) {
            maxWidthRatio = 0;
            maxHeightRatio = 0;
            List<SizeRatioNRectColor>.Enumerator enumerator = WidthHeightRatio.GetEnumerator();
            while (enumerator.MoveNext()) {
                SizeRatioNRectColor current = enumerator.Current;
                int widthRatio = current.WidthRatio;
                if (widthRatio > maxWidthRatio) {
                    maxWidthRatio = widthRatio;
                }
                int heightRatio = current.HeightRatio;
                if (heightRatio > maxHeightRatio) {
                    maxHeightRatio = heightRatio;
                }
            }
        }
        public static Size GetMaxSizeOfSizeRatioByWidth(int contentWidth) {
            int maxWidthRatio = 0;
            int maxHeightRatio = 0;
            GetMaxSizeOfSizeRatio(out maxWidthRatio, out maxHeightRatio);

            int maxWidth = contentWidth;
            int maxHeight = (int) (maxWidth / (decimal) maxWidthRatio * maxHeightRatio);
            return new(maxWidth, maxHeight);
        }
        public static Size GetMaxSizeOfSizeRatioByHeight(int contentHeight) {
            int maxWidthRatio = 0;
            int maxHeightRatio = 0;
            GetMaxSizeOfSizeRatio(out maxWidthRatio, out maxHeightRatio);

            int maxWidth = (int) (contentHeight / (decimal) maxHeightRatio * maxWidthRatio);
            return new(maxWidth, contentHeight);
        }
        public static Size GetProperSizeAccordingToSizeRatio(Size contentSize, Size size)
            => GetProperSizeAccordingToSizeRatio(contentSize, size.Width, size.Height);
        public static Size GetProperSizeAccordingToSizeRatio(Size contentSize, int width, int height) {
            int newWidth = contentSize.Width;
            int newHeight = (int) (height / ((decimal) width / newWidth));
            if (newHeight > contentSize.Height) {
                newHeight = contentSize.Height;
                newWidth = (int) (width / ((decimal) height / newHeight));
            }
            return new(newWidth, newHeight);
        }

        public static string CheckKeyPosition(string keyPosition) {
            string errorMsg = "";
            for (int i = 0; i < keyPosition.Length; i++) {
                char c = keyPosition[i];
                if (!char.IsDigit(c) && c != ',' && c != '-' && c != ' ') {
                    errorMsg += "条码关键位匹配格式错误。请使用','或'-'隔开";
                    break;
                }
                if (c == '-') {
                    if (i == 0 || i == keyPosition.Length - 1) {
                        errorMsg += "符号'-'不能在开头或结尾";
                        break;
                    } else if (!char.IsDigit(keyPosition[i - 1]) || !char.IsDigit(keyPosition[i + 1])) {
                        errorMsg += "符号'-'前后必须是数字";
                        break;
                    } else {
                        int prevIndex = i - 1;
                        string prev = keyPosition[prevIndex].ToString();
                        while (prevIndex != 0) {
                            prevIndex--;
                            if (!char.IsDigit(keyPosition[prevIndex])) {
                                break;
                            }
                            prev = keyPosition[prevIndex].ToString() + prev;
                        }
                        int prevNum = int.Parse(prev);

                        int nextIndex = i + 1;
                        string follow = keyPosition[nextIndex].ToString();
                        while (nextIndex != keyPosition.Length - 1) {
                            nextIndex++;
                            if (!char.IsDigit(keyPosition[nextIndex])) {
                                break;
                            }
                            follow += keyPosition[nextIndex].ToString();
                        }
                        int followNum = int.Parse(follow);

                        if (prevNum >= followNum) {
                            errorMsg += $"符号'-'前面的数字[{prevNum}]必须小于后面的数字[{followNum}]";
                            break;
                        }
                    }
                }
            }
            List<int> keyPositionList = GetKeyPositionList(keyPosition);
            var enumerable = keyPositionList.GroupBy(i => i).Where(g => g.Count() > 1).Select(g => g.Key);
            if (enumerable.Count() > 0) {
                errorMsg += $"存在重复关键位：{string.Join(", ", enumerable)}";
            }
            return errorMsg;
        }
        public static List<int> GetKeyPositionList(string keyPosition) {
            keyPosition = keyPosition.Replace(" ", "");
            Dictionary<string, string> temp = new();
            string[] parts = keyPosition.Split(',');
            foreach (string part in parts) {
                if (part.Contains('-')) {
                    string[] partTemp = part.Split('-');
                    int prev = int.Parse(partTemp[0]);
                    int follow = int.Parse(partTemp[1]);

                    string newString = "";
                    for (int j = prev; j <= follow; j++) {
                        if (j != prev) {
                            newString += ",";
                        }
                        newString += $"{j}";
                    }
                    temp.Add(prev + "-" + follow, newString);
                }
            }
            foreach (KeyValuePair<string, string> pair in temp) {
                keyPosition = keyPosition.Replace(pair.Key, pair.Value);
            }
            return keyPosition.Split(',').Select(int.Parse).ToList();
        }
        public static List<char> GetKeyCharList(string keyChar) {
            List<char> listTemp = new();
            string[] strings = keyChar.Replace(" ", "").Split(',');
            foreach (string s in strings) {
                if (s.Length == 1) {
                    listTemp.Add(char.Parse(s));
                } else {
                    listTemp.AddRange(s.ToList());
                }
            }
            return listTemp;
        }
        public static Dictionary<int, char>? GetKeyMatchingRule(string? keyPosition, string? keyChar) {
            if (keyPosition == null || keyChar == null) {
                return null;
            }
            List<int> keyPositionList = GetKeyPositionList(keyPosition);
            List<char> keyCharList = GetKeyCharList(keyChar);
            if (keyPositionList.Count != keyCharList.Count) {
                return null;
            }
            Dictionary<int, char> matchingRule = new();
            for (int i = 0; i < keyPositionList.Count; i++) {
                matchingRule.Add(keyPositionList[i], keyCharList[i]);
            }
            return matchingRule;
        }
        public static bool CheckBarCodeIsMatched(string barCode, BarCodeMatchingRuleDTO dto) {
            return CheckBarCodeIsMatched(barCode, dto.end_char, dto.length, dto.key_position, dto.key_char);
        }
        public static bool CheckBarCodeIsMatched(string barCode, string? endChar, int? length, string? keyPosition, string? keyChar) {
            return CheckBarCodeIsMatched(barCode, endChar, length, GetKeyMatchingRule(keyPosition, keyChar));
        }
        public static bool CheckBarCodeIsMatched(string barCode, string? endChar, int? length, Dictionary<int, char>? matchingRules) {
            if (string.IsNullOrEmpty(barCode)) {
                return false;
            }
            if (!string.IsNullOrEmpty(endChar)) {
                barCode = barCode.Substring(0, barCode.IndexOf(endChar) + 1);
            }
            if (length != null && length > 0 && barCode.Length != length) {
                return false;
            }
            if (matchingRules != null) {
                foreach (KeyValuePair<int, char> pair in matchingRules) {
                    if (barCode.Length < pair.Key || barCode[pair.Key - 1] != pair.Value) {
                        return false;
                    }
                }
            }
            return true;
        }

        public static byte[] ToBytes(string hexString) {
            if (hexString.Length % 2 != 0) {
                string errorMsg = $"Value[{hexString}] can not convert to bytes because its length is not an even number.";
                logger.Error(errorMsg);
                throw new InvalidCastException(errorMsg);
            }
            return Enumerable.Range(0, hexString.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hexString.Substring(x, 2), 16))
                             .ToArray();
        }

        public static byte[] ToBytes(int intValue) {
            int maxToByte = 256 * 256 - 1;
            if (intValue > maxToByte) {
                string errorMsg = $"Value[{intValue}] too large for 2 bytes value, can not greater than {maxToByte}.";
                logger.Error(errorMsg);
                throw new InvalidCastException(errorMsg);
            }
            return ToBytes(ToHexString2(intValue));
        }
        public static byte[] ToSingleBytes(int intValue) {
            int maxToByte = 256 - 1;
            if (intValue > maxToByte) {
                string errorMsg = $"Value[{intValue}] too large for 1 bytes value, can not greater than {maxToByte}.";
                logger.Error(errorMsg);
                throw new InvalidCastException(errorMsg);
            }
            return ToBytes(ToHexString1(intValue));
        }

        public static byte[] ToBytesByBinaryString(string binaryString) {
            if (binaryString.Length % 8 != 0) {
                string errorMsg = $"Value[{binaryString}] can not convert to bytes because its length is not an even number.";
                logger.Error(errorMsg);
                throw new InvalidCastException(errorMsg);
            }
            int byteNum = binaryString.Length / 8;
            byte[] bytes = new byte[byteNum];
            for (int i = 0; i < byteNum; i++) {
                bytes[i] = Convert.ToByte(binaryString.Substring(i * 8, 8), 2);
            }
            return bytes;
        }

        public static string ToHexString1(int intValue) {
            int maxToTwoBytes = 16 * 16 - 1;
            if (intValue > maxToTwoBytes) {
                string errorMsg = $"Value[{intValue}] too large for 2 bytes value, can not greater than {maxToTwoBytes}.";
                logger.Error(errorMsg);
                throw new InvalidCastException(errorMsg);
            }
            return Convert.ToString(intValue, 16).PadLeft(2, '0');
        }
        public static string ToHexString2(int intValue) {
            int maxToFourBytes = 16 * 16 * 16 * 16 - 1;
            if (intValue > maxToFourBytes) {
                string errorMsg = $"Value[{intValue}] too large for 4 bytes value, can not greater than {maxToFourBytes}.";
                logger.Error(errorMsg);
                throw new InvalidCastException(errorMsg);
            }
            return Convert.ToString(intValue, 16).PadLeft(4, '0');
        }

        public static string ToHexString(byte[] hexBytes) => BitConverter.ToString(hexBytes).Replace("-", "");

        public static string ToHexString(string binaryString) => ToHexString(ToBytesByBinaryString(binaryString));

        public static string ToBinaryString(int intValue) {
            int maxToOneByte = (int) Math.Pow(16, 2) - 1;
            if (intValue > maxToOneByte) {
                string errorMsg = $"Value[{intValue}] too large for 1 bytes value, can not greater than {maxToOneByte}.";
                logger.Error(errorMsg);
                throw new InvalidCastException(errorMsg);
            }
            return Convert.ToString(intValue, 2).PadLeft(8, '0');
        }
        public static string ToBinaryString_half(int intValue) {
            string a = Convert.ToString(intValue, 2);
            int maxToHalfBytes = 16 - 1;
            if (intValue > maxToHalfBytes) {
                string errorMsg = $"Value[{intValue}] too large for half of a bytes value, can not greater than {maxToHalfBytes}.";
                logger.Error(errorMsg);
                throw new InvalidCastException(errorMsg);
            }
            return Convert.ToString(intValue, 2).PadLeft(4, '0');
        }

        public static string ToBinaryString(byte[] hexBytes) => string.Join(string.Empty, ToHexString(hexBytes).Select(c => Convert.ToString(Convert.ToInt32(c.ToString(), 16), 2).PadLeft(4, '0')));
        public static string ToBinaryString2(byte[] hexBytes) => string.Join(string.Empty, ToHexString(hexBytes).Select(c => Convert.ToString(Convert.ToInt32(c.ToString(), 16), 2).PadLeft(4, '0')));

        public static string ToBinaryString(string hexString) => string.Join(string.Empty, hexString.Select(c => Convert.ToString(Convert.ToInt32(c.ToString(), 16), 2).PadLeft(4, '0')));

        public static int[] ToIntsByHexString(string hexString) => ToIntsByBinaryString(ToBinaryString(hexString));

        public static int[] ToIntsByBinaryString(string binaryString) {
            int[] intValues = new int[binaryString.Length];
            for (int i = 0; i < binaryString.Length; i++) {
                char c = binaryString[i];
                intValues[i] = int.Parse(c.ToString());
            }
            return intValues;
        }

        public static int ToIntByBinaryString(string binaryString) => Convert.ToInt32(binaryString, 2);

        public static int ToIntByHexString(string hexString) => Convert.ToInt32(hexString, 16);

        public static byte[] Crc16ToBytes(IEnumerable<byte> data) {
            var numArray = new byte[2];
            var maxValue1 = byte.MaxValue;
            var maxValue2 = byte.MaxValue;
            byte num1 = 1;
            byte num2 = 160;
            foreach (var t in data) {
                maxValue1 ^= t;
                for (int index2 = 0; index2 <= 7; ++index2) {
                    byte num3 = maxValue2;
                    byte num4 = maxValue1;
                    maxValue2 >>= 1;
                    maxValue1 >>= 1;
                    if ((num3 & 1) == 1)
                        maxValue1 |= 128;
                    if ((num4 & 1) == 1) {
                        maxValue2 ^= num2;
                        maxValue1 ^= num1;
                    }
                }
            }
            return new[] { maxValue1, maxValue2 };
        }

        public static string Crc16ToString(IEnumerable<byte> data) {
            return ToHexString(Crc16ToBytes(data));
        }

        public static T DeepCopy<T>(T t) {
            // 将对象序列化为 JSON 字符串
            string jsonString = JsonConvert.SerializeObject(t);

            // 从 JSON 字符串反序列化为新的对象
            return JsonConvert.DeserializeObject<T>(jsonString);
        }
    }
}
