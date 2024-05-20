using CustomLibrary.Panels;
using OperationGuidance_new.Views;
using OperationGuidance_new.Constants;
using System.Diagnostics;

namespace OperationGuidance_new.Configs {
    public static class SystemConfigs {
        // System menus
        public static List<MenuConfig> MenuCongfigs => _menuConfigs;

        private static readonly List<MenuConfig> _menuConfigs = new() {
            new() {
                Id = 100, 
                Name = "任务管理", 
                Icon = Properties.Resources.mission_management, 
                ViewTypes = new() {
                    {AppVersion.STANDARD, typeof(CustomTabPanel)},
                }, 
                Children = new() {
                    new() {
                        Id = 101,
                        Name = "任务列表",
                        Icon = Properties.Resources.mission_list,
                        ViewTypes = new() {
                            {AppVersion.STANDARD, typeof(MissionManagementView)},
                            {AppVersion.SCII, typeof(MissionManagementView_SCII)},
                        }, 
                    },
                    new() {
                        Id = 102,
                        Name = "任务编辑",
                        Icon = Properties.Resources.mission_edition,
                        ViewTypes = new() {
                            {AppVersion.STANDARD, typeof(MissionEditionView)},
                            {AppVersion.SCII, typeof(MissionEditionView_SCII)},
                        }, 
                    },
                },
            },
            new() {
                Id = 200, 
                Name = "工作台", 
                Icon = Properties.Resources.workplace, 
                ViewTypes = new() {
                    {AppVersion.STANDARD, typeof(WorkplaceMissionView)},
                    {AppVersion.SCII, typeof(WorkplaceMissionView_SCII)},
                    {AppVersion.YF, typeof(WorkplaceMissionView_YF)},
                }, 
                OpenFirst = true,
            },
            new() {
                Id = 300, 
                Name = "数据查询", 
                Icon = Properties.Resources.data_query, 
                ViewTypes = new() {
                    {AppVersion.STANDARD, typeof(DataQueryView)},
                    {AppVersion.SCII, typeof(DataQueryView_SCII)},
                }, 
            },
            new() {
                Id = 400, 
                Name = "事件日志", 
                Icon = Properties.Resources.event_log, 
                ViewTypes = new() {
                    {AppVersion.STANDARD, typeof(EventLogView)},
                }, 
            },
            new() {
                Id = 500, 
                Name = "参数配置", 
                Icon = Properties.Resources.variable_settings, 
                ViewTypes = new() {
                    {AppVersion.STANDARD, typeof(CustomTabPanel)},
                }, 
                Children = new() {
                    new() {
                        Id = 501,
                        Name = "账户管理",
                        Icon = Properties.Resources.user_info,
                        ViewTypes = new() {
                            {AppVersion.STANDARD, typeof(AccountManagementView)},
                        }, 
                    },
                    new() {
                        Id = 502,
                        Name = "站点配置",
                        Icon = Properties.Resources.workstation,
                        ViewTypes = new() {
                            {AppVersion.STANDARD, typeof(WorkStationView)},
                            {AppVersion.SCII, typeof(WorkStationView_SCII)},
                        }, 
                    },
                    new() {
                        Id = 503,
                        Name = "工具管理",
                        Icon = Properties.Resources.device_screw_gun,
                        ViewTypes = new() {
                            {AppVersion.STANDARD, typeof(DeviceToolView)},
                        }, 
                    },
                    new() {
                        Id = 504,
                        Name = "力臂管理",
                        Icon = Properties.Resources.device_arm,
                        ViewTypes = new() {
                            {AppVersion.STANDARD, typeof(DeviceArmView)},
                        }, 
                    },
                    new() {
                        Id = 505,
                        Name = "通讯设备管理",
                        Icon = Properties.Resources.device_communication,
                        ViewTypes = new() {
                            {AppVersion.STANDARD, typeof(DeviceCommunicationView)},
                        }, 
                    },
                    new() {
                        Id = 506,
                        Name = "串口设备管理",
                        Icon = Properties.Resources.device_serial_port,
                        ViewTypes = new() {
                            {AppVersion.STANDARD, typeof(DeviceSerialPortView)},
                        }, 
                    },
                    new() {
                        Id = 510,
                        Name = "IO设备管理",
                        Icon = Properties.Resources.device_io_box,
                        ViewTypes = new() {
                            {AppVersion.STANDARD, typeof(DeviceIoView)},
                        }, 
                    },
                    new() {
                        Id = 507,
                        Name = "条码匹配管理",
                        Icon = Properties.Resources.bar_code,
                        ViewTypes = new() {
                            {AppVersion.STANDARD, typeof(BarCodeMatchingRuleManagementView)},
                        }, 
                    },
                    new() {
                        Id = 508,
                        Name = "软件许可",
                        Icon = Properties.Resources.software_license,
                        ViewTypes = new() {
                            {AppVersion.STANDARD, typeof(SoftwareLicenseView)},
                        }, 
                    },
                    new() {
                        Id = 509,
                        Name = "系统设置",
                        Icon = Properties.Resources.variable_settings,
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
                ViewTypes = new() {
                    {AppVersion.STANDARD, typeof(CustomTabPanel)},
                }, 
                IsUserInfoPanel = true,
                Children = new() {
                    new() {
                        Id = 601,
                        Name = "用户个人信息",
                        Icon = Properties.Resources.user_info,
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
                        Process.GetCurrentProcess().Kill();
                    }
                }),
            },
        };
    }
}
