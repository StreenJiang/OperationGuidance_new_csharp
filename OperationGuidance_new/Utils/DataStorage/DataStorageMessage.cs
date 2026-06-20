using OperationGuidance_new.Constants;
using OperationGuidance_service.Models.DTOs;

namespace OperationGuidance_new.Utils.DataStorage {
    public abstract record DataStorageMessage;

    public record TighteningDataMessage(OperationDataDTO Data) : DataStorageMessage;

    public record CurveDataMessage(CurveDataTemp Data, int DeviceId) : DataStorageMessage;

    public record ExportDataMessage(string Result) : DataStorageMessage;
}
