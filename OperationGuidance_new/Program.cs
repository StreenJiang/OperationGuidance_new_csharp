using CustomLibrary.Utils;
using OperationGuidance_new.Utils;
using OperationGuidance_service.Configurations;

namespace OperationGuidance_new {
    internal static class Program {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main() {
            // Avoid launch multiple times
            using (Mutex mutex = new Mutex(false, System.AppDomain.CurrentDomain.FriendlyName)) {
                if (!mutex.WaitOne(0, false)) {
                    WidgetUtils.ShowWarningPopUp("程序已经在运行中");
                    return;
                }
                // To customize application configuration such as set high DPI settings or default font,
                // see https://aka.ms/applicationconfiguration.
                ApplicationConfiguration.Initialize();
                // Initialize dependencies injection 
                DependencyInjector.Initialize();

                // Run main form
                try {
                    MainForm mainForm = new MainForm();
                    if (!mainForm.IsDisposed) {
                        mainForm.HandleDestroyed += (s, e) => MainUtils.AppRunning = false;
                        Application.Run(mainForm);
                    }
                } catch (Exception e) {
                    MainUtils.logger.Error($"Error while runing application, e = {e}");

                    WidgetUtils.ShowErrorPopUp($"程序运行错误，错误信息e: {e}");
                    throw e;
                }
            }
        }
    }
}
