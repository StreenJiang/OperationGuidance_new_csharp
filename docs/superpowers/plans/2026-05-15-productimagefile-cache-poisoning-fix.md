# ProductImageFile Cache Poisoning Fix — Implementation Plan

> Plan self-contained, no external references needed.

**Goal:** Fix `ProductImageFile.Dispose()` poisoning `ProductImageCache` by disposing shared Image references it doesn't own.

**Approach:** `_ownsImage` flag — defaults `true`, set `false` only in constructor (the single cache-reference path). `ReloadImage()` clones cache result so it always returns an owned image. `Dispose()` checks flag.

**Files:** 1 — `OperationGuidance_new/Views/ReusableWidgets/ProductImageFile.cs`

---

### Task 1: Add _ownsImage flag and fix Dispose

- [ ] **Step 1: Add `_ownsImage` field**

After `private Rectangle? _imageRange;` (line 28), add:

```csharp
private bool _ownsImage = true;
```

- [ ] **Step 2: Set `_ownsImage = false` in constructor**

After line 61 (`_image = image;`), add:

```csharp
_ownsImage = false;  // cache reference, must not dispose
```

- [ ] **Step 3: Fix `Dispose()` to check `_ownsImage`**

Replace lines 427-429:

```csharp
public void Dispose() {
    if (_ownsImage) {
        _image?.Dispose();
    }
}
```

- [ ] **Step 4: Fix `ReloadImage()` cache path to clone**

Replace lines 139-154 (the `_imageFileName` branch):

```csharp
if (reloadedImage == null && !string.IsNullOrEmpty(_imageFileName)) {
    try {
        logger.Info($"正在通过图像文件名重新加载图像: {_imageFileName}");
        var cached = MainUtils.GetProductImage(_imageFileName);
        if (cached != null) {
            try {
                reloadedImage = new Bitmap(cached);
                logger.Info($"成功通过图像文件名加载图像 - 尺寸: {reloadedImage.Width}x{reloadedImage.Height}");
            } catch (ArgumentException) {
                logger.Warn($"通过图像文件名加载图像失败 (缓存图片已损坏): {_imageFileName}");
            }
        } else {
            logger.Warn($"通过图像文件名未能加载图像: {_imageFileName}");
        }
    } catch (ArgumentException ex) {
        logger.Warn($"通过图像文件名加载图像失败 (参数无效): {_imageFileName}, 错误: {ex.Message}");
    } catch (FileNotFoundException ex) {
        logger.Warn($"图像文件未找到: {_imageFileName}, 错误: {ex.Message}");
    } catch (Exception ex) {
        logger.Error($"通过图像文件名加载图像时发生未知错误: {_imageFileName}", ex);
    }
}
```

- [ ] **Step 5: Build and verify**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

- [ ] **Step 6: Commit**

```bash
git add OperationGuidance_new/Views/ReusableWidgets/ProductImageFile.cs
git commit -m "fix: prevent ProductImageFile from disposing shared ProductImageCache references"
```

---

## Why default `_ownsImage = true`

All other `_image` assignment paths (CopyFrom's `new Bitmap`, ImageSelect's `FromFile`, ImageCrop's `CropImage`, RecalculateZoomingRatio's `ReloadImage`) create or return new Image objects that must be disposed. Defaulting to `true` covers them all without touching those lines.

The constructor is the ONE path that holds a cache reference — it explicitly sets `false`. `ReloadImage()` now clones the cache result so it always returns an owned image.
