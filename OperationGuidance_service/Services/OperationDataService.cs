using OperationGuidance_service.Attributes;
using OperationGuidance_service.Services.AbstractClasses;
using OperationGuidance_service.Models;
using OperationGuidance_service.Wrapper;
using OperationGuidance_service.Constants;

namespace OperationGuidance_service.Services {
    [Service]
    public class OperationDataService: AServiceBase<OperationData, OperationDataWrapper> {

        // 查询数据列表
        public List<OperationData> QueryList(int? userId, int? missionRecordId) {
            string sql = $"select * from {TableName} where deleted = @deleted";
            Dictionary<string, object> parameters = new();
            parameters.Add("deleted", (int) YesOrNo.NO);

            if (userId != null) {
                sql += $" and user_id = @user_id";
                parameters.Add("user_id", userId);
            }
            if (missionRecordId != null) {
                sql += $" and mission_record_id = @mission_record_id";
                parameters.Add("mission_record_id", missionRecordId);
            }

            return FindBySql(sql, parameters);
        }

        // 根据站点 ids 查询任务 ids 和 站点 ids 的对应数据
        public Dictionary<int, List<int>> GetMissionRecordIdsByWorkstationIds(List<int> workstationIds) {
            string sql = $"select mission_record_id, workstation_id from {TableName} where workstation_id in @workstation_ids group by mission_record_id, workstation_id";

            List<OperationData> operationDatas = Wrapper.FindBySql(sql, new() { { "workstation_ids", workstationIds } });

            Dictionary<int, List<int>> result = new();
            operationDatas.ForEach(od => {
                if (od.workstation_id != null && od.mission_record_id != null) {
                    if (!result.ContainsKey(od.workstation_id.Value)) {
                        result.Add(od.workstation_id.Value, new() { od.mission_record_id.Value });
                    } else {
                        result[od.workstation_id.Value].Add(od.mission_record_id.Value);
                    }
                }
            });
            return result;
        }

        // 根据任务记录 ids 查询对应的站点 ids
        public Dictionary<int, Dictionary<int, string>> GetWorkstationInfoByMissionRecordIds(List<int> missionRecordIds) {
            string sql = $"select distinct(mission_record_id), workstation_id from {TableName} where mission_record_id in @ids";

            List<OperationData> operationDatas = Wrapper.FindBySql(sql, new() { { "ids", missionRecordIds } });

            Dictionary<int, Dictionary<int, string>> result = new();
            operationDatas.ForEach(od => {
                if (od.workstation_id != null && od.mission_record_id != null) {
                    if (result.ContainsKey(od.mission_record_id.Value)) {
                        if (!result[od.mission_record_id.Value].ContainsKey(od.workstation_id.Value)) {
                            result[od.mission_record_id.Value].Add(od.workstation_id.Value, "");
                        }
                    } else {
                        result.Add(od.mission_record_id.Value, new() { { od.workstation_id.Value, "" } });
                    }
                }
            });
            return result;
        }

    }
}
