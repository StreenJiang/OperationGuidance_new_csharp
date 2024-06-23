using OperationGuidance_new.Constants;

namespace OperationGuidance_new.Configs {
    public class MenuConfig {
        private int _id;
        private string _name;
        private bool _enabled = false;
        private Image _icon;
        private bool _isToggleButton = true;
        private Dictionary<AppVersion, Type>? _viewTypes = null;
        private bool _isUserInfoPanel = false;
        private List<MenuConfig>? _children = null;
        private EventHandler? _click;
        private bool _openFirst = false;

        public int Id { get => _id; set => _id = value; }
        public string Name { get => _name; set => _name = value; }
        public bool Enabled { get => _enabled; set => _enabled = value; }
        public Image Icon { get => _icon; set => _icon = value; }
        public bool IsToggleButton { get => _isToggleButton; set => _isToggleButton = value; }
        public Dictionary<AppVersion, Type>? ViewTypes { get => _viewTypes; set => _viewTypes = value; }
        public bool IsUserInfoPanel { get => _isUserInfoPanel; set => _isUserInfoPanel = value; }
        public List<MenuConfig>? Children { get => _children; set => _children = value; }
        public EventHandler? Click { get => _click; set => _click = value; }
        public bool OpenFirst { get => _openFirst; set => _openFirst = value; }

        public event EventHandler OnClick { add => _click += value; remove => _click -= value; }

        public MenuConfig(int id, string name, Image icon) {
            _id = id;
            _name = name;
            _icon = icon;
        }
    }
}
