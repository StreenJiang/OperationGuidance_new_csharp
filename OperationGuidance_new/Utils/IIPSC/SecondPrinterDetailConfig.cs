using OperationGuidance_new.Configs;

namespace OperationGuidance_new.Utils.IIPSC {
    public class SecondPrinterDetailConfig: ConfigBase {
        // DPI dots per mm: 8=203dpi, 12=300dpi, 24=600dpi
        public double dpmm { get; set; }
        // 标签尺寸(mm)，正方形标签边长
        public double label_size_mm { get; set; }
        // QR码目标尺寸(mm)
        public double qr_size_mm { get; set; }
        public const double MarginFactorMin = 0;
        public const double MarginFactorMax = 2;
        // 边距系数: 0=左/上对齐, 1=居中, 2=右/下对齐
        public double margin_x_factor { get; set; }
        public double margin_y_factor { get; set; }

        public SecondPrinterDetailConfig() {
            dpmm = ZplQrCodePrinter.DPMM_300DPI;
            label_size_mm = 9;
            qr_size_mm = 5.4;
            margin_x_factor = 1;
            margin_y_factor = 1;
        }
    }
}
