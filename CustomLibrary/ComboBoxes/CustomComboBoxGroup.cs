using CustomLibrary.Configs;
using System.ComponentModel;

namespace CustomLibrary.ComboBoxes {
    [DesignerCategory("Code")] // This makes it directly open the code window except design mode window
    public class CustomComboBoxGroup<T>: UserControl {
        private string _textName;
        private int _nameWidth;
        private HorizontalAlignment _nameAlignment;
        private int _gapNameAndBox;

        private double? _ratio;
        private FlowLayoutPanel _elementsPanel;
        private CustomComboBox<T> _comboBox;

        public new bool Enabled { get => _comboBox.Enabled; set => _comboBox.Enabled = value; }
        public bool NeedDefaultLabel { get => _comboBox.NeedDefaultLabel; set => _comboBox.NeedDefaultLabel = value; }
        public string TextName { get => this._textName; set => this._textName = value; }
        public int GapNameAndBox { get => _gapNameAndBox; set => _gapNameAndBox = value; }
        public double? Ratio { get => this._ratio; set => this._ratio = value; }
        public FlowLayoutPanel ElementsPanel { get => _elementsPanel; set => _elementsPanel = value; }
        protected CustomComboBox<T> ComboBox { get => _comboBox; set => _comboBox = value; }
        public new Color BackColor { get; private set; }
        public new Control Parent { 
            get => base.Parent; 
            set {
                base.Parent = value;
                BackColor = value.BackColor;
            } 
        }
        public Color BoxBackColor { 
            get => _comboBox.BackColor; 
            set => _comboBox.BackColor = value; 
        }
        public Color? DisabledBackColor { get => _comboBox.DisabledBackColor; set => _comboBox.DisabledBackColor = value; }
        public Color? BorderColor { get => _comboBox.BorderColor; set => _comboBox.BorderColor = value; }
        public Color? BorderColorError { get => _comboBox.BorderColorError; set => _comboBox.BorderColorError = value; }
        public int GapBetweenNameNBoxes { get => this._gapNameAndBox; set => this._gapNameAndBox = value; }
        public HorizontalAlignment NameAlignment {
            get => this._nameAlignment;
            set {
                if (value == HorizontalAlignment.Center) {
                    throw new InvalidEnumArgumentException("Can not use 'HorizontalAligment.Center' in this custom widget.");
                }
                this._nameAlignment = value;
            }
        }
        public bool ShowRealValue { get => _comboBox.ShowRealValue; set => _comboBox.ShowRealValue = value; }
        public bool IsError { get => _comboBox.IsError; }
        public event Action ItemSelected { add => _comboBox.ItemSelected += value; remove => _comboBox.ItemSelected -= value; }
        public List<T?> Items { get => _comboBox.Items; }
        public List<string> Names { get => _comboBox.Names; }
        public Dictionary<string, T?> NamesAndItems { get => _comboBox.NamesAndItems; }
        public string Key => _comboBox.Key;
        public T? Value => _comboBox.Value;

        public CustomComboBoxGroup(string textName) : base() {
            Margin = new(0);
            // Initialize fields
            _textName = textName;
            _nameWidth = 0;
            _nameAlignment = HorizontalAlignment.Left;
            _elementsPanel = new() {
                Parent = this,
                Margin = new(0),
            };
            // Initialize combo box
            _comboBox = new();
            _comboBox.Parent = _elementsPanel;
        }

        public int AddItem(string itemName, T? obj) => _comboBox.AddItem(itemName, obj);
        public void RemoveItem(int index) => _comboBox.RemoveItem(index);
        public CustomComboBox<T>.ComboBoxItem<T>? GetChosenItem() => _comboBox.GetChosenItem();
        public void SetCurrent(int index) => _comboBox.SetCurrent(index);
        public int IndexOf(T t) => _comboBox.IndexOf(t);
        public int GetCurrentIndex() => _comboBox.GetCurrentIndex();
        public bool IsDefaultValue() => _comboBox.IsDefaultValue();
        public void SetError(bool isError) => _comboBox.IsError = isError;
        public void CheckError(bool flag) => _comboBox.CheckError(flag);
        public void Reset() => _comboBox.Reset();
        public static string ResetName() => CustomComboBox<T>.ResetName();

        protected override void OnHandleCreated(EventArgs e) {
            base.OnHandleCreated(e);
            SizeChanged += ResizeChildren;
        }

        public void ResizeChildren() => ResizeChildren(this, EventArgs.Empty);
        private void ResizeChildren(object? sender, EventArgs eventArgs) {
            // Set Font
            Font = new Font(WidgetsConfigs.SystemFontFamily, (Height - Padding.Size.Height) * .55f, FontStyle.Regular, GraphicsUnit.Pixel);
            // Calculate gap between name and box
            _gapNameAndBox = Padding.Size.Width > 0 ? Padding.Size.Width / 2 : (int) (Height / 3.5);
            // Get width of name text
            using (Graphics g = CreateGraphics()) {
                _nameWidth = (int) g.MeasureString(_textName, Font).Width;
            }
            // Calculate width of combo box
            int boxWidth = GetBoxWidth();
            // Resize combo box first to get font of it
            _comboBox.Size = new(boxWidth, _elementsPanel.Height);
        }

        protected virtual int GetBoxWidth() {
            int boxWidth;
            if (_ratio != null) {
                boxWidth = (int) ((Width - Padding.Size.Width) * _ratio.Value / 10);
            } else {
                boxWidth = Width - _nameWidth - Padding.Size.Width - _gapNameAndBox;
            }
            _elementsPanel.Size = new(boxWidth, Height - Padding.Size.Height);
            _elementsPanel.Location = new(Width - boxWidth - Padding.Right, (Height - _elementsPanel.Height) / 2);
            return boxWidth;
        }

        protected override void OnPaint(PaintEventArgs e) {
            e.Graphics.Clear(this.BackColor);
            base.OnPaint(e);

            // Draw name
            int x = Padding.Left;
            if (_nameAlignment == HorizontalAlignment.Right) {
                x = _elementsPanel.Location.X - _nameWidth - _gapNameAndBox;
            }
            e.Graphics.DrawString(_textName, Font, new SolidBrush(ForeColor), new Point(x, (Height - Font.Height) / 2));
        }

        protected override void OnForeColorChanged(EventArgs e) {
            base.OnForeColorChanged(e);
            _comboBox.ForeColor = ForeColor;
        }

        protected override void OnParentBackColorChanged(EventArgs e) {
            base.OnParentBackColorChanged(e);
            BackColor = Parent.BackColor;
        }
    }
}
