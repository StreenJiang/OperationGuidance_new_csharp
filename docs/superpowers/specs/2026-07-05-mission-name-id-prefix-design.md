# 任务名称自动 ID 前缀 — 设计文档

**日期**: 2026-07-05
**状态**: 设计完成，待评审

## 需求

任务编辑功能中，任何任务在保存时自动检测名称是否带有 `{id} - ` 前缀，没有则自动添加。
编辑框（主界面 + 详情弹窗）中不显示此前缀。

示例：任务 ID 为 123，用户输入名称 `拧紧任务A`，保存后数据库存储 `123 - 拧紧任务A`。

## 设计

### 1. 新增 `MissionNameHelper` 工具类

`OperationGuidance_new/Utils/MissionNameHelper.cs`

```csharp
public static class MissionNameHelper
{
    public static string MakePrefix(int id) => $"{id} - ";

    /// <summary>加载显示时去掉前缀，id ≤ 0 原样返回</summary>
    public static string StripPrefix(string name, int id)
    {
        if (id <= 0 || string.IsNullOrEmpty(name)) return name;
        string prefix = MakePrefix(id);
        return name.StartsWith(prefix) ? name.Substring(prefix.Length) : name;
    }

    /// <summary>保存时加上前缀，id ≤ 0 或已有前缀则原样返回（幂等）</summary>
    public static string ApplyPrefix(string name, int id)
    {
        if (id <= 0 || string.IsNullOrEmpty(name)) return name;
        string prefix = MakePrefix(id);
        return name.StartsWith(prefix) ? name : prefix + name;
    }
}
```

### 2. 改动点

两个文件，各 3 处：

| # | 文件 | 场景 | 操作 |
|---|---|---|---|
| ① | `MissionEditionView.cs:199` | 主界面加载名称到输入框 | `StripPrefix(_missionDTO.name, _missionDTO.id)` |
| ② | `MissionEditionView.cs:1448` | 详情弹窗打开→回填输入框 | `StripPrefix(missionDTO.name, missionDTO.id)` |
| ③ | `MissionEditionView.cs:~376` | 复制→构造新名称 | `StripPrefix(_missionDTO.name, _missionDTO.id) + "_copy"` |
| ④ | `MissionEditionView.cs:~297` | 主保存按钮→调用 API 前 | `ApplyPrefix(_missionDTO.name, _missionDTO.id)` |
| ⑤ | `MissionEditionView_SCII.cs:200` | 主界面加载名称到输入框 | 同 ① |
| ⑥ | `MissionEditionView_SCII.cs:1798` | 详情弹窗打开→回填输入框 | 同 ② |
| ⑦ | `MissionEditionView_SCII.cs:~582` | 复制→构造新名称 | 同 ③ |
| ⑧ | `MissionEditionView_SCII.cs:~489` | 主保存按钮→调用 API 前 | 同 ④ |

> 注：复制保存后与新建任务一样需要 ID 反向更新（复制品 id = -1），在各自复制 Click 处理器中处理。

> 注：详情弹窗确认后**不**需要 `ApplyPrefix`，因为此时只更新内存中的 DTO，最终保存由主保存按钮统一加前缀。

### 3. 保存流程（含新建任务 ID 反向更新）

两个文件保存按钮 Click 处理器的逻辑一致：

```
1. ApplyPrefix(_missionDTO.name, _missionDTO.id)   // 保证名称带前缀
2. 检查重名（此时 _missionDTO.name 已带前缀，与 DB 中其他任务可比对）
3. API 保存 → 获取返回的 DTO（新建任务 id 从 -1 变为真实值）
4. 若旧 id ≤ 0 且新 id > 0（新建任务首次保存成功）：
   a. ApplyPrefix(_missionDTO.name, _missionDTO.id)   // 此时 id 已是真实值
   b. 再次调用 API 更新名称
   c. _missionDTO = rsp.ProductMissionDTO
5. 保存图片、弹窗提示成功
6. MissionSaved.Invoke（此时名称已含正确前缀，触发列表刷新）
7. 跳转列表界面（VisibleToTrue 重新拉取 API，保证列表展示一致）
```

整个 Click 处理器为同步执行，步骤 4 的二次更新在步骤 6/7 之前完成，不存在竞态。

### 4. 重名检查

保存前 `_missionDTO.name` 已通过 `ApplyPrefix` 加上前缀，而 `allMissions` 来自 API 查询（DB 中历史任务也已带前缀），因此重名比对正确。

### 5. 边界情况

| 场景 | 行为 |
|---|---|
| 新建任务首次保存（id = -1） | 不加前缀直接保存，保存成功后二次更新带上真实 ID 前缀 |
| 已有任务编辑保存（id > 0） | 保存前自动加上前缀 |
| 旧数据迁移（DB 中名称无前缀） | 下次编辑保存时自动补充 |
| 用户手动输入含前缀的名称 | `ApplyPrefix` 幂等，不会重复添加 |
| 用户编辑名称后再打开详情弹窗 | 弹窗显示无前缀名称（`_missionDTO.name` 已被 TextChanged 更新为无前缀版本），确认回写后仍保持无前缀，等待主保存时统一添加 |
| 复制已有任务 | 复制时先 `StripPrefix` 去掉源任务前缀，生成 `原名_copy`，保存成功后与新任务一样做 `ApplyPrefix` + 二次更新 |

### 6. 测试要点

- [ ] 新建任务 → 保存 → 列表显示 `123 - xxx` 格式
- [ ] 编辑已有任务 → 输入框不显示前缀 → 修改名称保存 → DB 更新为新前缀名称
- [ ] 详情弹窗打开 → 名称输入框不显示前缀
- [ ] 详情弹窗修改名称 → 保存 → 主输入框同步更新（不含前缀）
- [ ] 旧任务（DB 中无前缀）→ 编辑保存 → 自动加上前缀
- [ ] SCII 版本同上所有场景
- [ ] 复制已有任务 → 新任务名称为 `{新ID} - 原名_copy`，不出现双层前缀
- [ ] 重名检测在加前缀后仍正常工作
