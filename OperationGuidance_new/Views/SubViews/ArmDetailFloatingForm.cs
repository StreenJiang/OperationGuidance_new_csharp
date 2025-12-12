using CustomLibrary.Configs;
using CustomLibrary.Forms;
using CustomLibrary.Panels;
using CustomLibrary.Utils;
using OperationGuidance_new.Tasks;

namespace OperationGuidance_new.Views.SubViews {
    public class ArmDetailFloatingForm: CustomFloatingForm {
        private readonly Image _statusIconConnected = Properties.Resources.device_connected;
        private readonly Image _statusIconDisconnected = Properties.Resources.device_disconnected;

        private int _panelHeight;

        public ArmDetailFloatingForm(string categoryName, List<IoBoxTask> armTasks, int panelHeight) {
            BorderColor = ColorConfigs.COLOR_POP_UP_BORDER;
            Title = "设备连接信息 - " + categoryName;
            ContentPanel.FlowDirection = FlowDirection.TopDown;
            _panelHeight = panelHeight;

            DisplayArmDetails(armTasks);
        }

        private void DisplayArmDetails(List<IoBoxTask> armTasks) {
            Font font = new(WidgetsConfigs.SystemFontFamily, _panelHeight * .55F, FontStyle.Regular, GraphicsUnit.Pixel);

            foreach (IoBoxTask armTask in armTasks) {
                CustomContentPanel panel = new() {
                    Parent = ContentPanel,
                };
                ContentPanel.SizeChanged += (sender, eventArgs) => {
                    panel.Size = new(ContentPanel.Width - ContentPanel.Padding.Size.Width, _panelHeight);
                };
                panel.Paint += (sender, eventArgs) => {
                    Graphics g = eventArgs.Graphics;
                    Image icon;
                    int imageSide = (int) (_panelHeight * .8);
                    if (armTask.Connected) {
                        icon = WidgetUtils.ResizeImage(_statusIconConnected, imageSide, imageSide);
                    } else {
                        icon = WidgetUtils.ResizeImage(_statusIconDisconnected, imageSide, imageSide);
                    }
                    int imageY = (_panelHeight - imageSide) / 2;
                    g.DrawImage(icon, new Point(0, imageY));
                    g.DrawString($"{armTask.Ip} : {armTask.Port}", font, new SolidBrush(ColorConfigs.COLOR_TEXT_BOX_FOREGROUND), new Point((int) (_panelHeight * 1.15), imageY));
                };
            }
        }
    }
}

