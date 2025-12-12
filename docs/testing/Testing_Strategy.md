# Pagination Abstraction Testing Strategy

## Overview

This document outlines a comprehensive testing strategy to ensure the pagination abstraction refactoring maintains 100% backward compatibility and introduces no regressions.

## Testing Pyramid

### 1. Unit Tests (Base Classes)
**Purpose**: Validate base class functionality in isolation

### 2. Unit Tests (Models)
**Purpose**: Verify model inheritance and property behavior

### 3. Integration Tests (Service Layer)
**Purpose**: Ensure service methods work correctly with base classes

### 4. API Tests (Controller Layer)
**Purpose**: Verify API endpoints return correct pagination data

### 5. Regression Tests (End-to-End)
**Purpose**: Ensure existing functionality remains intact

---

## 1. Base Class Unit Tests

### PaginationRequestBase Tests

#### File: `Tests/Models/AbstractClasses/PaginationRequestBaseTests.cs`

```csharp
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OperationGuidance_service.Models.AbstractClasses;
using OperationGuidance_service.Models.Requests;

namespace OperationGuidance_service.Tests.Models.AbstractClasses {
    [TestClass]
    public class PaginationRequestBaseTests {
        private class TestPaginationRequest : PaginationRequestBase {
            public string? TestProperty { get; set; }
        }

        #region PageNumber Tests

        [TestMethod]
        public void PageNumber_DefaultValue_ShouldBe1() {
            // Arrange
            var request = new TestPaginationRequest();

            // Act & Assert
            Assert.AreEqual(1, request.PageNumber);
        }

        [TestMethod]
        public void PageNumber_SetValidValue_ShouldStoreCorrectly() {
            // Arrange
            var request = new TestPaginationRequest();

            // Act
            request.PageNumber = 5;

            // Assert
            Assert.AreEqual(5, request.PageNumber);
        }

        [TestMethod]
        public void PageNumber_SetZero_ShouldCorrectTo1() {
            // Arrange
            var request = new TestPaginationRequest();

            // Act
            request.PageNumber = 0;

            // Assert
            Assert.AreEqual(1, request.PageNumber);
        }

        [TestMethod]
        public void PageNumber_SetNegative_ShouldCorrectTo1() {
            // Arrange
            var request = new TestPaginationRequest();

            // Act
            request.PageNumber = -10;

            // Assert
            Assert.AreEqual(1, request.PageNumber);
        }

        [TestMethod]
        public void PageNumber_SetLargeValue_ShouldStoreCorrectly() {
            // Arrange
            var request = new TestPaginationRequest();

            // Act
            request.PageNumber = 999999;

            // Assert
            Assert.AreEqual(999999, request.PageNumber);
        }

        #endregion

        #region PageSize Tests

        [TestMethod]
        public void PageSize_DefaultValue_ShouldBe50() {
            // Arrange
            var request = new TestPaginationRequest();

            // Act & Assert
            Assert.AreEqual(50, request.PageSize);
        }

        [TestMethod]
        public void PageSize_SetValidValue_ShouldStoreCorrectly() {
            // Arrange
            var request = new TestPaginationRequest();

            // Act
            request.PageSize = 100;

            // Assert
            Assert.AreEqual(100, request.PageSize);
        }

        [TestMethod]
        public void PageSize_SetZero_ShouldCorrectTo50() {
            // Arrange
            var request = new TestPaginationRequest();

            // Act
            request.PageSize = 0;

            // Assert
            Assert.AreEqual(50, request.PageSize);
        }

        [TestMethod]
        public void PageSize_SetNegative_ShouldCorrectTo50() {
            // Arrange
            var request = new TestPaginationRequest();

            // Act
            request.PageSize = -10;

            // Assert
            Assert.AreEqual(50, request.PageSize);
        }

        [TestMethod]
        public void PageSize_SetAbove1000_ShouldCorrectTo1000() {
            // Arrange
            var request = new TestPaginationRequest();

            // Act
            request.PageSize = 2000;

            // Assert
            Assert.AreEqual(1000, request.PageSize);
        }

        [TestMethod]
        public void PageSize_SetMaximumAllowed_ShouldStore1000() {
            // Arrange
            var request = new TestPaginationRequest();

            // Act
            request.PageSize = 1000;

            // Assert
            Assert.AreEqual(1000, request.PageSize);
        }

        #endregion

        #region ToPaginationParams Tests

        [TestMethod]
        public void ToPaginationParams_WithDefaults_ShouldCreateCorrectParams() {
            // Arrange
            var request = new TestPaginationRequest {
                PageNumber = 3,
                PageSize = 25
            };

            // Act
            var paginationParams = request.ToPaginationParams();

            // Assert
            Assert.AreEqual(3, paginationParams.PageNumber);
            Assert.AreEqual(25, paginationParams.PageSize);
            Assert.AreEqual("id", paginationParams.OrderBy);
            Assert.IsFalse(paginationParams.Descending);
        }

        [TestMethod]
        public void ToPaginationParams_WithCustomOrderBy_ShouldUseCustomValue() {
            // Arrange
            var request = new TestPaginationRequest {
                PageNumber = 2,
                PageSize = 10
            };

            // Act
            var paginationParams = request.ToPaginationParams("create_time", true);

            // Assert
            Assert.AreEqual("create_time", paginationParams.OrderBy);
            Assert.IsTrue(paginationParams.Descending);
        }

        [TestMethod]
        public void ToPaginationParams_WithPageNumber1_ShouldCreateCorrectOffset() {
            // Arrange
            var request = new TestPaginationRequest {
                PageNumber = 1,
                PageSize = 50
            };

            // Act
            var paginationParams = request.ToPaginationParams();

            // Assert
            Assert.AreEqual(0, paginationParams.Offset);
        }

        [TestMethod]
        public void ToPaginationParams_WithPageNumber2_ShouldCreateCorrectOffset() {
            // Arrange
            var request = new TestPaginationRequest {
                PageNumber = 2,
                PageSize = 50
            };

            // Act
            var paginationParams = request.ToPaginationParams();

            // Assert
            Assert.AreEqual(50, paginationParams.Offset);
        }

        #endregion

        #region Offset Tests

        [TestMethod]
        public void Offset_Page1Size50_ShouldBe0() {
            // Arrange
            var request = new TestPaginationRequest {
                PageNumber = 1,
                PageSize = 50
            };

            // Act & Assert
            Assert.AreEqual(0, request.Offset);
        }

        [TestMethod]
        public void Offset_Page2Size50_ShouldBe50() {
            // Arrange
            var request = new TestPaginationRequest {
                PageNumber = 2,
                PageSize = 50
            };

            // Act & Assert
            Assert.AreEqual(50, request.Offset);
        }

        [TestMethod]
        public void Offset_Page3Size25_ShouldBe50() {
            // Arrange
            var request = new TestPaginationRequest {
                PageNumber = 3,
                PageSize = 25
            };

            // Act & Assert
            Assert.AreEqual(50, request.Offset);
        }

        #endregion

        #region Inheritance Tests

        [TestMethod]
        public void Inheritance_InstanceOfHttpRequest_ShouldBeTrue() {
            // Arrange
            var request = new TestPaginationRequest();

            // Act & Assert
            Assert.IsInstanceOf<HttpRequest>(request);
        }

        [TestMethod]
        public void Inheritance_InstanceOfPaginationRequestBase_ShouldBeTrue() {
            // Arrange
            var request = new TestPaginationRequest();

            // Act & Assert
            Assert.IsInstanceOf<PaginationRequestBase>(request);
        }

        #endregion
    }
}
```

### PaginationResponseBase<T> Tests

#### File: `Tests/Models/AbstractClasses/PaginationResponseBaseTests.cs`

```csharp
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OperationGuidance_service.Models.AbstractClasses;
using OperationGuidance_service.Models.DTOs;
using OperationGuidance_service.Models.Responses;

namespace OperationGuidance_service.Tests.Models.AbstractClasses {
    [TestClass]
    public class PaginationResponseBaseTests {
        private class TestPaginationResponse : PaginationResponseBase<string> {
        }

        #region Constructor Tests

        [TestMethod]
        public void Constructor_Default_ShouldInitializePagedResult() {
            // Arrange & Act
            var response = new TestPaginationResponse();

            // Assert
            Assert.IsNotNull(response.PagedResult);
            Assert.IsNotNull(response.PagedResult.Data);
            Assert.AreEqual(0, response.PagedResult.Data.Count);
        }

        [TestMethod]
        public void Constructor_WithPagedResult_ShouldUseProvidedValue() {
            // Arrange
            var pagedResult = new PagedResult<string> {
                Data = new List<string> { "item1", "item2" },
                TotalCount = 2,
                PageNumber = 1,
                PageSize = 10
            };

            // Act
            var response = new TestPaginationResponse(pagedResult);

            // Assert
            Assert.AreEqual(2, response.PagedResult.Data.Count);
            Assert.AreEqual("item1", response.PagedResult.Data[0]);
        }

        [TestMethod]
        public void Constructor_WithNullPagedResult_ShouldInitializeNewInstance() {
            // Arrange & Act
            var response = new TestPaginationResponse(null);

            // Assert
            Assert.IsNotNull(response.PagedResult);
            Assert.AreEqual(0, response.PagedResult.Data.Count);
        }

        #endregion

        #region Items Property Tests

        [TestMethod]
        public void Items_WithEmptyData_ShouldReturnEmptyList() {
            // Arrange
            var response = new TestPaginationResponse();

            // Act & Assert
            Assert.IsNotNull(response.Items);
            Assert.AreEqual(0, response.Items.Count);
        }

        [TestMethod]
        public void Items_WithData_ShouldReturnCorrectList() {
            // Arrange
            var response = new TestPaginationResponse();
            response.SetPagedData(new List<string> { "a", "b", "c" }, 3, 1, 10);

            // Act & Assert
            Assert.AreEqual(3, response.Items.Count);
            Assert.AreEqual("a", response.Items[0]);
            Assert.AreEqual("b", response.Items[1]);
            Assert.AreEqual("c", response.Items[2]);
        }

        #endregion

        #region Metadata Properties Tests

        [TestMethod]
        public void TotalCount_WithData_ShouldReturnCorrectValue() {
            // Arrange
            var response = new TestPaginationResponse();
            response.SetPagedData(new List<string>(), 150, 1, 50);

            // Act & Assert
            Assert.AreEqual(150, response.TotalCount);
        }

        [TestMethod]
        public void PageNumber_WithData_ShouldReturnCorrectValue() {
            // Arrange
            var response = new TestPaginationResponse();
            response.SetPagedData(new List<string>(), 100, 3, 50);

            // Act & Assert
            Assert.AreEqual(3, response.PageNumber);
        }

        [TestMethod]
        public void PageSize_WithData_ShouldReturnCorrectValue() {
            // Arrange
            var response = new TestPaginationResponse();
            response.SetPagedData(new List<string>(), 100, 1, 25);

            // Act & Assert
            Assert.AreEqual(25, response.PageSize);
        }

        [TestMethod]
        public void TotalPages_With50TotalCountAnd10PageSize_ShouldBe5() {
            // Arrange
            var response = new TestPaginationResponse();
            response.SetPagedData(new List<string>(), 50, 1, 10);

            // Act & Assert
            Assert.AreEqual(5, response.TotalPages);
        }

        [TestMethod]
        public void TotalPages_With51TotalCountAnd10PageSize_ShouldBe6() {
            // Arrange
            var response = new TestPaginationResponse();
            response.SetPagedData(new List<string>(), 51, 1, 10);

            // Act & Assert
            Assert.AreEqual(6, response.TotalPages);
        }

        [TestMethod]
        public void TotalPages_WithZeroTotalCount_ShouldBe0() {
            // Arrange
            var response = new TestPaginationResponse();
            response.SetPagedData(new List<string>(), 0, 1, 10);

            // Act & Assert
            Assert.AreEqual(0, response.TotalPages);
        }

        [TestMethod]
        public void HasPreviousPage_WithPage1_ShouldBeFalse() {
            // Arrange
            var response = new TestPaginationResponse();
            response.SetPagedData(new List<string>(), 100, 1, 10);

            // Act & Assert
            Assert.IsFalse(response.HasPreviousPage);
        }

        [TestMethod]
        public void HasPreviousPage_WithPage2_ShouldBeTrue() {
            // Arrange
            var response = new TestPaginationResponse();
            response.SetPagedData(new List<string>(), 100, 2, 10);

            // Act & Assert
            Assert.IsTrue(response.HasPreviousPage);
        }

        [TestMethod]
        public void HasNextPage_WithLastPage_ShouldBeFalse() {
            // Arrange
            var response = new TestPaginationResponse();
            response.SetPagedData(new List<string>(), 100, 10, 10);

            // Act & Assert
            Assert.IsFalse(response.HasNextPage);
        }

        [TestMethod]
        public void HasNextPage_WithMiddlePage_ShouldBeTrue() {
            // Arrange
            var response = new TestPaginationResponse();
            response.SetPagedData(new List<string>(), 100, 5, 10);

            // Act & Assert
            Assert.IsTrue(response.HasNextPage);
        }

        #endregion

        #region SetPagedData Tests

        [TestMethod]
        public void SetPagedData_WithValidData_ShouldSetAllProperties() {
            // Arrange
            var response = new TestPaginationResponse();
            var data = new List<string> { "x", "y", "z" };

            // Act
            response.SetPagedData(data, 100, 2, 25);

            // Assert
            Assert.AreEqual(3, response.PagedResult.Data.Count);
            Assert.AreEqual(100, response.PagedResult.TotalCount);
            Assert.AreEqual(2, response.PagedResult.PageNumber);
            Assert.AreEqual(25, response.PagedResult.PageSize);
        }

        [TestMethod]
        public void SetPagedData_WithNullData_ShouldInitializeEmptyList() {
            // Arrange
            var response = new TestPaginationResponse();

            // Act
            response.SetPagedData(null, 0, 1, 10);

            // Assert
            Assert.IsNotNull(response.PagedResult.Data);
            Assert.AreEqual(0, response.PagedResult.Data.Count);
        }

        #endregion

        #region Inheritance Tests

        [TestMethod]
        public void Inheritance_InstanceOfHttpResponse_ShouldBeTrue() {
            // Arrange
            var response = new TestPaginationResponse();

            // Act & Assert
            Assert.IsInstanceOf<HttpResponse>(response);
        }

        [TestMethod]
        public void Inheritance_InstanceOfPaginationResponseBase_ShouldBeTrue() {
            // Arrange
            var response = new TestPaginationResponse();

            // Act & Assert
            Assert.IsInstanceOf<PaginationResponseBase<string>>(response);
        }

        #endregion
    }
}
```

---

## 2. Model Unit Tests

### QueryOperationDataListReq Tests

#### File: `Tests/Models/Requests/QueryOperationDataListReqTests.cs`

```csharp
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OperationGuidance_service.Models.Requests;

namespace OperationGuidance_service.Tests.Models.Requests {
    [TestClass]
    public class QueryOperationDataListReqTests {
        #region Inheritance Tests

        [TestMethod]
        public void Inheritance_InstanceOfPaginationRequestBase_ShouldBeTrue() {
            // Arrange & Act
            var request = new QueryOperationDataListReq();

            // Assert
            Assert.IsInstanceOf<PaginationRequestBase>(request);
        }

        #endregion

        #region Search Condition Tests

        [TestMethod]
        public void VinNumber_SetValue_ShouldStoreCorrectly() {
            // Arrange
            var request = new QueryOperationDataListReq();

            // Act
            request.VinNumber = "ABC123";

            // Assert
            Assert.AreEqual("ABC123", request.VinNumber);
        }

        [TestMethod]
        public void WorkstationId_SetValue_ShouldStoreCorrectly() {
            // Arrange
            var request = new QueryOperationDataListReq();

            // Act
            request.WorkstationId = 5;

            // Assert
            Assert.AreEqual(5, request.WorkstationId);
        }

        [TestMethod]
        public void StartDate_SetValue_ShouldStoreCorrectly() {
            // Arrange
            var request = new QueryOperationDataListReq();
            var date = new DateTime(2025, 1, 1);

            // Act
            request.StartDate = date;

            // Assert
            Assert.AreEqual(date, request.StartDate);
        }

        [TestMethod]
        public void EndDate_SetValue_ShouldStoreCorrectly() {
            // Arrange
            var request = new QueryOperationDataListReq();
            var date = new DateTime(2025, 1, 31);

            // Act
            request.EndDate = date;

            // Assert
            Assert.AreEqual(date, request.EndDate);
        }

        #endregion

        #region UserId and MissionRecordId Tests

        [TestMethod]
        public void UserId_SetValue_ShouldStoreCorrectly() {
            // Arrange
            var request = new QueryOperationDataListReq();

            // Act
            request.UserId = 123;

            // Assert
            Assert.AreEqual(123, request.UserId);
        }

        [TestMethod]
        public void MissionRecordId_SetValue_ShouldStoreCorrectly() {
            // Arrange
            var request = new QueryOperationDataListReq();

            // Act
            request.MissionRecordId = 456;

            // Assert
            Assert.AreEqual(456, request.MissionRecordId);
        }

        #endregion

        #region ToPaginationParams Tests

        [TestMethod]
        public void ToPaginationParams_Called_ShouldReturnPaginationParams() {
            // Arrange
            var request = new QueryOperationDataListReq {
                PageNumber = 2,
                PageSize = 30
            };

            // Act
            var paginationParams = request.ToPaginationParams("vin_number", true);

            // Assert
            Assert.IsInstanceOf<PaginationParams>(paginationParams);
            Assert.AreEqual(2, paginationParams.PageNumber);
            Assert.AreEqual(30, paginationParams.PageSize);
            Assert.AreEqual("vin_number", paginationParams.OrderBy);
            Assert.IsTrue(paginationParams.Descending);
        }

        #endregion
    }
}
```

### QueryOperationDataListRsp Tests

#### File: `Tests/Models/Responses/QueryOperationDataListRspTests.cs`

```csharp
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OperationGuidance_service.Models.DTOs;
using OperationGuidance_service.Models.Responses;

namespace OperationGuidance_service.Tests.Models.Responses {
    [TestClass]
    public class QueryOperationDataListRspTests {
        #region Inheritance Tests

        [TestMethod]
        public void Inheritance_InstanceOfPaginationResponseBase_ShouldBeTrue() {
            // Arrange & Act
            var response = new QueryOperationDataListRsp();

            // Assert
            Assert.IsInstanceOf<PaginationResponseBase<OperationDataDTO>>(response);
        }

        #endregion

        #region Backward Compatibility Tests

        [TestMethod]
        public void OperationDataDTOs_WithData_ShouldReturnCorrectList() {
            // Arrange
            var response = new QueryOperationDataListRsp();
            var data = new List<OperationDataDTO> {
                new OperationDataDTO { Id = 1 },
                new OperationDataDTO { Id = 2 }
            };

            // Act
            response.SetPagedData(data, 2, 1, 50);

            // Assert
            Assert.AreEqual(2, response.OperationDataDTOs.Count);
            Assert.AreEqual(1, response.OperationDataDTOs[0].Id);
            Assert.AreEqual(2, response.OperationDataDTOs[1].Id);
        }

        [TestMethod]
        public void PagedResult_WithData_ShouldReturnCorrectPagedResult() {
            // Arrange
            var response = new QueryOperationDataListRsp();
            var data = new List<OperationDataDTO> {
                new OperationDataDTO { Id = 1 }
            };

            // Act
            response.SetPagedData(data, 100, 2, 50);

            // Assert
            Assert.IsNotNull(response.PagedResult);
            Assert.AreEqual(1, response.PagedResult.Data.Count);
            Assert.AreEqual(100, response.PagedResult.TotalCount);
            Assert.AreEqual(2, response.PagedResult.PageNumber);
            Assert.AreEqual(50, response.PagedResult.PageSize);
        }

        #endregion

        #region New Properties Tests

        [TestMethod]
        public void Items_WithData_ShouldReturnCorrectList() {
            // Arrange
            var response = new QueryOperationDataListRsp();
            var data = new List<OperationDataDTO> {
                new OperationDataDTO { Id = 1 }
            };

            // Act
            response.SetPagedData(data, 100, 1, 50);

            // Assert
            Assert.AreEqual(1, response.Items.Count);
            Assert.AreEqual(1, response.Items[0].Id);
        }

        [TestMethod]
        public void TotalCount_WithData_ShouldReturnCorrectValue() {
            // Arrange
            var response = new QueryOperationDataListRsp();
            response.SetPagedData(new List<OperationDataDTO>(), 250, 1, 50);

            // Act & Assert
            Assert.AreEqual(250, response.TotalCount);
        }

        [TestMethod]
        public void PageNumber_WithData_ShouldReturnCorrectValue() {
            // Arrange
            var response = new QueryOperationDataListRsp();
            response.SetPagedData(new List<OperationDataDTO>(), 100, 3, 50);

            // Act & Assert
            Assert.AreEqual(3, response.PageNumber);
        }

        [TestMethod]
        public void PageSize_WithData_ShouldReturnCorrectValue() {
            // Arrange
            var response = new QueryOperationDataListRsp();
            response.SetPagedData(new List<OperationDataDTO>(), 100, 1, 25);

            // Act & Assert
            Assert.AreEqual(25, response.PageSize);
        }

        #endregion
    }
}
```

---

## 3. Service Layer Integration Tests

### OperationDataService Tests

#### File: `Tests/Services/OperationDataServiceTests.cs`

```csharp
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using OperationGuidance_service.Models.Requests;
using OperationGuidance_service.Services;

namespace OperationGuidance_service.Tests.Services {
    [TestClass]
    public class OperationDataServiceTests {
        private Mock<OperationDataWrapper> _mockWrapper;
        private OperationDataService _service;

        [TestInitialize]
        public void Setup() {
            _mockWrapper = new Mock<OperationDataWrapper>();
            _service = new OperationDataService(_mockWrapper.Object);
        }

        [TestMethod]
        public void QueryOperationDataListWithPagination_WithValidRequest_ShouldCallWrapperCorrectly() {
            // Arrange
            var request = new QueryOperationDataListReq {
                PageNumber = 2,
                PageSize = 20,
                VinNumber = "ABC123",
                WorkstationId = 5
            };

            var expectedPagedResult = new PagedResult<OperationData> {
                Data = new List<OperationData>(),
                TotalCount = 100,
                PageNumber = 2,
                PageSize = 20
            };

            _mockWrapper.Setup(w => w.FindWithPagination(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>(), It.IsAny<PaginationParams>()))
                        .Returns(expectedPagedResult);

            // Act
            var result = _service.QueryOperationDataListWithPagination(request);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.PageNumber);
            Assert.AreEqual(20, result.PageSize);

            _mockWrapper.Verify(w => w.FindWithPagination(
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>>(),
                It.Is<PaginationParams>(p => p.PageNumber == 2 && p.PageSize == 20)
            ), Times.Once);
        }

        [TestMethod]
        public void QueryOperationDataListWithPagination_WithVinNumber_ShouldIncludeInWhereClause() {
            // Arrange
            var request = new QueryOperationDataListReq {
                PageNumber = 1,
                PageSize = 10,
                VinNumber = "XYZ789"
            };

            _mockWrapper.Setup(w => w.FindWithPagination(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>(), It.IsAny<PaginationParams>()))
                        .Returns(new PagedResult<OperationData>());

            // Act
            _service.QueryOperationDataListWithPagination(request);

            // Assert
            _mockWrapper.Verify(w => w.FindWithPagination(
                It.Is<string>(sql => sql.Contains("vin_number")),
                It.Is<Dictionary<string, object>>(dict => dict.ContainsKey("vin_number")),
                It.IsAny<PaginationParams>()
            ), Times.Once);
        }

        [TestMethod]
        public void QueryOperationDataListWithPagination_DefaultOrderBy_ShouldBeId() {
            // Arrange
            var request = new QueryOperationDataListReq {
                PageNumber = 1,
                PageSize = 10
            };

            _mockWrapper.Setup(w => w.FindWithPagination(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>(), It.IsAny<PaginationParams>()))
                        .Returns(new PagedResult<OperationData>());

            // Act
            _service.QueryOperationDataListWithPagination(request);

            // Assert
            _mockWrapper.Verify(w => w.FindWithPagination(
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>>(),
                It.Is<PaginationParams>(p => p.OrderBy == "id" && p.Descending == true)
            ), Times.Once);
        }

        [TestMethod]
        public void QueryOperationDataListWithPagination_CustomOrderBy_ShouldUseCustomValue() {
            // Arrange
            var request = new QueryOperationDataListReq {
                PageNumber = 1,
                PageSize = 10
            };

            _mockWrapper.Setup(w => w.FindWithPagination(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>(), It.IsAny<PaginationParams>()))
                        .Returns(new PagedResult<OperationData>());

            // Act - Note: This would require a method overload or configuration
            _service.QueryOperationDataListWithPagination(request);

            // For this test to work, we'd need to pass custom order by to ToPaginationParams
            // This is just to demonstrate the pattern
        }
    }
}
```

---

## 4. API Integration Tests

### Controller Tests

#### File: `Tests/Controllers/OperationGuidanceApisTests.cs`

```csharp
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using OperationGuidance_service.Controllers;
using OperationGuidance_service.Models.Requests;
using OperationGuidance_service.Models.Responses;
using OperationGuidance_service.Services;

namespace OperationGuidance_service.Tests.Controllers {
    [TestClass]
    public class OperationGuidanceApisTests {
        private Mock<OperationDataService> _mockOperationDataService;
        private Mock<MissionRecordService> _mockMissionRecordService;
        private OperationGuidanceApis _controller;

        [TestInitialize]
        public void Setup() {
            _mockOperationDataService = new Mock<OperationDataService>();
            _mockMissionRecordService = new Mock<MissionRecordService>();
            _controller = new OperationGuidanceApis(
                _mockOperationDataService.Object,
                _mockMissionRecordService.Object
            );
        }

        [TestMethod]
        public void QueryOperationDataList_WithValidRequest_ShouldReturnSuccess() {
            // Arrange
            var request = new QueryOperationDataListReq {
                PageNumber = 1,
                PageSize = 50
            };

            var serviceResult = new PagedResult<OperationData> {
                Data = new List<OperationData>(),
                TotalCount = 100,
                PageNumber = 1,
                PageSize = 50
            };

            _mockOperationDataService.Setup(s => s.QueryOperationDataListWithPagination(request))
                                     .Returns(serviceResult);

            // Act
            var response = _controller.QueryOperationDataList(request);

            // Assert
            Assert.IsNotNull(response);
            Assert.AreEqual(0, response.ResponseCode);
            Assert.AreEqual("Success", response.ResponseMessage);
            Assert.IsNotNull(response.PagedResult);
            Assert.AreEqual(100, response.PagedResult.TotalCount);
        }

        [TestMethod]
        public void QueryMissionRecordList_WithValidRequest_ShouldReturnSuccess() {
            // Arrange
            var request = new QueryMissionRecordListReq {
                PageNumber = 1,
                PageSize = 50
            };

            var serviceResult = new PagedResult<MissionRecord> {
                Data = new List<MissionRecord>(),
                TotalCount = 50,
                PageNumber = 1,
                PageSize = 50
            };

            _mockMissionRecordService.Setup(s => s.QueryMissionRecordListWithPagination(request))
                                    .Returns(serviceResult);

            // Act
            var response = _controller.QueryMissionRecordList(request);

            // Assert
            Assert.IsNotNull(response);
            Assert.AreEqual(0, response.ResponseCode);
            Assert.AreEqual("Success", response.ResponseMessage);
            Assert.IsNotNull(response.PagedResult);
            Assert.AreEqual(50, response.PagedResult.TotalCount);
        }

        [TestMethod]
        public void QueryOperationDataList_WithBackwardCompatibility_ShouldWork() {
            // Arrange
            var request = new QueryOperationDataListReq {
                PageNumber = 2,
                PageSize = 25
            };

            var data = new List<OperationData> {
                new OperationData { Id = 1 },
                new OperationData { Id = 2 }
            };

            var serviceResult = new PagedResult<OperationData> {
                Data = data,
                TotalCount = 100,
                PageNumber = 2,
                PageSize = 25
            };

            _mockOperationDataService.Setup(s => s.QueryOperationDataListWithPagination(request))
                                     .Returns(serviceResult);

            // Act
            var response = _controller.QueryOperationDataList(request);

            // Assert - Old way (backward compatibility)
            Assert.AreEqual(2, response.OperationDataDTOs.Count);
            Assert.AreEqual(1, response.OperationDataDTOs[0].Id);

            // Assert - New way
            Assert.AreEqual(2, response.Items.Count);
            Assert.AreEqual(1, response.Items[0].Id);
            Assert.AreEqual(100, response.TotalCount);
            Assert.AreEqual(2, response.PageNumber);
            Assert.AreEqual(25, response.PageSize);
            Assert.AreEqual(4, response.TotalPages);
            Assert.IsTrue(response.HasPreviousPage);
            Assert.IsTrue(response.HasNextPage);
        }
    }
}
```

---

## 5. Regression Tests

### End-to-End Tests

#### File: `Tests/Integration/PaginationRegressionTests.cs`

```csharp
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OperationGuidance_service.Models.Requests;
using OperationGuidance_service.Models.Responses;

namespace OperationGuidance_service.Tests.Integration {
    [TestClass]
    public class PaginationRegressionTests {
        [TestMethod]
        public void Serialization_QueryOperationDataListReq_ShouldWork() {
            // Arrange
            var request = new QueryOperationDataListReq {
                PageNumber = 2,
                PageSize = 25,
                VinNumber = "ABC123",
                WorkstationId = 5
            };

            // Act & Assert - Verify serialization works
            var json = JsonSerializer.Serialize(request);
            Assert.IsNotNull(json);
            Assert.IsTrue(json.Contains("\"pageNumber\":2"));
            Assert.IsTrue(json.Contains("\"pageSize\":25"));

            // Act & Assert - Verify deserialization works
            var deserialized = JsonSerializer.Deserialize<QueryOperationDataListReq>(json);
            Assert.IsNotNull(deserialized);
            Assert.AreEqual(2, deserialized.PageNumber);
            Assert.AreEqual(25, deserialized.PageSize);
            Assert.AreEqual("ABC123", deserialized.VinNumber);
            Assert.AreEqual(5, deserialized.WorkstationId);
        }

        [TestMethod]
        public void Serialization_QueryOperationDataListRsp_ShouldWork() {
            // Arrange
            var response = new QueryOperationDataListRsp();
            response.SetPagedData(
                new List<OperationDataDTO> {
                    new OperationDataDTO { Id = 1 },
                    new OperationDataDTO { Id = 2 }
                },
                100,
                2,
                50
            );

            // Act & Assert - Verify serialization works
            var json = JsonSerializer.Serialize(response);
            Assert.IsNotNull(json);
            Assert.IsTrue(json.Contains("\"totalCount\":100"));
            Assert.IsTrue(json.Contains("\"pageNumber\":2"));

            // Act & Assert - Verify deserialization works
            var deserialized = JsonSerializer.Deserialize<QueryOperationDataListRsp>(json);
            Assert.IsNotNull(deserialized);
            Assert.AreEqual(100, deserialized.PagedResult.TotalCount);
            Assert.AreEqual(2, deserialized.PagedResult.PageNumber);
            Assert.AreEqual(50, deserialized.PagedResult.PageSize);
            Assert.AreEqual(2, deserialized.OperationDataDTOs.Count);
            Assert.AreEqual(2, deserialized.Items.Count);
        }

        [TestMethod]
        public void BackwardCompatibility_OldClientCode_ShouldStillWork() {
            // Arrange
            var response = new QueryOperationDataListRsp();
            response.SetPagedData(
                new List<OperationDataDTO> {
                    new OperationDataDTO { Id = 1 }
                },
                50,
                1,
                25
            );

            // Act - Old way (backward compatibility)
            var oldWayData = response.OperationDataDTOs;
            var oldWayPagedResult = response.PagedResult;

            // Assert - Old way should still work
            Assert.IsNotNull(oldWayData);
            Assert.AreEqual(1, oldWayData.Count);
            Assert.IsNotNull(oldWayPagedResult);
            Assert.AreEqual(1, oldWayPagedResult.Data.Count);

            // Act - New way
            var newWayData = response.Items;
            var newWayTotalCount = response.TotalCount;

            // Assert - New way should also work
            Assert.IsNotNull(newWayData);
            Assert.AreEqual(1, newWayData.Count);
            Assert.AreEqual(50, newWayTotalCount);
        }

        [TestMethod]
        public void ToPaginationParams_DefaultBehavior_ShouldMatchOldManualCreation() {
            // Arrange
            var request = new QueryOperationDataListReq {
                PageNumber = 3,
                PageSize = 30
            };

            // Act - New way
            var newWay = request.ToPaginationParams("id", true);

            // Old way (manual creation)
            var oldWay = new PaginationParams {
                PageNumber = 3,
                PageSize = 30,
                OrderBy = "id",
                Descending = true
            };

            // Assert - Both should produce same result
            Assert.AreEqual(newWay.PageNumber, oldWay.PageNumber);
            Assert.AreEqual(newWay.PageSize, oldWay.PageSize);
            Assert.AreEqual(newWay.OrderBy, oldWay.OrderBy);
            Assert.AreEqual(newWay.Descending, oldWay.Descending);
            Assert.AreEqual(newWay.Offset, oldWay.Offset);
        }
    }
}
```

---

## Test Execution Plan

### Phase 1: Unit Tests (Base Classes)
1. Run `PaginationRequestBaseTests` - All tests must pass
2. Run `PaginationResponseBaseTests` - All tests must pass
3. Verify code coverage > 95%

### Phase 2: Unit Tests (Models)
1. Run `QueryOperationDataListReqTests` - All tests must pass
2. Run `QueryOperationDataListRspTests` - All tests must pass
3. Run `QueryMissionRecordListReqTests` - All tests must pass
4. Run `QueryMissionRecordListRspTests` - All tests must pass

### Phase 3: Service Layer Tests
1. Run `OperationDataServiceTests` - All tests must pass
2. Run `MissionRecordServiceTests` - All tests must pass
3. Verify ToPaginationParams() is called correctly

### Phase 4: API Tests
1. Run `OperationGuidanceApisTests` - All tests must pass
2. Test all pagination endpoints
3. Verify response format

### Phase 5: Regression Tests
1. Run `PaginationRegressionTests` - All tests must pass
2. Verify backward compatibility
3. Test serialization/deserialization
4. Verify no breaking changes

### Phase 6: Manual Testing
1. Test API endpoints with Swagger UI
2. Verify pagination works correctly
3. Check JSON response format
4. Verify backward compatibility with existing clients

---

## Success Criteria

### All Tests Must Pass
- [ ] 100% of unit tests pass
- [ ] 100% of integration tests pass
- [ ] 100% of regression tests pass
- [ ] Code coverage > 90%

### No Breaking Changes
- [ ] Existing client code works without modification
- [ ] API contracts remain unchanged
- [ ] JSON serialization/deserialization works correctly
- [ ] All existing functionality preserved

### Performance
- [ ] No performance regression
- [ ] Pagination query times unchanged
- [ ] Response times unchanged

---

## Test Data Management

### Test Data Setup
- Use in-memory data for unit tests
- Use test database for integration tests
- Use factories for creating test objects

### Test Object Factory
```csharp
public static class TestDataFactory {
    public static QueryOperationDataListReq CreateValidQueryOperationDataListReq() {
        return new QueryOperationDataListReq {
            PageNumber = 1,
            PageSize = 50,
            UserId = 1,
            VinNumber = "TEST123"
        };
    }

    public static QueryOperationDataListRsp CreateValidQueryOperationDataListRsp() {
        var response = new QueryOperationDataListRsp();
        var data = new List<OperationDataDTO> {
            new OperationDataDTO { Id = 1 },
            new OperationDataDTO { Id = 2 }
        };
        response.SetPagedData(data, 100, 1, 50);
        return response;
    }
}
```

---

## Continuous Integration

### GitHub Actions Workflow
```yaml
name: Pagination Refactoring Tests

on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v2

      - name: Setup .NET
        uses: actions/setup-dotnet@v1
        with:
          dotnet-version: '6.0.x'

      - name: Restore dependencies
        run: dotnet restore

      - name: Build
        run: dotnet build --no-restore

      - name: Test
        run: dotnet test --no-build --verbosity normal --collect:"XPlat Code Coverage"
        env:
          ConnectionStrings__DefaultConnection: "Server=test;Database=test;Trusted_Connection=true;"

      - name: Upload coverage reports
        uses: codecov/codecov-action@v2
```

---

This comprehensive testing strategy ensures that the pagination abstraction refactoring maintains 100% backward compatibility, introduces no regressions, and improves code quality while preserving all existing functionality.
