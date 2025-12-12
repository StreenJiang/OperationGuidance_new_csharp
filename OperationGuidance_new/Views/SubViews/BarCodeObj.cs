namespace OperationGuidance_new.Views.SubViews {
    public class BarCodeObj {
        public string ProductBarCode { get; set; } = string.Empty;
        public List<string> PartsBarCodes { get; } = new();
        public List<int> PartsMatchingRulesCached { get; } = new();
        public int PartsRulesCount { get; set; } = 0;

        public void Reset() {
            ProductBarCode = string.Empty;
            PartsBarCodes.Clear();
            PartsMatchingRulesCached.Clear();
            PartsRulesCount = 0;
        }
    }
}

