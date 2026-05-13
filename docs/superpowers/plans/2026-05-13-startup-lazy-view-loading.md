# Startup Lazy View Loading Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Eliminate post-login UI freeze by deferring view creation from startup to first menu click, reducing mouse-freeze time from multi-second to ~100ms.

**Architecture:** Split `AfterLogin()` loop into shell-only eager path (menu buttons, panels) and lazy view path (reflection-based view creation on first click). Single semi-transparent LoadingMaskPanel covers content area during view construction.

**Tech Stack:** WinForms .NET 6, C# closures for lazy-creation state, `async void` for OpenFirst preload

---

## File Map

| Action | File | Responsibility |
|--------|------|----------------|
| New | `Views/ReusableWidgets/LoadingMaskPanel.cs` | Semi-transparent overlay blocking interaction during view creation |
| Modify | `MainForm.Designer.cs` | Add `_loadingMask` field, change `AfterLogin` to `async void`, extract `CreateViewPanel` helper, restructure menu loop for lazy loading |

---

### Task 1: Create LoadingMaskPanel

**Files:**
- Create: `OperationGuidance_new/Views/ReusableWidgets/LoadingMaskPanel.cs`

- [ ] **Step 1: Write LoadingMaskPanel class**

```csharp
using System.Drawing;

namespace OperationGuidance_new.Views.ReusableWidgets {
    public class LoadingMaskPanel : Panel {
        private readonly Label _loadingLabel;

        public LoadingMaskPanel() {
            BackColor = Color.FromArgb(180, 0, 0, 0);
            Dock = DockStyle.Fill;
            Visible = false;

            _loadingLabel = new Label {
                Text = "加载中...",
                ForeColor = Color.White,
                Font = new Font("Microsoft YaHei", 16F, FontStyle.Bold, GraphicsUnit.Point),
                AutoSize = true,
            };
            Controls.Add(_loadingLabel);
        }

        protected override void OnSizeChanged(EventArgs e) {
            base.OnSizeChanged(e);
            _loadingLabel.Location = new Point(
                (Width - _loadingLabel.Width) / 2,
                (Height - _loadingLabel.Height) / 2
            );
        }

        public void ShowMask() {
            Visible = true;
            BringToFront();
        }

        public void HideMask() {
            Visible = false;
        }
    }
}
```

- [ ] **Step 2: Build to verify compilation**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

Expected: Build succeeds (no usages yet, but file compiles).

- [ ] **Step 3: Commit**

```bash
git add OperationGuidance_new/Views/ReusableWidgets/LoadingMaskPanel.cs
git commit -m "$(cat <<'EOF'
feat: add LoadingMaskPanel for startup lazy loading

- Semi-transparent overlay (alpha 180 black background)
- Centered "加载中..." label for loading state
- ShowMask()/HideMask() with BringToFront for event capture
- Single instance shared across all lazy-loaded views

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>
EOF
)"
```

---

### Task 2: Add `_loadingMask` field and `CreateViewPanel` helper

**Files:**
- Modify: `OperationGuidance_new/MainForm.Designer.cs`

- [ ] **Step 1: Add `_loadingMask` field and `using` for the new namespace**

Add after line 48 (`private OperatorView? _operatorView = null;`):

```csharp
private LoadingMaskPanel? _loadingMask;
```

The `using OperationGuidance_new.Views.ReusableWidgets;` is already covered by `using OperationGuidance_new.Views;` since the file uses no other types from `ReusableWidgets` explicitly... Actually, `LoadingMaskPanel` is in `OperationGuidance_new.Views.ReusableWidgets`. The existing `using OperationGuidance_new.Views;` does NOT cover sub-namespaces in C#. Need to verify.

Add this using alongside the existing ones around line 16:

```csharp
using OperationGuidance_new.Views.ReusableWidgets;
```

- [ ] **Step 2: Add `CreateViewPanel` private method**

Add before `#endregion` at line 487:

```csharp
private CustomVScrollingContentPanel CreateViewPanel(Type type, string name, CustomMenuButton button) {
    object instance = type.Assembly.CreateInstance(type.FullName)!;
    CustomContentPanel contentPanel = (CustomContentPanel)instance;
    contentPanel.Name = name;
    if (contentPanel.Controls.Count == 0) {
        int hPadding = contentPanel.Width / 2;
        int vPadding = contentPanel.Height / 2;
        contentPanel.Controls.Add(new Label() {
            Text = "载入错误，没有找到对应的功能",
            AutoSize = true,
            Margin = new Padding(hPadding, vPadding, hPadding, vPadding)
        });
    }
    contentPanel.CorrespondingMenuButton = button;
    WidgetUtils.AddView(contentPanel);
    return new CustomVScrollingContentPanel(null, contentPanel, false, true) {
        Name = name
    };
}
```

- [ ] **Step 3: Build to verify**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

Expected: Build succeeds (method not yet called, but compiles).

- [ ] **Step 4: Commit**

```bash
git add OperationGuidance_new/MainForm.Designer.cs
git commit -m "$(cat <<'EOF'
feat: add _loadingMask field and CreateViewPanel helper

- _loadingMask field for shared overlay during lazy view creation
- CreateViewPanel extracts common view instantiation logic:
  reflection-based instance creation, naming, error-label fallback,
  WidgetUtils.AddView registration, VScrollingContentPanel wrapping

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>
EOF
)"
```

---

### Task 3: Restructure AfterLogin — split eager shell from lazy views

**Files:**
- Modify: `OperationGuidance_new/MainForm.Designer.cs`

This is the core change. The loop at lines 263-428 gets restructured to:
- Eager path: buttons, menu panels, child shells
- Lazy path: view creation deferred to first click via pre-subscribed Click handler

- [ ] **Step 1: Change `AfterLogin` signature from `void` to `async void`**

Line 205 — change:
```csharp
private void AfterLogin(Size mainFormSize) {
```
to:
```csharp
private async void AfterLogin(Size mainFormSize) {
```

- [ ] **Step 2: Create `_loadingMask` after `mainContentPanel`**

After line 245 (`mainContentPanel.Name = "mainContentPanel";`), add:

```csharp
// Loading mask for lazy view creation
_loadingMask = new LoadingMaskPanel();
mainContentPanel.Controls.Add(_loadingMask);
```

- [ ] **Step 3: Restructure the menu loop — helper closure for top-level lazy view creation**

Before the for loop at line 264, capture the appVersion once (currently it's inside the `else` branch at line 285):

```csharp
AppVersion appVersion = (AppVersion)Enum.Parse(typeof(AppVersion), MainUtils.License.AppVersion);
```

Then replace lines 273-307 (the top-level view creation block inside the `if (mainMenuConfig.ViewTypes != null ...)` branch) with the new lazy pattern:

```csharp
if (mainMenuConfig.ViewTypes != null && mainMenuConfig.ViewTypes.Count > 0) {
    if (!MainUtils.License.MenuIds.ContainsKey(mainMenuConfig.Id)) {
        // License missing — lightweight error panel created eagerly
        CustomContentPanel contentPanelTemp = new();
        contentPanelTemp.Name = "mainContentPanel_" + mainMenuConfig.Id;
        int hPadding = contentPanelTemp.Width / 2;
        int vPadding = contentPanelTemp.Height / 2;
        contentPanelTemp.Controls.Add(new Label() {
            Text = "许可证信息缺失", AutoSize = true,
            Margin = new Padding(hPadding, vPadding, hPadding, vPadding)
        });
        contentPanelTemp.CorrespondingMenuButton = mainMenuButton;
        mainMenuButton.CorrespondingContentPanel = new CustomVScrollingContentPanel(null, contentPanelTemp, false, true) {
            Name = contentPanelTemp.Name
        };
    } else {
        Type type;
        if (mainMenuConfig.ViewTypes.ContainsKey(appVersion)) {
            type = mainMenuConfig.ViewTypes[appVersion];
        } else {
            type = mainMenuConfig.ViewTypes[AppVersion.STANDARD];
        }

        if (type == typeof(CustomTabPanel)) {
            // Child-menu shell — create eagerly
            CustomTabPanel childTapPanel = new();
            CustomChildMenuFirstPanel childMenuPanel = new();
            CustomContentPanelBase childContentPanel = new();
            childMenuPanel.BackColor = ColorConfigs.COLOR_CHILD_MENU_BACKGROUND;
            childMenuPanel.Margin = new Padding(0);
            childMenuPanel.Name = "mainMenuPanel";
            childMenuPanel.PanelDirection = MenuPanelDirection.LEFT;
            childMenuPanel.NeedFoldButton = true;
            childMenuPanel.FoldButton.FoldedIcon = Properties.Resources.navigator_fold;
            childMenuPanel.FoldButton.UnfoldedIcon = Properties.Resources.navigator_unfold;
            childMenuPanel.FoldButton.ForeColor = ColorConfigs.COLOR_MENU_FOREGROUND;
            if (mainMenuConfig.IsUserInfoPanel) {
                childMenuPanel.ShowUserInfoPanel(SystemUtils.LoggedUserName);
            }
            childContentPanel.BackColor = ColorConfigs.COLOR_MAIN_FORM_BACKGROUND;
            childContentPanel.Margin = new Padding(0);
            childContentPanel.Name = "mainContentPanel";

            List<MenuConfig> childMenuConfigs = mainMenuConfig.Children!;
            for (int j = 0; j < childMenuConfigs.Count; j++) {
                MenuConfig childMenuConfig = childMenuConfigs[j];
                CustomChildMenuFirstButton childMenuFirstButton = new();
                childMenuFirstButton.Name = "childMenuFirstButton_" + childMenuConfig.Id;
                childMenuFirstButton.Icon = childMenuConfig.Icon;
                childMenuFirstButton.Label = childMenuConfig.Name;
                if (childMenuConfig.Click != null) {
                    childMenuFirstButton.OnMenuButtonClick += childMenuConfig.Click;
                }
                if (childMenuConfig.ViewTypes != null && childMenuConfig.ViewTypes.Count > 0) {
                    Type childType;
                    if (childMenuConfig.ViewTypes.ContainsKey(appVersion)) {
                        childType = childMenuConfig.ViewTypes[appVersion];
                    } else {
                        if (!childMenuConfig.ViewTypes.ContainsKey(AppVersion.STANDARD)) {
                            continue;
                        }
                        childType = childMenuConfig.ViewTypes[AppVersion.STANDARD];
                    }
                    // License check: missing → eager error; OK → lazy placeholder
                    if (MainUtils.License.MenuIds[mainMenuConfig.Id] == null
                            || !MainUtils.License.MenuIds[mainMenuConfig.Id].Contains(childMenuConfig.Id)) {
                        CustomContentPanel childContentPanelTemp = new();
                        childContentPanelTemp.Name = "childContentPanel_" + j;
                        int hp = childContentPanelTemp.Width / 2;
                        int vp = childContentPanelTemp.Height / 2;
                        childContentPanelTemp.Controls.Add(new Label() {
                            Text = "许可证信息缺失", AutoSize = true,
                            Margin = new Padding(hp, vp, hp, vp)
                        });
                        childContentPanelTemp.CorrespondingMenuButton = childMenuFirstButton;
                        childMenuFirstButton.CorrespondingContentPanel = new CustomVScrollingContentPanel(null, childContentPanelTemp, false, true) {
                            Name = childContentPanelTemp.Name
                        };
                    } else {
                        // Lazy placeholder for child view
                        CustomContentPanel childPlaceholder = new();
                        childPlaceholder.Name = "lazyPlaceholder_child_" + childMenuConfig.Id;
                        childMenuFirstButton.CorrespondingContentPanel = childPlaceholder;

                        // Pre-subscribe Click for lazy creation (fires before menu panel toggle handler)
                        EventHandler childLazyLoader = null!;
                        childLazyLoader = (s, e) => {
                            childMenuFirstButton.Click -= childLazyLoader;
                            _loadingMask!.ShowMask();
                            CustomVScrollingContentPanel realView = CreateViewPanel(childType, "childContentPanel_" + j, childMenuFirstButton);
                            int replaceIdx = childContentPanel.Controls.IndexOf(childPlaceholder);
                            childContentPanel.Controls.RemoveAt(replaceIdx);
                            childPlaceholder.Dispose();
                            childContentPanel.Controls.Add(realView);
                            childMenuFirstButton.CorrespondingContentPanel = realView;
                            realView.Visible = false; // toggle handler will show it
                            _loadingMask!.HideMask();
                        };
                        childMenuFirstButton.Click += childLazyLoader;
                    }
                }
                childMenuFirstButton.ToggledButton = childMenuConfig.IsToggleButton;
                childMenuFirstButton.GroupMode = true;
                childMenuFirstButton.BackColor = ColorConfigs.COLOR_CHILD_MENU_BACKGROUND;
                childMenuFirstButton.ConerRadius = 0;
                childMenuFirstButton.FlatAppearance.BorderSize = 0;
                childMenuFirstButton.FlatStyle = FlatStyle.Flat;
                childMenuFirstButton.ForeColor = ColorConfigs.COLOR_MENU_FOREGROUND;
                childMenuFirstButton.Margin = new Padding(0);
                childMenuFirstButton.ToggleBar = true;
                childMenuFirstButton.ToggleBarDirection = AbstractCustomButton.ToggleBarDirectionEnum.LEFT;
                childMenuFirstButton.ToggledColor = ColorConfigs.COLOR_MENU_TOGGLED;

                WidgetUtils.AddChildMenu(childMenuConfig.Id, childMenuFirstButton);
                childMenuPanel.Controls.Add(childMenuFirstButton);
                childContentPanel.Controls.Add(childMenuFirstButton.CorrespondingContentPanel);
            }

            childTapPanel.BackColor = ColorConfigs.COLOR_MAIN_FORM_BACKGROUND;
            childTapPanel.Controls.Add(childMenuPanel);
            childTapPanel.Controls.Add(childContentPanel);
            childTapPanel.Margin = new Padding(0);
            childTapPanel.Name = "childFirstPanel";

            mainMenuButton.CorrespondingContentPanel = childTapPanel;
        } else {
            // Simple CustomContentPanel view — lazy
            if (mainMenuConfig.OpenFirst) {
                // Eager: placeholder for now, real view created after mainPanel.Show()
                CustomContentPanel openFirstPlaceholder = new();
                openFirstPlaceholder.Name = "lazyPlaceholder_" + mainMenuConfig.Id;
                mainMenuButton.CorrespondingContentPanel = openFirstPlaceholder;
            } else {
                // Lazy placeholder
                CustomContentPanel placeholder = new();
                placeholder.Name = "lazyPlaceholder_" + mainMenuConfig.Id;
                mainMenuButton.CorrespondingContentPanel = placeholder;

                EventHandler lazyLoader = null!;
                lazyLoader = (s, e) => {
                    mainMenuButton.Click -= lazyLoader;
                    _loadingMask!.ShowMask();
                    CustomVScrollingContentPanel realView = CreateViewPanel(type, "mainContentPanel_" + mainMenuConfig.Id, mainMenuButton);
                    int replaceIdx = mainContentPanel.Controls.IndexOf(placeholder);
                    mainContentPanel.Controls.RemoveAt(replaceIdx);
                    placeholder.Dispose();
                    mainContentPanel.Controls.Add(realView);
                    mainMenuButton.CorrespondingContentPanel = realView;
                    realView.Visible = false; // toggle handler will show it
                    _loadingMask!.HideMask();
                };
                mainMenuButton.Click += lazyLoader;
            }
        }
    }
}
```

- [ ] **Step 4: Update the `CorrespondingContentPanel != null` guard and remove in-loop PerformClick**

Change lines 421-427, keeping the null guard but removing in-loop `PerformClick` (moved to after loop):

```csharp
if (mainMenuButton.CorrespondingContentPanel != null) {
    mainMenuButton.CorrespondingContentPanel.Visible = false;
}
```

Remove lines 424-427 (`if (mainMenuConfig.OpenFirst) { ... PerformClick() }`) — the OpenFirst is handled after the loop.

- [ ] **Step 5: Replace lines 430-431 with OpenFirst preload + mask flow**

Replace:
```csharp
AllCreated = true;
mainPanel.Show();
```

with:

```csharp
// Show mask before the shell renders
_loadingMask!.ShowMask();
AllCreated = true;
mainPanel.Show();

// Find OpenFirst button
CustomMainMenuButton? openFirstBtn = null;
MenuConfig? openFirstConfig = null;
Type? openFirstType = null;
foreach (MenuConfig mc in menuCongfigs) {
    if (mc.OpenFirst && mc.ViewTypes != null && mc.ViewTypes.Count > 0
            && MainUtils.License.MenuIds.ContainsKey(mc.Id)) {
        openFirstConfig = mc;
        if (openFirstConfig.ViewTypes.ContainsKey(appVersion)) {
            openFirstType = openFirstConfig.ViewTypes[appVersion];
        } else {
            openFirstType = openFirstConfig.ViewTypes[AppVersion.STANDARD];
        }
        openFirstBtn = WidgetUtils.GetMainMenu(mc.Id);
        break;
    }
}

// Yield to let shell + mask render
await Task.Yield();

if (openFirstBtn != null && openFirstType != null && openFirstConfig != null) {
    // Create OpenFirst view
    CustomVScrollingContentPanel realView = CreateViewPanel(openFirstType,
        "mainContentPanel_" + openFirstConfig.Id, openFirstBtn);

    // Replace placeholder with real view
    Control? oldPlaceholder = openFirstBtn.CorrespondingContentPanel;
    if (oldPlaceholder != null) {
        int replaceIdx = mainContentPanel.Controls.IndexOf(oldPlaceholder);
        if (replaceIdx >= 0) {
            mainContentPanel.Controls.RemoveAt(replaceIdx);
            oldPlaceholder.Dispose();
        }
    }
    mainContentPanel.Controls.Add(realView);
    openFirstBtn.CorrespondingContentPanel = realView;
    WidgetUtils.CurrentPanel = realView.ContentPanel as CustomContentPanel;
}

// Hide mask
_loadingMask!.HideMask();
```

- [ ] **Step 6: Remove the leftover `appVersion` declaration inside the old block**

The `AppVersion appVersion = ...` declaration at the old line 285 is now obsolete since it's moved outside the loop. Remove it from the old location.

- [ ] **Step 7: Build and verify no compile errors**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj
```

Expected: Build succeeds.

- [ ] **Step 8: Commit**

```bash
git add OperationGuidance_new/MainForm.Designer.cs
git commit -m "$(cat <<'EOF'
feat: implement startup lazy view loading in AfterLogin

- Change AfterLogin to async void for await Task.Yield()
- Create _loadingMask on mainContentPanel after panel init
- Extract appVersion scope outside menu loop
- Top-level views: placeholder + pre-subscribed Click for lazy creation
- Child-menu shell: created eagerly (lightweight panels + buttons)
- Child-menu views: same placeholder + lazy Click pattern
- License-missing views: created eagerly (just a label, negligible)
- OpenFirst: placeholder set in loop, real view created after
  mainPanel.Show() + mask, then revealed
- Remove in-loop PerformClick for OpenFirst

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>
EOF
)"
```

---

### Task 4: Verify behavior

- [ ] **Step 1: Build release configuration**

```bash
dotnet build OperationGuidance_new/OperationGuidance_new.csproj -c Release
```

Expected: Build succeeds.

- [ ] **Step 2: Manual verification checklist**

Cannot run automated tests for WinForms startup. Manual verification:
1. Launch app → login → verify shell (menu bar + content area) appears within ~1s
2. Verify loading mask appears briefly then first view (WorkplaceMission) loads
3. Click each main menu item → verify mask appears, view loads, mask disappears
4. Click child menu items (任务管理 → 任务列表, 参数配置 → 账号管理等) → same mask+load behavior
5. Re-login → verify same behavior (shell fast, views lazy)
6. Verify non-view menu items still work (退出, 用户信息)

---

### Self-Review

**1. Spec coverage:**
- LoadingMaskPanel class → Task 1 ✓
- AfterLogin async void + Task.Yield → Task 3 Step 1, Step 5 ✓
- Eager shell (buttons + panels) → Task 3 Step 3 ✓
- Placeholder + pre-subscribe Click pattern → Task 3 Step 3 ✓
- CreateViewPanel helper → Task 2 Step 2 ✓
- Child menu shells eager, child views lazy → Task 3 Step 3 ✓
- OpenFirst preload flow → Task 3 Step 5 ✓
- Single mask on mainContentPanel → Task 2 Step 1, Task 3 Step 2 ✓
- Controls.Add() for replacement → Task 3 Step 3 (lazyLoader) ✓
- Closures for storage → Task 3 Step 3 (captured in lambda) ✓
- Re-login scenario unchanged → spec says compatible, no code change ✓

**2. Placeholder scan:** No TBD/TODO/incomplete code blocks. All code shown complete.

**3. Type consistency:**
- `_loadingMask` typed as `LoadingMaskPanel?` in Task 2, used as `_loadingMask!.ShowMask()` in Task 3 ✓
- `CreateViewPanel` returns `CustomVScrollingContentPanel`, assigned to `realView` consistently ✓
- `CustomContentPanel` used as placeholder type, `Dispose()` called on replacement ✓
- `CustomMainMenuButton` and `CustomChildMenuFirstButton` both extend `CustomMenuButton`, compatible with `CreateViewPanel` parameter ✓
