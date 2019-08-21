using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bee.Util;

namespace Bee.Data
{
    public abstract class BeeObject<T> where T : class
    {
        public void Delete(SqlCriteria sqlCriteria)
        {
            DbSession.Current.Delete<T>(sqlCriteria);
        }

        public int Save()
        {
            return DbSession.Current.Save(this);
        }

        public int Insert()
        {
            return DbSession.Current.Insert(this, true);
        }

        public int Update()
        {
            return DbSession.Current.Save(this);
        }

        public List<T> Query()
        {
            return DbSession.Current.Query<T>();
        }

        public List<T> Query(SqlCriteria sqlCriterial)
        {
            return DbSession.Current.Query<T>(sqlCriterial);
        }

        public List<T> Query(SqlCriteria sqlCriterial, string orderbyClause, int pageIndex, int pageSize, ref int recordCount)
        {
            return DbSession.Current.Query<T>(sqlCriterial, orderbyClause, pageIndex, pageSize, ref recordCount);
        }

        public T Clone()
        {
            return ConvertUtil.CommonClone<T>(this);
        }
    }
}
