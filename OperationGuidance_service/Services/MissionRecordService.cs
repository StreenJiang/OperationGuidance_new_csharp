using OperationGuidance_service.Attributes;
using OperationGuidance_service.Constants;
using OperationGuidance_service.Models;
using OperationGuidance_service.Models.DTOs;
using OperationGuidance_service.Models.Requests;
using OperationGuidance_service.Services.AbstractClasses;
using OperationGuidance_service.Wrapper;
using System.Text;

namespace OperationGuidance_service.Services {
    [Service]
    public class MissionRecordService: AServiceBase<MissionRecord, MissionRecordWrapper> {

        // 【分页查询】新增分页查询方法，支持多种搜索条件
        public PagedResult<MissionRecord> QueryMissionRecordListWithPagination(QueryMissionRecordListReq req) {
            // 构建完整的SQL查询（包含JOIN product_mission表用于过滤条件）
            var sqlBuilder = new StringBuilder();
            sqlBuilder.Append("SELECT mr.* ");
            sqlBuilder.Append("FROM mission_record mr ");
            sqlBuilder.Append("LEFT JOIN product_mission pm ON mr.mission_id = pm.id ");
            sqlBuilder.Append("WHERE mr.deleted = @deleted and pm.deleted = @deleted ");

            var parameters = new Dictionary<string, object> {
                ["deleted"] = (int) YesOrNo.NO
            };

            // 添加用户ID条件
            if (req.UserId.HasValue) {
                sqlBuilder.Append("AND mr.user_id = @user_id ");
                parameters["user_id"] = req.UserId.Value;
            }

            // 添加ID列表条件
            if (req.Ids != null && req.Ids.Any()) {
                sqlBuilder.Append("AND mr.id IN @ids ");
                parameters["ids"] = req.Ids;
            }

            // 添加日期条件
            if (req.Date.HasValue) {
                sqlBuilder.Append("AND date(mr.create_time) = date(@date) ");
                parameters["date"] = req.Date.Value;
            }

            // 添加任务ID条件
            if (req.MissionId.HasValue) {
                sqlBuilder.Append("AND mr.mission_id = @mission_id ");
                parameters["mission_id"] = req.MissionId.Value;
            }

            // 添加产品批次条件
            if (!string.IsNullOrEmpty(req.ProductBatch)) {
                sqlBuilder.Append("AND mr.product_batch LIKE @product_batch ");
                parameters["product_batch"] = $"%{req.ProductBatch}%";
            }

            // 添加总成码/追溯码条件
            if (!string.IsNullOrEmpty(req.ProductBarCode)) {
                sqlBuilder.Append("AND mr.product_bar_code LIKE @product_bar_code ");
                parameters["product_bar_code"] = $"%{req.ProductBarCode}%";
            }

            // 添加物料码条件
            if (!string.IsNullOrEmpty(req.PartsBarCode)) {
                sqlBuilder.Append("AND mr.parts_bar_code LIKE @parts_bar_code ");
                parameters["parts_bar_code"] = $"%{req.PartsBarCode}%";
            }

            // 添加任务名称条件（通过JOIN的product_mission表查询）
            if (!string.IsNullOrEmpty(req.MissionName)) {
                sqlBuilder.Append("AND pm.name LIKE @mission_name ");
                parameters["mission_name"] = $"%{req.MissionName}%";
            }

            // 添加是否挑战任务条件（通过JOIN的product_mission表查询）
            if (req.IsChallengeMission.HasValue && req.IsChallengeMission.Value) {
                sqlBuilder.Append("AND pm.is_challenge_mission = @is_challenge_mission ");
                parameters["is_challenge_mission"] = (int) YesOrNo.YES;
            } else {
                sqlBuilder.Append("AND (pm.is_challenge_mission = @is_challenge_mission OR pm.is_challenge_mission is null)");
                parameters["is_challenge_mission"] = (int) YesOrNo.NO;
            }

            string baseSql = sqlBuilder.ToString();

            // 使用新的便捷方法进行分页查询
            return Wrapper.FindWithPaginationBySql(
                baseSql,
                parameters,
                req.PageNumber,
                req.PageSize,
                "mr.id DESC"
            );
        }
    }
}
