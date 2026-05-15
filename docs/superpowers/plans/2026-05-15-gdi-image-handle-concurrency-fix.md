# GDI+ Image Handle Concurrency Fix — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fix concurrent GDI+ Image handle corruption causing `ArgumentException` in `CalcImageSize` by cloning before assignment and adding recovery fallback.

**Architecture:** Three-tier defense: (1) `CoverImage` setter clones via `NormalizeImageHandle` to prevent shared references from entering the block, (2) `CalcImageSize` catches corrupted-handle exceptions and attempts `NormalizeImageHandle` → disk reload → default-image fallback, (3) `ImageFileName` property bridges filename from `LoadOneCoverAsync` to `InnerButton` for disk reload recovery.

**Tech Stack:** C# WinForms, GDI+, System.Drawing

---

### Task 1: Make NormalizeImageHandle public

**Files:**
- Modify: `CustomLibrary/Utils/WidgetUtils.cs:368`

- [ ] **Step 1: Change access modifier**

Line 368, change `private` to `public`:

```csharp
public static Image? NormalizeImageHandle(Image image, ILog? logger) {
```

- [ ] **Step 2: Build and verify**

Run: `dotnet build OperationGuidance_new/OperationGuidance_new.csproj`
Expected: Build succeeds (no other changes yet, no callers broken).

- [ ] **Step 3: Commit**

```bash
git add CustomLibrary/Utils/WidgetUtils.cs
git commit -m "feat: expose NormalizeImageHandle as public for cross-assembly GDI+ handle recovery"
```

---

### Task 2: Add ImageFileName property and clone in CoverImage setter

**Files:**
- Modify: `OperationGuidance_new/Views/ReusableWidgets/ProductMissionBlock.cs`

- [ ] **Step 1: Add `ImageFileName` property to `ProductMissionBlock<T>`**

Insert the property after the `Entity` property (currently ends at line 22), before the `CoverImage` property (starts at line 23). The result should look like:

```csharp
public T Entity {
    get => _t;
    set => _t = value;
}
public string? ImageFileName { get; set; }
public Image? CoverImage {
```

- [ ] **Step 2: Clone image in `CoverImage` setter**

Change the setter (lines 25-29) from:

```csharp
set {
    _coverImage = value;
    _innerButton.Icon = value;
    _innerButton.RefreshImage();
}
```

To:

```csharp
set {
    _coverImage = value;
    _innerButton.Icon = value != null ? WidgetUtils.NormalizeImageHandle(value, null) ?? value : null;
    _innerButton.RefreshImage();
}
```

- [ ] **Step 3: Build and verify**

Run: `dotnet build OperationGuidance_new/OperationGuidance_new.csproj`
Expected: Build succeeds.

- [ ] **Step 4: Commit**

```bash
git add OperationGuidance_new/Views/ReusableWidgets/ProductMissionBlock.cs
git commit -m "feat: clone Image in CoverImage setter to prevent shared GDI+ handle corruption"
```

---

### Task 3: Add CalcImageSize try-catch + RecoverIconHandle

**Files:**
- Modify: `OperationGuidance_new/Views/ReusableWidgets/ProductMissionBlock.cs`

- [ ] **Step 1: Extract `CalcImageDimensions` helper**

Add this private method to `InnerButton<T>` right after `CalcImageSize` (after line 208). Returns a tuple because the width-cap branch modifies both dimensions:

```csharp
private (int width, int height) CalcImageDimensions(Image image, int maxHeight) {
    int newHeight = maxHeight;
    int newWidth = (int)(newHeight / (decimal)image.Height * image.Width);
    if (newWidth > (Width * .9)) {
        newWidth = (int)(Width * .9);
        newHeight = (int)(newWidth / (decimal)image.Width * image.Height);
    }
    return (newWidth, newHeight);
}
```

- [ ] **Step 2: Add `RecoverIconHandle` method to `InnerButton<T>`**

Add after the helper:

```csharp
private Image? RecoverIconHandle() {
    // Tier 1: try to repair corrupted handle via PNG round-trip
    var normalized = WidgetUtils.NormalizeImageHandle(this.Icon!, null);
    if (normalized != null) {
        this.Icon!.Dispose();
        this.Icon = normalized;
        _missionBlock._coverImage = normalized;
        return normalized;
    }

    // Tier 2: reload from disk bypassing cache
    this.Icon!.Dispose();
    var fileName = _missionBlock.ImageFileName;
    if (!string.IsNullOrEmpty(fileName)) {
        var reloaded = MainUtils.LoadProductImageFromDisk(fileName);
        if (reloaded != null) {
            this.Icon = reloaded;
            _missionBlock._coverImage = reloaded;
            return reloaded;
        }
    }

    // Tier 3: give up, fall through to _defaultImage
    this.Icon = null;
    _missionBlock._coverImage = null;
    return null;
}
```

Note: `MainUtils` namespace is `OperationGuidance_new.Utils`. Add `using OperationGuidance_new.Utils;` at the top of the file if not already present. Currently the file imports `CustomLibrary.Utils` only (line 4).

- [ ] **Step 3: Add `using OperationGuidance_new.Utils;`**

After line 4 (`using CustomLibrary.Utils;`), add:

```csharp
using OperationGuidance_new.Utils;
```

- [ ] **Step 4: Rewrite `CalcImageSize` with try-catch**

Replace the existing `CalcImageSize` (lines 193-208):

```csharp
private Size CalcImageSize() {
    int newHeight = (int)(Height * ImageRatio);
    int newWidth;
    if (this.Icon != null) {
        try {
            (newWidth, newHeight) = CalcImageDimensions(this.Icon, newHeight);
        } catch (ArgumentException) {
            var recovered = RecoverIconHandle();
            if (recovered != null) {
                (newWidth, newHeight) = CalcImageDimensions(recovered, newHeight);
            } else if (_defaultImage != null) {
                newWidth = (int)(newHeight / (decimal)_defaultImage.Height * _defaultImage.Width);
            } else {
                newWidth = (int)(Width * .8);
            }
        }
    } else if (_defaultImage != null) {
        newWidth = (int)(newHeight / (decimal)_defaultImage.Height * _defaultImage.Width);
    } else {
        newWidth = (int)(Width * .8);
    }
    return new(newWidth, newHeight);
}
```

- [ ] **Step 5: Build and verify**

Run: `dotnet build OperationGuidance_new/OperationGuidance_new.csproj`
Expected: Build succeeds. If `MainUtils` is not accessible, verify the `using` and that `LoadProductImageFromDisk` is `internal` (it is — same assembly, OK).

- [ ] **Step 6: Commit**

```bash
git add OperationGuidance_new/Views/ReusableWidgets/ProductMissionBlock.cs
git commit -m "feat: add GDI+ handle recovery in CalcImageSize with three-tier fallback"
```

---

### Task 4: Pass ImageFileName from LoadOneCoverAsync

**Files:**
- Modify: `OperationGuidance_new/Views/ReusableWidgets/MissionListPanel.cs`

- [ ] **Step 1: Capture filename in Task.Run lambda**

Change `LoadOneCoverAsync` (lines 170-203). The `Task.Run` return type changes from `Image?` to `(Image?, string?)`.

Replace lines 170-203 with:

```csharp
private async Task LoadOneCoverAsync(ProductMissionBlock<ProductMissionDTO> block, CancellationToken ct) {
    await _loadSemaphore.WaitAsync(ct);
    try {
        (Image? image, string? fileName) = await Task.Run(() => {
            ct.ThrowIfCancellationRequested();
            Image? loaded = null;
            string? capturedFileName = null;
            if (block.Entity.ProductSides != null) {
                foreach (var side in block.Entity.ProductSides) {
                    if (!string.IsNullOrEmpty(side.image)) {
                        loaded = ProductImageCache.GetOrLoad(side.image);
                        if (loaded != null) {
                            capturedFileName = side.image;
                            if (side.rotate_angle != null) {
                                loaded = WidgetUtils.RotateImage(loaded, side.rotate_angle.Value, dispose: false);
                            }
                            break;
                        }
                    }
                }
            }
            return (loaded, capturedFileName);
        }, ct);

        if (image != null && !ct.IsCancellationRequested && !IsDisposed) {
            BeginInvoke(() => {
                if (!block.IsDisposed && block.Parent != null) {
                    block.ImageFileName = fileName;
                    block.CoverImage = image;
                }
            });
        }
    } catch (OperationCanceledException) {
    } finally {
        _loadSemaphore.Release();
    }
}
```

- [ ] **Step 2: Build and verify**

Run: `dotnet build OperationGuidance_new/OperationGuidance_new.csproj`
Expected: Build succeeds.

- [ ] **Step 3: Commit**

```bash
git add OperationGuidance_new/Views/ReusableWidgets/MissionListPanel.cs
git commit -m "feat: pass image filename to ProductMissionBlock for handle recovery"
```

---

### Task 5: Final verification and review

- [ ] **Step 1: Full build**

Run: `dotnet build OperationGuidance_new/OperationGuidance_new.csproj`
Expected: Clean build, no warnings.

- [ ] **Step 2: Verify only intended files changed**

Run: `git diff master...HEAD --name-only`
Expected: Exactly 3 files: `CustomLibrary/Utils/WidgetUtils.cs`, `OperationGuidance_new/Views/ReusableWidgets/ProductMissionBlock.cs`, `OperationGuidance_new/Views/ReusableWidgets/MissionListPanel.cs`.

---

## Summary of Changes

| File | Change |
|---|---|
| `CustomLibrary/Utils/WidgetUtils.cs:368` | `private` → `public` on `NormalizeImageHandle` |
| `ProductMissionBlock.cs` | Add `ImageFileName` property; clone in `CoverImage` setter; `CalcImageDimensions` helper; `RecoverIconHandle` three-tier recovery; `CalcImageSize` try-catch |
| `MissionListPanel.cs` | `LoadOneCoverAsync` returns tuple `(Image?, string?)`, sets `block.ImageFileName` before `block.CoverImage` |
