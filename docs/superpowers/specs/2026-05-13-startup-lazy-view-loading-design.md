# Startup Lazy View Loading Design

## Problem

After login, `AfterLogin()` creates all ~30 views synchronously on the UI thread via reflection before calling `mainPanel.Show()`. The UI is frozen (blank screen + unresponsive mouse) until all views are constructed.

## Goal

- **Primary**: Minimize mouse freeze time after login — UI shell appears and is responsive within ~100ms
- **Secondary**: Views load lazily on first menu click so users can interact immediately with the first view
- **Fallback**: Loading mask overlay blocks interaction while a view is being created

## Design

### Architecture: Shell-First, Views On-Demand

```
Login → Create shell + menu buttons (lightweight) → mainPanel.Show() → async create OpenFirst view
                                                                         ↑ UI responsive immediately
Other views → created on first menu click → loading mask covers content area during creation
```

### Three Changes

1. **`MainForm.Designer.cs` `AfterLogin()`** — split the for loop: buttons created eagerly, views deferred
2. **New file `Views/ReusableWidgets/LoadingMaskPanel.cs`** — semi-transparent overlay with centered "Loading..." label
3. **Menu buttons** — pre-subscribe Click before panel adds; first click creates view and replaces placeholder

### LoadingMaskPanel

- `Panel` subclass, `Dock.Fill`, semi-transparent black background (alpha 180)
- Centered `Label` with text "Loading..."
- `ShowMask()` / `HideMask()` methods
- Being topmost naturally captures all mouse events, blocking interaction
- Single instance on `mainContentPanel`, shared for all views (including child menu views)

### AfterLogin Changes

**Current** (lines 264-428): for each menuConfig, creates button AND view via reflection, adds both to panels, then `mainPanel.Show()`.

**New**: for each menuConfig:
- Create button (lightweight)
- If `OpenFirst`: create view immediately, assign to button
- Else: set lightweight placeholder `CustomContentPanel` as `CorrespondingContentPanel`, pre-subscribe Click for lazy creation (closure captures creation params)
- For child menus: create shell structure (`CustomTabPanel` + child panels) eagerly (lightweight), defer child views with same pattern
- After loop: `mainPanel.Show()`, then show mask, `await Task.Yield()`, create OpenFirst view, hide mask, trigger click

### Lazy View Creation (on first menu click)

```
User clicks menu button
  → Pre-subscribed Click handler fires (before menu panel toggle handler)
    → Check if CorrespondingContentPanel is placeholder
    → LoadingMaskPanel.ShowMask()
    → CreateViewPanel(type, name, button) — extracted helper method
    → Replace placeholder in mainContentPanel.Controls (RemoveAt + Add)
    → Update button.CorrespondingContentPanel
    → LoadingMaskPanel.HideMask()
    → Unsubscribe self
  → Menu panel toggle handler fires → ShowContentPanel() on real view
```

### CreateViewPanel Helper

Extracted private method to avoid duplicating view creation logic:
```
CustomVScrollingContentPanel CreateViewPanel(Type type, string name, CustomMenuButton button)
  - type.Assembly.CreateInstance(type.FullName)
  - Cast to CustomContentPanel, set Name, CorrespondingMenuButton
  - WidgetUtils.AddView(contentPanel)
  - Return new CustomVScrollingContentPanel wrapping it
```

### OpenFirst Preload

After `mainPanel.Show()`:
1. `ShowMask()` — immediate
2. `await Task.Yield()` — yield UI thread so shell renders + mask appears
3. `CreateViewPanel(...)` for OpenFirst view
4. `HideMask()`
5. `openFirstButton.PerformClick()`

`AfterLogin` changes from `void` to `async void` to support `await Task.Yield()`.

### Re-login Scenario

`WidgetUtils.BackToLoginView` calls `AfterLogin()` again on re-login. Since `AfterLogin` is modified to lazy-load, re-login behaves identically — shell appears fast, views created on demand. `AllCreated` semantics unchanged (means "shell exists").

### Key Decisions (from grilling)

- **Placeholder**: simple `CustomContentPanel` (no subclasses), replaced via pre-subscribed Click handler — cleaner than `OnVisibleChanged` approach
- **Child menus**: shell (CustomTabPanel + panels + buttons) created eagerly; only child VIEWS deferred
- **Storage**: closure captures creation params (Type, name, button) — no new types or dictionaries
- **Loading mask**: single instance on `mainContentPanel`, shared — not per-child-content-area
- **View replacement**: `Controls.Add()` rather than `Insert()` — only one visible at a time, order irrelevant
- **`AddView()` / `AddChildMenu()` / `AddMainMenu()`**: button registries called eagerly (buttons exist); `AddView()` called inside `CreateViewPanel` (view exists when created)

### Scope

- **Modified**: `MainForm.Designer.cs` (1 file)
- **New**: `Views/ReusableWidgets/LoadingMaskPanel.cs` (1 file)
- **Not modified**: `CustomLibrary`, `MenuConfig`, `SystemConfigs`, `TaskInitializer`
