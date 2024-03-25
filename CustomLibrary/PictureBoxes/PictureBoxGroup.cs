using CustomLibrary.Configs;
using CustomLibrary.Utils;
using System.ComponentModel;

namespace CustomLibrary.Buttons {
    [DesignerCategory("Code")] // This makes it directly open the code window except design mode window
    public class PictureBoxGroup: UserControl {
        private bool _enabled;
        private string _textName;
        private int _nameWidth;
        private HorizontalAlignment _nameAlignment;
        private int _gapNameAndButton;
        private int _buttonHeight;
        private int _vGap;
        private int _pictureBoxHeightBase;
        private int _picturePanelHeight;
        private int _buttonX;

        private Color _foreColorExpectButton;
        private double? _ratio;
        private CommonButton _selectButton;
        private string _fileName;
        private Image _image;
        private bool _showingDefault;
        private Panel _picturePanel;
        private Action _iamgeChanged;

        public new bool Enabled { get => _selectButton.Enabled; set => _selectButton.Enabled = value; }
        public new Size Size { get => base.Size; }
        public new int Width { get => base.Width; }
        public new int Height { get => base.Height; }
        public int PictureBoxHeightBase { get => _pictureBoxHeightBase; set => _pictureBoxHeightBase = value; }
        public string TextName { get => this._textName; set => this._textName = value; }
        public string FileName { 
            get => _fileName; 
            set {
                _fileName = value; 
                Invalidate();
            }
        }
        public Image Image { 
            get => _image; 
            set {
                _image = value; 
                _showingDefault = false;
                Invalidate();
                _iamgeChanged();
            }
        }
        public double? Ratio { get => this._ratio; set => this._ratio = value; }
        public Color ForeColorExpectButton { get => _foreColorExpectButton; set => _foreColorExpectButton = value; }
        public new Color BackColor { get; private set; }
        public new Control Parent { 
            get => base.Parent; 
            set {
                base.Parent = value;
                BackColor = value.BackColor;
                _picturePanel.BackColor = WidgetUtils.DarkenColor(BackColor, .1);
            } 
        }
        public Color ButtonBackColor { 
            get => _selectButton.BackColor; 
            set => _selectButton.BackColor = value; 
        }
        public int GapBetweenNameNBoxes { get => this._gapNameAndButton; set => this._gapNameAndButton = value; }
        public HorizontalAlignment NameAlignment {
            get => this._nameAlignment;
            set {
                if (value == HorizontalAlignment.Center) {
                    throw new InvalidEnumArgumentException("Can not use 'HorizontalAligment.Center' in this custom widget.");
                }
                this._nameAlignment = value;
            }
        }
        public event Action ImageChanged{ add => _iamgeChanged += value; remove => _iamgeChanged -= value; }

        public PictureBoxGroup(string textName) : base() {
            Margin = new(0);
            // Initialize fields
            _textName = textName;
            _nameWidth = 0;
            _nameAlignment = HorizontalAlignment.Left;
            // Initialize select button
            _selectButton = new();
            _selectButton.Label = "选择图片...";
            _selectButton.Parent = this;
            _selectButton.Click += (sender, eventArgs) => {
                SelectImage();
            };
            // Initialize picture box
            _picturePanel = new() {
                Parent = this,
                Margin = new(0),
            };
            _picturePanel.Paint += PicturePanelPaint;
            _image = Resources.CustomResources.image_default;
            _showingDefault = true;
        }

        private void SelectImage() {
            OpenFileDialog dialog = new() {
                Title = "请选择设备图标",
                Filter = "Picture file|*.jpg;*.jpeg;*.png",
            };
            if (dialog.ShowDialog() == DialogResult.OK) {
                string filePath = dialog.FileName;
                _fileName = dialog.SafeFileName;
                Image = Image.FromFile(filePath);
            }
        }

        public void SetSize(int width, int buttonHeight, int pictureBoxHeightBase, float pictureSizeRatio, int widthAfterRatio = 0) {
            _buttonHeight = buttonHeight;
            _pictureBoxHeightBase = pictureBoxHeightBase;
            _vGap = _buttonHeight / 3;
            _picturePanelHeight = (int) (_pictureBoxHeightBase * pictureSizeRatio);
            // Set Font
            Font = new Font(WidgetsConfigs.SystemFontFamily, (_buttonHeight - Padding.Size.Height) * .55f, FontStyle.Regular, GraphicsUnit.Pixel);
            // Calculate gap between name and box
            _gapNameAndButton = Padding.Size.Width > 0 ? Padding.Size.Width / 2 : (int) (_buttonHeight / 3.5);
            // Get width of name text
            using (Graphics g = CreateGraphics()) {
                _nameWidth = (int) g.MeasureString(_textName, Font).Width;
            }
            // Calculate width of combo box
            if (_ratio != null) {
                _buttonX = width - (int) ((width - Padding.Size.Width) * _ratio.Value / 10);
            } else {
                _buttonX = _nameWidth + Padding.Size.Width + _gapNameAndButton;
            }
            base.Size = new(width + widthAfterRatio, _buttonHeight + _vGap + _picturePanelHeight);
        }

        protected override void OnHandleCreated(EventArgs e) {
            base.OnHandleCreated(e);
            SizeChanged += ResizeChildren;
        }

        public void ResizeChildren() => ResizeChildren(this, EventArgs.Empty);
        private void ResizeChildren(object? sender, EventArgs eventArgs) {
            // Resize select button first to get font of it
            int buttonH = _buttonHeight - Padding.Size.Height;
            _selectButton.Size = new(buttonH * 4, buttonH);
            // Relocate select button (height plus extra 1 makes better display)
            _selectButton.Location = new(_buttonX, (_buttonHeight - _selectButton.Height) / 2 + 1);
            // Relocate
            _picturePanel.Location = new(_buttonX, _buttonHeight + _vGap);
            // Resize picture box
            _picturePanel.Size = new(Width - Padding.Size.Width - _buttonX, _picturePanelHeight);
        }

        protected override void OnPaint(PaintEventArgs e) {
            e.Graphics.Clear(this.BackColor);
            base.OnPaint(e);

            // Draw name
            int x = Padding.Left;
            if (_nameAlignment == HorizontalAlignment.Right) {
                x = _selectButton.Location.X - _nameWidth - _gapNameAndButton;
            }
            e.Graphics.DrawString(_textName, Font, new SolidBrush(_foreColorExpectButton), new Point(x, (_buttonHeight - Font.Height) / 2));
            int fileNameX = _selectButton.Location.X + _selectButton.Width + _gapNameAndButton;
            e.Graphics.DrawString(_fileName, Font, new SolidBrush(_foreColorExpectButton), new Point(fileNameX, (_buttonHeight - Font.Height) / 2));
        }

        private void PicturePanelPaint(object? sender, PaintEventArgs eventArgs) {
            Graphics g = eventArgs.Graphics;
            g.Clear(_picturePanel.BackColor);
            
            Image imageShowing;
            if (_showingDefault) {
                int side = _picturePanelHeight / 3;
                imageShowing = WidgetUtils.ResizeImage(_image, side, side);
            } else {
                float widthRatio = _picturePanel.Width / (float) _image.Width;
                float heightRatio = _picturePanelHeight / (float) _image.Height;
                imageShowing = WidgetUtils.ResizeImageByZoomingRatio(_image, Math.Min(widthRatio, heightRatio));
            }
            g.DrawImage(imageShowing, new Point((_picturePanel.Width - imageShowing.Width) / 2, (_picturePanelHeight - imageShowing.Height) / 2));
        }

        protected override void OnForeColorChanged(EventArgs e) {
            base.OnForeColorChanged(e);
            _selectButton.ForeColor = ForeColor;
        }

        protected override void OnParentBackColorChanged(EventArgs e) {
            base.OnParentBackColorChanged(e);
            BackColor = Parent.BackColor;
            _picturePanel.BackColor = WidgetUtils.DarkenColor(BackColor, .1);
        }
    }
}
