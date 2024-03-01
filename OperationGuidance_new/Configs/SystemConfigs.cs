using CustomLibrary.Panels;
using CustomLibrary.Utils;
using OperationGuidance_new.Views;

namespace OperationGuidance_new.Configs {
    public static class SystemConfigs {
        // System menus
        public static List<Dictionary<string, object?>> MenuCongfigs => _menusConfig;

        private static readonly List<Dictionary<string, object?>> _menusConfig = new() {
            new() {
                {Key_ID, 100},
                {Key_Enabled, true},
                {Key_Name, "任务管理"},
                {Key_Icon, Properties.Resources.mission_management},
                {Key_Toggle_Button, true},
                {Key_Click, null},
                {Key_View_Name, typeof(CustomTabPanel)},
                {Key_Is_User_Info_panel, false},
                {
                    Key_Children, new List<Dictionary<string, object?>>(){
                        new() {
                            {Key_ID, 101},
                            {Key_Name, "任务列表"},
                            {Key_Icon, Properties.Resources.mission_list},
                            {Key_Toggle_Button, true},
                            {Key_Click, null},
                            {Key_View_Name, typeof(MissionManagementView)},
                        },
                        new() {
                            {Key_ID, 102},
                            {Key_Name, "任务编辑"},
                            {Key_Icon, Properties.Resources.mission_edition},
                            {Key_Toggle_Button, true},
                            {Key_Click, null},
                            {Key_View_Name, typeof(MissionEditionView)},
                        },
                    }
                },
            },
            new() {
                {Key_ID, 200},
                {Key_Enabled, true},
                {Key_Name, "工作台"},
                {Key_Icon, Properties.Resources.workplace},
                {Key_Toggle_Button, true},
                {Key_Click, null},
                {Key_View_Name, typeof(WorkplaceMissionView)},
                {Key_Is_User_Info_panel, false},
                {Key_Children, null},
            },
            new() {
                {Key_ID, 300},
                {Key_Enabled, true},
                {Key_Name, "数据查询"},
                {Key_Icon, Properties.Resources.data_query},
                {Key_Toggle_Button, true},
                {Key_Click, null},
                {Key_View_Name, typeof(DataQueryView)},
                {Key_Is_User_Info_panel, false},
                {Key_Children, null},
            },
            new() {
                {Key_ID, 400},
                {Key_Enabled, true},
                {Key_Name, "事件日志"},
                {Key_Toggle_Button, true},
                {Key_Icon, Properties.Resources.event_log},
                {Key_Click, null},
                {Key_View_Name, typeof(EventLogView)},
                {Key_Is_User_Info_panel, false},
                {Key_Children, null},
            },
            new() {
                {Key_ID, 500},
                {Key_Enabled, true},
                {Key_Name, "参数配置"},
                {Key_Icon, Properties.Resources.variable_settings},
                {Key_Toggle_Button, true},
                {Key_Click, null},
                {Key_View_Name, typeof(CustomTabPanel)},
                {Key_Is_User_Info_panel, false},
                {
                    Key_Children, new List<Dictionary<string, object?>>() {
                        new() {
                            {Key_ID, 501},
                            {Key_Name, "账户管理"},
                            {Key_Icon, Properties.Resources.user_info},
                            {Key_Toggle_Button, true},
                            {Key_Click, null},
                            {Key_View_Name, typeof(AccountManagementView)},
                        },
                        new() {
                            {Key_ID, 502},
                            {Key_Name, "站点配置"},
                            {Key_Icon, Properties.Resources.workstation},
                            {Key_Toggle_Button, true},
                            {Key_Click, null},
                            {Key_View_Name, typeof(WorkStationView)},
                        },
                        new() {
                            {Key_ID, 503},
                            {Key_Name, "工具管理"},
                            {Key_Icon, Properties.Resources.device_screw_gun},
                            {Key_Toggle_Button, true},
                            {Key_Click, null},
                            {Key_View_Name, typeof(DeviceToolView)},
                        },
                        new() {
                            {Key_ID, 504},
                            {Key_Name, "力臂管理"},
                            {Key_Icon, Properties.Resources.device_arm},
                            {Key_Toggle_Button, true},
                            {Key_Click, null},
                            {Key_View_Name, typeof(DeviceArmView)},
                        },
                        new() {
                            {Key_ID, 505},
                            {Key_Name, "通讯设备管理"},
                            {Key_Icon, Properties.Resources.device_communication},
                            {Key_Toggle_Button, true},
                            {Key_Click, null},
                            {Key_View_Name, typeof(DeviceCommunicationView)},
                        },
                        new() {
                            {Key_ID, 506},
                            {Key_Name, "串口设备管理"},
                            {Key_Icon, Properties.Resources.device_serial_port},
                            {Key_Toggle_Button, true},
                            {Key_Click, null},
                            {Key_View_Name, typeof(DeviceSerialPortView)},
                        },
                        // new() {
                        //     {Key_ID, 507},
                        //     {Key_Name, "开发者选项"},
                        //     {Key_Icon, Properties.Resources.developer_choices},
                        //     {Key_Toggle_Button, true},
                        //     {Key_Click, null},
                        //     {Key_View_Name, typeof(DeveloperChoicesView)},
                        // },
                        new() {
                            {Key_ID, 508},
                            {Key_Name, "软件许可"},
                            {Key_Icon, Properties.Resources.software_license},
                            {Key_Toggle_Button, true},
                            {Key_Click, null},
                            {Key_View_Name, typeof(SoftwareLicenseView)},
                        },
                        new() {
                            {Key_ID, 509},
                            {Key_Name, "系统设置"},
                            {Key_Icon, Properties.Resources.variable_settings},
                            {Key_Toggle_Button, true},
                            {Key_Click, null},
                            {Key_View_Name, typeof(VariableSettingsView)},
                        },
                    }
                },
            },
            new() {
                {Key_ID, 600},
                {Key_Enabled, true},
                {Key_Name, "用户信息"},
                {Key_Icon, Properties.Resources.user_info},
                {Key_Toggle_Button, true},
                {Key_Click, null},
                {Key_View_Name, typeof(CustomTabPanel)},
                {Key_Is_User_Info_panel, true},
                {
                    Key_Children, new List<Dictionary<string, object?>>(){
                        new() {
                            {Key_ID, 601},
                            {Key_Name, "用户个人信息"},
                            {Key_Icon, Properties.Resources.mission_list},
                            {Key_Toggle_Button, true},
                            {Key_Click, null},
                            {Key_View_Name, typeof(UserInfoView)},
                        },
                        new() {
                            {Key_ID, 602},
                            {Key_Name, "退出登录"},
                            {Key_Icon, Properties.Resources.mission_edition},
                            {Key_Toggle_Button, false},
                            {
                                Key_Click, new EventHandler(
                                    (sender, EventArgs) => {
                                        DialogResult result = MessageBox.Show(null, "确定要退出登录吗？", "退出登录", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                                    }
                                )
                            },
                            {Key_View_Name, null},
                        },
                    }
                },
            },
            new() {
                {Key_ID, 700},
                {Key_Enabled, true},
                {Key_Name, "退出"},
                {Key_Icon, Properties.Resources.exit},
                {Key_Toggle_Button, false},
                {
                    Key_Click, new EventHandler(
                        (sender, EventArgs) => {
                            DialogResult result = MessageBox.Show(null, "确定要退出吗？", "退出程序", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                            if (result == DialogResult.Yes) {
                                Application.Exit();
                            }
                        }
                    )
                },
                {Key_View_Name, null},
                {Key_Children, null},
            },
        };

        public const string Key_ID = "id";
        public const string Key_Enabled = "enabled";
        public const string Key_Name = "name";
        public const string Key_Icon = "icon";
        public const string Key_Click = "click";
        public const string Key_View_Name = "view_name";
        public const string Key_Children = "children";
        public const string Key_Toggle_Button = "toggle_button";
        public const string Key_Is_User_Info_panel = "is_user_info_panel";
    }
}
