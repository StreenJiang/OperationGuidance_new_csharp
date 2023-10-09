namespace OperationGuidance_service.Utils {
    public static class ArgumentValidator {
        public static void Validate(int arguement, string errorMsg) {
            if (arguement <= 0) {
                throw new ArgumentNullException(errorMsg);
            }
        }

    }
}
