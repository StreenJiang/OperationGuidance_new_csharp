using Dapper;
using log4net;
using OperationGuidance_service.Attributes;
using OperationGuidance_service.Configurations;
using OperationGuidance_service.Constants;
using OperationGuidance_service.Database;
using OperationGuidance_service.Exceptions;
using OperationGuidance_service.Models.AbstractClasses;
using OperationGuidance_service.Models.DTOs;
using OperationGuidance_service.Utils;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Common;
using System.Reflection;
using System.Text.RegularExpressions;

namespace OperationGuidance_service.Wrapper.AbstractClasses {
    [Wrapper]
    public abstract class AWrapperBase<T> where T : AEntityBase, new() {
        protected ILog logger = SystemUtils.GetLogger(typeof(T));

        private string _tabelName;
        private DbConnection? _conn;
        private DbTransaction? _transaction;
        private const int commandTimeout = 10;

        // 允许的ORDER BY字段白名单（从实体类反射获取）
        private static readonly HashSet<string> AllowedOrderByFields = new(StringComparer.OrdinalIgnoreCase);

        public string TableName { get => _tabelName; }

        public AWrapperBase() {
            _tabelName = GetTableName();
            InitializeAllowedFields();
        }

        /// <summary>
        /// 从实体类T的属性中获取允许的ORDER BY字段
        /// </summary>
        private static void InitializeAllowedFields() {
            var properties = typeof(T).GetProperties().Select(p => p.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
            AllowedOrderByFields.Clear();
            AllowedOrderByFields.UnionWith(properties);
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

        #region Pagination Methods

        /// <summary>
        /// Finds all records with pagination support.
        /// </summary>
        /// <param name="pagination">Pagination parameters.</param>
        /// <returns>A paged result containing the data and pagination metadata.</returns>
        public PagedResult<T> FindWithPagination(PaginationParams pagination) {
            string whereClause = ConditionWithoutUserId();
            return FindWithPagination(whereClause, null, pagination);
        }

        /// <summary>
        /// Finds records matching the WHERE clause with pagination support.
        /// </summary>
        /// <param name="whereClause">The WHERE clause without 'WHERE' keyword.</param>
        /// <param name="params">Optional parameters for the WHERE clause.</param>
        /// <param name="pagination">Pagination parameters.</param>
        /// <returns>A paged result containing the data and pagination metadata.</returns>
        public PagedResult<T> FindWithPagination(string whereClause, Dictionary<string, object>? @params, PaginationParams pagination) {
            // 【参数验证】确保分页参数的有效性
            if (pagination.PageNumber < 1) {
                logger.Warn($"Invalid page number: {pagination.PageNumber}, using default value 1");
                pagination.PageNumber = 1;
            }
            if (pagination.PageSize < 1) {
                logger.Warn($"Invalid page size: {pagination.PageSize}, using default value 10");
                pagination.PageSize = 10;
            }
            if (pagination.PageSize > 1000) {
                logger.Warn($"Page size exceeds maximum (1000): {pagination.PageSize}, using default value 10");
                pagination.PageSize = 10;
            }

            PagedResult<T> result = new() {
                PageNumber = pagination.PageNumber,
                PageSize = pagination.PageSize
            };

            try {
                string baseSql = $"SELECT * FROM {_tabelName} WHERE {whereClause}";
                result = FindWithPaginationBySql(baseSql, @params, pagination);
            } catch (Exception e) {
                logger.Warn($"FindWithPagination error: {e}");
            }

            return result;
        }

        /// <summary>
        /// Executes a custom SQL query with pagination support.
        /// </summary>
        /// <param name="baseSql">The base SQL query (without pagination clauses).</param>
        /// <param name="params">Optional parameters for the query.</param>
        /// <param name="pagination">Pagination parameters.</param>
        /// <returns>A paged result containing the data and pagination metadata.</returns>
        public PagedResult<T> FindWithPaginationBySql(string baseSql, Dictionary<string, object>? @params, PaginationParams pagination) {
            PagedResult<T> result = new() {
                PageNumber = pagination.PageNumber,
                PageSize = pagination.PageSize
            };

            try {
                // Get total count
                result.TotalCount = GetTotalCount(baseSql, @params);

                // Build pagination SQL
                string paginatedSql = BuildPaginationSqlFromBase(baseSql, pagination);

                // Prepare parameters with pagination values
                var sqlParams = @params != null ? new Dictionary<string, object>(@params) : new Dictionary<string, object>();
                sqlParams["Offset"] = pagination.Offset;
                sqlParams["PageSize"] = pagination.PageSize;

                // Execute query
                result.Data = FindBySqlWithParams(paginatedSql, sqlParams);

                logger.Info($"FindWithPaginationBySql: Page {pagination.PageNumber}, Size {pagination.PageSize}, Total {result.TotalCount}, Returned {result.Data.Count}");
            } catch (Exception e) {
                logger.Error($"Pagination query failed - Page: {pagination.PageNumber}, Size: {pagination.PageSize}, OrderBy: {pagination.OrderBy}, Descending: {pagination.Descending}, Base SQL: {baseSql}", e);
                throw new InvalidOperationException($"Pagination query failed for page {pagination.PageNumber} with size {pagination.PageSize}: {e.Message}", e);
            }

            return result;
        }

        /// <summary>
        /// Executes a custom SQL query with pagination support (simplified parameters).
        /// This method is more convenient for service layers that need to join multiple tables.
        /// </summary>
        /// <param name="baseSql">The base SQL query (can include JOINs, without pagination clauses).</param>
        /// <param name="params">Optional parameters for the query.</param>
        /// <param name="pageNumber">Page number (1-based).</param>
        /// <param name="pageSize">Number of records per page.</param>
        /// <param name="orderBy">Order by clause (e.g., "id DESC" or just "id").</param>
        /// <returns>A paged result containing the data and pagination metadata.</returns>
        public PagedResult<T> FindWithPaginationBySql(string baseSql, Dictionary<string, object>? @params, int pageNumber, int pageSize, string? orderBy = null) {
            // Parse and validate the orderBy clause (e.g., "id DESC" -> orderBy="id", descending=true)
            string validatedOrderBy;
            bool descending;

            if (string.IsNullOrWhiteSpace(orderBy)) {
                validatedOrderBy = "id";
                descending = false;
            } else {
                string[] parts = orderBy.Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                string fieldName = parts[0];
                string direction = parts.Length > 1 ? parts[1].ToUpper() : "ASC";

                // Validate the field name
                validatedOrderBy = ValidateOrderByField(fieldName);
                descending = direction == "DESC";
            }

            var pagination = new PaginationParams {
                PageNumber = pageNumber,
                PageSize = pageSize,
                OrderBy = validatedOrderBy,
                Descending = descending
            };

            return FindWithPaginationBySql(baseSql, @params, pagination);
        }

        /// <summary>
        /// Validates and sanitizes an ORDER BY field name to prevent SQL injection.
        /// </summary>
        /// <param name="fieldName">The field name to validate.</param>
        /// <returns>A validated field name.</returns>
        protected string ValidateOrderByField(string fieldName) {
            if (string.IsNullOrWhiteSpace(fieldName)) {
                return "id";
            }

            // 【强化安全检查】防止SQL注入
            // 只允许字母、数字、下划线，不允许其他特殊字符
            string sanitized = fieldName.Trim();

            // 字段白名单验证 - 确保orderBy字段是实体类的有效属性
            if (!AllowedOrderByFields.Contains(sanitized)) {
                logger.Warn($"Invalid ORDER BY column: {fieldName}. Allowed columns: {string.Join(", ", AllowedOrderByFields)}. Using default 'id'.");
                return "id";
            }

            // 严格正则验证 - 只允许字母、数字、下划线作为字段名
            if (!Regex.IsMatch(sanitized, @"^[a-zA-Z_][a-zA-Z0-9_]*$", RegexOptions.IgnoreCase)) {
                logger.Warn($"Invalid ORDER BY clause format: {fieldName}. Only letters, numbers and underscore are allowed. Using default 'id'.");
                return "id";
            }

            return sanitized;
        }

        /// <summary>
        /// Builds pagination SQL based on the current database type.
        /// </summary>
        /// <param name="orderBy">The ORDER BY clause (e.g., "id ASC").</param>
        /// <returns>The pagination SQL fragment.</returns>
        protected string BuildPaginationSql(string? orderBy = null) {
            string orderClause = BuildOrderByClause(orderBy);
            DBTypes dbType = SystemUtils.GetDBTypes();

            return dbType switch {
                DBTypes.SQLSERVER => BuildSqlServerPagination(orderClause),
                DBTypes.MYSQL => BuildMySqlPagination(orderClause),
                DBTypes.SQLITE => BuildSqlitePagination(orderClause),
                _ => BuildSqlitePagination(orderClause)
            };
        }

        /// <summary>
        /// Builds a complete paginated SQL query from a base SQL statement.
        /// </summary>
        /// <param name="baseSql">The base SQL query.</param>
        /// <param name="pagination">Pagination parameters.</param>
        /// <returns>The complete paginated SQL query.</returns>
        protected string BuildPaginationSqlFromBase(string baseSql, PaginationParams pagination) {
            string orderBy = pagination.OrderBy ?? "id";
            string direction = pagination.Descending ? "DESC" : "ASC";
            string orderClause = $"{orderBy} {direction}";

            // Remove any existing ORDER BY clause from the base SQL
            string cleanedSql = RemoveOrderByClause(baseSql);

            DBTypes dbType = SystemUtils.GetDBTypes();

            return dbType switch {
                DBTypes.SQLSERVER => $"{cleanedSql} {BuildSqlServerPagination(orderClause)}",
                DBTypes.MYSQL => $"{cleanedSql} {BuildMySqlPagination(orderClause)}",
                DBTypes.SQLITE => $"{cleanedSql} {BuildSqlitePagination(orderClause)}",
                _ => $"{cleanedSql} {BuildSqlitePagination(orderClause)}"
            };
        }

        /// <summary>
        /// Builds SQL Server specific pagination clause.
        /// Uses OFFSET...FETCH syntax (SQL Server 2012+).
        /// </summary>
        private string BuildSqlServerPagination(string orderClause) {
            return $"ORDER BY {orderClause} OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";
        }

        /// <summary>
        /// Builds MySQL specific pagination clause.
        /// Uses LIMIT...OFFSET syntax.
        /// </summary>
        private string BuildMySqlPagination(string orderClause) {
            return $"ORDER BY {orderClause} LIMIT @PageSize OFFSET @Offset";
        }

        /// <summary>
        /// Builds SQLite specific pagination clause.
        /// Uses LIMIT...OFFSET syntax (same as MySQL).
        /// </summary>
        private string BuildSqlitePagination(string orderClause) {
            return $"ORDER BY {orderClause} LIMIT @PageSize OFFSET @Offset";
        }

        /// <summary>
        /// Builds an ORDER BY clause from the given column specification.
        /// </summary>
        /// <param name="orderBy">The order specification (e.g., "name DESC" or just "name").</param>
        /// <returns>A valid ORDER BY clause value.</returns>
        private string BuildOrderByClause(string? orderBy) {
            if (string.IsNullOrWhiteSpace(orderBy)) {
                return "id ASC";
            }

            // 【强化安全检查】防止SQL注入
            // 只允许字母、数字、下划线，不允许其他特殊字符
            string sanitized = orderBy.Trim();

            // 字段白名单验证 - 确保orderBy字段是实体类的有效属性
            if (!AllowedOrderByFields.Contains(sanitized)) {
                logger.Warn($"Invalid ORDER BY column: {orderBy}. Allowed columns: {string.Join(", ", AllowedOrderByFields)}. Using default 'id ASC'.");
                return "id ASC";
            }

            // 严格正则验证 - 只允许字母、数字、下划线作为字段名
            if (!Regex.IsMatch(sanitized, @"^[a-zA-Z_][a-zA-Z0-9_]*$", RegexOptions.IgnoreCase)) {
                logger.Warn($"Invalid ORDER BY clause format: {orderBy}. Only letters, numbers and underscore are allowed. Using default 'id ASC'.");
                return "id ASC";
            }

            return sanitized + " ASC";
        }

        /// <summary>
        /// Gets the total count of records matching the base SQL query.
        /// </summary>
        /// <param name="baseSql">The base SQL query.</param>
        /// <param name="params">Optional parameters for the query.</param>
        /// <returns>The total count of matching records.</returns>
        private int GetTotalCount(string baseSql, Dictionary<string, object>? @params) {
            try {
                // Remove ORDER BY clause for count query
                string cleanedSql = RemoveOrderByClause(baseSql);

                // Build count query by wrapping the base SQL
                string countSql = $"SELECT COUNT(*) FROM ({cleanedSql}) AS CountQuery";

                // Log count SQL and parameters for debugging (use Debug level to reduce noise in production)
                logger.Debug($"Count SQL: {countSql}");
                logger.Debug($"Count Params: {GetParamsStr(@params)}");

                int count;
                if (_conn == null) {
                    using (DbConnection conn = DbConnector.GetConnection()) {
                        count = conn.QueryFirst<int>(countSql, @params, commandTimeout: commandTimeout);
                    }
                } else {
                    count = _conn.QueryFirst<int>(countSql, @params, _transaction, commandTimeout: commandTimeout);
                }

                logger.Info($"Total count result: {count}");
                return count;
            } catch (Exception e) {
                // Improved error handling: log detailed info and rethrow exception
                logger.Error($"GetTotalCount failed. SQL: {baseSql}, Params: {GetParamsStr(@params)}", e);
                throw new InvalidOperationException($"Failed to get total count: {e.Message}", e);
            }
        }

        /// <summary>
        /// Removes the ORDER BY clause from a SQL statement.
        /// </summary>
        /// <param name="sql">The SQL statement.</param>
        /// <returns>The SQL statement without the ORDER BY clause.</returns>
        private string RemoveOrderByClause(string sql) {
            // Match ORDER BY clause at the end of the SQL (case-insensitive)
            // This regex handles ORDER BY with column names, directions, and multiple columns
            string pattern = @"\s+ORDER\s+BY\s+[\w\s,\.]+(?:\s+(?:ASC|DESC))?(?:\s*,\s*[\w\s\.]+(?:\s+(?:ASC|DESC))?)*\s*$";
            return Regex.Replace(sql, pattern, "", RegexOptions.IgnoreCase).Trim();
        }

        /// <summary>
        /// Internal method to execute SQL with parameters (used by pagination).
        /// </summary>
        private List<T> FindBySqlWithParams(string sql, Dictionary<string, object> @params) {
            List<T> result = new();
            try {
                logger.Info($"sql: [{sql}], @params: [{GetParamsStr(@params)}]");
                IEnumerable<T> enumerable;
                if (_conn == null) {
                    using (DbConnection conn = DbConnector.GetConnection()) {
                        enumerable = conn.Query<T>(sql, @params, commandTimeout: commandTimeout);
                    }
                } else {
                    enumerable = _conn.Query<T>(sql, @params, _transaction, commandTimeout: commandTimeout);
                }
                result = enumerable.ToList();

                logger.Info("Size of result: " + result.Count);
            } catch (Exception e) {
                logger.Warn($"FindBySqlWithParams error: {e}");
            }
            return result;
        }

        #endregion
    }
}
