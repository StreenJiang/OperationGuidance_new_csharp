using OperationGuidance_service.Configurations;

namespace OperationGuidance_new {
    internal static class Program {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main() {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            // Initialize dependencies injection 
            DependencyInjector.Initialize();

            // Run main form
            try{
                Application.Run(new MainForm());
            } catch (Exception e) {
                Console.WriteLine(e); 
                // WidgetUtils.ShowNoticePopUp($"出错啦！e: {e}");
                throw e;
            }
        }
    }
}
