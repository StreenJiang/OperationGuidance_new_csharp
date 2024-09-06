using CustomLibrary.Buttons.BaseClasses;
using CustomLibrary.Panels.BaseClasses;
using CustomLibrary.Utils;

namespace CustomLibrary.Buttons {
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

        protected override void OnClick(EventArgs e) {
            if (WidgetUtils.CheckSaved || WidgetUtils.ShowConfirmPopUp("当前界面存在未保存数据，确定离开当前界面？")) {
                WidgetUtils.CheckSaved = true;
                base.OnClick(e);
                if (_correspondingContentPanel != null) {
                    if (_correspondingContentPanel is CustomVScrollingContentPanel scrollPanel) {
                        WidgetUtils.CurrentPanel = scrollPanel.ContentPanel;
                    } else {
                        WidgetUtils.CurrentPanel = _correspondingContentPanel;
                    }
                }
                if (_onMenuButtonClick != null) {
                    _onMenuButtonClick(this, e);
                } else if (!this.ToggledButton) {
                    throw new NullReferenceException("A non-toggle menu button must have a OnMenuButtonClick method.");
                }
            }
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
