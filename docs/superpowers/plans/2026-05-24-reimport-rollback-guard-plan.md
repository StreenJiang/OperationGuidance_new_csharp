# ReimportPartsBarcode Rollback Guard Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Guard `transaction.Rollback()` with connection state check and show user-friendly popup for table-lock errors.

**Architecture:** Single-file change to `OperationGuidanceApis.cs` — modify the catch block in `ReimportPartsBarcode` and add a private static helper `IsTableLockError`.

**Tech Stack:** C#, Dapper, WinForms (`SystemUtils.ShowWarningPopUp`)

---

### Task 1: Guard rollback + add lock-error popup

**Files:**
- Modify: `OperationGuidance_service/Controllers/OperationGuidanceApis.cs:335-337`

- [ ] **Step 1: Replace the catch block and add IsTableLockError helper**

The catch block at lines 335-337 currently reads:
```csharp
            } catch (Exception ex) {
                transaction.Rollback();
                rsp.ErrorMessage = ex.Message;
            }
```

Replace with:
```csharp
            } catch (Exception ex) {
                if (conn.State == ConnectionState.Open) {
                    transaction.Rollback();
                }
                rsp.ErrorMessage = ex.Message;

                if (IsTableLockError(ex)) {
                    SystemUtils.ShowWarningPopUp("数据库繁忙，请稍后重试。\n\n（某个数据表正在执行其他操作，请等待片刻后再次点击\"重新导入物料码\"）");
                }
            }
```

Then add the helper method inside the class. Insert it after the `ReimportPartsBarcode` method's closing brace (after line 345), before `#endregion` (line 346):

```csharp
        private static bool IsTableLockError(Exception ex) {
            string msg = ex.Message;
            return msg.Contains("lock", StringComparison.OrdinalIgnoreCase)
                || msg.Contains("busy", StringComparison.OrdinalIgnoreCase)
                || msg.Contains("timeout", StringComparison.OrdinalIgnoreCase);
        }
```

- [ ] **Step 2: Build to verify**

Run: `dotnet build OperationGuidance_new/OperationGuidance_new.csproj`
Expected: Build succeeds with zero errors.

- [ ] **Step 3: Commit**

```bash
git add OperationGuidance_service/Controllers/OperationGuidanceApis.cs
git commit -m "fix: guard transaction rollback with connection check and show popup for table-lock errors"
```
