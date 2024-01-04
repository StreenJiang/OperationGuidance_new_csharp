using CustomLibrary.Buttons;
using CustomLibrary.Configs;
using CustomLibrary.Constants;
using CustomLibrary.Panels;
using CustomLibrary.TextBoxes;
using CustomLibrary.Utils;
using OperationGuidance_new.Configs;

namespace OperationGuidance_new.Views {
    public class VariableSettingsView: CustomContentPanel {
        private readonly float _titleHeightRatio = 0.059F;
        private readonly float _contentHGapRatio = 0.025F;
        private readonly float _contentVGapRatio = 0.05F;
        private readonly float _contentHPaddingRatio = 0.15F;
        private readonly float _contentVPaddingRatio = 0.03F;
        private int _titleHeight;
        private int _contentHGap;
        private int _contentVGap;
        private int _contentHPadding;
        private int _contentVPadding;

        // Resolution options content panel
        private CustomContentPanel _resolutionPanel;
        private TitlePanel _resolutionTitlePanel;
        private CustomContentPanel _resolutionContentPanel;
        private CustomComboBoxGroup<KeyValuePair<Size, SizeRatioNRectColor>> _resolutionOptionsBox;
        private CommonButton _resolutionConfirmButton;

        public VariableSettingsView() {
            FlowDirection = FlowDirection.TopDown;
            InitializeResolutionPanel();
        }

        protected override void ResizeChildren(object? sender, EventArgs eventArgs) {
            Control mainParent = WidgetUtils.MainPanel.Parent;
            _titleHeight = (int) (mainParent.Height * _titleHeightRatio);
            _contentHGap = (int) (mainParent.Height * _contentHGapRatio);
            _contentVGap = (int) (mainParent.Height * _contentVGapRatio);
            _contentHPadding = (int) (mainParent.Width * .015);
            _contentVPadding = (int) (mainParent.Height * .03);
            ResizeResolutionPanel();
        }

        private void InitializeResolutionPanel() {
            _resolutionPanel = new() {
                Parent = this,
                FlowDirection = FlowDirection.TopDown,
            };
            _resolutionTitlePanel = new("分辨率") {
                Parent = _resolutionPanel,
                UnderlineColor = ConfigsVariables.COLOR_TITLE_UNDERLINE,
            };
            _resolutionContentPanel = new() {
                Parent = _resolutionPanel,
            };
            _resolutionOptionsBox = new("分辨率") {
                Parent = _resolutionContentPanel,
                BorderColor = ConfigsVariables.COLOR_TEXT_BOX_BORDER,
                ForeColor = ConfigsVariables.COLOR_TEXT_BOX_FOREGROUND,
                BoxBackColor = ConfigsVariables.COLOR_TEXT_BOX_BACKGROUND,
                BorderColorError = ConfigsVariables.COLOR_TEXT_BOX_BORDER_ERROR,
                NameAlignment = HorizontalAlignment.Right,
            };
            Dictionary<Size, SizeRatioNRectColor>.Enumerator enumerator = WidgetsConfigs.Resolutions.GetEnumerator();
            Size screenSize = WidgetUtils.GetScreenResolution();
            while (enumerator.MoveNext()) {
                KeyValuePair<Size, SizeRatioNRectColor> current = enumerator.Current;
                if (current.Key.Width > screenSize.Width || current.Key.Height > screenSize.Height) {
                    continue;
                }
                string itemName = $"{current.Key.Width} x {current.Key.Height}（{current.Value.WidthRatio} x {current.Value.HeightRatio}）";
                if (current.Key.Width == screenSize.Width && current.Key.Height == screenSize.Height) {
                    itemName += "（全屏）";
                }
                _resolutionOptionsBox.AddItem(itemName, current);
            }
            _resolutionConfirmButton = new() {
                Parent = _resolutionContentPanel,
                Label = "应用",
            };
            _resolutionConfirmButton.Click += (sender, eventArgs) => {
                KeyValuePair<Size, SizeRatioNRectColor> value = _resolutionOptionsBox.Value;
                if (value.Key == new Size(0, 0)) {
                    // If user select the defualt item, then set IsError = true
                    _resolutionOptionsBox.SetError(true);
                } else {
                    // Resize main form according to chosen resolution
                    Form mainParent = (Form) WidgetUtils.MainPanel.Parent;
                    Size newSize = value.Key;
                    if (newSize != mainParent.Size) {
                        if (_resolutionOptionsBox.IsError) {
                            _resolutionOptionsBox.SetError(false);
                        }
                        if (newSize == screenSize) {
                            mainParent.WindowState = FormWindowState.Maximized;
                        } else {
                            mainParent.WindowState = FormWindowState.Normal;
                            mainParent.Size = newSize;
                            mainParent.ClientSize = newSize;
                            mainParent.Location = new((screenSize.Width - newSize.Width) / 2, (screenSize.Height - newSize.Height) / 2);
                        }
                    }
                }
            };
        }

        private void ResizeResolutionPanel() {
            // Resize title
            _resolutionTitlePanel.Size = new(Width, _titleHeight);
            // Resize Content
            int boxHeight = WidgetUtils.TextOrComboBoxHeight();
            int contentHeight = boxHeight + _contentVPadding * 2;
            _resolutionContentPanel.Size = new(Width, contentHeight);
            _resolutionContentPanel.Padding = new(_contentHPadding, _contentVPadding, _contentHPadding, _contentVPadding);
            // Resize box and button
            _resolutionOptionsBox.Size = new((int) (Width - _resolutionContentPanel.Padding.Size.Width - _contentHGap) / 2, boxHeight);
            _resolutionOptionsBox.Margin = new(0, 0, _contentHGap / 2, 0);
            _resolutionConfirmButton.Height = WidgetUtils.CommonButtonHeight();
            int labelWidth = TextRenderer.MeasureText(_resolutionConfirmButton.Label, _resolutionConfirmButton.Font).Width;
            _resolutionConfirmButton.Width = (int) (labelWidth + _resolutionConfirmButton.Height * 1.2);
            _resolutionConfirmButton.Margin = new(_contentHGap / 2, 0, 0, 0);
            // Resize outer panel
            _resolutionPanel.Size = new(Width, _resolutionTitlePanel.Height + _resolutionContentPanel.Height);
        }

        public override void VisibleToTrue() {
            base.VisibleToTrue();
            // Reset current resolution
            List<KeyValuePair<Size, SizeRatioNRectColor>> items = _resolutionOptionsBox.Items;
            Control mainParent = WidgetUtils.MainPanel.Parent;
            for (int i = 0; i < items.Count; i++) {
                KeyValuePair<Size, SizeRatioNRectColor> item = items[i];
                if (item.Key == mainParent.Size) {
                    _resolutionOptionsBox.SetDefault(i);
                }
            }
        }

        public override bool CheckNeedsScrollBar(int parentNewHeight) {
            return false;
        }
    }
}
