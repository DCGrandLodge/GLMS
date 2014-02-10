using System;
using System.Linq;
using System.Data;
using System.Collections.Generic;

namespace EntityFramework.Mapping
{
    /// <summary>
    /// Convert a base data type to another base data type
    /// </summary>
    public static class DbTypeConversion
    {
        private class DbTypeMapEntry
        {
            public Type Type;
            public DbType DbType;
            public SqlDbType SqlDbType;
            public DbTypeMapEntry(Type type, DbType dbType, SqlDbType sqlDbType)
            {
                this.Type = type;
                this.DbType = dbType;
                this.SqlDbType = sqlDbType;
            }
        };
        private static List<DbTypeMapEntry> dbTypeList = new List<DbTypeMapEntry>();

        #region Constructors
        static DbTypeConversion()
        {
            dbTypeList.Add(new DbTypeMapEntry(typeof(bool), DbType.Boolean, SqlDbType.Bit));
            dbTypeList.Add(new DbTypeMapEntry(typeof(byte), DbType.Double, SqlDbType.TinyInt));
            dbTypeList.Add(new DbTypeMapEntry(typeof(byte[]), DbType.Binary, SqlDbType.Image));
            dbTypeList.Add(new DbTypeMapEntry(typeof(DateTime), DbType.DateTime, SqlDbType.DateTime));
            dbTypeList.Add(new DbTypeMapEntry(typeof(Decimal), DbType.Decimal, SqlDbType.Decimal));
            dbTypeList.Add(new DbTypeMapEntry(typeof(double), DbType.Double, SqlDbType.Float));
            dbTypeList.Add(new DbTypeMapEntry(typeof(Guid), DbType.Guid, SqlDbType.UniqueIdentifier));
            dbTypeList.Add(new DbTypeMapEntry(typeof(Int16), DbType.Int16, SqlDbType.SmallInt));
            dbTypeList.Add(new DbTypeMapEntry(typeof(Int32), DbType.Int32, SqlDbType.Int));
            dbTypeList.Add(new DbTypeMapEntry(typeof(Int64), DbType.Int64, SqlDbType.BigInt));
            dbTypeList.Add(new DbTypeMapEntry(typeof(object), DbType.Object, SqlDbType.Variant));
            dbTypeList.Add(new DbTypeMapEntry(typeof(string), DbType.String, SqlDbType.VarChar));
            dbTypeList.Add(new DbTypeMapEntry(typeof(DBNull), DbType.Object, SqlDbType.Variant));
        }

        #endregion

        #region Methods
        /// <summary>
        /// Convert db type to .Net data type
        /// </summary>
        /// <param name="dbType"></param>
        /// <returns></returns>
        public static Type ToNetType(DbType dbType)
        {
            DbTypeMapEntry entry = Find(dbType);
            return entry.Type;
        }

        /// <summary>
        /// Convert TSQL type to .Net data type
        /// </summary>
        /// <param name="sqlDbType"></param>
        /// <returns></returns>
        public static Type ToNetType(SqlDbType sqlDbType)
        {
            DbTypeMapEntry entry = Find(sqlDbType);
            return entry.Type;
        }
        /// <summary>
        /// Convert .Net type to Db type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static DbType ToDbType(Type type)
        {
            DbTypeMapEntry entry = Find(type);
            return entry.DbType;
        }
        /// <summary>
        /// Convert TSQL data type to DbType
        /// </summary>
        /// <param name="sqlDbType"></param>
        /// <returns></returns>
        public static DbType ToDbType(SqlDbType sqlDbType)
        {
            DbTypeMapEntry entry = Find(sqlDbType);
            return entry.DbType;
        }

        /// <summary>
        /// Convert .Net type to TSQL data type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static SqlDbType ToSqlDbType(Type type)
        {
            DbTypeMapEntry entry = Find(type);
            return entry.SqlDbType;
        }

        /// <summary>
        /// Convert DbType type to TSQL data type
        /// </summary>
        /// <param name="dbType"></param>
        /// <returns></returns>
        public static SqlDbType ToSqlDbType(DbType dbType)
        {
            DbTypeMapEntry entry = Find(dbType);
            return entry.SqlDbType;
        }

        private static DbTypeMapEntry Find(Type type)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                type = type.GetGenericArguments()[0];
            }
            DbTypeMapEntry result = dbTypeList.FirstOrDefault(x => x.Type == type);
            if(result==null)
            {
                throw new ApplicationException("Referenced an unsupported Type");
            }
            return result;
        }
        private static DbTypeMapEntry Find(DbType dbType)
        {
            DbTypeMapEntry result = dbTypeList.FirstOrDefault(x => x.DbType == dbType);
            if (result == null)
            {
                throw new ApplicationException("Referenced an unsupported DbType");
            }
            return result;

        }
        private static DbTypeMapEntry Find(SqlDbType sqlDbType)
        {
            DbTypeMapEntry result = dbTypeList.FirstOrDefault(x => x.SqlDbType == sqlDbType);
            if (result == null)
            {
                throw new ApplicationException("Referenced an unsupported SqlDbType");
            }
            return result;
        }
        #endregion
    }
}
