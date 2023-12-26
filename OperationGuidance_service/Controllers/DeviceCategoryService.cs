using OperationGuidance_service.Attributes;
using OperationGuidance_service.Controllers.AbstractClasses;
using OperationGuidance_service.Models;
using OperationGuidance_service.Wrapper;

namespace OperationGuidance_service.Controllers {
    [Service]
    public class DeviceCategoryService: AServiceBase<DeviceCategory, DeviceCategoryWrapper> {
    }
}
