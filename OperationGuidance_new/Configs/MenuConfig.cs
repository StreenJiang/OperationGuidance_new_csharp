namespace OperationGuidance_new.Configs {
    public class MenuConfig {
        private int _id;
        private string _name;
        private bool _enabled = false;
        private Image _icon;
        private bool _isToggleButton = true;
        private Type _viewType;
        private bool _isUserInfoPanel = false;
        private List<MenuConfig> _children;
        private EventHandler? _click;

        public int Id { get => _id; set => _id = value; }
        public string Name { get => _name; set => _name = value; }
        public bool Enabled { get => _enabled; set => _enabled = value; }
        public Image Icon { get => _icon; set => _icon = value; }
        public bool IsToggleButton { get => _isToggleButton; set => _isToggleButton = value; }
        public Type ViewType { get => _viewType; set => _viewType = value; }
        public bool IsUserInfoPanel { get => _isUserInfoPanel; set => _isUserInfoPanel = value; }
        public List<MenuConfig> Children { get => _children; set => _children = value; }
        public EventHandler? Click { get => _click; set => _click = value; }

        public event EventHandler OnClick { add => _click += value; remove => _click -= value; }

    }
}
