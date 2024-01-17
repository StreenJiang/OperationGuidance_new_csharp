namespace OperationGuidance_service.Utils {
    public static class ArgumentValidator {
        public static void ValidateInt(int arguement, string errorMsg) {
            if (arguement <= 0) {
                throw new ArgumentNullException(errorMsg);
            }
        }

    }
}
