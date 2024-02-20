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
            return splitValues.All(r => byte.TryParse(r, out tempForParsing));
        }

        public static bool ValidatePortInWindows(string portString) {
            if (String.IsNullOrWhiteSpace(portString)) return false;

            int port;
            if (!int.TryParse(portString, out port)) return false;

            return port >= 1 && port <= 65535;
        }
    }
}
