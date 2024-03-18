using CustomLibrary.Buttons;
using CustomLibrary.Configs;
using CustomLibrary.Panels;
using CustomLibrary.Utils;
using OperationGuidance_service.Utils;

public class TitlePanel: Panel {
    private Label _title;
    private CustomContentPanel _buttonsPanel;
    private List<Control> _rightButtons;
    private Color? _underlineColor;
    private int _underlineThickness;

    public Color? UnderlineColor { get => _underlineColor; set => _underlineColor = value; }
    public List<Control> RightButtons { get => _rightButtons; set => _rightButtons = value; }

    public TitlePanel(string title) {
        Margin = new(0);
        _underlineColor = ColorConfigs.COLOR_TITLE_UNDERLINE;
        _title = new() {
            Text = title,
            Parent = this,
            BackColor = Color.Transparent,
        };
        _buttonsPanel = new() {
            Parent = this,
            Visible = false,
        };
        _rightButtons = new();
    }

    public T AddRightButton<T>(string? label = null) where T : Control, new() {
        T control = new() {
            Parent = _buttonsPanel,
        };
        if (label != null && control.GetType() == typeof(RightButton)) {
            CommonUtils.CannotBeNull(control as RightButton).Label = label;
        } else if (control.GetType() == typeof(ToggleButton)) {
            // ToggleButton toggleButton = CommonUtils.CannotBeNull(control as ToggleButton);
        }
        _rightButtons.Add(control);
        if (!_buttonsPanel.Visible) {
            _buttonsPanel.Visible = true;
        }
        ResizeChildren();
        return control;
    }

    public T GetRightButton<T>(int index) where T : Control {
        return (T) _rightButtons[index];
    }

    protected override void OnHandleCreated(EventArgs e) {
        base.OnHandleCreated(e);
        SizeChanged += ResizeChildren;
    }

    public void ResizeChildren() => ResizeChildren(this, EventArgs.Empty);
    private void ResizeChildren(object? sender, EventArgs eventArgs) {
        if (Width <= 0 || Height <= 0 || IsDisposed) {
            return;
        }
        // Resize title and right buttons
        using (Graphics g = CreateGraphics()) {
            // Resize title label
            _title.Height = (int) (Height * .7);
            _title.Font = new Font(WidgetsConfigs.SystemFontFamily, (int) (_title.Height * .65), FontStyle.Regular, GraphicsUnit.Pixel);
            int labelWidth = (int) (g.MeasureString(_title.Text, _title.Font).Width * 1.2);
            _title.Width = labelWidth;
            _title.Location = new(0, (int) ((Height - _title.Height) / 1.25));
            _underlineThickness = WidgetUtils.BorderThickness();

            // Resize and right buttons
            if (_rightButtons.Count > 0) {
                _buttonsPanel.Height = Height - _underlineThickness;
                int rightButtonHeight = (int) (Height * .65);
                int toggleButtonHeight = (int) (rightButtonHeight * .7);
                int buttonGap = rightButtonHeight / 3;
                int buttonsPanelWidth = 0;
                // Set height first to get new Font
                for (int i = 0; i < _rightButtons.Count; i++) {
                    Control control = _rightButtons[i];
                    Type controlType = control.GetType();
                    if (controlType == typeof(RightButton)) {
                        RightButton button = CommonUtils.CannotBeNull(control as RightButton);
                        // Height must be set first then ResizeTextLabel can be invoked, then the Font can be set
                        button.Height = rightButtonHeight;
                        // Calculate new width
                        int btnLabelWidth = (int) g.MeasureString(button.Label, button.Font).Width;
                        button.Width = (int) (btnLabelWidth + rightButtonHeight * 1.2);
                    } else if (controlType == typeof(ToggleButton)) {
                        ToggleButton button = CommonUtils.CannotBeNull(control as ToggleButton);
                        button.Size = new(toggleButtonHeight * 3, toggleButtonHeight);
                    }
                    // Add the width of all buttons to get the width of panel
                    buttonsPanelWidth += control.Width;
                    Padding controlMargin = new(0, (int) ((_buttonsPanel.Height - control.Height) / 1.5) - _underlineThickness, 0, 0);
                    if (i != 0) {
                        controlMargin.Left = buttonGap;
                        buttonsPanelWidth += buttonGap;
                    }
                    control.Margin = controlMargin;
                }
                _buttonsPanel.Width = buttonsPanelWidth;
                _buttonsPanel.Location = new(Width - buttonsPanelWidth, 0);
            }
        }
    }

    protected override void OnPaint(PaintEventArgs e) {
        base.OnPaint(e);
        if (_underlineColor != null) {
            e.Graphics.DrawLine(new(_underlineColor.Value, _underlineThickness), new(0, Height - 1), new(Width, Height - 1));
        }
    }

    public class RightButton: CommonButton {
        protected override void ResizeTextLabel() {
            if (Label != null) {
                Font = new Font(WidgetsConfigs.SystemFontFamily, (int) (Height * .55), FontStyle.Bold, GraphicsUnit.Pixel);
                using (Graphics g = CreateGraphics()) {
                    LabelX = (int) ((Width - g.MeasureString(Label, Font).Width) / 2 + Width * .02);
                }
                LabelY = (Height - Font.Height) / 2;
            }
        }
    }
}
