using CustomLibrary.Configs;
using CustomLibrary.Forms;
using CustomLibrary.Panels;
using OperationGuidance_new.Tasks;
using OperationGuidance_service.Models.DTOs;
using OperationGuidance_service.Utils;

namespace OperationGuidance_new.Views.SubViews {
    public class ArmDetailPopUpForm: CustomPopUpForm {
        private List<WorkstationDTO> _workstationDTOs;
        private List<IoBoxTask> _armTasks;
        private int _panelHeight;

        private List<CoordinatesPanel> armPanels = new();

        public ArmDetailPopUpForm(string categoryName, List<WorkstationDTO> workstationDTOs, List<IoBoxTask> armTasks, int panelHeight) {
            BorderColor = ColorConfigs.COLOR_POP_UP_BORDER;
            Title = "设备连接信息 - " + categoryName;
            _workstationDTOs = workstationDTOs;
            _armTasks = armTasks;
            _panelHeight = panelHeight;

            InitializeDisplay();
        }

        private void InitializeDisplay() {
            foreach (IoBoxTask task in _armTasks) {
                if (task.ArmType != null) {
                    int armId = task.ArmType.DeviceId;
                    WorkstationDTO? dto = _workstationDTOs.SingleOrDefault(dto => dto.arm_id == armId);
                    CoordinatesPanel panel = new(dto != null ? dto.name : "未配置站点", _panelHeight, ResetCoordinatesPositionX) {
                        Parent = ContentPanel,
                    };
                    ContentPanel.SizeChanged += (sender, eventArgs) => {
                        panel.Size = new(ContentPanel.Width - ContentPanel.Padding.Size.Width, _panelHeight);
                    };
                    armPanels.Add(panel);
                    // Bind delegate 
                    task.ArmType.ActionAfterCoordinatesReceived += panel.SetCoordinates;
                    // Remove delegate
                    panel.HandleDestroyed += (sender, eventArgs) => {
                        task.ArmType.ActionAfterCoordinatesReceived -= panel.SetCoordinates;
                    };
                }
            }
            void ResetCoordinatesPositionX() {
                int maxX = armPanels.Select(p => p.CoordinatesX).Max();
                foreach (CoordinatesPanel panel in armPanels) {
                    if (panel.CoordinatesX < maxX) {
                        panel.CoordinatesX = maxX;
                    }
                }
            }
        }

        private class CoordinatesPanel: CustomContentPanel {
            private int _panelHeight;
            private string _content;
            private int _Y = 0;
            private int _coordinatesX = 0;
            private Action _resetPositionX;

            public string XStr { get; set; }
            public string YStr { get; set; }
            public string ZStr { get; set; }
            public int CoordinatesX { get => _coordinatesX; set => _coordinatesX = value; }

            public CoordinatesPanel(string workstationName, int panelHeight, Action resetPositionX) {
                _content = $"站点：{workstationName}";
                _panelHeight = panelHeight;
                _resetPositionX = resetPositionX;

                XStr = "0";
                YStr = "0";
                ZStr = "0";
            }

            protected override void ResizeChildren(object? sender, EventArgs eventArgs) {
                base.ResizeChildren(sender, eventArgs);
                Font = new(WidgetsConfigs.SystemFontFamily, _panelHeight * .45F, FontStyle.Regular, GraphicsUnit.Pixel);
                _coordinatesX = (int) (TextRenderer.MeasureText(_content, Font).Width * 1.2);
                _Y = (_panelHeight - Font.Height) / 2;
                _resetPositionX();
            }

            protected override void OnPaint(PaintEventArgs e) {
                base.OnPaint(e);
                Graphics g = e.Graphics;

                string coordinates = $"坐标：  X-{XStr}    Y-{YStr}";
                if (ZStr != "0") {
                    coordinates += $"    Z-{ZStr}";
                }
                g.DrawString(_content, Font, new SolidBrush(ColorConfigs.COLOR_TEXT_BOX_FOREGROUND), new Point(0, _Y));
                g.DrawString(coordinates, Font, new SolidBrush(ColorConfigs.COLOR_TEXT_BOX_FOREGROUND), new Point(_coordinatesX, _Y));
            }

            public void SetCoordinates(int maxValue, Coordinates3D coordinates) {
                Task.Run(() => {
                    BeginInvoke(() => {
                        XStr = coordinates.X + "";
                        YStr = coordinates.Y + "";
                        ZStr = coordinates.Z + "";
                        Invalidate();
                    });
                });
            }
        }
    }
}

