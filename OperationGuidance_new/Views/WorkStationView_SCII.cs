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
                            if (WidgetUtils.ShowConfirmPopUp("迁移后原站点所在站点及该工站中的所有信息将被当前机器替换，确定要迁移？")) {
                                // 先查询选中站点对应的 mac 记录
                                MacAddressesDTO? macAddressesDTO = apis.FindMacAddressesById(new(_dataDTOList.Single(dto => dto.id == ids[0]).macs_id)).MacAddressesDTO;
                                if (macAddressesDTO != null) {
                                    // 根据选中的macid查询相关联的所有数据，改为当前正在登录的电脑的macid
                                    apis.UpdateMacsIds(new(macAddressesDTO.id, SystemUtils.MacAddressesDTO.id));
                                }

                                WidgetUtils.ShowNoticePopUp("操作成功", 2);
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
