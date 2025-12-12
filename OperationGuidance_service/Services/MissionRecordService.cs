using OperationGuidance_service.Attributes;
using OperationGuidance_service.Services.AbstractClasses;
using OperationGuidance_service.Models;
using OperationGuidance_service.Wrapper;
using OperationGuidance_service.Models.DTOs;
using OperationGuidance_service.Models.Requests;
using OperationGuidance_service.Constants;

namespace OperationGuidance_service.Services {
    [Service]
    public class MissionRecordService: AServiceBase<MissionRecord, MissionRecordWrapper> {

        // 【分页查询】新增分页查询方法，支持多种搜索条件
        public PagedResult<MissionRecord> QueryMissionRecordListWithPagination(QueryMissionRecordListReq req) {
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

            // 添加ID列表条件
            if (req.Ids != null && req.Ids.Any()) {
                conditions.Add("id in @ids");
                parameters["ids"] = req.Ids;
            }

            // 添加日期条件
            if (req.Date.HasValue) {
                conditions.Add("date(create_time) = date(@date)");
                parameters["date"] = req.Date.Value;
            }

            // 添加任务ID条件
            if (req.MissionId.HasValue) {
                conditions.Add("mission_id = @mission_id");
                parameters["mission_id"] = req.MissionId.Value;
            }

            // 添加产品批次条件
            if (!string.IsNullOrEmpty(req.ProductBatch)) {
                conditions.Add("product_batch LIKE @product_batch");
                parameters["product_batch"] = $"%{req.ProductBatch}%";
            }

            // 添加总成码/追溯码条件
            if (!string.IsNullOrEmpty(req.ProductBarCode)) {
                conditions.Add("product_bar_code LIKE @product_bar_code");
                parameters["product_bar_code"] = $"%{req.ProductBarCode}%";
            }

            // 添加物料码条件
            if (!string.IsNullOrEmpty(req.PartsBarCode)) {
                conditions.Add("parts_bar_code LIKE @parts_bar_code");
                parameters["parts_bar_code"] = $"%{req.PartsBarCode}%";
            }

            // 添加是否挑战任务条件
            if (req.IsChallengeMission.HasValue) {
                conditions.Add("is_challenge_mission = @is_challenge_mission");
                parameters["is_challenge_mission"] = req.IsChallengeMission.Value ? (int) YesOrNo.YES : (int) YesOrNo.NO;
            }

            // 组合WHERE子句
            string whereClause = string.Join(" AND ", conditions);

            // 使用基类便利方法构建分页参数
            var paginationParams = req.ToPaginationParams("id", true);

            // 使用Wrapper的分页查询方法
            return Wrapper.FindWithPagination(whereClause, parameters, paginationParams);
        }
    }
}
