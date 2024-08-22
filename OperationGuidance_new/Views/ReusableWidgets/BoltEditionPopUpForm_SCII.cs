using CustomLibrary.Buttons;
using CustomLibrary.ComboBoxes;
using CustomLibrary.TextBoxes;
using CustomLibrary.Utils;
using OperationGuidance_new.Constants;
using OperationGuidance_service.Models.DTOs;
using OperationGuidance_service.Utils;

namespace OperationGuidance_new.Views.ReusableWidgets {
    public class BoltEditionPopUpForm_SCII: BoltEditionPopUpForm {
        private readonly int _partsBarCodesMax = 3;

        private List<BarCodeMatchingRuleDTO> _barCodeMatchingRuleDTOs;
        private Dictionary<int, string?> _partBarCodeInfos = new();

        private SubPanel<ProductBoltDTO> _partsBarCodesSubPanel;
        private ToggleButton _partsBarCodesToggle;
        private List<CustomComboBoxGroup<int>> _partsBarCodeIdBoxes;
        private List<CustomTextBoxButtonGroup> _partsBarCodeNameBoxes;

        public ToggleButton PartsBarCodesToggle { get => _partsBarCodesToggle; set => _partsBarCodesToggle = value; }
        public List<CustomComboBoxGroup<int>> PartsBarCodeIdBoxes { get => _partsBarCodeIdBoxes; set => _partsBarCodeIdBoxes = value; }
        public List<CustomTextBoxButtonGroup> PartsBarCodeNameBoxes { get => _partsBarCodeNameBoxes; set => _partsBarCodeNameBoxes = value; }

        public BoltEditionPopUpForm_SCII(ProductBoltDTO boltDTO, List<BarCodeMatchingRuleDTO> barCodeMatchingRuleDTOs) : base(boltDTO) {
            _barCodeMatchingRuleDTOs = barCodeMatchingRuleDTOs;
            foreach (BarCodeMatchingRuleDTO rule in barCodeMatchingRuleDTOs) {
                if (rule.type == BarCodeTypes.PARTS.Id) {
                    _partBarCodeInfos.Add(rule.id, rule.name);
                }
            }

            // Add a new sub panel
            _partsBarCodesSubPanel = AddSubPanel("物料条码");
            _partsBarCodesSubPanel.TablePanel.Hide();
            _partsBarCodesSubPanel.TablePanel.ColumnCount = _columnCount;
            _partsBarCodesToggle = _partsBarCodesSubPanel.TitlePanel.AddRightButton<ToggleButton>();

            // Add first combo box of bar code rule ID
            _partsBarCodeIdBoxes = new() {
                new("条码规则ID1") {
                    Parent = _partsBarCodesSubPanel.TablePanel,
                    Ratio = _boxRatio,
                    NameAlignment = HorizontalAlignment.Right,
                },
            };
            foreach (KeyValuePair<int, string?> pair in _partBarCodeInfos) {
                _partsBarCodeIdBoxes[0].AddItem(pair.Key + "", pair.Key);
            }
            // Add first text box of bar code rule name
            _partsBarCodeNameBoxes = new() {
                new("条码名称1") {
                    Parent = _partsBarCodesSubPanel.TablePanel,
                    Ratio = _boxRatio,
                    NameAlignment = HorizontalAlignment.Right,
                    Enabled = false,
                },
            };
            SignButton addButton = _partsBarCodeNameBoxes[0].AddButton<SignButton>();
            addButton.Enabled = true;
            addButton.Icon = Properties.Resources.sign_plus;
            addButton.Click += (s, e) => AddPartsBarCodeAndFlush();

            // Bind event that combines combo box and text box
            SetValueAfterSelecting(_partsBarCodeIdBoxes[0], _partsBarCodeNameBoxes[0]);

            // Toggle changed event
            _partsBarCodesToggle.CheckedChanged += (s, e) => {
                if (_partsBarCodesToggle.Checked) {
                    _partsBarCodesSubPanel.TablePanel.Show();
                    if (!string.IsNullOrEmpty(boltDTO.parts_bar_code_ids) && _partsBarCodeIdBoxes.Count == 1) {
                        List<int> partsBarCodeRuleIds = CommonUtils.StringToList(boltDTO.parts_bar_code_ids);
                        for (int i = 0; i < partsBarCodeRuleIds.Count; i++) {
                            int id = partsBarCodeRuleIds[i];

                            if (i > 0) {
                                AddPartsBarCodeAndFlush();
                            }

                            CustomComboBoxGroup<int> comboBox = _partsBarCodeIdBoxes[i];
                            comboBox.SetCurrent(comboBox.IndexOf(id));
                        }
                    }
                } else {
                    _partsBarCodesSubPanel.TablePanel.Hide();
                }
                for (int i = 0; i < _partsBarCodeIdBoxes.Count; i++) {
                    _partsBarCodeIdBoxes[i].ResizeChildren();
                    _partsBarCodeNameBoxes[i].ResizeChildren();
                }

                ResizeSelf();
            };
        }

        protected override void OnHandleCreated(EventArgs e) {
            base.OnHandleCreated(e);

            // Back fill according to data
            // Action asynchronously to avoid other controls to initialize
            BackFillAsync();
        }

        private async void BackFillAsync() {
            await Task.Run(() => {
                BeginInvoke(() => {
                    _partsBarCodesToggle.Checked = !string.IsNullOrEmpty(_modifiedBoltDTO.parts_bar_code_ids);
                });
            });
        }

        private void AddPartsBarCodeAndFlush() {
            AddPartsBarCode();
            ResizeSelf();
        }

        private void AddPartsBarCode() {
            int currentCount = _partsBarCodeNameBoxes.Count;
            if (currentCount >= _partsBarCodesMax) {
                WidgetUtils.ShowWarningPopUp($"物料条码每个点位最多支持配置{_partsBarCodesMax}个");
                return;
            }

            CustomComboBoxGroup<int> comboBox = new($"条码规则ID{currentCount + 1}") {
                Parent = _partsBarCodesSubPanel.TablePanel,
                Ratio = _boxRatio,
                NameAlignment = HorizontalAlignment.Right,
            };
            foreach (KeyValuePair<int, string?> pair in _partBarCodeInfos) {
                comboBox.AddItem(pair.Key + "", pair.Key);
            }

            CustomTextBoxButtonGroup box = new($"条码名称{currentCount + 1}") {
                Parent = _partsBarCodesSubPanel.TablePanel,
                Ratio = _boxRatio,
                NameAlignment = HorizontalAlignment.Right,
                Enabled = false,
            };
            SignButton minusButton = box.AddButton<SignButton>();
            minusButton.Enabled = true;
            minusButton.Icon = Properties.Resources.sign_minus;
            minusButton.Click += (s, e) => {
                _partsBarCodeIdBoxes.Remove(comboBox);
                _partsBarCodeNameBoxes.Remove(box);
                comboBox.Dispose();
                box.Dispose();
                for (int i = 0; i < _partsBarCodeNameBoxes.Count; i++) {
                    _partsBarCodeIdBoxes[i].TextName = $"条码规则ID{i + 1}";
                    _partsBarCodeIdBoxes[i].ResizeChildren();
                    _partsBarCodeNameBoxes[i].TextName = $"条码名称{i + 1}";
                    _partsBarCodeNameBoxes[i].ResizeChildren();
                }
                ResizeSelf();
                CheckAndSetIds();
            };

            SetValueAfterSelecting(comboBox, box);

            _partsBarCodeIdBoxes.Add(comboBox);
            _partsBarCodeNameBoxes.Add(box);
            for (int i = 0; i < _partsBarCodeNameBoxes.Count; i++) {
                _partsBarCodeIdBoxes[i].TextName = $"条码规则ID{i + 1}";
                _partsBarCodeNameBoxes[i].TextName = $"条码名称{i + 1}";
            }
        }

        private void SetValueAfterSelecting(CustomComboBoxGroup<int> comboBox, CustomTextBoxButtonGroup box) {
            comboBox.ItemSelected += () => {
                if (!comboBox.IsDefaultValue() && comboBox.Value > 0) {
                    if (!string.IsNullOrEmpty(_partBarCodeInfos[comboBox.Value])) {
                        box.SetValue(0, _partBarCodeInfos[comboBox.Value]);
                    } else {
                        box.SetValue(0, "-");
                    }
                } else {
                    box.SetValue(0, "");
                }
                CheckAndSetIds();
            };
        }

        private void CheckAndSetIds() {
            if (_partsBarCodesToggle.Checked) {
                List<int> ids = new();
                for (int i = 0; i < _partsBarCodeIdBoxes.Count; i++) {
                    CustomComboBoxGroup<int> combo = _partsBarCodeIdBoxes[i];
                    if (!combo.IsDefaultValue() && combo.Value > 0) {
                        if (ids.IndexOf(combo.Value) >= 0) {
                            break;
                        }
                        ids.Add(combo.Value);
                    }
                }
                _modifiedBoltDTO.parts_bar_code_ids = CommonUtils.ListToString(ids);
            } else {
                _modifiedBoltDTO.parts_bar_code_ids = null;
            }
        }
    }
}
