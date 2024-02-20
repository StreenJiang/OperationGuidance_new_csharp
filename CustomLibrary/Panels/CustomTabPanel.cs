using CustomLibrary.Constants;
using CustomLibrary.Panels.BaseClasses;

namespace CustomLibrary.Panels {
    public class CustomTabPanel: CustomContentPanelBase {
        private CustomMenuPanelBase? _menuPanel;
        private CustomContentPanelBase? _contentPanel;

        public CustomTabPanel() : base() { }

        protected override void OnControlAdded(ControlEventArgs e) {
            base.OnControlAdded(e);
            Control control = e.Control;
            if (control != null) {
                bool isSubClassOfMenuPanelBase = control is CustomMenuPanelBase;
                bool isSubClassOfContentPanelBase = control is CustomContentPanelBase;
                //bool isSubClassOfMenuPanelBase = WidgetUtils.IsSubClass<CustomMenuPanelBase>(control.GetType());
                //bool isSubClassOfContentPanelBase = WidgetUtils.IsSubClass<CustomContentPanelBase>(control.GetType());
                if (isSubClassOfMenuPanelBase || isSubClassOfContentPanelBase) {
                    if (_menuPanel == null && isSubClassOfMenuPanelBase) {
                        _menuPanel = (CustomMenuPanelBase) control;
                    } else if (_contentPanel == null && isSubClassOfContentPanelBase) {
                        _contentPanel = (CustomContentPanelBase) control;
                        _contentPanel.ControlAdded += (sender, eventArgs) => {
                            eventArgs.Control.VisibleChanged += (s, e) => {
                                if (eventArgs.Control.Visible) {
                                    eventArgs.Control.Size = _contentPanel.Size;
                                }
                            };
                        };
                    } else {
                        this.Controls.Remove(control);
                    }
                } else {
                    MessageBox.Show(
                        "Only <" + typeof(CustomMenuPanelBase) + "> and <" +
                        typeof(CustomContentPanelBase) + "> can be added in this panel. You are pass [" + control.GetType() + "].",
                        "warning", MessageBoxButtons.OK
                    );
                    this.Controls.Remove(control);
                }
            }
        }

        protected override void ResizeChildren(object? sender, EventArgs eventArgs) {
            if (!IsDisposed && _menuPanel != null && _contentPanel != null) {
                // Repositoin menu panel
                _menuPanel.ChangePosition();
                // Resize menu panel
                _menuPanel.ResizeSelf();
                // Resize content panel
                switch (_menuPanel.PanelDirection) {
                    case MenuPanelDirection.TOP:
                    case MenuPanelDirection.BOTTOM:
                        _contentPanel.Size = new Size(this.Width, this.Height - _menuPanel.Height);
                        break;
                    case MenuPanelDirection.LEFT:
                    case MenuPanelDirection.RIGHT:
                        _contentPanel.Size = new Size(this.Width - _menuPanel.Width, this.Height);
                        break;
                }
                if (_contentPanel.Controls.Count > 0) {
                    foreach (Control control in _contentPanel.Controls) {
                        if (!control.IsDisposed && control.Visible 
                                && control is CustomContentPanelBase && control is not CustomContentPanel) {
                            control.Size = _contentPanel.Size;
                        }
                    }
                }
            }
        }
    }
}
