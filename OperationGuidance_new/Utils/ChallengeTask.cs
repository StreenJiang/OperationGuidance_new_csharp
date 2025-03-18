namespace OperationGuidance_new.Utils {
    public class ChallengeTask {
        private int _missionId;
        private DateTime? _checkTime;
        private List<ChallengeTaskEnum>? _checks;

        public int MissionId { get => _missionId; set => _missionId = value; }
        public DateTime? CheckTime { get => _checkTime; set => _checkTime = value; }
        public List<ChallengeTaskEnum>? Checks { get => _checks; set => _checks = value; }

        public bool IsToday() => _checkTime != null && _checkTime?.Date == DateTime.Now.Date;

        public bool ProductBarCodeErrorOK() => IsToday() && _checks != null
            && _checks.Contains(ChallengeTaskEnum.PRODUCT_BAR_CODE_ERROR);

        public bool ProductBarCodeRedoOK() => IsToday() && _checks != null
            && _checks.Contains(ChallengeTaskEnum.PRODUCT_BAR_CODE_REDO);

        public bool PartsBarCodeErrorOK() => IsToday() && _checks != null
            && _checks.Contains(ChallengeTaskEnum.PARTS_BAR_CODE_ERROR);

        public bool PartsBarCodeRedoOK() => IsToday() && _checks != null
            && _checks.Contains(ChallengeTaskEnum.PARTS_BAR_CODE_REDO);

        public bool PredecessorOK() => IsToday() && _checks != null
            && _checks.Contains(ChallengeTaskEnum.PREDECESSOR);

        public bool MissionOK() => IsToday() && _checks != null
            && _checks.Contains(ChallengeTaskEnum.MISSION_OK);

        public void AddResult(ChallengeTaskEnum type) {
            if (!IsToday()) {
                _checkTime = DateTime.Now.Date;
                _checks = null;
            }

            if (_checks == null) {
                _checks = new();
            }
            _checks.Add(type);
        }
    }
}
