using OperationGuidance_service.Attributes;
using OperationGuidance_service.Services.AbstractClasses;
using OperationGuidance_service.Models;
using OperationGuidance_service.Wrapper;

namespace OperationGuidance_service.Services {
    [Service]
    public class WorkstationService : AServiceBase<Workstation, WorkstationWrapper> {
        public Dictionary<int, string> GetWorkstationNamesByIds(List<int> workstationIds) {
            string sql = $"select id, name from {TableName} where id in @ids";

            List<Workstation> workstations = Wrapper.FindBySql(sql, new() { {"ids", workstationIds} });

            return workstations.ToDictionary(w => w.id, w => w.name);
        }
    }
}
