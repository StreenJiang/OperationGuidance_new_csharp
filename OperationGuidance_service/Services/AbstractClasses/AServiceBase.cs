using OperationGuidance_service.Attributes;
using OperationGuidance_service.Constants;
using OperationGuidance_service.Models.AbstractClasses;
using OperationGuidance_service.Utils;
using OperationGuidance_service.Wrapper.AbstractClasses;
using System.Data.Common;

namespace OperationGuidance_service.Services.AbstractClasses {
    [Service]
    public abstract class AServiceBase<T, E> where T : AEntityBase, new() where E : AWrapperBase<T> {
        [Autowired]
        protected E Wrapper {
            set; get;
        }
        public string TableName => Wrapper.TableName;
        public string ConditionWithoutUserId => Wrapper.ConditionWithoutUserId();

        public void UseConnection(DbConnection conn) {
            Wrapper.UseConnection(conn);
        }
        public void ReleaseConnection() { 
            Wrapper.ReleaseConnection();
        }

        public List<T> QueryList(int userId) {
            // Validate each parameter
            ArgumentValidator.ValidateInt(userId, "UserId should greater than 0. Passing 'userId = " + userId + "' incorrectly.");

            // TODO: use cache to prevent fetching data every time
            return Wrapper.FindBySql($"select * from {Wrapper.TableName} where {Wrapper.CommonCondition()}", new() { { "@user_id", userId } });
        }
        public List<T> QueryListWithoutUserId() {
            // TODO: use cache to prevent fetching data every time
            return Wrapper.FindBySql($"select * from {Wrapper.TableName} where {Wrapper.ConditionWithoutUserId()}");
        }

        public T? AddEntity(T entity) {
            return this.Wrapper.Add(entity);
        }

        public int AddBatch(List<T> entities) {
            return this.Wrapper.AddBatch(entities);
        }

        public T? UpdateEntity(T entity) {
            return this.Wrapper.Update(entity);
        }

        public bool DeleteEntity(T entity) {
            return this.Wrapper.Delete(entity);
        }

        public T? FindById(int id) {
            T? t = this.Wrapper.FindById(id);
            if (t == null || t.deleted == (int) YesOrNo.YES) {
                return null;
            }
            return t;
        }

        public T? InsertOrUpdate(T entity) {
            if (entity.id > 0) {
                return UpdateEntity(entity);
            } else {
                return AddEntity(entity);
            }
        }

        public int DeleteByIds(List<int> ids) {
            return Wrapper.DeleteByIds(ids);
        }

        public List<T> FindBySqlWithoutUserId(string? sqlCondition) {
            if (!string.IsNullOrEmpty(sqlCondition)) {
                return Wrapper.FindBySql($"select * from {Wrapper.TableName} where {Wrapper.ConditionWithoutUserId()} and " + sqlCondition);
            }
            return QueryListWithoutUserId();
        }

        public List<T> FindBySql(string sql) {
            return Wrapper.FindBySql(sql);
        }

        public List<T> FindBySql(string sql, Dictionary<string, object>? parameterObj) {
            return Wrapper.FindBySql(sql, parameterObj);
        }
    }
}
