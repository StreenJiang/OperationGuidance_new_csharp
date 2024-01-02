using CustomLibrary.Buttons;
using CustomLibrary.Configs;
using CustomLibrary.Panels;
using OperationGuidance_new.Configs;

public class TitlePanel: Panel {
    private Label _title;
    private CustomContentPanel _buttonsPanel;
    private List<RightButton> _rightButtons;
    private Color? _underlineColor;

    public Color? UnderlineColor { get => _underlineColor; set => _underlineColor = value; }

    public TitlePanel(string title) {
        UnderlineColor = ConfigsVariables.COLOR_TITLE_UNDERLINE;
        Margin = new(0);
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

    public RightButton AddRightButton(string label) {
        RightButton button = new() {
            Parent = _buttonsPanel,
            Label = label,
        };
        _rightButtons.Add(button);
        if (!_buttonsPanel.Visible) {
            _buttonsPanel.Visible = true;
        }
        ResizeChildren();
        return button;
    }

    public RightButton GetRightButton(int index) {
        return _rightButtons[index];
    }

    protected override void OnHandleCreated(EventArgs e) {
        base.OnHandleCreated(e);
        SizeChanged += ResizeChildren;
    }

    public void ResizeChildren() => ResizeChildren(this, EventArgs.Empty);
    private void ResizeChildren(object? sender, EventArgs eventArgs) {
        if (Width <= 0 || Height <= 0) {
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

            // Resize and right buttons
            if (_rightButtons.Count > 0) {
                int buttonHeight = (int) (Height * .65);
                _buttonsPanel.Height = buttonHeight;
                int buttonGap = buttonHeight / 3;
                int buttonsPanelWidth = 0;
                // Set height first to get new Font
                for (int i = 0; i < _rightButtons.Count; i++) {
                    RightButton button = _rightButtons[i];
                    // Height must be set first then ResizeTextLabel can be invoked, then the Font can be set
                    button.Height = buttonHeight;
                    // Calculate new width
                    int btnLabelWidth = (int) g.MeasureString(button.Label, button.Font).Width;
                    button.Width = (int) (btnLabelWidth + buttonHeight * 1.2);
                    buttonsPanelWidth += button.Width;
                    if (i != 0) {
                        button.Margin = new(buttonGap, 0, 0, 0);
                        buttonsPanelWidth += buttonGap;
                    }
                }
                _buttonsPanel.Width = buttonsPanelWidth;
                _buttonsPanel.Location = new(Width - buttonsPanelWidth, (Height - buttonHeight) / 2);
            }
        }
    }

    protected override void OnPaint(PaintEventArgs e) {
        base.OnPaint(e);
        if (_underlineColor != null) {
            int penBorder = (int) Math.Ceiling((double)((Parent.Width + Parent.Height) / 400D));
            e.Graphics.DrawLine(new(_underlineColor.Value, penBorder), new(0, Height), new(Width, Height));
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
