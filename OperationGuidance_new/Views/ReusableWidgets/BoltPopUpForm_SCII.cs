using CustomLibrary.ComboBoxes;
using CustomLibrary.TextBoxes;
using OperationGuidance_new.Constants;
using OperationGuidance_service.Models.DTOs;
using OperationGuidance_service.Utils;

namespace OperationGuidance_new.Views.ReusableWidgets {
    public class BoltPopUpForm_SCII: BoltPopUpForm {
        private List<BarCodeMatchingRuleDTO> _barCodeMatchingRuleDTOs;
        private Dictionary<int, string?> _partBarCodeInfos = new();

        private List<CustomComboBoxGroup<int>> _partsBarCodeIdBoxes;
        private List<CustomTextBoxButtonGroup> _partsBarCodeNameBoxes;

        public List<CustomComboBoxGroup<int>> PartsBarCodeIdBoxes { get => _partsBarCodeIdBoxes; set => _partsBarCodeIdBoxes = value; }
        public List<CustomTextBoxButtonGroup> PartsBarCodeNameBoxes { get => _partsBarCodeNameBoxes; set => _partsBarCodeNameBoxes = value; }

        public BoltPopUpForm_SCII(ProductBoltDTO boltDTO, List<BarCodeMatchingRuleDTO> barCodeMatchingRuleDTOs) : base(boltDTO) {
            _barCodeMatchingRuleDTOs = barCodeMatchingRuleDTOs;
            foreach (BarCodeMatchingRuleDTO rule in barCodeMatchingRuleDTOs) {
                if (rule.type == BarCodeTypes.PARTS.Id) {
                    _partBarCodeInfos.Add(rule.id, rule.name);
                }
            }

            _partsBarCodeIdBoxes = new();
            _partsBarCodeNameBoxes = new();
        }

        private CustomComboBoxGroup<int> AddPartsBarCode() {
            int currentCount = _partsBarCodeNameBoxes.Count;

            CustomComboBoxGroup<int> comboBox = new($"条码规则ID{currentCount + 1}") {
                Parent = _tablePanel,
                Ratio = _boxRatio,
                NameAlignment = HorizontalAlignment.Right,
                Enabled = false,
            };
            foreach (KeyValuePair<int, string?> pair in _partBarCodeInfos) {
                comboBox.AddItem(pair.Key + "", pair.Key);
            }

            CustomTextBoxButtonGroup box = new($"条码名称{currentCount + 1}") {
                Parent = _tablePanel,
                Ratio = _boxRatio,
                NameAlignment = HorizontalAlignment.Right,
                Enabled = false,
            };

            SetValueAfterSelecting(comboBox, box);

            _partsBarCodeIdBoxes.Add(comboBox);
            _partsBarCodeNameBoxes.Add(box);

            return comboBox;
        }

        protected override async void OnHandleCreated(EventArgs e) {
            base.OnHandleCreated(e);

            await Task.Run(() => {
                BeginInvoke(() => {
                    if (!string.IsNullOrEmpty(_originalBoltDTO.parts_bar_code_ids)) {
                        List<int> partsBarCodeRuleIds = CommonUtils.StringToList(_originalBoltDTO.parts_bar_code_ids);
                        foreach (int id in partsBarCodeRuleIds) {
                            CustomComboBoxGroup<int> comboBox = AddPartsBarCode();
                            comboBox.SetCurrent(comboBox.IndexOf(id));
                        }
                    }

                    ResizeSelf();
                });
            });
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
            };
        }
    }
}
