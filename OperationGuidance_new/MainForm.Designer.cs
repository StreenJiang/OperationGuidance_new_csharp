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
using OperationGuidance_service.Database;
using OperationGuidance_service.Exceptions;
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

        private void InitializeComponentManually() {
            String thisprocessname = Process.GetCurrentProcess().ProcessName;
            // AllocConsole();

            // 先连接一下数据库，看看数据库是否正常
            try {
                DbConnector.GetConnection();
            } catch (DatabaseException de) {
                throw de;
            }

            // TODO: 判断mac地址（这段后面要用许可证来做）
            List<NetworkInterface> networkInterfaces = NetworkInterface.GetAllNetworkInterfaces().ToList();
            List<string> macs = networkInterfaces.Select(ni => ni.GetPhysicalAddress().ToString()).Where(mac => !string.IsNullOrEmpty(mac)).ToList();
            if (!(macs.Contains("002B677C56BC")
                || macs.Contains("BC542FD57669")
                || macs.Contains("BE542FD57668")
                || macs.Contains("BC542FD57668")
                || macs.Contains("BC542FD5766C")
                /* living room */
                || macs.Contains("A4B1C1C841E1")
                || macs.Contains("A4B1C1C841E5")
                || macs.Contains("B42E9954DB93")
                || macs.Contains("A4B1C1C841E2")
                || macs.Contains("A6B1C1C841E1")
                /* others */
                || macs.Contains("E43A6E5CBE6A")
                || macs.Contains("E43A6E4B2F12")
                /* SCII */
                || macs.Contains("E43A6E7936B1")
            )) {
                throw new Exception("当前设备未授权");
            }
            // TODO: 检查软件版本（这个也是许可证）
            MainUtils.Version = AppVersion.YF;

            // 检查当前设备是否已存在于物理地址表，用于隔离物理机器
            SystemUtils.MacAddressesDTO = SystemUtils.GetApis().FindMacAddressesByMacs(new(macs)).MacAddressesDTO;
            if (SystemUtils.MacAddressesDTO == null) {
                MacAddressesDTO? macAddressesDTOTemp = SystemUtils.GetApis().AddOrUpdateMacAddresses(new(new() { macs = string.Join(",", macs) })).MacAddressesDTO;
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
                                foreach (Control c in WidgetUtils.MainForm.Controls) {
                                    if (c is LoginView) {
                                        continue;
                                    }
                                    c.Dispose();
                                }
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

                // WidgetUtils.ClearViews();
                // WidgetUtils.ClearMainMenus();
                // WidgetUtils.ClearChildMenus();
                List<MenuConfig> menuCongfigs = SystemConfigs.MenuCongfigs;
                // TODO: 根据许可证权限过滤菜单
                for (int i = 0; i < menuCongfigs.Count; i++) {
                    MenuConfig mainMenuConfig = menuCongfigs[i];
                    CustomMainMenuButton mainMenuButton = new(ColorConfigs.COLOR_MAIN_MENU_BACKGROUND_TOGGLED_UP, ColorConfigs.COLOR_MAIN_MENU_BACKGROUND_TOGGLED_DOWN);
                    mainMenuButton.Name = "mainMenuButton_" + mainMenuConfig.Id;
                    mainMenuButton.Icon = mainMenuConfig.Icon;
                    mainMenuButton.Label = mainMenuConfig.Name;
                    if (mainMenuConfig.Click != null) {
                        mainMenuButton.OnMenuButtonClick += mainMenuConfig.Click;
                    }
                    if (mainMenuConfig.ViewTypes != null && mainMenuConfig.ViewTypes.Count > 0) {
                        Type type;
                        if (mainMenuConfig.ViewTypes.ContainsKey(MainUtils.Version)) {
                            type = mainMenuConfig.ViewTypes[MainUtils.Version];
                        } else {
                            type = mainMenuConfig.ViewTypes[AppVersion.STANDARD];
                        }
                        object instance = type.Assembly.CreateInstance(type.FullName);
                        if (instance is CustomContentPanel) {
                            CustomContentPanel contentPanelTemp = (CustomContentPanel) instance;
                            //contentPanelTemp.PenBorderColor = ConfigsVariables.COLOR_CONTENT_PANEL_INNER_BORDER;
                            contentPanelTemp.Name = "mainContentPanel_" + i;
                            if (contentPanelTemp.Controls.Count == 0) {
                                contentPanelTemp.Controls.Add(new TextBox() { Text = contentPanelTemp.Name, Width = 150, Margin = new(0) });
                            }
                            contentPanelTemp.CorrespondingMenuButton = mainMenuButton;
                            mainMenuButton.CorrespondingContentPanel = new CustomVScrollingContentPanel(
                                ColorConfigs.COLOR_CONTENT_PANEL_INNER_BORDER, contentPanelTemp
                            ) {
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
                                CustomChildMenuFirstButton childMenuFirstButton = new(ColorConfigs.COLOR_CHILD_MENU_BACKGROUND_TOGGLED_LEFT, ColorConfigs.COLOR_CHILD_MENU_BACKGROUND_TOGGLED_RIGHT);
                                childMenuFirstButton.Name = "childMenuFirstButton_" + childMenuConfig.Id;
                                childMenuFirstButton.Icon = childMenuConfig.Icon;
                                childMenuFirstButton.Label = childMenuConfig.Name;
                                if (childMenuConfig.Click != null) {
                                    childMenuFirstButton.OnMenuButtonClick += childMenuConfig.Click;
                                }
                                if (childMenuConfig.ViewTypes != null && childMenuConfig.ViewTypes.Count > 0) {
                                    Type childType;
                                    if (childMenuConfig.ViewTypes.ContainsKey(MainUtils.Version)) {
                                        childType = childMenuConfig.ViewTypes[MainUtils.Version];
                                    } else {
                                        childType = childMenuConfig.ViewTypes[AppVersion.STANDARD];
                                    }
                                    object childInstance = childType.Assembly.CreateInstance(childType.FullName);
                                    if (childInstance is CustomContentPanel) {
                                        CustomContentPanel childContentPanelTemp = (CustomContentPanel) childInstance;
                                        //childContentPanelTemp.PenBorderColor = ConfigsVariables.COLOR_CONTENT_PANEL_INNER_BORDER;
                                        childContentPanelTemp.Name = "childContentPanel_" + j;
                                        if (childContentPanelTemp.Controls.Count == 0) {
                                            childContentPanelTemp.Controls.Add(new TextBox() { Text = childContentPanelTemp.Name, Width = 150, Margin = new(0) });
                                        }
                                        childContentPanelTemp.CorrespondingMenuButton = childMenuFirstButton;
                                        childMenuFirstButton.CorrespondingContentPanel = new CustomVScrollingContentPanel(
                                            ColorConfigs.COLOR_CONTENT_PANEL_INNER_BORDER, childContentPanelTemp
                                        ) {
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
