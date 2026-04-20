
using log4net;
using OperationGuidance_new.Configs;
using S7.Net;

namespace OperationGuidance_new.Utils {
    public class PlcConfig_GLB: SettingsFileUtil {
        private ILog log = LogManager.GetLogger(typeof(PlcConfigsKeys_GLB));

        public PlcConfig_GLB() : base("GlbPlcConfig", ".ini") {
        }

        public void Init() {
            string cupType = Read(PlcConfigsKeys_GLB.CpuType);

            string configStr_barCode = Read(PlcConfigsKeys_GLB.ConfigString, PlcConfigsKeys_GLB.SectionName_BarCode);
            string configStr_barCodeDone = Read(PlcConfigsKeys_GLB.ConfigString, PlcConfigsKeys_GLB.SectionName_BarCodeDone);
            string configStr_startSignal = Read(PlcConfigsKeys_GLB.ConfigString, PlcConfigsKeys_GLB.SectionName_StartSignal);
            string configStr_jobFinished = Read(PlcConfigsKeys_GLB.ConfigString, PlcConfigsKeys_GLB.SectionName_JobFinished);
            string configStr_jobResult = Read(PlcConfigsKeys_GLB.ConfigString, PlcConfigsKeys_GLB.SectionName_JobResult);

            if (string.IsNullOrEmpty(cupType)) {
                // 写注释
                if (!KeyExists(PlcConfigsKeys_GLB.CpuType)) {
                    List<string> cupTypeList = new();
                    foreach (string model in Enum.GetNames<CpuType>()) {
                        cupTypeList.Add(model);
                    }
                    WriteComment($"# ==== 【cup_type】支持类型：{string.Join("/", cupTypeList)} ===");
                }

                Write(PlcConfigsKeys_GLB.CpuType, "?");
            }
            if (string.IsNullOrEmpty(configStr_barCode)) {
                Write(PlcConfigsKeys_GLB.ConfigString, "", PlcConfigsKeys_GLB.SectionName_BarCode);
            }
            if (string.IsNullOrEmpty(configStr_barCodeDone)) {
                Write(PlcConfigsKeys_GLB.ConfigString, "", PlcConfigsKeys_GLB.SectionName_BarCodeDone);
            }
            if (string.IsNullOrEmpty(configStr_startSignal)) {
                Write(PlcConfigsKeys_GLB.ConfigString, "", PlcConfigsKeys_GLB.SectionName_StartSignal);
            }
            if (string.IsNullOrEmpty(configStr_jobFinished)) {
                Write(PlcConfigsKeys_GLB.ConfigString, "", PlcConfigsKeys_GLB.SectionName_JobFinished);
            }
            if (string.IsNullOrEmpty(configStr_jobResult)) {
                Write(PlcConfigsKeys_GLB.ConfigString, "", PlcConfigsKeys_GLB.SectionName_JobResult);
            }
        }

        public CpuType GetCpuType() {
            string cpuType = Read(PlcConfigsKeys_GLB.CpuType);

            if (string.IsNullOrWhiteSpace(cpuType)) {
                throw new InvalidOperationException($"PLC 型号配置项 '{PlcConfigsKeys_GLB.CpuType}' 为空或未设置，请检查配置文件。");
            }

            if (Enum.TryParse<CpuType>(cpuType, ignoreCase: true, out CpuType result)) {
                return result;
            }

            var validValues = string.Join(", ", Enum.GetNames(typeof(CpuType)));
            throw new InvalidOperationException(
                $"无法解析 PLC 型号：'{cpuType}'。" +
                $"支持的类型（不区分大小写）：{validValues}。" +
                $"请检查配置文件中 '{PlcConfigsKeys_GLB.CpuType}' 的值是否正确。");
        }

        public PlcTagConfig_GLB BarCodeConfig() => BarCodeConfig(PlcConfigsKeys_GLB.SectionName_BarCode, "读取条码");
        public PlcTagConfig_GLB BarCodeDoneConfig() => BarCodeConfig(PlcConfigsKeys_GLB.SectionName_BarCodeDone, "条码读取成功");
        public PlcTagConfig_GLB StartSignalConfig() => BarCodeConfig(PlcConfigsKeys_GLB.SectionName_StartSignal, "启动信号");
        public PlcTagConfig_GLB JobFinishedConfig() => BarCodeConfig(PlcConfigsKeys_GLB.SectionName_JobFinished, "完成信号");
        public PlcTagConfig_GLB JobResultConfig() => BarCodeConfig(PlcConfigsKeys_GLB.SectionName_JobResult, "结果信号");

        private PlcTagConfig_GLB BarCodeConfig(string sectionName, string description) {
            string configStr = Read(PlcConfigsKeys_GLB.ConfigString, sectionName);
            if (string.IsNullOrEmpty(configStr)) {
                throw new InvalidOperationException($"{description}的配置（{sectionName} - {PlcConfigsKeys_GLB.ConfigString}）未配置，请检查配置文件。");
            }

            try {
                return PlcTagConfig_GLB.FromDeviceAddress(configStr);
            } catch (Exception ex) {
                log.Error($"{description}的配置（{sectionName} - {PlcConfigsKeys_GLB.ConfigString}）出错，请检查配置文件。", ex);
                throw ex;
            }
        }
    }
}
