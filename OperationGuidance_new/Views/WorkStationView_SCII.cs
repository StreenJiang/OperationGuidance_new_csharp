using CustomLibrary.Buttons;
using CustomLibrary.Utils;
using OperationGuidance_service.Constants;
using OperationGuidance_service.Models.DTOs;
using OperationGuidance_service.Utils;
namespace OperationGuidance_new.Views {
    public class WorkStationView_SCII: WorkStationView {
        private CommonButton? detailBtn;

        public override void VisibleToTrue() {
            base.VisibleToTrue();

            Roles? role = SystemUtils.GetRoleNameByUserId(SystemUtils.LoggedUserId);
            if (role != null && role == Roles.DEVELOPER) {
                if (detailBtn == null) {
                    // 添加站点迁移按钮
                    detailBtn = DataGridView.AddExtraButton("站点迁移");
                    detailBtn.Click += (s, e) => {
                        List<int> ids = DataGridView.GetSelectedIds();
                        if (ids.Count <= 0) {
                            WidgetUtils.ShowNoticePopUp("请选择要迁移的站点");
                        } else if (ids.Count > 1) {
                            WidgetUtils.ShowNoticePopUp("每次操作只能选择一个站点");
                        } else {
                            if (WidgetUtils.ShowConfirmPopUp("迁移后原站点所在及其所有信息将被当前机器替换，确定要迁移？")) {
                                // 先查询选中站点对应的 mac 记录
                                MacAddressesDTO? macAddressesDTO = apis.FindMacAddressesById(new(_dataDTOList.Single(dto => dto.id == ids[0]).macs_id)).MacAddressesDTO;
                                if (macAddressesDTO != null) {
                                    // 删除当前 mac 记录 mac 记录
                                    SystemUtils.MacAddressesDTO.deleted = (int) YesOrNo.YES;
                                    apis.AddOrUpdateMacAddresses(new(SystemUtils.MacAddressesDTO));

                                    // 将当前 mac 信息存入查到的站带你对应的 mac 记录中
                                    macAddressesDTO.macs = SystemUtils.MacAddressesDTO.macs;
                                    SystemUtils.MacAddressesDTO = CommonUtils.CannotBeNull(apis.AddOrUpdateMacAddresses(new(macAddressesDTO)).MacAddressesDTO);
                                }

                                WidgetUtils.ShowNoticePopUp("操作成功");
                            }
                        }
                    };
                    Width -= 1;
                    Width += 1;
                } else if (!detailBtn.Visible) {
                    detailBtn.Show();
                    Width -= 1;
                    Width += 1;
                }
            } else {
                if (detailBtn != null && detailBtn.Visible) {
                    detailBtn.Hide();
                    Width -= 1;
                    Width += 1;
                }
            }

        }
    }
}
