using System.Reflection;
using CustomLibrary.Configs;
using CustomLibrary.Panels;
using CustomLibrary.Panels.BaseClasses;
using OperationGuidance_new.Configs;
using OperationGuidance_new.Constants;
using OperationGuidance_new.Utils;

namespace OperationGuidance_new.Views {
    public class OperatorView: Panel {
        #region Constructors
        public OperatorView() {
            // 根据 ID = 200 拿到工作台的配置参数
            Dictionary<Constants.AppVersion, Type>? viewTypes = SystemConfigs.MenuCongfigs.Find(m => m.Id == 200)?.ViewTypes;
            if (viewTypes != null && viewTypes.Count > 0) {
                Type type;
                // 根据配置决定显示哪个版本的工作台
                AppVersion appVersion = (AppVersion) Enum.Parse(typeof(AppVersion), MainUtils.License.AppVersion);
                if (viewTypes.ContainsKey(appVersion)) {
                    type = viewTypes[appVersion];
                } else {
                    type = viewTypes[AppVersion.STANDARD];
                }
                if (type.FullName != null) {
                    // 利用反射机制创建实例
                    object? instance = type.Assembly.CreateInstance(type.FullName, false, BindingFlags.Default, null, new object[] { true }, null, null);
                    if (instance != null && instance is CustomContentPanel workplaceView) {
                        CustomVScrollingContentPanel outerScrollingPanel = new(ColorConfigs.COLOR_CONTENT_PANEL_INNER_BORDER, workplaceView);
                    }
                }
            }
        }
        #endregion
    }
}
