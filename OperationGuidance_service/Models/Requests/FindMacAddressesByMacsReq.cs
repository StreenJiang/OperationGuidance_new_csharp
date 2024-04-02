using OperationGuidance_service.Models.AbstractClasses;

namespace OperationGuidance_service.Models.Requests {
    public class FindMacAddressesByMacsReq: HttpRequest {
        public List<string> Macs { get; set; }

        public FindMacAddressesByMacsReq(List<string> macs) => Macs = macs;
    }
}
