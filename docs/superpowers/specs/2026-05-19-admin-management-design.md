# Admin Management Feature Design

2026-05-19

## Overview

Add a back-office management interface accessible from the login form. Only DEVELOPER (role 1) and ADMIN (role 2) can enter. Two features: admin password management and parts-barcode re-import.

## Flow

```
LoginView (shared)
├── [后台管理] → login validation → role check → AdminManagementView
├── [登录]     → normal login → MainForm normal UI
└── [退出]     → exit app
```

- **Admin login**: validates credentials via existing `CheckLoginByApi`. If `IsAdmin` (DEVELOPER or ADMIN), MainForm loads `AdminManagementView` instead of normal menu layout. Non-admin gets "权限不足，仅管理员可访问后台管理".
- **Return button**: top-left, returns to LoginView.
- **LoginView**: `_btnAdminLogin` placed left of existing "登录" button. LoginView itself unchanged — caller controls post-login destination.

## New/Modified Files

| File | Action | Purpose |
|------|--------|---------|
| `Views/LoginView.cs` | Modify | Add `_btnAdminLogin` button + callback distinction |
| `Views/AdminManagementView.cs` | **New** | Admin management main view, inherits `CustomContentPanel` |
| `CustomLibrary/Panels/CardPanel.cs` | **New** | GDI+ custom-drawn card control |
| `OperationGuidance_service/OperationGuidanceApis.cs` | Modify | Add `ChangeAdminPassword`, `ReimportPartsBarcode` APIs |
| `MainForm.Designer.cs` | Modify | Branch on admin login to load `AdminManagementView` |
| `OperationGuidance_service/Constants/Roles.cs` | No change | Existing `IsAdmin` covers DEVELOPER + ADMIN |

## CardPanel — Custom GDI+ Control

Inherits `Panel`. Designed for low-spec industrial PCs.

- **Rounded corners**: 8px radius, `GraphicsPath` rebuilt on `OnResize`, cached in field.
- **Shadow**: offset rectangle (4px right, 4px down), solid `#D0D0D0`, drawn before card body.
- **Fill**: white background (`#FFFFFF`) with 1px border `#E0E0E0`.
- **No gradients, no blur, no animation** — static paint only.
- `OnPaint` reads cached `GraphicsPath`, draws shadow rect, draws card body. Zero allocation per paint.

Internal content uses standard WinForms controls (Label, TextBox, Button) added as children — no GDI+ text rendering.

## Card 1: Modify Admin Password

- Title label: "修改管理员密码"
- Input fields (TextBox, masked): `password` (login password), `operation_password` (operation password)
- Both fields empty = no change for that field
- "保存修改" Button → calls `ChangeAdminPassword` API
- API: look up account="admin" in `user_account_info`, MD5-hash non-empty inputs, update corresponding columns
- Success → `MessageBox.Show("密码修改成功")`

## Card 2: Re-import Parts Barcode

- Title label: "重新导入物料码"
- Description label: explains the operation and warns it may take time
- "重新导入物料码" Button
- Click → confirmation `MessageBox` ("此操作将清空并重新导入物料码数据，可能需要较长时间，确定继续？")
- Confirmed → show loading overlay (static text "正在重新导入物料码，请稍候..." + `ProgressBarStyle.Marquee`) → run via `Task.Run`
- API `ReimportPartsBarcode` (C# logic, database-agnostic):
  1. `DELETE FROM parts_bar_code WHERE deleted = 2`
  2. Query `mission_record` rows where `parts_bar_code IS NOT NULL AND parts_bar_code != ''`
  3. For each row, `Split(',')` on the barcode field, `INSERT INTO parts_bar_code` per barcode value
  4. Check `sql_execute_record` for filename matching current DB type (e.g., `modify_mysql_20250625_1` for MySQL, `modify_sqlite_20250625_1` for SQLite) — if absent, insert record to prevent re-execution by migration system
- Complete → hide overlay → `MessageBox` with elapsed time / row count
- Error → hide overlay → `MessageBox` with error detail
- API validates `IsAdmin` before execution

## Database Abstraction

Re-import uses C# string splitting + bulk INSERT, not raw SQL from the migration scripts. This avoids MySQL vs SQLite vs SQL Server dialect differences. Only the DELETE and SELECT are standard SQL.

## Performance Considerations

- CardPanel draws once, no invalidation loop — negligible GPU/CPU cost
- Marquee progress bar cost is trivial (one timer tick, zero data coupling)
- `Task.Run` executes DB work off UI thread; single `BeginInvoke` back for result
- No per-row UI updates — all barcode rows processed in one batch on thread-pool thread
- `Split(',')` is allocation-heavy for large datasets but acceptable given this is a one-shot maintenance operation on a low-QPS admin screen

## Permission Model

- UI gate: LoginView validates `IsAdmin` before navigating to AdminManagementView
- API gate: `ChangeAdminPassword` and `ReimportPartsBarcode` both check `IsAdmin` server-side
- `IsAdmin` = `role_type == (int)Roles.DEVELOPER || role_type == (int)Roles.ADMIN`
