using OperationGuidance_service.Attributes;
using OperationGuidance_service.Services.AbstractClasses;
using OperationGuidance_service.Models;
using OperationGuidance_service.Wrapper;
using OperationGuidance_service.Constants;
using OperationGuidance_service.Models.DTOs;
using OperationGuidance_service.Models.Requests;

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

        // 【分页查询】新增分页查询方法，支持多种搜索条件
        public PagedResult<OperationData> QueryOperationDataListWithPagination(QueryOperationDataListReq req) {
            // 构建WHERE条件
            var conditions = new List<string> {
                $"deleted = @deleted"
            };
            var parameters = new Dictionary<string, object> {
                ["deleted"] = (int) YesOrNo.NO
            };

            // 添加用户ID条件
            if (req.UserId.HasValue) {
                conditions.Add("user_id = @user_id");
                parameters["user_id"] = req.UserId.Value;
            }

            // 添加任务记录ID条件
            if (req.MissionRecordId.HasValue) {
                conditions.Add("mission_record_id = @mission_record_id");
                parameters["mission_record_id"] = req.MissionRecordId.Value;
            }

            // 添加VIN号搜索条件
            if (!string.IsNullOrEmpty(req.VinNumber)) {
                conditions.Add("vin_number LIKE @vin_number");
                parameters["vin_number"] = $"%{req.VinNumber}%";
            }

            // 添加站点ID条件
            if (req.WorkstationId.HasValue) {
                conditions.Add("workstation_id = @workstation_id");
                parameters["workstation_id"] = req.WorkstationId.Value;
            }

            // 添加日期范围条件
            if (req.StartDate.HasValue) {
                conditions.Add("create_time >= @start_date");
                parameters["start_date"] = req.StartDate.Value;
            }
            if (req.EndDate.HasValue) {
                conditions.Add("create_time <= @end_date");
                parameters["end_date"] = req.EndDate.Value;
            }

            // 组合WHERE子句
            string whereClause = string.Join(" AND ", conditions);

            // 使用基类便利方法构建分页参数
            var paginationParams = req.ToPaginationParams("id", true);

            // 使用Wrapper的分页查询方法
            return Wrapper.FindWithPagination(whereClause, parameters, paginationParams);
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
