using OperationGuidance_service.Models.DTOs;

namespace OperationGuidance_new.Views {
    public partial class MissionEditionView_SCII_XT: MissionEditionView_SCII {
        public MissionEditionView_SCII_XT() : base() { }

        // Class: inner page panel
        public class MissionEditionPage_SCII_XT: MissionEditionPage_SCII {
            public MissionEditionPage_SCII_XT(MissionEditionView_SCII_XT parent, ProductMissionDTO missionDTO) : base(parent, missionDTO) { }

        }
    }
}
