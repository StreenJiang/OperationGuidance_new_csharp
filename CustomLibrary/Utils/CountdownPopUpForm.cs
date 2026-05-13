using System.Runtime.InteropServices;
using CustomLibrary.Configs;

namespace CustomLibrary.Utils;

[System.ComponentModel.DesignerCategory("Code")]
internal class CountdownPopUpForm : Form {
    private const string CountdownTextFormat = "将在 {0} 秒后自动关闭";

    private readonly System.Windows.Forms.Timer _timer;
    private readonly Label _countdownLabel;
    private int _countdown;

    private static Font? _cachedFont;
    private static readonly Dictionary<MessageBoxIcon, Bitmap> _iconCache = new();

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SHGetStockIconInfo(SHSTOCKICONID siid, SHGSI uFlags, ref SHSTOCKICONINFO psii);

    [DllImport("user32.dll")]
    private static extern bool DestroyIcon(IntPtr hIcon);

    private enum SHSTOCKICONID {
        SIID_INFO = 79,
        SIID_WARNING = 78,
        SIID_ERROR = 80,
    }

    [Flags]
    private enum SHGSI : uint {
        ICON = 0x100,
        SHELLICONSIZE = 0x4,
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct SHSTOCKICONINFO {
        public uint cbSize;
        public IntPtr hIcon;
        public int iSysImageIndex;
        public int iIcon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szPath;
    }

    private static Bitmap GetStockIcon(SHSTOCKICONID siid, out int iconSize) {
        var sii = new SHSTOCKICONINFO();
        sii.cbSize = (uint)Marshal.SizeOf(typeof(SHSTOCKICONINFO));
        int hr = SHGetStockIconInfo(siid, SHGSI.ICON | SHGSI.SHELLICONSIZE, ref sii);
        if (hr != 0 || sii.hIcon == IntPtr.Zero) {
            iconSize = 32;
            return SystemIcons.Information.ToBitmap();
        }
        using var icon = Icon.FromHandle(sii.hIcon);
        iconSize = icon.Width;
        var bmp = icon.ToBitmap();
        DestroyIcon(sii.hIcon);
        return bmp;
    }

    private static Bitmap GetCachedIcon(MessageBoxIcon icon) {
        if (!_iconCache.TryGetValue(icon, out var bitmap)) {
            var siid = icon switch {
                MessageBoxIcon.Error => SHSTOCKICONID.SIID_ERROR,
                MessageBoxIcon.Warning => SHSTOCKICONID.SIID_WARNING,
                _ => SHSTOCKICONID.SIID_INFO
            };
            bitmap = GetStockIcon(siid, out _);
            _iconCache[icon] = bitmap;
        }
        return bitmap;
    }

    public CountdownPopUpForm(string message, string title, MessageBoxIcon icon, int countdownSeconds) {
        _countdown = countdownSeconds;

        int formWidth = (int)(WidgetUtils.MainSize.Width * .32);
        int formHeight = (int)(WidgetUtils.MainSize.Height * .22);
        int hPadding = (int)(WidgetUtils.MainSize.Width * .018);
        int vPadding = (int)(WidgetUtils.MainSize.Height * .025);
        int hSpacing = (int)(WidgetUtils.MainSize.Width * .015);
        int iconPadding = Math.Max(hSpacing, 6);

        var iconImage = GetCachedIcon(icon);
        int iconSize = iconImage.Width;
        int iconColWidth = iconSize + iconPadding * 2;

        int btnHeight = WidgetUtils.PopUpOrFloatingFormCommonButtonHeight();

        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        Text = title;
        AutoSize = false;
        Size = new Size(formWidth, formHeight);

        var rootPanel = new Panel {
            Dock = DockStyle.Fill,
            Padding = new Padding(hPadding, vPadding, hPadding, vPadding),
        };

        _countdownLabel = new Label {
            Text = string.Format(CountdownTextFormat, _countdown),
            AutoSize = true,
            ForeColor = Color.Gray,
            TextAlign = ContentAlignment.MiddleLeft,
        };
        var okButton = new Button {
            Text = "确定",
            Height = btnHeight,
            Width = btnHeight * 5 / 2,
            DialogResult = DialogResult.OK,
        };
        CancelButton = okButton;
        var bottomPanel = new TableLayoutPanel {
            Height = btnHeight + 8,
            Dock = DockStyle.Bottom,
            ColumnCount = 3,
            Padding = new Padding(0),
        };
        bottomPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        bottomPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        bottomPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        bottomPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        bottomPanel.Controls.Add(_countdownLabel, 1, 0);
        bottomPanel.Controls.Add(okButton, 2, 0);
        _countdownLabel.Anchor = AnchorStyles.Right;
        _countdownLabel.Margin = new Padding(0, 0, iconPadding, 0);
        okButton.Anchor = AnchorStyles.Right;

        var contentPanel = new TableLayoutPanel {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Padding = new Padding(0),
        };
        contentPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, iconColWidth + iconPadding));
        contentPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        var iconPanel = new TableLayoutPanel {
            Width = iconColWidth,
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(0),
        };
        iconPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        iconPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        iconPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        var iconBox = new PictureBox {
            Image = iconImage,
            Size = new Size(iconSize, iconSize),
            SizeMode = PictureBoxSizeMode.CenterImage,
            Anchor = AnchorStyles.None,
        };
        iconPanel.Controls.Add(iconBox, 0, 1);
        contentPanel.Controls.Add(iconPanel, 0, 0);

        var messageLabel = new Label {
            Text = message,
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleLeft,
            Dock = DockStyle.Fill,
        };
        contentPanel.Controls.Add(messageLabel, 1, 0);

        rootPanel.Controls.Add(contentPanel);
        rootPanel.Controls.Add(bottomPanel);
        Controls.Add(rootPanel);

        _timer = new System.Windows.Forms.Timer { Interval = 1000 };
        _timer.Tick += OnTimerTick;
        _timer.Start();
    }

    private void OnTimerTick(object? sender, EventArgs e) {
        _countdown--;
        if (_countdown <= 0) {
            _timer.Stop();
            DialogResult = DialogResult.OK;
            Close();
            return;
        }
        _countdownLabel.Text = string.Format(CountdownTextFormat, _countdown);
    }

    protected override void OnHandleCreated(EventArgs e) {
        base.OnHandleCreated(e);
        if (WidgetsConfigs.SystemFontFamily != null) {
            if (_cachedFont == null) {
                _cachedFont = new Font(WidgetsConfigs.SystemFontFamily, 10f, FontStyle.Regular, GraphicsUnit.Point);
            }
            Font = _cachedFont;
        }
    }

    protected override void Dispose(bool disposing) {
        if (disposing) {
            _timer.Stop();
            _timer.Dispose();
        }
        base.Dispose(disposing);
    }
}
