using Newtonsoft.Json;
using System.Data.Common;
using System.Data.SQLite;
using OperationGuidance_service.Attributes;
using OperationGuidance_service.Utils;

namespace OperationGuidance_service.Services {
    public class LocalDataCache {
        private readonly string _dbPath;
        private readonly string _connectionString;
        private int _backoffMs = 0;
        private const int BaseBackoffMs = 500;
        private const int MaxBackoffMs = 30000;
        private const double BackoffMultiplier = 1.5;

        public LocalDataCache() {
            string cacheDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "OperationGuidance", "cache");
            Directory.CreateDirectory(cacheDir);
            _dbPath = Path.Combine(cacheDir, "pending_data.db");
            _connectionString = $"Data source={_dbPath}; UseUTF16Encoding=True; Connection Timeout=5;";
            InitTable();
        }

        private void InitTable() {
            using var conn = new SQLiteConnection(_connectionString);
            conn.Open();
            // WAL mode — allows concurrent reads with a single writer (no "database is locked")
            using (var pragma = conn.CreateCommand()) {
                pragma.CommandText = "PRAGMA journal_mode=WAL;";
                pragma.ExecuteNonQuery();
            }
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS pending_data (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    table_name TEXT NOT NULL,
                    entity_json TEXT NOT NULL,
                    created_at TEXT NOT NULL,
                    retry_count INTEGER DEFAULT 0
                )";
            cmd.ExecuteNonQuery();
        }

        /// <summary>INSERT 失败时将实体序列化并写入本地缓存。</summary>
        public void Enqueue(string tableName, object entity) {
            string json = JsonConvert.SerializeObject(entity);
            using var conn = new SQLiteConnection(_connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"INSERT INTO pending_data (table_name, entity_json, created_at)
                VALUES (@table, @json, @created)";
            AddParameter(cmd, "@table", tableName);
            AddParameter(cmd, "@json", json);
            AddParameter(cmd, "@created", DateTime.Now.ToString("O"));
            cmd.ExecuteNonQuery();
        }

        /// <summary>取出最多 maxCount 条指定表的待重试数据，按入队时间升序。</summary>
        public List<(int Id, string TableName, string EntityJson)> DequeuePending(int maxCount, string tableName) {
            var result = new List<(int, string, string)>();
            using var conn = new SQLiteConnection(_connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT id, table_name, entity_json FROM pending_data WHERE table_name = @table ORDER BY id ASC LIMIT @limit";
            AddParameter(cmd, "@table", tableName);
            AddParameter(cmd, "@limit", maxCount);
            using var reader = cmd.ExecuteReader();
            while (reader.Read()) {
                result.Add((reader.GetInt32(0), reader.GetString(1), reader.GetString(2)));
            }
            return result;
        }

        /// <summary>重试成功后删除缓存记录。</summary>
        public void ConfirmDequeued(List<int> ids) {
            if (ids.Count == 0) return;
            using var conn = new SQLiteConnection(_connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"DELETE FROM pending_data WHERE id IN ({string.Join(",", ids)})";
            cmd.ExecuteNonQuery();
        }

        public bool IsEmpty => Count == 0;

        public int Count {
            get {
                using var conn = new SQLiteConnection(_connectionString);
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT COUNT(*) FROM pending_data";
                return Convert.ToInt32((long)cmd.ExecuteScalar()!);
            }
        }

        public int GetBackoffMs() => _backoffMs;

        public void ResetBackoff() => _backoffMs = 0;

        public void IncreaseBackoff() {
            if (_backoffMs == 0) _backoffMs = BaseBackoffMs;
            else _backoffMs = Math.Min((int)(_backoffMs * BackoffMultiplier), MaxBackoffMs);
        }

        private static void AddParameter(SQLiteCommand cmd, string name, object value) {
            var p = cmd.CreateParameter();
            p.ParameterName = name;
            p.Value = value;
            cmd.Parameters.Add(p);
        }
    }
}
