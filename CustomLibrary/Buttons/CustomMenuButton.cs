using CustomLibrary.Buttons.BaseClasses;
using CustomLibrary.Panels.BaseClasses;

namespace CustomLibrary.Buttons
{
    public class CustomMenuButton: CustomImageTextButtonBase {
        private EventHandler? _onMenuButtonClick;
        private CustomContentPanelBase? _correspondingContentPanel;

        public event EventHandler? OnMenuButtonClick {
            add {
                if (_onMenuButtonClick == null)
                    _onMenuButtonClick = value;
                else
                    Events.AddHandler(_onMenuButtonClick, value);
            }
            remove {
                if (_onMenuButtonClick != null)
                    Events.RemoveHandler(_onMenuButtonClick, value);
            }
        }
        public CustomContentPanelBase? CorrespondingContentPanel {
            get => _correspondingContentPanel;
            set => _correspondingContentPanel = value;
        }

        public CustomMenuButton() {
            this.Click += (sender, eventArgs) => {
                if (_onMenuButtonClick != null) {
                    _onMenuButtonClick(sender, eventArgs);
                } else if (!this.ToggledButton) {
                    throw new NullReferenceException("A non-toggle menu button must have a OnMenuButtonClick method.");
                }
            };
        }

        public void ShowContentPanel(bool flag = true) {
            if (_correspondingContentPanel != null) {
                _correspondingContentPanel.Visible = flag;
            }
        }

        public void HideContentPanle() {
            ShowContentPanel(false);
        }

        public void TriggerClick(EventArgs eventArgs) {
            OnClick(eventArgs);
        }
    }
}
