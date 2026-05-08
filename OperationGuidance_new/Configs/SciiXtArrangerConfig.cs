using Newtonsoft.Json;
using OperationGuidance_new.Attributes;
using OperationGuidance_new.Configs.DTOs;

namespace OperationGuidance_new.Configs {
    public class SciiXtArrangerConfig: ConfigBase {
        public string groups { get; set; } = "[]";

        [ConfigIgnore]
        public List<ArrangerGroupDTO> GroupList {
            get {
                try { return JsonConvert.DeserializeObject<List<ArrangerGroupDTO>>(groups) ?? new(); }
                catch { return new(); }
            }
            set { groups = JsonConvert.SerializeObject(value ?? new()); }
        }
    }
}
