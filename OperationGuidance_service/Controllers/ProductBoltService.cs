using OperationGuidance_service.Attributes;
using OperationGuidance_service.Controllers.AbstractClasses;
using OperationGuidance_service.Models;
using OperationGuidance_service.Wrapper;

namespace OperationGuidance_service.Controllers {
    [Service]
    public class ProductBoltService: AServiceBase<ProductBolt, ProductBoltWrapper> {

        public ProductBolt? InsertOrUpdate(ProductBolt productBolt) {
            if (productBolt.id > 0) {
                return UpdateEntity(productBolt);
            } else {
                return AddEntity(productBolt);
            }
        }
    }
}
