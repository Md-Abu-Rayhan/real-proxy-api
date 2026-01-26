using Dapper;
using MySqlConnector;
using System.Data;

namespace real_proxy_api.Infrastructure
{
    public class MySqlDateTimeTypeHandler : SqlMapper.TypeHandler<DateTime>
    {
        public override void SetValue(IDbDataParameter parameter, DateTime value)
        {
            parameter.Value = value;
        }

        public override DateTime Parse(object value)
        {
            if (value is MySqlDateTime mdt)
            {
                if (mdt.IsValidDateTime)
                    return mdt.GetDateTime();
                return default;
            }
            if (value is DateTime dt)
            {
                return dt;
            }
            return Convert.ToDateTime(value);
        }
    }

    public class NullableMySqlDateTimeTypeHandler : SqlMapper.TypeHandler<DateTime?>
    {
        public override void SetValue(IDbDataParameter parameter, DateTime? value)
        {
            parameter.Value = value;
        }

        public override DateTime? Parse(object value)
        {
            if (value == null || value is DBNull) return null;
            
            if (value is MySqlDateTime mdt)
            {
                if (mdt.IsValidDateTime)
                    return mdt.GetDateTime();
                return null;
            }
            if (value is DateTime dt)
            {
                return dt;
            }
            return null;
        }
    }
}
