using OperationGuidance_service.Models.DTOs;
using System.Collections.Concurrent;

namespace OperationGuidance_new.Utils {
    public static class ProductImageCache {
        private static readonly ConcurrentDictionary<string, Image> _cache = new();

        public static Image? GetOrLoad(string fileName) {
            if (string.IsNullOrEmpty(fileName)) return null;
            if (_cache.TryGetValue(fileName, out var cached)) return cached;
            var loaded = MainUtils.LoadProductImageFromDisk(fileName);
            if (loaded != null) {
                _cache.TryAdd(fileName, loaded);
            }
            return loaded;
        }

        public static void Invalidate(string fileName) {
            if (string.IsNullOrEmpty(fileName)) return;
            if (_cache.TryRemove(fileName, out var image)) {
                image?.Dispose();
            }
        }

        public static void InvalidateByMission(ProductMissionDTO mission) {
            if (mission?.ProductSides == null) return;
            foreach (var side in mission.ProductSides) {
                if (!string.IsNullOrEmpty(side.image))
                    Invalidate(side.image);
            }
        }

        public static void Clear() {
            foreach (var kv in _cache) {
                kv.Value?.Dispose();
            }
            _cache.Clear();
        }
    }
}
