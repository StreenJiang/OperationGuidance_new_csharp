# Close Button FormClosing Fix & Wider Overlay Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fix close button not working because FormClosing handler cancels UserClosing, and widen overlay to 700px.

**Architecture:** Single-file change to `AdminManagementView.cs` — extract FormClosing lambdas into named methods for unsubscription, remove handlers in OnReimport finally before enabling close button, increase widths.

**Tech Stack:** C# WinForms, .NET 8

---

### Task 1: Fix close button + widen overlay

**Files:**
- Modify: `OperationGuidance_new/Views/AdminManagementView.cs`

- [ ] **Step 1: Replace backdrop FormClosing lambda with named method**

Old (lines 54-56):
```csharp
            _overlayBackdrop.FormClosing += (s, e) => {
                if (e.CloseReason == CloseReason.UserClosing) e.Cancel = true;
            };
```

New:
```csharp
            _overlayBackdrop.FormClosing += OnOverlayFormClosing;
```

- [ ] **Step 2: Replace popup FormClosing lambda with named method**

Old (lines 67-69):
```csharp
            _overlayPopup.FormClosing += (s, e) => {
                if (e.CloseReason == CloseReason.UserClosing) e.Cancel = true;
            };
```

New:
```csharp
            _overlayPopup.FormClosing += OnPopupFormClosing;
```

- [ ] **Step 3: Add named FormClosing methods**

Add after `CreateOverlayForms()` method (after its closing `}` at line ~155), before the constructor:

```csharp
        private void OnOverlayFormClosing(object? sender, FormClosingEventArgs e) {
            if (e.CloseReason == CloseReason.UserClosing) e.Cancel = true;
        }

        private void OnPopupFormClosing(object? sender, FormClosingEventArgs e) {
            if (e.CloseReason == CloseReason.UserClosing) e.Cancel = true;
        }
```

- [ ] **Step 4: Remove FormClosing handlers in finally before enabling close button**

Old (lines 421-425):
```csharp
            } finally {
                _closeBtn.Text = "关闭";
                _closeBtn.Enabled = true;
                _reimportBtn.Enabled = true;
            }
```

New:
```csharp
            } finally {
                _overlayBackdrop.FormClosing -= OnOverlayFormClosing;
                _overlayPopup.FormClosing -= OnPopupFormClosing;
                _closeBtn.Text = "关闭";
                _closeBtn.Enabled = true;
                _reimportBtn.Enabled = true;
            }
```

- [ ] **Step 5: Widen overlay**

- Popup `Width = 600` (line 64) → `Width = 700`
- LogBox `Width = 536` (line 96) → `Width = 636`
- ProgressBar `Width = 536` (line 103) → `Width = 636`

- [ ] **Step 6: Build**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

- [ ] **Step 7: Commit**

```bash
git add OperationGuidance_new/Views/AdminManagementView.cs
git commit -m "fix: remove FormClosing handlers before enabling close button, widen to 700"
```

---

### Task 2: Final build and verify

- [ ] **Step 1: Full build**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```
Expected: 0 errors.

- [ ] **Step 2: Commit log**

```bash
git log --oneline -3
```

- [ ] **Step 3: Smoke test**

1. Login as admin, click "重新导入物料码"
2. Wait for completion → close button enabled "关闭"
3. **Click "关闭" → both forms close**
