using Bee.Data;
using Bee.Logging;
using Bee.Util;
using Bee.Web;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bee.Web
{
    public interface IAPIModel
    {
        void Validate();
    }

    public abstract class APIControllerBase : ControllerBase
    {

    }

    public abstract class APIControllerBase<T> : ControllerBase<T> where T : class
    {
        public virtual BeeDataList<T> GetList(BeeDataAdapter dataAdapter)
        {
            DbSession dbSession = GetDbSession();

            DataTable dataTable = null;
            try
            {
                InitPagePara(dataAdapter);
                string tableName = OrmUtil.GetTableName<T>();

                SqlCriteria sqlCriteria = GetQueryCondition(dataAdapter);

                int recordCount = dataAdapter.TryGetValue<int>("recordcount", 0);

                string selectClause = GetQuerySelectClause(typeof(T));

                dataTable = InnerQuery(tableName, selectClause, dataAdapter, sqlCriteria);

                recordCount = dataAdapter.TryGetValue<int>("recordcount", 0);
                List<T> dataList = ConvertUtil.ConvertDataToObject<T>(dataTable);

                return new BeeDataList<T>() { Total = recordCount, Items = dataList };

            }
            catch (Exception e)
            {
                Logger.Error("GetList object({0}) Error".FormatWith(typeof(T)), e);
                throw;
            }
            finally
            {
                dbSession.Dispose();
            }

        }

        public virtual T GetById(int id)
        {
            try
            { 
                using (DbSession dbSession = GetDbSession())
                {
                    T result =
                        dbSession.Query<T>(SqlCriteria.New.Equal(OrmUtil.GetIdentityColumnName<T>(), id)).FirstOrDefault();

                    return result;
                }
            }
            catch (Exception e)
            {
                Logger.Error("GetById ({0}) Error".FormatWith(typeof(T)), e);
                throw;
            }
        }
    }

    public class BeeDataList<T> where T : class
    {
        public int Total { get; set; }
        public List<T> Items { get; set; }
    }
}
