using CustomLibrary.Configs;
using CustomLibrary.Forms;
using CustomLibrary.Utils;
using OperationGuidance_new.Utils;
using OperationGuidance_new.Utils.IIPSC;
using OperationGuidance_new.Views;
using OperationGuidance_service.Constants;

namespace OperationGuidance_new.Views.SubViews {
    public class PrinterOperationPopUpForm: CustomPopUpForm {
        private TableLayoutPanel _tablePanel;
        private FunctionButton _btnReprintLid;
        private FunctionButton _btnReprintDiverter;

        public int ContentWidth {
            get {
                int btnH = WidgetUtils.TextOrComboBoxHeight();
                using (var font = new Font(WidgetsConfigs.SystemFontFamily,
                                           btnH * 0.425f, FontStyle.Bold, GraphicsUnit.Pixel)) {
                    int w1 = TextRenderer.MeasureText(_btnReprintLid.Label, font).Width;
                    int w2 = TextRenderer.MeasureText(_btnReprintDiverter.Label, font).Width;
                    return Math.Max(w1, w2) + btnH * 2 + 10;
                }
            }
        }

        private readonly WorkplaceContentPanel_SCII_XT _workplace;

        public PrinterOperationPopUpForm(WorkplaceContentPanel_SCII_XT workplace) {
            _workplace = workplace;
            BorderColor = ColorConfigs.COLOR_POP_UP_BORDER;
            Title = "打印机操作";

            var config = ConfigUtils.LoadConfig<SciiXtPrinterConfig>();

            bool firstEnabled = config.enabled == (int) YesOrNo.YES
                && !string.IsNullOrEmpty(config.printer_name);
            bool secondEnabled = config.enabled_second == (int) YesOrNo.YES
                && !string.IsNullOrEmpty(config.second_printer_name);

            string firstLabel = firstEnabled
                ? $"上盖码重打 — {config.printer_name}"
                : "上盖码重打（未启用）";
            string secondLabel = secondEnabled
                ? $"分流器码重打 — {config.second_printer_name}"
                : "分流器码重打（未启用）";

            _tablePanel = new() {
                Margin = new(0),
                Padding = new(0),
                ColumnCount = 1,
                RowCount = 2,
                Parent = ContentPanel,
            };

            _btnReprintLid = new() {
                Label = firstLabel,
                Enabled = firstEnabled,
                Parent = _tablePanel,
            };
            _btnReprintLid.Click += (s, e) => {
                Dispose();
                _workplace.OpenLidCodeReprintPopUp();
            };
            _btnReprintDiverter = new() {
                Label = secondLabel,
                Enabled = secondEnabled,
                Parent = _tablePanel,
            };
            _btnReprintDiverter.Click += (s, e) => {
                Dispose();
                _workplace.OpenDiverterCodeReprintPopUp();
            };

            FunctionButton btnClose = AddButton("关闭");
            btnClose.Click += (s, e) => Dispose();
        }

        protected override void ResizeChildren(object? sender, EventArgs eventArgs) {
            base.ResizeChildren(sender, eventArgs);
            _tablePanel.Size = new(ContentPanel.Width - ContentPanel.Padding.Size.Width,
                                    ContentPanel.Height - ContentPanel.Padding.Size.Height);
            int gap = _tablePanel.Height / 6;
            int btnHeight = (_tablePanel.Height - gap) / 2;
            _btnReprintLid.Size = new(_tablePanel.Width, btnHeight);
            _btnReprintDiverter.Size = new(_tablePanel.Width, btnHeight);
            _btnReprintDiverter.Margin = new Padding(0, gap, 0, 0);
        }
    }
}
