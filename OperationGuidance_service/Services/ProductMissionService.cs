using OperationGuidance_service.Attributes;
using OperationGuidance_service.Services.AbstractClasses;
using OperationGuidance_service.Models;
using OperationGuidance_service.Utils;
using OperationGuidance_service.Wrapper;

namespace OperationGuidance_service.Services {
    [Service]
    public class ProductMissionService: AServiceBase<ProductMission, ProductMissionWrapper> {

        public List<ProductMission> QueryList(int userId) {
            // Validate each parameter
            ArgumentValidator.Validate(userId, "UserId should greater than 0. Passing 'userId = " + userId + "' incorrectly.");

            // TODO: use cache to prevent fetching data every time
            return Wrapper.FindBySql($"select * from {Wrapper.TabelName} where {Wrapper.CommonCondition()}", new { @user_id = userId });
        }
    }
}
