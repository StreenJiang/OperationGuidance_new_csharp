using CustomLibrary.Buttons;
using CustomLibrary.Buttons.AbstractClasses;
using CustomLibrary.Constants;
using CustomLibrary.Panels;
using CustomLibrary.Panels.BaseClasses;
using CustomLibrary.Utils;
using Gma.System.MouseKeyHook;
using OperationGuidance_service.Utils;
using OperationGuidance_new.Configs;
using OperationGuidance_new.Utils;
using CustomLibrary.Events;
using CustomLibrary.Configs;
using OperationGuidance_new.Tasks;
using System.Net.NetworkInformation;
using System.ComponentModel;
using OperationGuidance_new.Views;
using OperationGuidance_service.Constants;
using OperationGuidance_service.Models.DTOs;
using System.Diagnostics;
using OperationGuidance_new.Constants;

namespace OperationGuidance_new {
    partial class MainForm {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        // Most important panels here
        private CustomTabPanel mainPanel;
        private CustomMainMenuPanel mainMenuPanel;
        private CustomContentPanelBase mainContentPanel;
        private IKeyboardMouseEvents HookEvents;
        private BackgroundWorker backgroundWorker;
        public bool AllCreated { get; set; } = false;
        private OperatorView? _operatorView = null;

        //[System.Runtime.InteropServices.DllImport("user32")]
        //protected static extern bool AnimateWindow(IntPtr hWnd, int dwTime, int dwFlags);

        #region Windows Form manually initialization code

        /// <summary>
        /// Write to whom that take over my job to continue all this shity code:
        /// This motherfucker leader X
        /// </summary>
        private void InitializeComponentManually() {
            String thisprocessname = Process.GetCurrentProcess().ProcessName;
            MainUtils.Self = this;
            // AllocConsole();

            // Set icon
            Icon = Properties.Resources.ico;

            // 执行一次这个开关检查，如果不存在就会默认插入一次
            SystemUtils.GetDBInitEnabled();
            // 先连接一下数据库，看看数据库是否正常
            if (!MainUtils.CheckDBConnection()) {
                WidgetUtils.ShowErrorPopUp("数据库连接失败，请检查数据库配置或网络连接状态");
                Close();
                return;
            }

            // 获取mac地址）
            List<NetworkInterface> networkInterfaces = NetworkInterface.GetAllNetworkInterfaces().ToList();
            MainUtils.Macs = networkInterfaces.Select(ni => ni.GetPhysicalAddress().ToString()).Where(mac => !string.IsNullOrEmpty(mac)).ToList();

            // Check license
            MainUtils.CheckLicense();

            // 检查当前设备是否已存在于物理地址表，用于隔离物理机器
            SystemUtils.MacAddressesDTO = SystemUtils.GetApis().FindMacAddressesByMacs(new(MainUtils.Macs)).MacAddressesDTO;
            if (SystemUtils.MacAddressesDTO == null) {
                MacAddressesDTO? macAddressesDTOTemp = SystemUtils.GetApis().AddOrUpdateMacAddresses(new(new() { macs = string.Join(",", MainUtils.Macs) })).MacAddressesDTO;
                if (macAddressesDTOTemp == null) {
                    throw new Exception("物理地址存储至数据库失败");
                }
                SystemUtils.MacAddressesDTO = macAddressesDTOTemp;
            }

            // MainForm
            // 设置最大化的范围
            MaximizedBounds = Screen.FromHandle(this.Handle).WorkingArea;
            WidgetUtils.MainForm = this;
            Name = "MainForm";
            Text = "MainForm";
            // Set size
            WidgetUtils.RefreshMainSize(MainUtils.GetSettingResolution());
            Size mainFormSize = WidgetUtils.MainSize;

            // Resize for login view
            Size loginViewSize = WidgetUtils.GetLoginViewSize(mainFormSize);
            Size = loginViewSize;
            ClientSize = loginViewSize;
            Rectangle workingArea = WidgetUtils.GetScreenWorkingArea();
            Location = new((workingArea.Width - loginViewSize.Width) / 2 + workingArea.Location.X, (workingArea.Height - loginViewSize.Height) / 2 + workingArea.Location.Y);
            LoginView loginView = new(loginViewSize, Properties.Resources.login_back, AfterLogin, mainFormSize);
            loginView.Parent = this;
            MainUtils.LoginView = loginView;
            WidgetUtils.BackToLoginView = needToAsk => {
                DialogResult result;
                if (needToAsk) {
                    result = MessageBox.Show(null, "确定要退出登录吗？", "退出登录", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                } else {
                    result = DialogResult.Yes;
                }
                if (result == DialogResult.Yes) {
                    if (MainUtils.ActionAfterLogout != null) {
                        MainUtils.ActionAfterLogout();
                        MainUtils.ActionAfterLogout = null;
                    }

                    Form mainForm = WidgetUtils.MainForm;
                    CustomTabPanel? mainPanel = WidgetUtils.MainPanel;
                    LoginView loginView = MainUtils.LoginView;
                    // 先清理所有UI组件
                    foreach (Control c in WidgetUtils.MainForm.Controls) {
                        c.Hide();
                    }
                    // 告诉登录界面现在的主界面尺寸
                    loginView.MainFormSize = mainForm.Size;
                    Size screenSize = WidgetUtils.GetScreenResolution();
                    loginView.AfterLogin = mainFormSize => {
                        if (mainFormSize == screenSize) {
                            mainForm.WindowState = FormWindowState.Maximized;
                        } else {
                            mainForm.Size = mainFormSize;
                            mainForm.ClientSize = mainFormSize;
                            mainForm.Location = new((screenSize.Width - mainFormSize.Width) / 2, (screenSize.Height - mainFormSize.Height) / 2);
                        }
                        MinimumSize = new Size(400, 300);
                        MaximumSize = screenSize;
                        if (SystemUtils.UserInfo.role_type == (int) Roles.OPERATOR) {
                            if (_operatorView != null) {
                                _operatorView.Dispose();
                            }
                            OperatorOpenning();
                        } else {
                            if (AllCreated) {
                                if (_operatorView != null) {
                                    _operatorView.Dispose();
                                }
                                if (mainPanel != null) {
                                    CustomMainMenuButton? openFirstButton = WidgetUtils.MainMenus.Values.SingleOrDefault(btn => btn.OpenFirst);
                                    if (openFirstButton != null) {
                                        openFirstButton.PerformClick();
                                    }
                                    mainPanel.Show();
                                }
                            } else {
                                // Reset some variables
                                WidgetUtils.ClearViews();
                                WidgetUtils.ClearMainMenus();
                                WidgetUtils.ClearChildMenus();

                                // Dispose all controls
                                foreach (Control c in WidgetUtils.MainForm.Controls) {
                                    if (c is LoginView) {
                                        continue;
                                    }
                                    c.Dispose();
                                }

                                // Re-create all
                                AfterLogin(mainFormSize);
                            }
                        }
                    };
                    // 重设登录界面尺寸
                    Size loginViewSize = WidgetUtils.GetLoginViewSize(mainForm.Size);
                    mainForm.Size = loginViewSize;
                    if (loginViewSize == screenSize) {
                        mainForm.WindowState = FormWindowState.Maximized;
                    } else {
                        mainForm.WindowState = FormWindowState.Normal;
                    }
                    mainForm.ClientSize = loginViewSize;
                    Point location = WidgetUtils.GetScreenWorkingArea().Location;
                    mainForm.Location = new((screenSize.Width - loginViewSize.Width) / 2 + location.X, (screenSize.Height - loginViewSize.Height) / 2 + location.Y);
                    loginView.Size = loginViewSize;
                    loginView.BackShowing = WidgetUtils.ResizeImage(loginView.Back, loginViewSize);
                    // 显示登录界面
                    loginView.Show();
                    loginView.ShowLoginForm();
                }
            };
        }

        private void OperatorOpenning() {
            _operatorView = new();
        }

        private void AfterLogin(Size mainFormSize) {
            // Reset back color after login
            BackColor = ColorConfigs.COLOR_MAIN_FORM_BACKGROUND;

            // Initialize all tasks for devices
            TaskInitializer.Init();

            if (SystemUtils.UserInfo.role_type == (int) Roles.OPERATOR) {
                OperatorOpenning();
            } else {
                // mainPanel
                mainPanel = new();
                mainPanel.Parent = this;
                mainPanel.BackColor = ColorConfigs.COLOR_MAIN_FORM_BACKGROUND;
                mainPanel.Margin = new Padding(0);
                mainPanel.Name = "mainPanel";
                mainPanel.Hide();
                // Store this mainPanel incase wherever needs to reach it
                WidgetUtils.MainPanel = mainPanel;
                // mainMenuPanel
                mainMenuPanel = new();
                mainMenuPanel.Parent = mainPanel;
                mainMenuPanel.BackColor = ColorConfigs.COLOR_MAIN_MENU_BACKGROUND;
                mainMenuPanel.MainMenuLogo = Properties.Resources.logo;
                mainMenuPanel.Margin = new Padding(0);
                mainMenuPanel.Name = "mainMenuPanel";
                mainMenuPanel.PanelDirection = MenuPanelDirection.TOP;
                // mainMenuPanel.OnlyIconMode = true;
                // Store this mainMenuPanel incase wherever needs to trigger it
                WidgetUtils.MainMenuPanel = mainMenuPanel;
                // mainContentPanel
                mainContentPanel = new();
                mainContentPanel.Parent = mainPanel;
                mainContentPanel.BackColor = ColorConfigs.COLOR_MAIN_FORM_BACKGROUND;
                mainContentPanel.Margin = new Padding(0);
                mainContentPanel.Name = "mainContentPanel";

                if (HookEvents == null) {
                    HookEvents = Hook.GlobalEvents();
                    // 全局鼠标事件
                    EventFuncs.MainForm = this;
                    HookEvents.MouseMove += (sender, eventArgs) => {
                        EventFuncs.GlobalMouseMove(sender, eventArgs);
                    };
                    // This event must be under above event, otherwise this will not triggered after view changing, don't knwo why
                    HookEvents.MouseUp += (sender, eventArgs) => {
                        EventFuncs.GlobalMouseUp(sender, eventArgs);
                    };
                    HookEvents.MouseDown += (sender, eventArgs) => {
                        EventFuncs.GlobalMouseDown(sender, eventArgs);
                    };
                }

                List<MenuConfig> menuCongfigs = SystemConfigs.MenuCongfigs;
                for (int i = 0; i < menuCongfigs.Count; i++) {
                    MenuConfig mainMenuConfig = menuCongfigs[i];
                    CustomMainMenuButton mainMenuButton = new();
                    mainMenuButton.Name = "mainMenuButton_" + mainMenuConfig.Id;
                    mainMenuButton.Icon = mainMenuConfig.Icon;
                    mainMenuButton.Label = mainMenuConfig.Name;
                    if (mainMenuConfig.Click != null) {
                        mainMenuButton.OnMenuButtonClick += mainMenuConfig.Click;
                    }
                    if (mainMenuConfig.ViewTypes != null && mainMenuConfig.ViewTypes.Count > 0) {
                        if (!MainUtils.License.MenuIds.ContainsKey(mainMenuConfig.Id)) {
                            CustomContentPanel contentPanelTemp = new();
                            contentPanelTemp.Name = "mainContentPanel_" + mainMenuConfig.Id;
                            int hPadding = contentPanelTemp.Width / 2;
                            int vPadding = contentPanelTemp.Height / 2;
                            contentPanelTemp.Controls.Add(new Label() { Text = "许可证信息缺失", AutoSize = true, Margin = new(hPadding, vPadding, hPadding, vPadding) });
                            contentPanelTemp.CorrespondingMenuButton = mainMenuButton;
                            mainMenuButton.CorrespondingContentPanel = new CustomVScrollingContentPanel(null, contentPanelTemp, false, true) {
                                Name = contentPanelTemp.Name
                            };
                        } else {
                            AppVersion appVersion = (AppVersion) Enum.Parse(typeof(AppVersion), MainUtils.License.AppVersion);
                            Type type;
                            if (mainMenuConfig.ViewTypes.ContainsKey(appVersion)) {
                                type = mainMenuConfig.ViewTypes[appVersion];
                            } else {
                                type = mainMenuConfig.ViewTypes[AppVersion.STANDARD];
                            }

                            object instance = type.Assembly.CreateInstance(type.FullName);
                            if (instance is CustomContentPanel) {
                                CustomContentPanel contentPanelTemp = (CustomContentPanel) instance;
                                //contentPanelTemp.PenBorderColor = ConfigsVariables.COLOR_CONTENT_PANEL_INNER_BORDER;
                                contentPanelTemp.Name = "mainContentPanel_" + mainMenuConfig.Id;
                                if (contentPanelTemp.Controls.Count == 0) {
                                    int hPadding = contentPanelTemp.Width / 2;
                                    int vPadding = contentPanelTemp.Height / 2;
                                    contentPanelTemp.Controls.Add(new Label() { Text = "载入错误，没有找到对应的功能", AutoSize = true, Margin = new(hPadding, vPadding, hPadding, vPadding) });
                                }
                                contentPanelTemp.CorrespondingMenuButton = mainMenuButton;
                                mainMenuButton.CorrespondingContentPanel = new CustomVScrollingContentPanel(null, contentPanelTemp, false, true) {
                                    Name = contentPanelTemp.Name
                                };
                                WidgetUtils.AddView(contentPanelTemp);
                            } else {
                                CustomTabPanel childTapPanel = (CustomTabPanel) instance;
                                CustomChildMenuFirstPanel childMenuPanel = new();
                                CustomContentPanelBase childContentPanel = new();
                                // childMenuPanel
                                childMenuPanel.BackColor = ColorConfigs.COLOR_CHILD_MENU_BACKGROUND;
                                childMenuPanel.Margin = new Padding(0);
                                childMenuPanel.Name = "mainMenuPanel";
                                childMenuPanel.PanelDirection = MenuPanelDirection.LEFT;
                                childMenuPanel.NeedFoldButton = true;
                                childMenuPanel.FoldButton.FoldedIcon = Properties.Resources.navigator_fold;
                                childMenuPanel.FoldButton.UnfoldedIcon = Properties.Resources.navigator_unfold;
                                childMenuPanel.FoldButton.ForeColor = ColorConfigs.COLOR_MENU_FOREGROUND;
                                if (mainMenuConfig.IsUserInfoPanel) {
                                    childMenuPanel.ShowUserInfoPanel(SystemUtils.LoggedUserName);
                                }
                                // childContentPanel
                                childContentPanel.BackColor = ColorConfigs.COLOR_MAIN_FORM_BACKGROUND;
                                childContentPanel.Margin = new Padding(0);
                                childContentPanel.Name = "mainContentPanel";

                                List<MenuConfig> childMenuConfigs = mainMenuConfig.Children;
                                for (int j = 0; j < childMenuConfigs.Count; j++) {
                                    MenuConfig childMenuConfig = childMenuConfigs[j];
                                    CustomChildMenuFirstButton childMenuFirstButton = new();
                                    childMenuFirstButton.Name = "childMenuFirstButton_" + childMenuConfig.Id;
                                    childMenuFirstButton.Icon = childMenuConfig.Icon;
                                    childMenuFirstButton.Label = childMenuConfig.Name;
                                    if (childMenuConfig.Click != null) {
                                        childMenuFirstButton.OnMenuButtonClick += childMenuConfig.Click;
                                    }
                                    if (childMenuConfig.ViewTypes != null && childMenuConfig.ViewTypes.Count > 0) {
                                        Type childType;
                                        if (childMenuConfig.ViewTypes.ContainsKey(appVersion)) {
                                            childType = childMenuConfig.ViewTypes[appVersion];
                                        } else {
                                            if (!childMenuConfig.ViewTypes.ContainsKey(AppVersion.STANDARD)) {
                                                continue;
                                            }
                                            childType = childMenuConfig.ViewTypes[AppVersion.STANDARD];
                                        }
                                        // TODO: License checking here
                                        object childInstance = childType.Assembly.CreateInstance(childType.FullName);
                                        if (MainUtils.License.MenuIds[mainMenuConfig.Id] == null || !MainUtils.License.MenuIds[mainMenuConfig.Id].Contains(childMenuConfig.Id)) {
                                            CustomContentPanel childContentPanelTemp = new();
                                            childContentPanelTemp.Name = "childContentPanel_" + j;
                                            int hPadding = childContentPanelTemp.Width / 2;
                                            int vPadding = childContentPanelTemp.Height / 2;
                                            childContentPanelTemp.Controls.Add(new Label() { Text = "许可证信息缺失", AutoSize = true, Margin = new(hPadding, vPadding, hPadding, vPadding) });
                                            childContentPanelTemp.CorrespondingMenuButton = childMenuFirstButton;
                                            childMenuFirstButton.CorrespondingContentPanel = new CustomVScrollingContentPanel(null, childContentPanelTemp, false, true) {
                                                Name = childContentPanelTemp.Name
                                            };
                                        } else if (childInstance is CustomContentPanel) {
                                            CustomContentPanel childContentPanelTemp = (CustomContentPanel) childInstance;
                                            //childContentPanelTemp.PenBorderColor = ConfigsVariables.COLOR_CONTENT_PANEL_INNER_BORDER;
                                            childContentPanelTemp.Name = "childContentPanel_" + j;
                                            if (childContentPanelTemp.Controls.Count == 0) {
                                                int hPadding = childContentPanelTemp.Width / 2;
                                                int vPadding = childContentPanelTemp.Height / 2;
                                                childContentPanelTemp.Controls.Add(new Label() { Text = "载入错误，没有找到对应的功能", AutoSize = true, Margin = new(hPadding, vPadding, hPadding, vPadding) });
                                            }
                                            childContentPanelTemp.CorrespondingMenuButton = childMenuFirstButton;
                                            childMenuFirstButton.CorrespondingContentPanel = new CustomVScrollingContentPanel(null, childContentPanelTemp, false, true) {
                                                Name = childContentPanelTemp.Name
                                            };
                                            WidgetUtils.AddView(childContentPanelTemp);
                                        }
                                    }
                                    childMenuFirstButton.ToggledButton = childMenuConfig.IsToggleButton;
                                    childMenuFirstButton.GroupMode = true;
                                    childMenuFirstButton.BackColor = ColorConfigs.COLOR_CHILD_MENU_BACKGROUND;
                                    childMenuFirstButton.ConerRadius = 0;
                                    childMenuFirstButton.FlatAppearance.BorderSize = 0;
                                    childMenuFirstButton.FlatStyle = FlatStyle.Flat;
                                    childMenuFirstButton.ForeColor = ColorConfigs.COLOR_MENU_FOREGROUND;
                                    childMenuFirstButton.Margin = new Padding(0);
                                    childMenuFirstButton.ToggleBar = true;
                                    childMenuFirstButton.ToggleBarDirection = AbstractCustomButton.ToggleBarDirectionEnum.LEFT;
                                    childMenuFirstButton.ToggledColor = ColorConfigs.COLOR_MENU_TOGGLED;

                                    WidgetUtils.AddChildMenu(childMenuConfig.Id, childMenuFirstButton);
                                    // Add child menu button an content panel into their parent panels
                                    childMenuPanel.Controls.Add(childMenuFirstButton);
                                    childContentPanel.Controls.Add(childMenuFirstButton.CorrespondingContentPanel);
                                }

                                // childTapPanel
                                childTapPanel.BackColor = ColorConfigs.COLOR_MAIN_FORM_BACKGROUND;
                                childTapPanel.Controls.Add(childMenuPanel);
                                childTapPanel.Controls.Add(childContentPanel);
                                childTapPanel.Margin = new Padding(0);
                                childTapPanel.Name = "childFirstPanel";

                                mainMenuButton.CorrespondingContentPanel = childTapPanel;
                            }
                        }
                    }
                    mainMenuButton.ToggledButton = mainMenuConfig.IsToggleButton;
                    mainMenuButton.GroupMode = true;
                    mainMenuButton.BackColor = ColorConfigs.COLOR_MAIN_MENU_BACKGROUND;
                    mainMenuButton.ConerRadius = 0;
                    mainMenuButton.FlatAppearance.BorderSize = 0;
                    mainMenuButton.FlatStyle = FlatStyle.Flat;
                    mainMenuButton.ForeColor = ColorConfigs.COLOR_MENU_FOREGROUND;
                    mainMenuButton.Margin = new Padding(0);
                    mainMenuButton.ToggleBar = false;
                    mainMenuButton.ToggledColor = ColorConfigs.COLOR_MENU_TOGGLED;

                    WidgetUtils.AddMainMenu(mainMenuConfig.Id, mainMenuButton);
                    // Add main menu button and main content panel into their parent panels
                    mainMenuPanel.Controls.Add(mainMenuButton);
                    mainContentPanel.Controls.Add(mainMenuButton.CorrespondingContentPanel);
                    if (mainMenuButton.CorrespondingContentPanel != null) {
                        mainMenuButton.CorrespondingContentPanel.Visible = false;
                    }
                    if (mainMenuConfig.OpenFirst) {
                        mainMenuButton.OpenFirst = mainMenuConfig.OpenFirst;
                        mainMenuButton.PerformClick();
                    }
                }

                AllCreated = true;
                mainPanel.Show();
            }

            // BackgroundWorker
            backgroundWorker = new() {
                WorkerReportsProgress = true,
            };
            backgroundWorker.DoWork += (sender, eventArgs) => {
                backgroundWorker.ReportProgress(100);
            };
            backgroundWorker.ProgressChanged += (sender, eventArgs) => {
                foreach (Control control in Controls) {
                    if (!MainUtils.LoginView.Visible) {
                        // control.Size = this.ClientSize;
                        // 留出边框的位置
                        control.Width = ClientSize.Width - 2;
                        control.Height = ClientSize.Height - 2;
                        if (control is LoginView loginView) {
                            loginView.Location = new(0, 0);
                        } else {
                            control.Location = new(1, 1);
                        }
                    }
                }
            };
            backgroundWorker.RunWorkerCompleted += (sender, eventArgs) => {
            };
            // SizeChanged event
            SizeChanged += async (sender, eventArgs) => {
                if (this.WindowState == FormWindowState.Minimized) {
                    return;
                }
                WidgetUtils.RefreshMainSize(Size);
                while (backgroundWorker.IsBusy) {
                    await Task.Delay(100);
                }
                backgroundWorker.RunWorkerAsync();
            };

            // Resize after login in
            Size screenSize = WidgetUtils.GetScreenResolution();
            if (mainFormSize == screenSize) {
                WindowState = FormWindowState.Maximized;
            } else {
                Size = mainFormSize;
                ClientSize = mainFormSize;
                CenterToScreen();
            }
            MinimumSize = new Size(400, 300);
            MaximumSize = screenSize;

            // 如果登录界面的分辨率与主界面一模一样，则会出现不触发 sizeChanged 事件的情况，因此这里手动触发一下
            if (!backgroundWorker.IsBusy) {
                backgroundWorker.RunWorkerAsync();
            }
        }
        #endregion
    }
}
