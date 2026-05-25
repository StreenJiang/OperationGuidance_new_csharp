# ReimportPartsBarcode Rollback Guard

2026-05-24

## Problem

`ReimportPartsBarcode` at line 336 calls `transaction.Rollback()` unconditionally in the catch block. When the original exception is a connection failure (network loss, timeout) or table lock, the connection is no longer valid/open — `Rollback()` throws `System.InvalidOperationException: 'Connection must be valid and open to rollback transaction'`, which masks the root cause.

Additionally, table-lock failures (common in SQLite with concurrent access) display only a raw exception to the user, who doesn't know what "database table is locked" means or what to do about it.

## Solution

Two changes to the catch block in `ReimportPartsBarcode` (`OperationGuidance_service/Controllers/OperationGuidanceApis.cs:335-337`):

### 1. Guard rollback with connection state check

```csharp
if (conn.State == ConnectionState.Open) {
    transaction.Rollback();
}
```

Prevents the secondary `InvalidOperationException` when the connection is already closed/broken.

### 2. Show user-friendly popup for table-lock errors

Add a private helper method:

```csharp
private static bool IsTableLockError(Exception ex) {
    string msg = ex.Message;
    return msg.Contains("lock", StringComparison.OrdinalIgnoreCase)
        || msg.Contains("busy", StringComparison.OrdinalIgnoreCase)
        || msg.Contains("timeout", StringComparison.OrdinalIgnoreCase);
}
```

In the catch block, after setting `rsp.ErrorMessage`:

```csharp
if (IsTableLockError(ex)) {
    SystemUtils.ShowWarningPopUp("数据库繁忙，请稍后重试。\n\n（某个数据表正在执行其他操作，请等待片刻后再次点击\"重新导入物料码\"）");
}
```

Covers: SQLite "database is locked" / "database table is locked", MySQL "Lock wait timeout exceeded", SQL Server lock timeout.

Non-lock errors still propagate via `rsp.ErrorMessage` and display in the UI log area (existing behavior from the overlay improvements).

## Files Changed

| File | Action | Purpose |
|------|--------|---------|
| `Controllers/OperationGuidanceApis.cs` | Modify | Guard rollback + lock-error popup |

## Data Flow

```
Exception thrown in try block (e.g., table lock)
  → catch (Exception ex)
    → conn.State == Open? → transaction.Rollback()  // safe rollback
    → rsp.ErrorMessage = ex.Message                   // always set
    → IsTableLockError(ex)? → ShowWarningPopUp(...)   // user-friendly message
  → return rsp
  → UI shows ErrorMessage in log area (or popup already shown for lock errors)
```
