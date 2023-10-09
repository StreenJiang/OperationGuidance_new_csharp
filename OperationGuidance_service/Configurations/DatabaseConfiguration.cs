using System.Reflection;

namespace OperationGuidance_service.Configurations {
    public static class DatabaseConfiguration {

        // Get database path
        public static string GetDatabasePath() {
            //string startupPath = System.IO.Directory.GetCurrentDirectory();
            //string startupPath2 = Environment.CurrentDirectory;
            //string currentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            //string baseDirectory = System.AppDomain.CurrentDomain.BaseDirectory;
            //string wanted_path = Path.GetDirectoryName(Path.GetDirectoryName(System.IO.Directory.GetCurrentDirectory()));


            ////D:\VisualStudioProjects\C#\OperationGuidance_new\OperationGuidance_service\Database\test_db.db
            return "Database/test_db.db";
        }
    }
}