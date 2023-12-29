using CustomLibrary.Buttons;
using CustomLibrary.Buttons.AbstractClasses;
using CustomLibrary.Constants;
using CustomLibrary.Panels;
using CustomLibrary.Panels.BaseClasses;
using CustomLibrary.Utils;
using Gma.System.MouseKeyHook;
using Microsoft.Extensions.DependencyInjection;
using OperationGuidance_service.Apis;
using OperationGuidance_service.Configurations;
using OperationGuidance_service.Models;
using OperationGuidance_service.Models.Requests;
using OperationGuidance_service.Models.Responses;
using OperationGuidance_service.Utils;
using OperationGuidance_new.Configs;
using OperationGuidance_new.Utils;
using CustomLibrary.Forms;
using CustomLibrary.Events;

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

        //[System.Runtime.InteropServices.DllImport("user32")]
        //protected static extern bool AnimateWindow(IntPtr hWnd, int dwTime, int dwFlags);

        #region Windows Form manually initialization code

        private void InitializeComponentManually() {
            // Get login user info
            OperationGuidanceApis? apis = DependencyInjector.Provider.GetService<OperationGuidanceApis>();
            SystemUtils.UserInfo = apis._userAccountInfoService.FindById(1);

            // Main panel
            mainPanel = new();
            // Store this mainPanel incase wherever needs to reach it
            WidgetUtils.MainPanel = mainPanel;
            mainMenuPanel = new();
            // Store this mainMenuPanel incase wherever needs to trigger it
            WidgetUtils.MainMenuPanel = mainMenuPanel;
            mainContentPanel = new();
            // mainMenuPanel
            mainMenuPanel.BackColor = ConfigsVariables.COLOR_MAIN_MENU_BACKGROUND;
            mainMenuPanel.MainMenuLogo = Properties.Resources.logo;
            mainMenuPanel.Margin = new Padding(0);
            mainMenuPanel.Name = "mainMenuPanel";
            mainMenuPanel.PanelDirection = MenuPanelDirection.TOP;
            // mainMenuPanel.OnlyIconMode = true;
            // mainContentPanel
            mainContentPanel.BackColor = ConfigsVariables.COLOR_MAIN_FORM_BACKGROUND;
            mainContentPanel.Margin = new Padding(0);
            mainContentPanel.Name = "mainContentPanel";
            HookEvents = Hook.GlobalEvents();

            // 全局鼠标事件
            EventFuncs.MainForm = this;
            HookEvents.MouseMove += (sender, eventArgs) => {
                EventFuncs.GlobalMouseMove(sender, eventArgs);
            };
            // This event must be under above event, otherwise this will not triggered after view changing, don't knwo why
            HookEvents.MouseClick += (sender, eventArgs) => {
                EventFuncs.GlobalMouseClick(sender, eventArgs);
            };

            List<Dictionary<string, object>> menuCongfigs = SystemConfigs.MenuCongfigs.Where(e => (bool) e[SystemConfigs.Key_Enabled]).ToList();
            for (int i = 0; i < menuCongfigs.Count; i++) {
                Dictionary<string, object> mainMenuConfig = menuCongfigs[i];
                CustomMainMenuButton mainMenuButton = new(ConfigsVariables.COLOR_MAIN_MENU_BACKGROUND_TOGGLED_UP, ConfigsVariables.COLOR_MAIN_MENU_BACKGROUND_TOGGLED_DOWN);
                mainMenuButton.Name = "mainMenuButton_" + mainMenuConfig[SystemConfigs.Key_ID];
                mainMenuButton.Icon = mainMenuConfig[SystemConfigs.Key_Icon] as Image;
                mainMenuButton.Label = mainMenuConfig[SystemConfigs.Key_Name] as string;
                if (mainMenuConfig[SystemConfigs.Key_Click] != null) {
                    mainMenuButton.OnMenuButtonClick += (EventHandler) mainMenuConfig[SystemConfigs.Key_Click];
                }
                if (mainMenuConfig[SystemConfigs.Key_View_Name] != null) {
                    Type type = mainMenuConfig[SystemConfigs.Key_View_Name] as Type;
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
                            ConfigsVariables.COLOR_CONTENT_PANEL_INNER_BORDER, contentPanelTemp
                        ) {
                            Name = contentPanelTemp.Name
                        };
                        WidgetUtils.AddView(contentPanelTemp);
                    } else {
                        CustomTabPanel childTapPanel = (CustomTabPanel) instance;
                        CustomChildMenuFirstPanel childMenuPanel = new();
                        CustomContentPanelBase childContentPanel = new();
                        // childMenuPanel
                        childMenuPanel.BackColor = ConfigsVariables.COLOR_CHILD_MENU_BACKGROUND;
                        childMenuPanel.Margin = new Padding(0);
                        childMenuPanel.Name = "mainMenuPanel";
                        childMenuPanel.PanelDirection = MenuPanelDirection.LEFT;
                        childMenuPanel.NeedFoldButton = true;
                        childMenuPanel.FoldButton.FoldedIcon = Properties.Resources.navigator_fold;
                        childMenuPanel.FoldButton.UnfoldedIcon = Properties.Resources.navigator_unfold;
                        childMenuPanel.FoldButton.ForeColor = ConfigsVariables.COLOR_MENU_FOREGROUND;
                        //childMenuPanel.OnlyIconMode = true;
                        // childContentPanel
                        childContentPanel.BackColor = ConfigsVariables.COLOR_MAIN_FORM_BACKGROUND;
                        childContentPanel.Margin = new Padding(0);
                        childContentPanel.Name = "mainContentPanel";

                        List<Dictionary<string, object>> childMenuConfigs = mainMenuConfig[SystemConfigs.Key_Children] as List<Dictionary<string, object>>;
                        for (int j = 0; j < childMenuConfigs.Count; j++) {
                            Dictionary<string, object> childMenuConfig = childMenuConfigs[j];
                            CustomChildMenuFirstButton childMenuFirstButton= new(ConfigsVariables.COLOR_CHILD_MENU_BACKGROUND_TOGGLED_LEFT, ConfigsVariables.COLOR_CHILD_MENU_BACKGROUND_TOGGLED_RIGHT);
                            childMenuFirstButton.Name = "childMenuFirstButton_" + childMenuConfig[SystemConfigs.Key_ID];
                            childMenuFirstButton.Icon = childMenuConfig[SystemConfigs.Key_Icon] as Image;
                            childMenuFirstButton.Label = childMenuConfig[SystemConfigs.Key_Name] as string;
                            if (childMenuConfig[SystemConfigs.Key_Click] != null) {
                                childMenuFirstButton.OnMenuButtonClick += (EventHandler) childMenuConfig[SystemConfigs.Key_Click];
                            }
                            if (childMenuConfig[SystemConfigs.Key_View_Name] != null) {
                                Type childType = childMenuConfig[SystemConfigs.Key_View_Name] as Type;
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
                                        ConfigsVariables.COLOR_CONTENT_PANEL_INNER_BORDER, childContentPanelTemp
                                    ) {
                                        Name = childContentPanelTemp.Name
                                    };
                                    WidgetUtils.AddView(childContentPanelTemp);
                                }
                            }
                            childMenuFirstButton.ToggledButton = (bool) childMenuConfig[SystemConfigs.Key_Toggle_Button];
                            childMenuFirstButton.GroupMode = true;
                            childMenuFirstButton.BackColor = ConfigsVariables.COLOR_CHILD_MENU_BACKGROUND;
                            childMenuFirstButton.ConerRadius = 0;
                            childMenuFirstButton.FlatAppearance.BorderSize = 0;
                            childMenuFirstButton.FlatStyle = FlatStyle.Flat;
                            childMenuFirstButton.ForeColor = ConfigsVariables.COLOR_MENU_FOREGROUND;
                            childMenuFirstButton.Margin = new Padding(0);
                            childMenuFirstButton.ToggleBar = true;
                            childMenuFirstButton.ToggleBarDirection = AbstractCustomButton.ToggleBarDirectionEnum.LEFT;
                            childMenuFirstButton.ToggledColor = ConfigsVariables.COLOR_MENU_TOGGLED;

                            WidgetUtils.AddChildMenu((int) childMenuConfig[SystemConfigs.Key_ID], childMenuFirstButton);
                            // Add child menu button an content panel into their parent panels
                            childMenuPanel.Controls.Add(childMenuFirstButton);
                            childContentPanel.Controls.Add(childMenuFirstButton.CorrespondingContentPanel);
                        }

                        // childTapPanel
                        childTapPanel.BackColor = ConfigsVariables.COLOR_MAIN_FORM_BACKGROUND;
                        childTapPanel.Controls.Add(childMenuPanel);
                        childTapPanel.Controls.Add(childContentPanel);
                        childTapPanel.Margin = new Padding(0);
                        childTapPanel.Name = "childFirstPanel";

                        mainMenuButton.CorrespondingContentPanel = childTapPanel;
                    }
                }
                mainMenuButton.ToggledButton = (bool) mainMenuConfig[SystemConfigs.Key_Toggle_Button];
                mainMenuButton.GroupMode = true;
                mainMenuButton.BackColor = ConfigsVariables.COLOR_MAIN_MENU_BACKGROUND;
                mainMenuButton.ConerRadius = 0;
                mainMenuButton.FlatAppearance.BorderSize = 0;
                mainMenuButton.FlatStyle = FlatStyle.Flat;
                mainMenuButton.ForeColor = ConfigsVariables.COLOR_MENU_FOREGROUND;
                mainMenuButton.Margin = new Padding(0);
                mainMenuButton.ToggleBar = false;
                mainMenuButton.ToggledColor = ConfigsVariables.COLOR_MENU_TOGGLED;

                WidgetUtils.AddMainMenu((int) mainMenuConfig[SystemConfigs.Key_ID], mainMenuButton);
                // Add main menu button and main content panel into their parent panels
                mainMenuPanel.Controls.Add(mainMenuButton);
                mainContentPanel.Controls.Add(mainMenuButton.CorrespondingContentPanel);
            }

            // mainPanel
            mainPanel.BackColor = ConfigsVariables.COLOR_MAIN_FORM_BACKGROUND;
            mainPanel.Controls.Add(mainMenuPanel);
            mainPanel.Controls.Add(mainContentPanel);
            mainPanel.Margin = new Padding(0);
            mainPanel.Name = "mainPanel";

            // MainForm
            BackColor = ConfigsVariables.COLOR_MAIN_FORM_BACKGROUND;
            Controls.Add(mainPanel);
            Size = new(800, 600);
            ClientSize = new(800, 600);
            MinimumSize = new Size(400, 300);
            Name = "MainForm";
            Text = "MainForm";


            // Test
            //ProductMissionListReq req = new();
            //req.UserId = SystemUtils.UserInfo.Id;
            //ProductMissionListRsp productMissionListRsp = apis.QueryProductMissionListRsp(req);

            //string b = CommonUtils.ImageToBase64(Properties.Resources.aneng_arm_error);

            Console.WriteLine();
        }

        #endregion

    }
}
