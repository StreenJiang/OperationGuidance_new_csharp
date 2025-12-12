# Sample Migration Code

This document shows the exact code changes for migrating existing request and response models to use the new base classes.

## 1. Updated Request Models

### QueryOperationDataListReq.cs (After Migration)

```csharp
using OperationGuidance_service.Models.AbstractClasses;

namespace OperationGuidance_service.Models.Requests {
    public class QueryOperationDataListReq: PaginationRequestBase {
        public int? UserId { get; set; }
        public int? MissionRecordId { get; set; }

        // Pagination parameters inherited from PaginationRequestBase:
        // - PageNumber (with validation, default: 1)
        // - PageSize (with validation, default: 50)
        // - ToPaginationParams() method
        // - Offset property

        // Search conditions (model-specific)
        public string? VinNumber { get; set; }           // VIN号搜索
        public int? WorkstationId { get; set; }          // 站点ID过滤
        public DateTime? StartDate { get; set; }         // 开始日期
        public DateTime? EndDate { get; set; }           // 结束日期
    }
}
```

**Changes Made**:
- ✅ Changed inheritance from `HttpRequest` to `PaginationRequestBase`
- ✅ Removed duplicate `PageNumber` and `PageSize` properties
- ✅ Removed duplicate default values (now in base class)
- ✅ All search conditions remain unchanged

**Benefits**:
- ✅ 7 lines of code removed
- ✅ Centralized validation for PageNumber and PageSize
- ✅ New ToPaginationParams() method available
- ✅ Computed Offset property available

### QueryMissionRecordListReq.cs (After Migration)

```csharp
using OperationGuidance_service.Models.AbstractClasses;

namespace OperationGuidance_service.Models.Requests {
    public class QueryMissionRecordListReq: PaginationRequestBase {
        public int? UserId { get; set; }
        public List<int>? Ids { get; set; }
        public DateTime? Date { get; set; }
        public int? MissionId { get; set; }
        public string? ProductBatch { get; set; }

        // Pagination parameters inherited from PaginationRequestBase:
        // - PageNumber (with validation, default: 1)
        // - PageSize (with validation, default: 50)
        // - ToPaginationParams() method
        // - Offset property

        // Search conditions (model-specific)
        public string? ProductBarCode { get; set; }      // 总成码/追溯码
        public string? PartsBarCode { get; set; }        // 物料码
        public string? MissionName { get; set; }         // 任务名称
        public bool? IsChallengeMission { get; set; }    // 是否挑战任务
    }
}
```

**Changes Made**:
- ✅ Changed inheritance from `HttpRequest` to `PaginationRequestBase`
- ✅ Removed duplicate `PageNumber` and `PageSize` properties
- ✅ Removed duplicate default values (now in base class)
- ✅ All search conditions remain unchanged

**Benefits**:
- ✅ 7 lines of code removed
- ✅ Centralized validation for PageNumber and PageSize
- ✅ New ToPaginationParams() method available
- ✅ Computed Offset property available

## 2. Updated Response Models

### QueryOperationDataListRsp.cs (After Migration)

```csharp
using OperationGuidance_service.Models.AbstractClasses;
using OperationGuidance_service.Models.DTOs;

namespace OperationGuidance_service.Models.Responses {
    public class QueryOperationDataListRsp: PaginationResponseBase<OperationDataDTO> {
        // PagedResult property inherited from PaginationResponseBase<OperationDataDTO>

        // Backward compatibility: provide便捷属性，保持现有API兼容
        public List<OperationDataDTO> OperationDataDTOs => Items;

        // No need for custom constructor - base class handles initialization
        // public QueryOperationDataListRsp() {
        //     PagedResult = new PagedResult<OperationDataDTO>();
        // }
    }
}
```

**Changes Made**:
- ✅ Changed inheritance from `HttpResponse` to `PaginationResponseBase<OperationDataDTO>`
- ✅ Removed explicit `PagedResult` property (now in base class)
- ✅ Updated `OperationDataDTOs` to use `Items` property
- ✅ Removed explicit constructor (base class handles it)

**Benefits**:
- ✅ 8 lines of code removed
- ✅ Enhanced backward compatibility through Items property
- ✅ Direct access to pagination metadata (TotalCount, TotalPages, etc.)
- ✅ SetPagedData() convenience method available

### QueryMissionRecordListRsp.cs (After Migration)

```csharp
using OperationGuidance_service.Models.AbstractClasses;
using OperationGuidance_service.Models.DTOs;

namespace OperationGuidance_service.Models.Responses {
    public class QueryMissionRecordListRsp: PaginationResponseBase<MissionRecordDTO> {
        // PagedResult property inherited from PaginationResponseBase<MissionRecordDTO>

        // Backward compatibility: provide便捷属性，保持现有API兼容
        public List<MissionRecordDTO> MissionRecordDTOs => Items;

        // No need for custom constructor - base class handles initialization
        // public QueryMissionRecordListRsp() {
        //     PagedResult = new PagedResult<MissionRecordDTO>();
        // }
    }
}
```

**Changes Made**:
- ✅ Changed inheritance from `HttpResponse` to `PaginationResponseBase<MissionRecordDTO>`
- ✅ Removed explicit `PagedResult` property (now in base class)
- ✅ Updated `MissionRecordDTOs` to use `Items` property
- ✅ Removed explicit constructor (base class handles it)

**Benefits**:
- ✅ 8 lines of code removed
- ✅ Enhanced backward compatibility through Items property
- ✅ Direct access to pagination metadata (TotalCount, TotalPages, etc.)
- ✅ SetPagedData() convenience method available

## 3. Updated Service Layer

### OperationDataService.cs (QueryOperationDataListWithPagination Method)

**Before**:
```csharp
public PagedResult<OperationData> QueryOperationDataListWithPagination(QueryOperationDataListReq req) {
    // ... build conditions ...

    // Build pagination parameters
    var paginationParams = new PaginationParams {
        PageNumber = req.PageNumber,
        PageSize = req.PageSize,
        OrderBy = "id",
        Descending = true
    };

    // Use Wrapper's pagination method
    return Wrapper.FindWithPagination(whereClause, parameters, paginationParams);
}
```

**After**:
```csharp
public PagedResult<OperationData> QueryOperationDataListWithPagination(QueryOperationDataListReq req) {
    // ... build conditions ...

    // Use inherited conversion method - cleaner and more maintainable
    var paginationParams = req.ToPaginationParams("id", true);

    // Use Wrapper's pagination method
    return Wrapper.FindWithPagination(whereClause, parameters, paginationParams);
}
```

**Changes Made**:
- ✅ Replaced manual PaginationParams construction with `req.ToPaginationParams()`
- ✅ Reduced code from 6 lines to 1 line
- ✅ Same functionality, cleaner code

**Benefits**:
- ✅ 5 lines of code removed
- ✅ Less boilerplate code
- ✅ Consistent pagination parameter handling
- ✅ Easier to maintain and modify

### MissionRecordService.cs (Similar Pattern)

**Before**:
```csharp
var paginationParams = new PaginationParams {
    PageNumber = req.PageNumber,
    PageSize = req.PageSize,
    OrderBy = "id",
    Descending = true
};
```

**After**:
```csharp
var paginationParams = req.ToPaginationParams("id", true);
```

## 4. Backward Compatibility Verification

### For Request Models

**Client Code (Unchanged)**:
```csharp
var request = new QueryOperationDataListReq {
    PageNumber = 2,
    PageSize = 20,
    VinNumber = "ABC123"
};
```

**Behavior**: ✅ Identical to before
- PageNumber accessible: `request.PageNumber` → returns 2
- PageSize accessible: `request.PageSize` → returns 20
- VinNumber accessible: `request.VinNumber` → returns "ABC123"
- Default values work: new request.PageNumber → returns 1

### For Response Models

**Client Code (Unchanged)**:
```csharp
var response = await api.GetOperationDataAsync(pageNumber: 1, pageSize: 50);

// Old way (still works)
List<OperationDataDTO> data = response.OperationDataDTOs;
PagedResult<OperationDataDTO> pagedResult = response.PagedResult;

// New way (also works)
List<OperationDataDTO> data2 = response.Items;
int totalCount = response.TotalCount;
int totalPages = response.TotalPages;
bool hasNext = response.HasNextPage;
```

**Behavior**: ✅ Fully backward compatible
- `response.OperationDataDTOs` → works, returns List<OperationDataDTO>
- `response.PagedResult` → works, returns PagedResult<OperationDataDTO>
- `response.Items` → NEW, returns List<OperationDataDTO>
- `response.TotalCount` → NEW, returns int
- `response.TotalPages` → NEW, returns int
- `response.HasNextPage` → NEW, returns bool

### JSON Serialization

**Request Serialization (Unchanged)**:
```json
{
  "pageNumber": 2,
  "pageSize": 20,
  "vinNumber": "ABC123",
  "workstationId": 5,
  "startDate": "2025-01-01T00:00:00",
  "endDate": "2025-01-31T23:59:59"
}
```

**Response Serialization (Enhanced)**:
```json
{
  "responseCode": 0,
  "responseMessage": "Success",
  "pagedResult": {
    "data": [ /* array of OperationDataDTO */ ],
    "totalCount": 150,
    "pageNumber": 2,
    "pageSize": 20,
    "totalPages": 8,
    "hasPreviousPage": true,
    "hasNextPage": true
  },
  "operationDataDTOs": [ /* same as pagedResult.data for backward compatibility */ ],
  "items": [ /* same as pagedResult.data */ ],
  "totalCount": 150,
  "pageNumber": 2,
  "pageSize": 20,
  "totalPages": 8,
  "hasPreviousPage": true,
  "hasNextPage": true
}
```

**Note**: The JSON includes both old and new properties for maximum compatibility. Clients can use either the old properties (OperationDataDTOs, PagedResult) or the new properties (Items, TotalCount, etc.).

## 5. Testing Examples

### Unit Test Example (Request Model)

```csharp
[Test]
public void QueryOperationDataListReq_InheritsPaginationRequestBase() {
    // Arrange
    var request = new QueryOperationDataListReq();

    // Act & Assert
    Assert.IsInstanceOf<PaginationRequestBase>(request);
    Assert.AreEqual(1, request.PageNumber); // default value
    Assert.AreEqual(50, request.PageSize);  // default value
}

[Test]
public void QueryOperationDataListReq_PageNumberValidation() {
    // Arrange
    var request = new QueryOperationDataListReq();

    // Act
    request.PageNumber = -5;

    // Assert
    Assert.AreEqual(1, request.PageNumber); // should be corrected to 1
}

[Test]
public void QueryOperationDataListReq_ToPaginationParams() {
    // Arrange
    var request = new QueryOperationDataListReq {
        PageNumber = 3,
        PageSize = 25
    };

    // Act
    var paginationParams = request.ToPaginationParams("create_time", true);

    // Assert
    Assert.AreEqual(3, paginationParams.PageNumber);
    Assert.AreEqual(25, paginationParams.PageSize);
    Assert.AreEqual("create_time", paginationParams.OrderBy);
    Assert.IsTrue(paginationParams.Descending);
}
```

### Unit Test Example (Response Model)

```csharp
[Test]
public void QueryOperationDataListRsp_InheritsPaginationResponseBase() {
    // Arrange
    var response = new QueryOperationDataListRsp();

    // Act & Assert
    Assert.IsInstanceOf<PaginationResponseBase<OperationDataDTO>>(response);
    Assert.IsNotNull(response.PagedResult);
}

[Test]
public void QueryOperationDataListRsp_BackwardCompatibility() {
    // Arrange
    var response = new QueryOperationDataListRsp();
    var testData = new List<OperationDataDTO> {
        new OperationDataDTO { Id = 1 },
        new OperationDataDTO { Id = 2 }
    };

    // Act
    response.SetPagedData(testData, 100, 1, 50);

    // Assert
    Assert.AreEqual(testData, response.OperationDataDTOs); // old way
    Assert.AreEqual(testData, response.Items);             // new way
    Assert.AreEqual(100, response.TotalCount);
    Assert.AreEqual(1, response.PageNumber);
    Assert.AreEqual(50, response.PageSize);
    Assert.AreEqual(2, response.TotalPages);
    Assert.IsFalse(response.HasPreviousPage);
    Assert.IsTrue(response.HasNextPage);
}
```

## Summary

The migration to base classes provides:

1. **Code Reduction**: ~30 lines of code removed across 4 files
2. **Enhanced Functionality**: New properties and methods for better usability
3. **100% Backward Compatibility**: All existing client code works without changes
4. **Improved Maintainability**: Single source of truth for pagination logic
5. **Better Consistency**: All paginated models follow the same pattern
6. **Easier Testing**: Base classes can be tested independently
7. **Future-Proof**: New query models can easily inherit from base classes
