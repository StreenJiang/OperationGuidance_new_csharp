using OperationGuidance_new.Configs;

namespace OperationGuidance_new.Utils.IIPSC {
    public class SecondPrinterDetailConfig: ConfigBase {
        // DPI dots per mm: 8=203dpi, 12=300dpi, 24=600dpi
        public double dpmm { get; set; }
        // 标签尺寸(mm)，正方形标签边长
        public double label_size_mm { get; set; }
        // QR码目标尺寸(mm)
        public double qr_size_mm { get; set; }
        // 边距系数(0-1)，0=左上角，1=最大边距
        public double margin_factor { get; set; }

        public SecondPrinterDetailConfig() {
            dpmm = ZplQrCodePrinter.DPMM_300DPI;
            label_size_mm = 9;
            qr_size_mm = 5.4;
            margin_factor = 0.5;
        }
    }
}
