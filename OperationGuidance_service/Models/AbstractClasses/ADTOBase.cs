using Newtonsoft.Json;
using OperationGuidance_service.Constants;
using OperationGuidance_service.Utils;

namespace OperationGuidance_service.Models.AbstractClasses {
    public abstract class ADTOBase {
        public int id { get; set; } = -1;
        public int user_id { get; set; } = SystemUtils.LoggedUserId;
        public int deleted { get; set; } = (int) YesOrNo.NO;
        public string creator { get; set; } = SystemUtils.LoggedUserName;
        public string modifier { get; set; } = SystemUtils.LoggedUserName;
        public DateTime create_time { get; set; } = DateTime.Now;
        public DateTime modify_time { get; set; } = DateTime.Now;

        public T Clone<T>() where T: ADTOBase {
            return CommonUtils.CannotBeNull(JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(this)));
        }
    }
}
