using CustomLibrary.Configs;
using CustomLibrary.Forms;
using CustomLibrary.TextBoxes;
using CustomLibrary.Utils;
using log4net;
using OperationGuidance_new.Configs.DTOs;
using OperationGuidance_new.Constants;
using OperationGuidance_new.Tasks;
using OperationGuidance_new.Utils;
using OperationGuidance_new.Views.AbstractViews;

namespace OperationGuidance_new.Views.SubViews {
    public class ArrangerOpenLidScanPopUpForm: CustomPopUpForm {
        private ILog log = MainUtils.GetLogger(typeof(ArrangerOpenLidScanPopUpForm));

        private string _expectedBarcode;
        private int _position;
        private IoBoxTask _ioBoxTask;
        private AWorkplaceContentPanel _workplace;
        private ArrangerGroupDTO _group;
        private bool _success;

        private CustomTextBoxGroup _barcodeBox;

        public CustomTextBoxGroup BarcodeBox { get => _barcodeBox; }
        public bool Success { get => _success; }

        public ArrangerOpenLidScanPopUpForm(ArrangerGroupDTO group, IoBoxTask ioBoxTask, AWorkplaceContentPanel workplace) {
            _group = group;
            _expectedBarcode = group.barcode;
            _position = group.position;
            _ioBoxTask = ioBoxTask;
            _workplace = workplace;

            BorderColor = ColorConfigs.COLOR_POP_UP_BORDER;
            Title = $"开盖扫码 - {group.name}";

            _barcodeBox = new("条码") {
                Parent = ContentPanel,
            };
            _barcodeBox.GetTextBox(0).Box.KeyDown += (s, e) => {
                if (e.KeyCode == Keys.Enter) {
                    e.SuppressKeyPress = true;
                    ValidateAndProcess(_barcodeBox.GetTextBox(0).Box.Text?.Trim() ?? "");
                }
            };

            var confirmBtn = AddButton("确认");
            confirmBtn.Click += (s, e) => ValidateAndProcess(_barcodeBox.GetTextBox(0).Box.Text?.Trim() ?? "");

            AddButton("取消").Click += (s, e) => {
                _success = false;
                Close();
            };
        }

        protected override void OnShown(EventArgs e) {
            base.OnShown(e);
            _workplace.ActiveBarcodeInterceptor = OnBarcodeReceived;
            _barcodeBox.GetTextBox(0).Box.Focus();
        }

        private bool OnBarcodeReceived(string barcode) {
            if (IsDisposed)
                return false;

            BeginInvoke(() => {
                _barcodeBox.GetTextBox(0).Box.Text = barcode;
                ValidateAndProcess(barcode);
            });

            return true;
        }

        private void ValidateAndProcess(string barcode) {
            if (IsDisposed)
                return;

            if (string.IsNullOrEmpty(barcode))
                return;

            if (barcode != _expectedBarcode) {
                _barcodeBox.GetTextBox(0).Box.Text = "";
                _barcodeBox.GetTextBox(0).Box.Focus();
                _barcodeBox.GetTextBox(0).IsError = true;
                return;
            }

            _success = true;
            _barcodeBox.GetTextBox(0).Box.Enabled = false;
            _ = SendPulseAndClose();
        }

        private async Task SendPulseAndClose() {
            if (_position < IoBoxArranger.min || _position > IoBoxArranger.max) {
                log.Error($"Invalid position {_position} for arranger group '{_group.name}'");
                return;
            }

            var arrangerType = _ioBoxTask.ArrangerType;
            if (arrangerType == null) {
                log.Error("ArrangerType is null, cannot send pulse");
                return;
            }

            try {
                int?[] pos = { null, null, null, null, null, null, null, null };
                pos[_position - 1] = 1;
                await arrangerType.SendPulseAsync(pos, 200);
            } catch (Exception ex) {
                log.Error("Failed to send pulse for open-lid", ex);
                return;
            }

            await Task.Delay(500);
            if (!IsDisposed)
                BeginInvoke(() => Close());
        }

        protected override void OnFormClosed(FormClosedEventArgs e) {
            base.OnFormClosed(e);
            _workplace.ActiveBarcodeInterceptor = null;
        }
    }
}
