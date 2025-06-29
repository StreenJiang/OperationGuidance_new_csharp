using CustomLibrary.Panels;
using OperationGuidance_new.Views;
using OperationGuidance_new.Constants;
using System.Diagnostics;

namespace OperationGuidance_new.Configs {
    public static class SystemConfigs {
        // System menus
        public static List<MenuConfig> MenuCongfigs => _menuConfigs;

        private static readonly List<MenuConfig> _menuConfigs = new() {
            new(id: 100, name: "任务管理", icon: Properties.Resources.mission_management) {
                ViewTypes = new() {
                    {AppVersion.STANDARD, typeof(CustomTabPanel)},
                },
                Children = new() {
                    new(id: 101, name: "任务列表", icon: Properties.Resources.mission_list) {
                        ViewTypes = new() {
                            {AppVersion.STANDARD, typeof(MissionManagementView)},
                            {AppVersion.SCII, typeof(MissionManagementView_SCII)},
                        },
                    },
                    new(id: 102, name: "任务编辑", icon: Properties.Resources.mission_edition) {
                        ViewTypes = new() {
                            {AppVersion.STANDARD, typeof(MissionEditionView)},
                            {AppVersion.SCII, typeof(MissionEditionView_SCII)},
                        },
                    },
                },
            },
            new(id: 200, name: "工作台", icon: Properties.Resources.workplace) {
                ViewTypes = new() {
                    {AppVersion.STANDARD, typeof(WorkplaceMissionView)},
                    {AppVersion.SCII, typeof(WorkplaceMissionView_SCII)},
                    {AppVersion.YF, typeof(WorkplaceMissionView_YF)},
                    {AppVersion.GLB, typeof(WorkplaceMissionView_GLB)},
                    {AppVersion.WHYC, typeof(WorkplaceMissionView_WHYC)},
                    {AppVersion.TZYX, typeof(WorkplaceMissionView_TZYX)},
                },
                OpenFirst = true,
            },
            new(id: 300, name: "数据查询", icon: Properties.Resources.data_query) {
                ViewTypes = new() {
                    {AppVersion.STANDARD, typeof(DataQueryView)},
                    {AppVersion.SCII, typeof(DataQueryView_SCII)},
                },
            },
            new(id: 400, name: "事件日志", icon: Properties.Resources.event_log) {
                ViewTypes = new() {
                    {AppVersion.STANDARD, typeof(EventLogView)},
                },
            },
            new(id: 500, name: "参数配置", icon: Properties.Resources.variable_settings) {
                ViewTypes = new() {
                    {AppVersion.STANDARD, typeof(CustomTabPanel)},
                },
                Children = new() {
                    new(id: 501, name: "账户管理", icon: Properties.Resources.user_info) {
                        ViewTypes = new() {
                            {AppVersion.STANDARD, typeof(AccountManagementView)},
                        },
                    },
                    new(id: 502, name: "站点配置", icon: Properties.Resources.workstation) {
                        ViewTypes = new() {
                            {AppVersion.STANDARD, typeof(WorkStationView)},
                            {AppVersion.SCII, typeof(WorkStationView_SCII)},
                        },
                    },
                    new(id: 503, name: "工具管理", icon: Properties.Resources.device_screw_gun) {
                        ViewTypes = new() {
                            {AppVersion.STANDARD, typeof(DeviceToolView)},
                        },
                    },
                    new(id: 504, name: "力臂管理", icon: Properties.Resources.device_arm) {
                        ViewTypes = new() {
                            {AppVersion.STANDARD, typeof(DeviceArmView)},
                        },
                    },
                    new(id: 505, name: "通讯设备管理", icon: Properties.Resources.device_communication) {
                        ViewTypes = new() {
                            {AppVersion.STANDARD, typeof(DeviceCommunicationView)},
                        },
                    },
                    new(id: 506, name: "串口设备管理", icon: Properties.Resources.device_serial_port) {
                        ViewTypes = new() {
                            {AppVersion.STANDARD, typeof(DeviceSerialPortView)},
                        },
                    },
                    new(id: 510, name: "IO设备管理", icon: Properties.Resources.device_io_box) {
                        ViewTypes = new() {
                            {AppVersion.STANDARD, typeof(DeviceIoView)},
                        },
                    },
                    new(id: 511, name: "外部数据库管理", icon: Properties.Resources.database) {
                        ViewTypes = new() {
                            {AppVersion.GLB, typeof(OuterDatabaseConfigGlbView)},
                        },
                    },
                    new(id: 507, name: "条码匹配管理", icon: Properties.Resources.bar_code) {
                        ViewTypes = new() {
                            {AppVersion.STANDARD, typeof(BarCodeMatchingRuleManagementView)},
                            {AppVersion.SCII, typeof(BarCodeMatchingRuleManagementView_SCII)},
                        },
                    },
                    new(id: 512, name: "MatCode管理", icon: Properties.Resources.map_table) {
                        ViewTypes = new() {
                            {AppVersion.WHYC, typeof(MatCodeMapWhycView_WHYC)},
                        },
                    },
                    new(id: 508, name: "软件许可", icon: Properties.Resources.software_license) {
                        ViewTypes = new() {
                            {AppVersion.STANDARD, typeof(SoftwareLicenseView)},
                        },
                    },
                    new(id: 509, name: "系统设置", icon: Properties.Resources.variable_settings) {
                        ViewTypes = new() {
                            {AppVersion.STANDARD, typeof(VariableSettingsView)},
                            {AppVersion.SCII, typeof(VariableSettingsView_SCII)},
                            {AppVersion.GLB, typeof(VariableSettingsView_GLB)},
                            {AppVersion.WHYC, typeof(VariableSettingsView_WHYC)},
                            {AppVersion.TZYX, typeof(VariableSettingsView_TZYX)},
                        },
                    },
                },
            },
            new(id: 600, name: "用户信息", icon: Properties.Resources.user_info) {
                ViewTypes = new() {
                    {AppVersion.STANDARD, typeof(CustomTabPanel)},
                },
                IsUserInfoPanel = true,
                Children = new() {
                    new(id: 601, name: "用户个人信息", icon: Properties.Resources.user_info) {
                        ViewTypes = new() {
                            {AppVersion.STANDARD, typeof(UserInfoView)},
                        },
                    },
                    new(id: 602, name: "退出登录", icon: Properties.Resources.user_logout) {
                        IsToggleButton = false,
                        Click = new((sender, eventArgs) => {
                            // DialogResult result = MessageBox.Show(null, "确定要退出登录吗？", "退出登录", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                        }),
                    },
                },
            },
            new(id: 700, name: "退出", icon: Properties.Resources.exit) {
                IsToggleButton = false,
                Click = new((sender, eventArgs) => {
                    DialogResult result = MessageBox.Show(null, "确定要退出吗？", "退出程序", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (result == DialogResult.Yes) {
                        Process.GetCurrentProcess().Kill();
                    }
                }),
            },
        };
    }
}
