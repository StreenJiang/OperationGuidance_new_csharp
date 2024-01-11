using OperationGuidance_service.Attributes;
using OperationGuidance_service.Services.AbstractClasses;
using OperationGuidance_service.Models;
using OperationGuidance_service.Wrapper;

namespace OperationGuidance_service.Services {
    [Service]
    public class ProductSideService: AServiceBase<ProductSide, ProductSideWrapper> {

        public ProductSide? GetById(int id) {
            return this.Wrapper.FindById(id);
            ;
        }
    }
}
