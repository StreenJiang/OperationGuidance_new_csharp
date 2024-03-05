namespace OperationGuidance_service.Utils {
    public static class ArgumentValidator {
        public static void ValidateInt(int arguement, string errorMsg) {
            if (arguement <= 0) {
                throw new ArgumentNullException(errorMsg);
            }
        }

        public static bool ValidateIPv4(string ipString) {
            if (String.IsNullOrWhiteSpace(ipString)) return false;

            string[] splitValues = ipString.Split('.');
            if (splitValues.Length != 4) return false;

            byte tempForParsing;
            bool result = splitValues.All(r => byte.TryParse(r, out tempForParsing));
            if (result) {
                // IP地址第一位不能小于等于0，检查一下
                for (int i = 0; i < splitValues.Length; i++) {
                    int value;
                    bool canParse = int.TryParse(splitValues[i], out value);
                    if (!canParse) {
                        return false;
                    }
                    if (i == 0 && (value < 1 || value > 255)) {
                        return false;
                    } else if (value < 0 || value > 255) {
                        return false;
                    }
                }
            }
            return result;
        }

        public static bool ValidatePortInWindows(string portString) {
            if (String.IsNullOrWhiteSpace(portString)) return false;

            int port;
            if (!int.TryParse(portString, out port)) return false;

            return port >= 1 && port <= 65535;
        }
    }
}
