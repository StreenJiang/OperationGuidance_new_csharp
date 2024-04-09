using CustomLibrary.Panels;
using OperationGuidance_new.Views;
using OperationGuidance_new.Constants;

namespace OperationGuidance_new.Configs {
    public static class SystemConfigs {
        // System menus
        public static List<MenuConfig> MenuCongfigs => _menuConfigs;

        private static readonly List<MenuConfig> _menuConfigs = new() {
            new() {
                Id = 100, 
                Name = "任务管理", 
                Icon = Properties.Resources.mission_management, 
                ViewType = typeof(CustomTabPanel), 
                ViewTypes = new() {
                    {AppVersion.STANDARD, typeof(CustomTabPanel)},
                }, 
                Children = new() {
                    new() {
                        Id = 101,
                        Name = "任务列表",
                        Icon = Properties.Resources.mission_list,
                        ViewType = typeof(MissionManagementView),
                        ViewTypes = new() {
                            {AppVersion.STANDARD, typeof(MissionManagementView)},
                        }, 
                    },
                    new() {
                        Id = 102,
                        Name = "任务编辑",
                        Icon = Properties.Resources.mission_edition,
                        ViewType = typeof(MissionEditionView),
                        ViewTypes = new() {
                            {AppVersion.STANDARD, typeof(MissionEditionView)},
                        }, 
                    },
                },
            },
            new() {
                Id = 200, 
                Name = "工作台", 
                Icon = Properties.Resources.workplace, 
                ViewType = typeof(WorkplaceMissionView), 
                ViewTypes = new() {
                    {AppVersion.STANDARD, typeof(WorkplaceMissionView)},
                }, 
                OpenFirst = true,
            },
            new() {
                Id = 300, 
                Name = "数据查询", 
                Icon = Properties.Resources.data_query, 
                ViewType = typeof(DataQueryView), 
                ViewTypes = new() {
                    {AppVersion.STANDARD, typeof(DataQueryView)},
                    {AppVersion.SCII, typeof(DataQueryView_SCII)},
                }, 
            },
            new() {
                Id = 400, 
                Name = "事件日志", 
                Icon = Properties.Resources.event_log, 
                ViewType = typeof(EventLogView), 
                ViewTypes = new() {
                    {AppVersion.STANDARD, typeof(EventLogView)},
                }, 
            },
            new() {
                Id = 500, 
                Name = "参数配置", 
                Icon = Properties.Resources.variable_settings, 
                ViewType = typeof(CustomTabPanel), 
                ViewTypes = new() {
                    {AppVersion.STANDARD, typeof(CustomTabPanel)},
                }, 
                Children = new() {
                    new() {
                        Id = 501,
                        Name = "账户管理",
                        Icon = Properties.Resources.user_info,
                        ViewType = typeof(AccountManagementView),
                        ViewTypes = new() {
                            {AppVersion.STANDARD, typeof(AccountManagementView)},
                        }, 
                    },
                    new() {
                        Id = 502,
                        Name = "站点配置",
                        Icon = Properties.Resources.workstation,
                        ViewType = typeof(WorkStationView),
                        ViewTypes = new() {
                            {AppVersion.STANDARD, typeof(WorkStationView)},
                        }, 
                    },
                    new() {
                        Id = 503,
                        Name = "工具管理",
                        Icon = Properties.Resources.device_screw_gun,
                        ViewType = typeof(DeviceToolView),
                        ViewTypes = new() {
                            {AppVersion.STANDARD, typeof(DeviceToolView)},
                        }, 
                    },
                    new() {
                        Id = 504,
                        Name = "力臂管理",
                        Icon = Properties.Resources.device_arm,
                        ViewType = typeof(DeviceArmView),
                        ViewTypes = new() {
                            {AppVersion.STANDARD, typeof(DeviceArmView)},
                        }, 
                    },
                    new() {
                        Id = 505,
                        Name = "通讯设备管理",
                        Icon = Properties.Resources.device_communication,
                        ViewType = typeof(DeviceCommunicationView),
                        ViewTypes = new() {
                            {AppVersion.STANDARD, typeof(DeviceCommunicationView)},
                        }, 
                    },
                    new() {
                        Id = 506,
                        Name = "串口设备管理",
                        Icon = Properties.Resources.device_serial_port,
                        ViewType = typeof(DeviceSerialPortView),
                        ViewTypes = new() {
                            {AppVersion.STANDARD, typeof(DeviceSerialPortView)},
                        }, 
                    },
                    new() {
                        Id = 507,
                        Name = "条码匹配管理",
                        Icon = Properties.Resources.bar_code,
                        ViewType = typeof(BarCodeMatchingRuleManagementView),
                        ViewTypes = new() {
                            {AppVersion.STANDARD, typeof(BarCodeMatchingRuleManagementView)},
                        }, 
                    },
                    new() {
                        Id = 508,
                        Name = "软件许可",
                        Icon = Properties.Resources.software_license,
                        ViewType = typeof(SoftwareLicenseView),
                        ViewTypes = new() {
                            {AppVersion.STANDARD, typeof(SoftwareLicenseView)},
                        }, 
                    },
                    new() {
                        Id = 509,
                        Name = "系统设置",
                        Icon = Properties.Resources.variable_settings,
                        ViewType = typeof(VariableSettingsView),
                        ViewTypes = new() {
                            {AppVersion.STANDARD, typeof(VariableSettingsView)},
                        }, 
                    },
                },
            },
            new() {
                Id = 600, 
                Name = "用户信息", 
                Icon = Properties.Resources.user_info, 
                ViewType = typeof(CustomTabPanel), 
                ViewTypes = new() {
                    {AppVersion.STANDARD, typeof(CustomTabPanel)},
                }, 
                IsUserInfoPanel = true,
                Children = new() {
                    new() {
                        Id = 601,
                        Name = "用户个人信息",
                        Icon = Properties.Resources.user_info,
                        ViewType = typeof(UserInfoView),
                        ViewTypes = new() {
                            {AppVersion.STANDARD, typeof(UserInfoView)},
                        }, 
                    },
                    new() {
                        Id = 602,
                        Name = "退出登录",
                        Icon = Properties.Resources.user_logout,
                        IsToggleButton = false,
                        Click = new((sender, eventArgs) => {
                            // DialogResult result = MessageBox.Show(null, "确定要退出登录吗？", "退出登录", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                        }),
                    },
                },
            },
            new() {
                Id = 700, 
                Name = "退出", 
                Icon = Properties.Resources.exit, 
                IsToggleButton = false,
                Click = new((sender, eventArgs) => {
                    DialogResult result = MessageBox.Show(null, "确定要退出吗？", "退出程序", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (result == DialogResult.Yes) {
                        Application.Exit();
                    }
                }),
            },
        };
    }
}
