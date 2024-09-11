using System.Data.Common;
using Dapper;
using OperationGuidance_service.Attributes;

namespace OperationGuidance_service.Services.AbstractClasses {
    [Service]
    public abstract class ADapperDBServiceBase {

        public int ExecuteSql(DbConnection conn, string sql) => conn.Execute(sql);
        public int ExecuteSql(DbConnection conn, string sql, Dictionary<string, object?> @params) => conn.Execute(sql, @params);

        public List<T> QueryBySql<T>(DbConnection conn, string sql) => conn.Query<T>(sql).ToList();
    }
}
