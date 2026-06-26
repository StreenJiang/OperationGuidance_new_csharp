using OperationGuidance_new.Views.AbstractViews;

namespace OperationGuidance_new.Views {
    public class VariableSettingsView: AVariableSettingsView {
        public VariableSettingsView() {
            // 导出相关 — 标准版全部可见（Excel + TXT）
            EnableExcelExportToggle.Ratio = 6.95F;
            EnableTxtExportToggle.Ratio = 6.95F;
            StoragePanel.Show();
            EnableExcelExportToggle.Show();
            EnableTxtExportToggle.Show();
            StoragePathTextBox.Show();
            StorageFieldsButton.Show();
            ExportTestButton.Show();

            // 将 TXT 开关移到 Excel 开关后面（同行各半宽），只做一次
            var controls = StorageContentPanel.Controls;
            int excelIdx = controls.IndexOf(EnableExcelExportToggle);
            int txtIdx = controls.IndexOf(EnableTxtExportToggle);
            controls.SetChildIndex(EnableTxtExportToggle, excelIdx + 1);

            // 导出测试按钮与对应开关联动
            EnableExcelExportToggle.CheckedChanged += (s, e) => {
                ExportTestButton.GetButton(0).Visible = EnableExcelExportToggle.Checked;
            };
            EnableTxtExportToggle.CheckedChanged += (s, e) => {
                ExportTestButton.GetButton(1).Visible = EnableTxtExportToggle.Checked;
            };

            // 初始状态：测试按钮隐藏（开关默认 OFF）
            ExportTestButton.GetButton(0).Hide();
            ExportTestButton.GetButton(1).Hide();
        }

        protected override void ResizeStoragePanel() {
            base.ResizeStoragePanel();

            // Excel + TXT 同行各半宽（覆盖 base 的全宽布局）
            int halfBoxWidth = (Width - ContentHPadding * 3) / 2;
            EnableExcelExportToggle.Size = new(halfBoxWidth, BoxNBtnHeight);
            EnableExcelExportToggle.Margin = new(0, 0, ContentHGap / 2, 0);
            EnableTxtExportToggle.Size = new(halfBoxWidth, BoxNBtnHeight);
            EnableTxtExportToggle.Margin = new(0, 0, 0, 0);

            // TXT 从第 5 行移到第 1 行 → 4 行布局，调整高度
            int boxVMargin = BoxNBtnHeight / 2;
            int contentHeight = BoxNBtnHeight * 4 + ContentVPadding * 2 + boxVMargin * 3;
            StorageContentPanel.Size = new(Width, contentHeight);
            StoragePanel.Size = new(Width, StorageTitlePanel.Height + StorageContentPanel.Height);
        }
    }
}
