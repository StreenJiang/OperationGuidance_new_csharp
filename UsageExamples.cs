using OperationGuidance_new.Utils;
using CustomLibrary.Utils;
using OperationGuidance_service.Models.DTOs;
using System.Collections.Concurrent;

/// <summary>
/// P0级性能优化使用示例
/// 展示如何使用新的异步API和缓存机制
/// </summary>
public class PerformanceOptimizationExamples {

    /// <summary>
    /// 示例1: 使用异步图片加载API
    /// </summary>
    public static async Task Example_AsyncImageLoading() {
        string imageFileName = "product_image_001.png";

        // 新异步API（推荐）
        Image? image = await MainUtils.GetProductImageAsync(imageFileName);
        if (image != null) {
            Console.WriteLine("图片加载成功");
            // 使用图片...
        }

        // 旧同步API（仍可用，但性能较差）
        Image? oldImage = MainUtils.GetProductImage(imageFileName);
        if (oldImage != null) {
            Console.WriteLine("同步加载成功");
        }
    }

    /// <summary>
    /// 示例2: 监控缓存性能统计
    /// </summary>
    public static void Example_MonitorCacheStats() {
        // 图片缓存统计
        var (hitCount, missCount, hitRate) = MainUtils.ImageCache.Statistics;
        Console.WriteLine($"图片缓存统计:");
        Console.WriteLine($"  命中次数: {hitCount}");
        Console.WriteLine($"  未命中次数: {missCount}");
        Console.WriteLine($"  命中率: {hitRate:P2}");

        // 旋转图片缓存统计
        var (rHitCount, rMissCount, rHitRate) = WidgetUtils.RotatedImageCache.Statistics;
        Console.WriteLine($"旋转缓存统计:");
        Console.WriteLine($"  命中次数: {rHitCount}");
        Console.WriteLine($"  未命中次数: {rMissCount}");
        Console.WriteLine($"  命中率: {rHitRate:P2}");
    }

    /// <summary>
    /// 示例3: 预热缓存（批量加载常用图片）
    /// </summary>
    public static async Task Example_WarmUpCache() {
        string[] commonImageNames = {
            "product_001.png",
            "product_002.png",
            "product_003.png"
        };

        // 预热缓存 - 预加载常用图片
        await MainUtils.ImageCache.WarmUpAsync(commonImageNames);
        Console.WriteLine("缓存预热完成");
    }

    /// <summary>
    /// 示例4: 清空缓存
    /// </summary>
    public static void Example_ClearCache() {
        // 清空图片缓存
        MainUtils.ImageCache.Clear();
        Console.WriteLine("图片缓存已清空");

        // 清空旋转缓存
        WidgetUtils.RotatedImageCache.Clear();
        Console.WriteLine("旋转缓存已清空");
    }

    /// <summary>
    /// 示例5: 使用旋转图片缓存
    /// </summary>
    public static void Example_RotatedImageCache() {
        // 加载原始图片
        Image? originalImage = MainUtils.GetProductImage("product_001.png");
        if (originalImage == null) return;

        // 旋转90度（会自动使用缓存）
        Image? rotated90 = WidgetUtils.RotateImage(originalImage, 90);
        Console.WriteLine("图片旋转90度完成");

        // 再次旋转90度（从缓存获取）
        Image? rotated90Again = WidgetUtils.RotateImage(originalImage, 90);
        Console.WriteLine("从缓存获取旋转图片");
    }

    /// <summary>
    /// 示例6: 批量并行加载图片
    /// </summary>
    public static async Task Example_BatchParallelLoading() {
        string[] imageNames = {
            "product_001.png",
            "product_002.png",
            "product_003.png",
            "product_004.png",
            "product_005.png"
        };

        // 并行加载所有图片
        var loadTasks = imageNames.Select(async fileName => {
            var image = await MainUtils.GetProductImageAsync(fileName);
            return (fileName, image);
        });

        var loadedImages = await Task.WhenAll(loadTasks);

        foreach (var (fileName, image) in loadedImages) {
            if (image != null) {
                Console.WriteLine($"加载成功: {fileName}");
            } else {
                Console.WriteLine($"加载失败: {fileName}");
            }
        }
    }

    /// <summary>
    /// 示例7: 带取消令牌的异步加载
    /// </summary>
    public static async Task Example_CancellableLoading() {
        using var cts = new CancellationTokenSource();

        // 启动加载任务
        var loadTask = Task.Run(async () => {
            string[] imageNames = Enumerable.Range(1, 100)
                .Select(i => $"product_{i:D3}.png")
                .ToArray();

            foreach (var fileName in imageNames) {
                if (cts.Token.IsCancellationRequested) {
                    Console.WriteLine("加载已取消");
                    return;
                }

                await MainUtils.GetProductImageAsync(fileName);
                Console.WriteLine($"加载完成: {fileName}");
            }
        });

        // 5秒后取消
        await Task.Delay(5000);
        cts.Cancel();

        try {
            await loadTask;
        } catch (OperationCanceledException) {
            Console.WriteLine("任务被取消");
        }
    }

    /// <summary>
    /// 示例8: MissionListPanel的异步刷新（原有API，无需修改）
    /// </summary>
    public static void Example_MissionListPanelRefresh() {
        // 创建任务列表
        var missions = new List<ProductMissionDTO> {
            new ProductMissionDTO { id = 1, name = "任务1" },
            new ProductMissionDTO { id = 2, name = "任务2" },
            // ... 更多任务
        };

        // 创建MissionListPanel实例
        var missionListPanel = new MissionListPanel();

        // 原有API调用方式不变，内部已自动优化为异步
        missionListPanel.RefreshMissionBlocks(
            missions,
            blockClickAction: (id) => {
                Console.WriteLine($"点击了任务: {id}");
            },
            toggleBlock: false
        );

        // 检查Loading状态
        if (missionListPanel.IsLoading) {
            Console.WriteLine("正在加载任务...");
        }
    }
}
