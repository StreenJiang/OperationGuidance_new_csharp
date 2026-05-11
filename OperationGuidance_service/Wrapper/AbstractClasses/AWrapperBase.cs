using Dapper;
using log4net;
using OperationGuidance_service.Attributes;
using OperationGuidance_service.Constants;
using OperationGuidance_service.Database;
using OperationGuidance_service.Exceptions;
using OperationGuidance_service.Models.AbstractClasses;
using OperationGuidance_service.Utils;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Common;
using System.Reflection;

namespace OperationGuidance_service.Wrapper.AbstractClasses {
    [Wrapper]
    public abstract class AWrapperBase<T> where T : AEntityBase, new() {
        protected ILog logger = SystemUtils.GetLogger(typeof(T));

        private string _tabelName;
        private DbConnection? _conn;
        private DbTransaction? _transaction;
        private const int commandTimeout = 10;

        public string TableName { get => _tabelName; }

        public AWrapperBase() {
            _tabelName = GetTableName();
        }

        public void UseConnection(DbConnection conn, DbTransaction transaction) {
            _conn = conn;
            _transaction = transaction;
        }
        public void ReleaseConnection() {
            if (_conn != null) {
                _conn.Dispose();
                _conn = null;
            }
            if (_transaction != null) {
                _transaction.Dispose();
                _transaction = null;
            }
        }

        public T Add(T entity) {
            try {
                string sql = GenerateInsertSql();
                logger.Info("sql: " + sql);
                string newEntitySql = GenerateQueryNewestSql(entity);
                logger.Info("newEntitySql: " + newEntitySql);

                int result = ExecuteWithRetry(sql, entity);
                entity.id = QueryFirstWithRetry(newEntitySql, entity);

                logger.Info("Result: " + result);
            } catch (Exception e) {
                logger.Warn($"Something wrong here, please check error: e = {e}");
            }
            return entity;
        }

        public int AddBatch(List<T> entities) {
            int result = 0;
            try {
                string sql = GenerateInsertSql();
                logger.Info("sql: " + sql);

                result = ExecuteWithRetry(sql, entities);
                logger.Info("Result: " + result);
            } catch (Exception e) {
                logger.Warn($"Something wrong here, please check error: e = {e}");
            }
            return result;
        }

        public T? FindById(int id) {
            T temp = new();
            List<T> entities = FindBySql($"select * from {_tabelName} where {nameof(temp.id)} = {id}");
            return entities.Count > 0 ? entities[0] : null;
        }

        public List<T> FindBySql(string sql) {
            List<T> result = new();
            try {
                logger.Info("sql: " + sql);
                IEnumerable<T> enumerable;
                if (_conn == null) {
                    using (DbConnection conn = DbConnector.GetConnection()) {
                        enumerable = conn.Query<T>(sql);
                    }
                } else {
                    // Don't use 'using' to release resource, probably is in a transaction
                    enumerable = _conn.Query<T>(sql, null, _transaction);
                }
                result = enumerable.ToList();

                logger.Info("Size of result: " + enumerable.Count());
            } catch (Exception e) {
                logger.Warn($"Something wrong here, please check error: e = {e}");
            }
            return result;
        }

        public List<T> FindBySql(string sql, Dictionary<string, object>? @params) {
            List<T> result = new();
            try {
                logger.Info($"sql: [{sql}], @params: [{GetParamsStr(@params)}]");
                IEnumerable<T> enumerable;
                if (_conn == null) {
                    using (DbConnection conn = DbConnector.GetConnection()) {
                        enumerable = conn.Query<T>(sql, @params);
                    }
                } else {
                    // Don't use 'using' to release resource, probably is in a transaction
                    enumerable = _conn.Query<T>(sql, @params, _transaction);
                }
                result = enumerable.ToList();

                logger.Info("Size of result: " + enumerable.Count());
            } catch (Exception e) {
                logger.Warn($"Something wrong here, please check error: e = {e}");
            }
            return result;
        }

        public T Update(T entity) {
            try {
                entity.modifier = SystemUtils.LoggedUserName;
                entity.modify_time = DateTime.Now;
                string sql = GenerateUpdateSql(entity);
                logger.Info("sql: " + sql);

                int result = ExecuteWithRetry(sql, entity);
                logger.Info("Result: " + result);
            } catch (Exception e) {
                logger.Warn($"Something wrong here, please check error: e = {e}");
            }
            return entity;
        }

        public int UpdateBatch(List<T> entities) {
            int rows = 0;
            try {
                T entity = entities[0];
                string sql = GenerateUpdateSql(entity);
                logger.Info("sql: " + sql);

                int result = ExecuteWithRetry(sql, entities);
                logger.Info("Result: " + rows);
            } catch (Exception e) {
                logger.Warn($"Something wrong here, please check error: e = {e}");
            }
            return rows;
        }

        public bool Delete(T entity) {
            entity.deleted = (int) YesOrNo.YES;
            return Update(entity)?.deleted == (int) YesOrNo.YES;
        }

        public bool DeleteById(int id) {
            T entity = FindById(id) ?? throw new EntityNotFoundException("Entity not found by id = " + id + ".");
            return Delete(entity);
        }

        public int DeleteByIds(List<int> ids) {
            string idsStr = string.Join(", ", ids.ToArray());
            List<T> entities = FindBySql($"select * from {_tabelName} where id in ({idsStr})");
            entities.ForEach(entity => entity.deleted = (int) YesOrNo.YES);
            return UpdateBatch(entities);
        }

        private string GenerateInsertSql() {
            T temp = new();
            string tableName = GetTableName();
            // Handle fields
            List<string> fields = GetFiedsList();
            // Generate sql
            string insertSql = $"insert into {tableName}(";
            int count = 0;
            foreach (string field in fields) {
                if (field != nameof(temp.id)) {
                    if (count != 0) {
                        insertSql += ", ";
                    }
                    insertSql += field;
                    count++;
                }
            }
            insertSql += ") values(";
            count = 0;
            foreach (string field in fields) {
                if (field != nameof(temp.id)) {
                    if (count != 0) {
                        insertSql += ", ";
                    }
                    insertSql += "@" + field;
                    count++;
                }
            }
            insertSql += ")";
            return insertSql;
        }

        private string GenerateQueryNewestSql(T entity) {
            string tableName = GetTableName();
            return $"select max(id) from {tableName} where {CommonCondition(entity)}";
        }

        private string GenerateUpdateSql(T entity) {
            string tableName = GetTableName();
            // Handle fields
            List<string> fields = GetFiedsList();
            // Generate sql
            string updateSql = $"update {tableName} set ";
            int count = 0;
            foreach (string field in fields) {
                // Skip field<id>, because it can not be modified
                if (field != nameof(entity.id)) {
                    if (count != 0) {
                        updateSql += ", ";
                    }
                    updateSql += field + " = @" + field;
                    count++;
                }
            }
            updateSql += $" where {nameof(entity.id)} = @{nameof(entity.id)}";
            return updateSql;
        }

        public string CommonCondition() {
            return CommonCondition(new());
        }
        public string CommonCondition(T entity) {
            return $"{nameof(entity.deleted)} = {(int) YesOrNo.NO} and {nameof(entity.user_id)} = @{nameof(entity.user_id)}";
        }

        public string ConditionWithoutUserId() {
            return ConditionWithoutUserId(new());
        }
        public string ConditionWithoutUserId(T entity) {
            return $"{nameof(entity.deleted)} = {(int) YesOrNo.NO}";
        }

        private List<string> GetFiedsList() {
            List<string> fields = new();
            foreach (PropertyInfo property in typeof(T).GetProperties()) {
                string fieldsName = property.Name;
                foreach (Attribute attribute in property.GetCustomAttributes()) {
                    if (attribute is ColumnAttribute) {
                        string? name = ((ColumnAttribute) attribute).Name;
                        if (name != null) {
                            fieldsName = name;
                        }
                    }
                }
                fields.Add(fieldsName);
            }
            return fields;
        }

        private string GetTableName() {
            foreach (object attribute in typeof(T).GetCustomAttributes(false)) {
                if (attribute is TableAttribute) {
                    return ((TableAttribute) attribute).Name;
                }
            }
            throw new InvalidDataException("Enetity<" + typeof(T).Name + "> attibute 'Table' not set, please check.");
        }

        private string? GetParamsStr(Dictionary<string, object>? @params) {
            if (@params != null) {
                string paramsStr = "";
                int index = 0;
                foreach (KeyValuePair<string, object> pair in @params) {
                    if (index > 0) {
                        paramsStr += ", ";
                    }
                    if (pair.Value.GetType() == typeof(List<>)) {
                        List<object> list = (List<object>) pair.Value;
                        paramsStr += $"{pair.Key} = {string.Join(",", list)}";
                    } else {
                        paramsStr += $"{pair.Key} = {pair.Value}";
                    }
                    index++;
                }
                return paramsStr;
            }

            return null;
        }

        public int ExecuteScalar(string sql, Dictionary<string, object>? @params = null) {
            try {
                if (_conn == null) {
                    using (DbConnection conn = DbConnector.GetConnection()) {
                        return conn.QueryFirst<int>(sql, @params);
                    }
                } else {
                    return _conn.QueryFirst<int>(sql, @params, _transaction);
                }
            } catch (Exception e) {
                logger.Warn($"Something wrong here, please check error: e = {e}");
                return 0;
            }
        }

        public int ExecuteSql(string sql) {
            int rows = 0;
            try {
                using (DbConnection conn = DbConnector.GetConnection()) {
                    rows = conn.Execute(sql);
                }
            } catch (Exception e) {
                logger.Warn($"Something wrong here, please check error: e = {e}");
            }
            return rows;
        }

        public int ExecuteWithRetry(string sql, object? param = null, DbTransaction? transaction = null) {
            const int maxRetries = 5;

            for (int attempt = 0; attempt < maxRetries; attempt++) {
                try {
                    if (_conn == null) {
                        using (DbConnection conn = DbConnector.GetConnection()) {
                            return conn.Execute(sql, param, commandTimeout: commandTimeout);
                        }
                    } else {
                        // Don't use 'using' to release resource, probably is in a transaction
                        return _conn.Execute(sql, param, transaction, commandTimeout: commandTimeout);
                    }
                } catch (Exception ex) when (IsDeadlockOrLockTimeout(ex) && attempt < maxRetries - 1) {
                    // 死锁或锁超时，指数退避重试
                    int delayMs = 50 * (attempt + 1);
                    Thread.Sleep(delayMs);
                }
            }

            // 如果所有重试都失败了，重新抛出异常（保持原有异常处理逻辑）
            if (_conn == null) {
                using (DbConnection conn = DbConnector.GetConnection()) {
                    return conn.Execute(sql, param, commandTimeout: commandTimeout);
                }
            } else {
                return _conn.Execute(sql, param, transaction, commandTimeout: commandTimeout);
            }
        }

        private int QueryFirstWithRetry(string sql, object? param = null) {
            const int maxRetries = 5;

            for (int attempt = 0; attempt < maxRetries; attempt++) {
                try {
                    if (_conn == null) {
                        using (DbConnection conn = DbConnector.GetConnection()) {
                            return conn.QueryFirst<int>(sql, param, commandTimeout: commandTimeout);
                        }
                    } else {
                        return _conn.QueryFirst<int>(sql, param, _transaction, commandTimeout: commandTimeout);
                    }
                } catch (Exception ex) when (IsDeadlockOrLockTimeout(ex) && attempt < maxRetries - 1) {
                    int delayMs = 50 * (attempt + 1);
                    Thread.Sleep(delayMs);
                }
            }

            // 最终尝试（保持原有逻辑）
            if (_conn == null) {
                using (DbConnection conn = DbConnector.GetConnection()) {
                    return conn.QueryFirst<int>(sql, param, commandTimeout: commandTimeout);
                }
            } else {
                return _conn.QueryFirst<int>(sql, param, _transaction, commandTimeout: commandTimeout);
            }
        }

        private bool IsDeadlockOrLockTimeout(Exception ex) {
            // MySQL 错误处理
            if (ex is MySql.Data.MySqlClient.MySqlException mySqlEx) {
                return mySqlEx.Number == 1213 || mySqlEx.Number == 1205;
            }

            // SQL Server 错误处理（兼容性）
            if (ex is System.Data.SqlClient.SqlException sqlEx) {
                return sqlEx.Number == 1205;
            }

            // // SQL Server 错误处理（兼容性）
            // if (ex is Microsoft.Data.SqlClient.SqlException sqlEx) {
            //     return sqlEx.Number == 1205;
            // }
            return false;
        }
    }
}
