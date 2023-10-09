using Microsoft.EntityFrameworkCore;
using OperationGuidance_service.Models.AbstractClasses;

namespace OperationGuidance_service.Database {
    public class DataContext<T>: DbContext where T : AEntityBase {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) {
            optionsBuilder.UseSqlite("Data Source = D:\\VisualStudioProjects\\C#\\OperationGuidance_new\\OperationGuidance_new\\Database\\test_db.db");
        }

        public DbSet<T> Data { get; set; }
    }
}
