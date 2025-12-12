using CustomLibrary.Configs;
using CustomLibrary.Panels;
using CustomLibrary.Utils;
using System.Drawing.Drawing2D;

namespace OperationGuidance_new.Views.SubViews {
    public class DataPanel: CustomContentPanel {
        private string _data;
        private string _unit;

        public string Data {
            get => _data;
            set {
                _data = value;
                Invalidate();
            }
        }
        public string Title { get => _unit; set => _unit = value; }

        public DataPanel(string unit) {
            _data = "0";
            _unit = unit;
            ForeColor = ColorConfigs.COLOR_TIGHTENING_DATA_NORMAL;
            BackColor = ColorConfigs.COLOR_MAIN_FORM_BACKGROUND;
        }

        protected override void OnPaint(PaintEventArgs e) {
            Graphics graphics = e.Graphics;
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.Clear(BackColor);
            base.OnPaint(e);

            Font dataFont;
            if (float.Parse(_data) > 0) {
                dataFont = WidgetUtils.GetProperFont(Size, _data, .45F, .95F);
            } else {
                dataFont = new Font(WidgetsConfigs.SystemFontFamily, Height * .45F, FontStyle.Bold, GraphicsUnit.Pixel);
            }
            Font unitFont = new Font(WidgetsConfigs.SystemFontFamily, Height * .15F, FontStyle.Regular, GraphicsUnit.Pixel);

            StringFormat dataStrFormat = new StringFormat();
            dataStrFormat.Alignment = StringAlignment.Near;
            dataStrFormat.LineAlignment = StringAlignment.Near;

            StringFormat unitStrFormat = new StringFormat();
            unitStrFormat.Alignment = StringAlignment.Far;
            unitStrFormat.LineAlignment = StringAlignment.Far;

            graphics.DrawString(_data, dataFont, new SolidBrush(ForeColor), new Point(0, 0), dataStrFormat);
            graphics.DrawString(_unit, unitFont, new SolidBrush(WidgetUtils.LightColor(ColorConfigs.COLOR_TIGHTENING_DATA_NORMAL, .5)), new Point(Width, (int) (Height * .95)), unitStrFormat);
        }
    }
}

