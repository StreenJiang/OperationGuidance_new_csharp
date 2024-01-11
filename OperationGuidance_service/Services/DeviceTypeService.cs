using OperationGuidance_service.Attributes;
using OperationGuidance_service.Services.AbstractClasses;
using OperationGuidance_service.Models;
using OperationGuidance_service.Wrapper;

namespace OperationGuidance_service.Services {
    [Service]
    public class DeviceTypeService: AServiceBase<DeviceModel, DeviceTypeWrapper> {
    }
}
