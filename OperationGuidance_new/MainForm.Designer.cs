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

        //[System.Runtime.InteropServices.DllImport("user32")]
        //protected static extern bool AnimateWindow(IntPtr hWnd, int dwTime, int dwFlags);

        #region Windows Form manually initialization code

        private void InitializeComponentManually() {
            // Get MAC address
            List<NetworkInterface> networkInterfaces = NetworkInterface.GetAllNetworkInterfaces().ToList();
            List<string> macs = networkInterfaces.Select(ni => ni.GetPhysicalAddress().ToString()).Where(mac => !string.IsNullOrEmpty(mac)).ToList();
            if (!(macs.Contains("002B677C56BC") 
                || macs.Contains("BC542FD57669")
                || macs.Contains("BE542FD57668")
                || macs.Contains("BC542FD57668")
                || macs.Contains("BC542FD5766C")
                // 客厅电脑
                || macs.Contains("A4B1C1C841E1")
                || macs.Contains("A4B1C1C841E5")
                || macs.Contains("B42E9954DB93")
                || macs.Contains("A4B1C1C841E2")
                || macs.Contains("A6B1C1C841E1")
                // others
                || macs.Contains("E43A6E5CBE6A")
            )) {
                WidgetUtils.ShowErrorPopUp("当前设备未授权");
                throw new Exception("当前设备未授权");
            }
            // MainForm
            Name = "MainForm";
            Text = "MainForm";
            // Set size
            Size mainFormSize;
            Size screenSize = WidgetUtils.GetScreenResolution();
            string resolution = MainUtils.Settings.Read(IniFileKeys.Resolution);
            if (!string.IsNullOrEmpty(resolution)) {
                string[] strings = resolution.Split(",");
                int width = int.Parse(strings[0].Trim());
                int height = int.Parse(strings[1].Trim());
                if (width == screenSize.Width && height == screenSize.Height) {
                    mainFormSize = screenSize;
                } else {
                    mainFormSize = new(width, height);
                }
            } else {
                mainFormSize = screenSize;
            }

            // mainPanel
            mainPanel = new();
            mainPanel.Parent = this;
            mainPanel.BackColor = ColorConfigs.COLOR_MAIN_FORM_BACKGROUND;
            mainPanel.Margin = new Padding(0);
            mainPanel.Name = "mainPanel";
            mainPanel.Size = mainFormSize;
            mainPanel.Hide();
            // Store this mainPanel incase wherever needs to reach it
            WidgetUtils.MainForm = this;
            WidgetUtils.MainPanel = mainPanel;
            // Resize for login view
            Size loginViewSize = WidgetUtils.GetLoginViewSize(mainFormSize);
            Size = loginViewSize;
            ClientSize = loginViewSize;
            CenterToScreen();
            LoginView loginView = new(loginViewSize, Properties.Resources.login_back, AfterLogin, mainFormSize);
            loginView.Parent = this;
            MainUtils.LoginView = loginView;
        }

        private void AfterLogin(Size mainFormSize) {
            // Reset back color after login
            BackColor = ColorConfigs.COLOR_MAIN_FORM_BACKGROUND;

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
            // BackgroundWorker
            backgroundWorker = new() {
                WorkerReportsProgress = true,
            };

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
                if (mainMenuConfig.ViewType != null) {
                    Type type = mainMenuConfig.ViewType;
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
                        //childMenuPanel.OnlyIconMode = true;
                        // childContentPanel
                        childContentPanel.BackColor = ColorConfigs.COLOR_MAIN_FORM_BACKGROUND;
                        childContentPanel.Margin = new Padding(0);
                        childContentPanel.Name = "mainContentPanel";

                        List<MenuConfig> childMenuConfigs = mainMenuConfig.Children;
                        for (int j = 0; j < childMenuConfigs.Count; j++) {
                            MenuConfig childMenuConfig = childMenuConfigs[j];
                            CustomChildMenuFirstButton childMenuFirstButton= new(ColorConfigs.COLOR_CHILD_MENU_BACKGROUND_TOGGLED_LEFT, ColorConfigs.COLOR_CHILD_MENU_BACKGROUND_TOGGLED_RIGHT);
                            childMenuFirstButton.Name = "childMenuFirstButton_" + childMenuConfig.Id;
                            childMenuFirstButton.Icon = childMenuConfig.Icon;
                            childMenuFirstButton.Label = childMenuConfig.Name;
                            if (childMenuConfig.Click != null) {
                                childMenuFirstButton.OnMenuButtonClick += childMenuConfig.Click;
                            }
                            if (childMenuConfig.ViewType != null) {
                                Type childType = childMenuConfig.ViewType;
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
            }

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
            mainPanel.Show();

            // Initialize all tasks for devices
            TaskInitializer.Init();

            // BackgroundWorker event
            backgroundWorker.DoWork += (sender, eventArgs) => {
                backgroundWorker.ReportProgress(100);
            };
            backgroundWorker.ProgressChanged += (sender, eventArgs) => {
                foreach (Control control in Controls) {
                    if (!MainUtils.LoginView.Visible) {
                        control.Size = this.ClientSize;
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
                while (backgroundWorker.IsBusy) {
                    await Task.Delay(100);
                }
                backgroundWorker.RunWorkerAsync();
            };
        }
        
        #endregion

    }
}
