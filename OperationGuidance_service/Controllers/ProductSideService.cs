using OperationGuidance_service.Attributes;
using OperationGuidance_service.Controllers.AbstractClasses;
using OperationGuidance_service.Models;
using OperationGuidance_service.Wrapper;

namespace OperationGuidance_service.Controllers {
    [Service]
    public class ProductSideService: AServiceBase<ProductSide, ProductSideWrapper> {

        public ProductSide? InsertOrUpdate(ProductSide productSide) {
            if (productSide.id > 0) {
                return UpdateEntity(productSide);
            } else {
                return AddEntity(productSide);
            }
        }

        public ProductSide? GetById(int id) {
            return this.Wrapper.FindById(id);
            ;
        }
    }
}
