using OperationGuidance_service.Attributes;
using OperationGuidance_service.Constants;
using OperationGuidance_service.Models.AbstractClasses;
using OperationGuidance_service.Utils;
using OperationGuidance_service.Wrapper.AbstractClasses;
using System.Data.SQLite;

namespace OperationGuidance_service.Services.AbstractClasses {
    [Service]
    public abstract class AServiceBase<T, E> where T : AEntityBase, new() where E : AWrapperBase<T> {
        [Autowired]
        protected E Wrapper {
            set; get;
        }

        public void UseConnection(SQLiteConnection conn) {
            Wrapper.UseConnection(conn);
        }
        public void ReleaseConnection() { 
            Wrapper.ReleaseConnection();
        }

        public T? AddEntity(T entity) {
            return this.Wrapper.Add(entity);
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

        public List<T> FindBySqlCondition(string sqlCondition) {
            return Wrapper.FindBySql($"select * from {Wrapper.TabelName} where {Wrapper.CommonCondition()} and " + sqlCondition, new { @user_id = SystemUtils.LoggedUserId() });
        }
    }
}
