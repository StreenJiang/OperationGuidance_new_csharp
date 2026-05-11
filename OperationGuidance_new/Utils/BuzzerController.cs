using System.Diagnostics;
using log4net;

namespace OperationGuidance_new.Utils {
    public static class BuzzerController {
        private static readonly ILog logger = LogManager.GetLogger(typeof(BuzzerController));

        private static readonly string LightOnCmd = "01050000F00089CA";
        private static readonly string LightOffCmd = "010500000000CDCA";
        private static readonly string SoundOnCmd = "01050003F00079CA";
        private static readonly string SoundOffCmd = "0105000300003DCA";

        private static string ExePath =>
            Path.Combine(Application.StartupPath, "didi_control", "DiDi.exe");

        public static void TurnOn() {
            SendCommand(LightOnCmd);
            SendCommand(SoundOnCmd);
        }

        public static void TurnOff() {
            SendCommand(LightOffCmd);
            SendCommand(SoundOffCmd);
        }

        private static void SendCommand(string command) {
            try {
                if (!File.Exists(ExePath)) {
                    logger.Warn($"[BuzzerController] DiDi.exe not found at: {ExePath}");
                    return;
                }
                using var _ = Process.Start(new ProcessStartInfo {
                    FileName = ExePath,
                    Arguments = $"POSTCOMMAND={command}",
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true,
                });
            } catch (Exception ex) {
                logger.Error($"[BuzzerController] Failed to send command '{command}'", ex);
            }
        }
    }
}
