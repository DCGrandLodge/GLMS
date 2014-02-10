using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Dynamic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using EntityFramework.Mapping;
using EntityFramework.Reflection;
using System.Collections;
using System.Data;
using System.Transactions;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Core.EntityClient;

namespace EntityFramework.Extensions
{
    /// <summary>
    /// An extensions class for batch queries.
    /// </summary>
    public static class BatchExtensions
    {
        /// <summary>
        /// Executes a delete statement using the query to filter the rows to be deleted.
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity.</typeparam>
        /// <param name="source">The source used to determine the table to delete from.</param>
        /// <param name="query">The IQueryable used to generate the where clause for the delete statement.</param>
        /// <returns>The number of row deleted.</returns>
        /// <remarks>
        /// When executing this method, the statement is immediately executed on the database provider
        /// and is not part of the change tracking system.  Also, changes will not be reflected on 
        /// any entities that have already been materialized in the current context.        
        /// </remarks>
        public static int Delete<TEntity>(
            this ObjectSet<TEntity> source,
            IQueryable<TEntity> query)
            where TEntity : class
        {
            if (source == null)
                throw new ArgumentNullException("source");

            if (query == null)
                throw new ArgumentNullException("query");

            ObjectContext objectContext = source.Context;
            if (objectContext == null)
                throw new ArgumentException("The ObjectContext for the source query can not be null.", "source");

            EntityMap entityMap = source.GetEntityMap<TEntity>();
            if (entityMap == null)
                throw new ArgumentException("Could not load the entity mapping information for the source ObjectSet.", "source");

            ObjectQuery<TEntity> objectQuery = query.ToObjectQuery();
            if (objectQuery == null)
                throw new ArgumentException("The query must be of type ObjectQuery or DbQuery.", "query");

            return Delete(objectContext, entityMap, objectQuery);
        }

        /// <summary>
        /// Executes a delete statement using an expression to filter the rows to be deleted.
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity.</typeparam>
        /// <param name="source">The source used to determine the table to delete from.</param>
        /// <param name="filterExpression">The filter expression used to generate the where clause for the delete statement.</param>
        /// <returns>The number of row deleted.</returns>
        /// <example>Delete all users with email domain @test.com.
        /// <code><![CDATA[
        /// var db = new TrackerEntities();
        /// string emailDomain = "@test.com";
        /// int count = db.Users.Delete(u => u.Email.EndsWith(emailDomain));
        /// ]]></code>
        /// </example>
        /// <remarks>
        /// When executing this method, the statement is immediately executed on the database provider
        /// and is not part of the change tracking system.  Also, changes will not be reflected on 
        /// any entities that have already been materialized in the current context.        
        /// </remarks>
        public static int Delete<TEntity>(
            this ObjectSet<TEntity> source,
            Expression<Func<TEntity, bool>> filterExpression)
            where TEntity : class
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (filterExpression == null)
                throw new ArgumentNullException("filterExpression");

            return source.Delete(source.Where(filterExpression));
        }

        /// <summary>
        /// Executes a delete statement using the query to filter the rows to be deleted.
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity.</typeparam>
        /// <param name="source">The source used to determine the table to delete from.</param>
        /// <param name="query">The IQueryable used to generate the where clause for the delete statement.</param>
        /// <returns>The number of row deleted.</returns>
        /// <remarks>
        /// When executing this method, the statement is immediately executed on the database provider
        /// and is not part of the change tracking system.  Also, changes will not be reflected on 
        /// any entities that have already been materialized in the current context.        
        /// </remarks>
        public static int Delete<TEntity>(
           this DbSet<TEntity> source,
           IQueryable<TEntity> query)
           where TEntity : class
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (query == null)
                throw new ArgumentNullException("query");

            ObjectQuery<TEntity> sourceQuery = source.ToObjectQuery();
            if (sourceQuery == null)
                throw new ArgumentException("The query must be of type ObjectQuery or DbQuery.", "source");

            ObjectContext objectContext = sourceQuery.Context;
            if (objectContext == null)
                throw new ArgumentException("The ObjectContext for the source query can not be null.", "source");

            EntityMap entityMap = sourceQuery.GetEntityMap<TEntity>();
            if (entityMap == null)
                throw new ArgumentException("Could not load the entity mapping information for the source ObjectSet.", "source");

            ObjectQuery<TEntity> objectQuery = query.ToObjectQuery();
            if (objectQuery == null)
                throw new ArgumentException("The query must be of type ObjectQuery or DbQuery.", "query");

            return Delete(objectContext, entityMap, objectQuery);
        }

        /// <summary>
        /// Executes a delete statement using an expression to filter the rows to be deleted.
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity.</typeparam>
        /// <param name="source">The source used to determine the table to delete from.</param>
        /// <param name="filterExpression">The filter expression used to generate the where clause for the delete statement.</param>
        /// <returns>The number of row deleted.</returns>
        /// <example>Delete all users with email domain @test.com.
        /// <code><![CDATA[
        /// var db = new TrackerContext();
        /// string emailDomain = "@test.com";
        /// int count = db.Users.Delete(u => u.Email.EndsWith(emailDomain));
        /// ]]></code>
        /// </example>
        /// <remarks>
        /// When executing this method, the statement is immediately executed on the database provider
        /// and is not part of the change tracking system.  Also, changes will not be reflected on 
        /// any entities that have already been materialized in the current context.        
        /// </remarks>
        public static int Delete<TEntity>(
            this DbSet<TEntity> source,
            Expression<Func<TEntity, bool>> filterExpression)
            where TEntity : class
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (filterExpression == null)
                throw new ArgumentNullException("filterExpression");

            return source.Delete(source.Where(filterExpression));
        }


        /// <summary>
        /// Executes an update statement using the query to filter the rows to be updated.
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity.</typeparam>
        /// <param name="source">The source used to determine the table to update.</param>
        /// <param name="query">The query used to generate the where clause.</param>
        /// <param name="updateExpression">The MemberInitExpression used to indicate what is updated.</param>
        /// <returns>The number of row updated.</returns>
        /// <remarks>
        /// When executing this method, the statement is immediately executed on the database provider
        /// and is not part of the change tracking system.  Also, changes will not be reflected on 
        /// any entities that have already been materialized in the current context.        
        /// </remarks>
        public static int Update<TEntity>(
            this ObjectSet<TEntity> source,
            IQueryable<TEntity> query,
            Expression<Func<TEntity, TEntity>> updateExpression)
            where TEntity : class
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (query == null)
                throw new ArgumentNullException("query");
            if (updateExpression == null)
                throw new ArgumentNullException("updateExpression");

            ObjectContext objectContext = source.Context;
            if (objectContext == null)
                throw new ArgumentException("The ObjectContext for the source query can not be null.", "source");

            EntityMap entityMap = source.GetEntityMap<TEntity>();
            if (entityMap == null)
                throw new ArgumentException("Could not load the entity mapping information for the source ObjectSet.", "source");

            ObjectQuery<TEntity> objectQuery = query.ToObjectQuery();
            if (objectQuery == null)
                throw new ArgumentException("The query must be of type ObjectQuery or DbQuery.", "query");

            return Update(objectContext, entityMap, objectQuery, updateExpression);
        }

        /// <summary>
        /// Executes an update statement using an expression to filter the rows that are updated.
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity.</typeparam>
        /// <param name="source">The source used to determine the table to update.</param>
        /// <param name="filterExpression">The filter expression used to generate the where clause.</param>
        /// <param name="updateExpression">The MemberInitExpression used to indicate what is updated.</param>
        /// <returns>The number of row updated.</returns>
        /// <example>Update all users in the test.com domain to be inactive.
        /// <code><![CDATA[
        /// var db = new TrackerEntities();
        /// string emailDomain = "@test.com";
        /// int count = db.Users.Update(
        ///   u => u.Email.EndsWith(emailDomain),
        ///   u => new User { IsApproved = false, LastActivityDate = DateTime.Now });
        /// ]]></code>
        /// </example>
        /// <remarks>
        /// When executing this method, the statement is immediately executed on the database provider
        /// and is not part of the change tracking system.  Also, changes will not be reflected on 
        /// any entities that have already been materialized in the current context.        
        /// </remarks>
        public static int Update<TEntity>(
            this ObjectSet<TEntity> source,
            Expression<Func<TEntity, bool>> filterExpression,
            Expression<Func<TEntity, TEntity>> updateExpression)
            where TEntity : class
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (filterExpression == null)
                throw new ArgumentNullException("filterExpression");

            return source.Update(source.Where(filterExpression), updateExpression);
        }

        /// <summary>
        /// Executes an update statement using the query to filter the rows to be updated.
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity.</typeparam>
        /// <param name="source">The source used to determine the table to update.</param>
        /// <param name="query">The query used to generate the where clause.</param>
        /// <param name="updateExpression">The MemberInitExpression used to indicate what is updated.</param>
        /// <returns>The number of row updated.</returns>
        /// <remarks>
        /// When executing this method, the statement is immediately executed on the database provider
        /// and is not part of the change tracking system.  Also, changes will not be reflected on 
        /// any entities that have already been materialized in the current context.        
        /// </remarks>
        public static int Update<TEntity>(
            this DbSet<TEntity> source,
            IQueryable<TEntity> query,
            Expression<Func<TEntity, TEntity>> updateExpression)
            where TEntity : class
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (query == null)
                throw new ArgumentNullException("query");
            if (updateExpression == null)
                throw new ArgumentNullException("updateExpression");

            ObjectQuery<TEntity> sourceQuery = source.ToObjectQuery();
            if (sourceQuery == null)
                throw new ArgumentException("The query must be of type ObjectQuery or DbQuery.", "source");

            ObjectContext objectContext = sourceQuery.Context;
            if (objectContext == null)
                throw new ArgumentException("The ObjectContext for the source query can not be null.", "source");

            EntityMap entityMap = sourceQuery.GetEntityMap<TEntity>();
            if (entityMap == null)
                throw new ArgumentException("Could not load the entity mapping information for the source.", "source");

            ObjectQuery<TEntity> objectQuery = query.ToObjectQuery();
            if (objectQuery == null)
                throw new ArgumentException("The query must be of type ObjectQuery or DbQuery.", "query");

            return Update(objectContext, entityMap, objectQuery, updateExpression);
        }

        /// <summary>
        /// Executes an update statement using an expression to filter the rows that are updated.
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity.</typeparam>
        /// <param name="source">The source used to determine the table to update.</param>
        /// <param name="filterExpression">The filter expression used to generate the where clause.</param>
        /// <param name="updateExpression">The MemberInitExpression used to indicate what is updated.</param>
        /// <returns>The number of row updated.</returns>
        /// <example>Update all users in the test.com domain to be inactive.
        /// <code><![CDATA[
        /// var db = new TrackerContext();
        /// string emailDomain = "@test.com";
        /// int count = db.Users.Update(
        ///   u => u.Email.EndsWith(emailDomain),
        ///   u => new User { IsApproved = false, LastActivityDate = DateTime.Now });
        /// ]]></code>
        /// </example>
        /// <remarks>
        /// When executing this method, the statement is immediately executed on the database provider
        /// and is not part of the change tracking system.  Also, changes will not be reflected on 
        /// any entities that have already been materialized in the current context.        
        /// </remarks>
        public static int Update<TEntity>(
            this DbSet<TEntity> source,
            Expression<Func<TEntity, bool>> filterExpression,
            Expression<Func<TEntity, TEntity>> updateExpression)
            where TEntity : class
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (filterExpression == null)
                throw new ArgumentNullException("filterExpression");

            return source.Update(source.Where(filterExpression), updateExpression);
        }

        public static int BulkInsert<TEntity>(this DbSet<TEntity> source, IEnumerable<TEntity> records,
            Expression<Func<TEntity, TEntity>> insertExpression) where TEntity : class
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (records == null)
                throw new ArgumentNullException("records");

            ObjectQuery<TEntity> sourceQuery = source.ToObjectQuery();
            if (sourceQuery == null)
                throw new ArgumentException("The query must be of type ObjectQuery or DbQuery.", "source");

            ObjectContext objectContext = sourceQuery.Context;
            if (objectContext == null)
                throw new ArgumentException("The ObjectContext for the source query can not be null.", "source");

            EntityMap entityMap = sourceQuery.GetEntityMap<TEntity>();
            if (entityMap == null)
                throw new ArgumentException("Could not load the entity mapping information for the source.", "source");

            return BulkInsert(objectContext, entityMap, records, insertExpression);
        }

        public static int BulkInsert(this DbSet source, IEnumerable records)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (records == null)
                throw new ArgumentNullException("records");

            ObjectQuery sourceQuery = source.ToObjectQuery();
            if (sourceQuery == null)
                throw new ArgumentException("The query must be of type ObjectQuery or DbQuery.", "source");

            ObjectContext objectContext = sourceQuery.Context;
            if (objectContext == null)
                throw new ArgumentException("The ObjectContext for the source query can not be null.", "source");

            var genRecords = records.Cast<object>();
            Type entityType = genRecords.FirstOrDefault().GetType();
            EntityMap entityMap = sourceQuery.GetEntityMap(entityType);
            if (entityMap == null)
                throw new ArgumentException("Could not load the entity mapping information for the source.", "source");

            return BulkInsert<object>(objectContext, entityMap, genRecords, null);
        }


        public static int InsertFrom<TSource,TEntity>(this DbSet<TEntity> source,
            IQueryable<TSource> query, Expression<Func<TSource, TEntity>> insertExpression)
            where TEntity : class where TSource : class
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (query == null)
                throw new ArgumentNullException("query");
            if (insertExpression == null)
                throw new ArgumentNullException("insertExpression");

            ObjectQuery<TEntity> sourceQuery = source.ToObjectQuery();
            if (sourceQuery == null)
                throw new ArgumentException("The query must be of type ObjectQuery or DbQuery.", "source");

            ObjectContext objectContext = sourceQuery.Context;
            if (objectContext == null)
                throw new ArgumentException("The ObjectContext for the source query can not be null.", "source");

            EntityMap entityMap = sourceQuery.GetEntityMap<TEntity>();
            if (entityMap == null)
                throw new ArgumentException("Could not load the entity mapping information for the source.", "source");

            ObjectQuery<TSource> objectQuery = query.ToObjectQuery();
            if (objectQuery == null)
                throw new ArgumentException("The query must be of type ObjectQuery or DbQuery.", "query");

            return InsertFrom(objectContext, entityMap, objectQuery, insertExpression);
        }


        private static int Delete<TEntity>(ObjectContext objectContext, EntityMap entityMap, ObjectQuery<TEntity> query)
            where TEntity : class
        {
            DbConnection deleteConnection = null;
            DbCommand deleteCommand = null;
            bool existingConnection = true;

            try
            {
                using (TransactionScope transaction = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions() { IsolationLevel = System.Transactions.IsolationLevel.ReadCommitted, Timeout = new TimeSpan(0, 5, 0) }))
                {
                    deleteConnection = GetStoreConnection(objectContext);
                    if (deleteConnection.State != System.Data.ConnectionState.Open)
                    {
                        existingConnection = false;
                        deleteConnection.Open();
                    }

                    deleteCommand = deleteConnection.CreateCommand();
                    if (objectContext.CommandTimeout.HasValue)
                        deleteCommand.CommandTimeout = objectContext.CommandTimeout.Value;

                    var innerSelect = GetSelectSql(query, entityMap, deleteCommand);

                    var sqlBuilder = new StringBuilder(innerSelect.Length * 2);

                    sqlBuilder.Append("DELETE ");
                    sqlBuilder.Append(entityMap.TableName);
                    sqlBuilder.AppendLine();

                    sqlBuilder.AppendFormat("FROM {0} AS j0 INNER JOIN (", entityMap.TableName);
                    sqlBuilder.AppendLine();
                    sqlBuilder.AppendLine(innerSelect);
                    sqlBuilder.Append(") AS j1 ON (");

                    bool wroteKey = false;
                    foreach (var keyMap in entityMap.KeyMaps)
                    {
                        if (wroteKey)
                            sqlBuilder.Append(" AND ");

                        sqlBuilder.AppendFormat("j0.{0} = j1.{0}", keyMap.ColumnName);
                        wroteKey = true;
                    }
                    sqlBuilder.Append(")");

                    deleteCommand.CommandText = sqlBuilder.ToString();

                    int result = deleteCommand.ExecuteNonQuery();
                    transaction.Complete();
                    return result;
                }
            }
            finally
            {
                if (deleteCommand != null)
                    deleteCommand.Dispose();
                if (deleteConnection != null && !existingConnection)
                    deleteConnection.Close();
            }
        }

        private static int Update<TEntity>(ObjectContext objectContext, EntityMap entityMap, ObjectQuery<TEntity> query, Expression<Func<TEntity, TEntity>> updateExpression)
            where TEntity : class
        {
            DbConnection updateConnection = null;
            DbCommand updateCommand = null;
            bool existingConnection = true;

            try
            {
                using (TransactionScope transaction = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions() { IsolationLevel = System.Transactions.IsolationLevel.ReadCommitted, Timeout = new TimeSpan(0, 5, 0) }))
                {
                    updateConnection = GetStoreConnection(objectContext);
                    if (updateConnection.State != System.Data.ConnectionState.Open)
                    {
                        existingConnection = false;
                        updateConnection.Open();
                    }


                    updateCommand = updateConnection.CreateCommand();
                    if (objectContext.CommandTimeout.HasValue)
                        updateCommand.CommandTimeout = objectContext.CommandTimeout.Value;

                    var innerSelect = GetSelectSql(query, entityMap, updateCommand);
                    var sqlBuilder = new StringBuilder(innerSelect.Length * 2);

                    sqlBuilder.Append("UPDATE ");
                    sqlBuilder.Append(entityMap.TableName);
                    sqlBuilder.AppendLine(" SET ");

                    var memberInitExpression = updateExpression.Body as MemberInitExpression;
                    if (memberInitExpression == null)
                        throw new ArgumentException("The update expression must be of type MemberInitExpression.", "updateExpression");

                    int nameCount = 0;
                    bool wroteSet = false;
                    foreach (MemberBinding binding in memberInitExpression.Bindings)
                    {
                        if (wroteSet)
                            sqlBuilder.AppendLine(", ");

                        string propertyName = binding.Member.Name;
                        string columnName = entityMap.PropertyMaps
                            .Where(p => p.PropertyName == propertyName)
                            .Select(p => p.ColumnName)
                            .FirstOrDefault();

                        string parameterName = "p__update__" + nameCount++;

                        var memberAssignment = binding as MemberAssignment;
                        if (memberAssignment == null)
                            throw new ArgumentException("The update expression MemberBinding must only by type MemberAssignment.", "updateExpression");

                        object value;
                        bool memberParsed = false;
                        if (memberAssignment.Expression.NodeType == ExpressionType.MemberAccess)
                        {
                            var memberExpression = memberAssignment.Expression as MemberExpression;
                            if (memberExpression.Expression is ParameterExpression)
                            {
                                string sourceColumnName = entityMap.PropertyMaps
                                    .Where(p => p.PropertyName == memberExpression.Member.Name)
                                    .Select(p => p.ColumnName)
                                    .FirstOrDefault();
                                sqlBuilder.AppendFormat("{0} = {1}", columnName, sourceColumnName);
                                memberParsed = true;
                            }
                        }
                        if(!memberParsed)
                        {
                            if (memberAssignment.Expression.NodeType == ExpressionType.Constant)
                            {
                                var constantExpression = memberAssignment.Expression as ConstantExpression;
                                if (constantExpression == null)
                                    throw new ArgumentException("The MemberAssignment expression is not a ConstantExpression.", "updateExpression");

                                value = constantExpression.Value;
                            }
                            else
                            {
                                LambdaExpression lambda = Expression.Lambda(memberAssignment.Expression, null);
                                value = lambda.Compile().DynamicInvoke();
                            }

                            var parameter = updateCommand.CreateParameter();
                            parameter.ParameterName = parameterName;
                            parameter.Value = value ?? DBNull.Value;
                            updateCommand.Parameters.Add(parameter);

                            sqlBuilder.AppendFormat("{0} = @{1}", columnName, parameterName);
                        }
                        wroteSet = true;
                    }

                    sqlBuilder.AppendLine(" ");
                    sqlBuilder.AppendFormat("FROM {0} AS j0 INNER JOIN (", entityMap.TableName);
                    sqlBuilder.AppendLine();
                    sqlBuilder.AppendLine(innerSelect);
                    sqlBuilder.Append(") AS j1 ON (");

                    bool wroteKey = false;
                    foreach (var keyMap in entityMap.KeyMaps)
                    {
                        if (wroteKey)
                            sqlBuilder.Append(" AND ");

                        sqlBuilder.AppendFormat("j0.{0} = j1.{0}", keyMap.ColumnName);
                        wroteKey = true;
                    }
                    sqlBuilder.Append(")");

                    updateCommand.CommandText = sqlBuilder.ToString();

                    int result = updateCommand.ExecuteNonQuery();
                    transaction.Complete();
                    return result;
                }
            }
            finally
            {
                if (updateCommand != null)
                    updateCommand.Dispose();
                if (updateConnection != null && !existingConnection)
                    updateConnection.Close();
            }
        }

        private static int InsertFrom<TSource,TEntity>(ObjectContext objectContext, EntityMap entityMap, ObjectQuery<TSource> query, Expression<Func<TSource, TEntity>> insertExpression)
            where TEntity : class where TSource : class
        {
            DbConnection insertConnection = null;
            DbCommand insertCommand = null;
            bool existingConnection = true;

            try
            {
                using (TransactionScope transaction = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions() { IsolationLevel = System.Transactions.IsolationLevel.ReadCommitted, Timeout = new TimeSpan(0, 5, 0) }))
                {
                    insertConnection = GetStoreConnection(objectContext);
                    if (insertConnection.State != System.Data.ConnectionState.Open)
                    {
                        existingConnection = false;
                        insertConnection.Open();
                    }

                    insertCommand = insertConnection.CreateCommand();
                    if (objectContext.CommandTimeout.HasValue)
                        insertCommand.CommandTimeout = objectContext.CommandTimeout.Value;

                    var memberInitExpression = insertExpression.Body as MemberInitExpression;
                    if (memberInitExpression == null)
                        throw new ArgumentException("The insert expression must be of type MemberInitExpression.", "insertExpression");

                    var innerSelect = GetInsertSelectSql(query, entityMap, memberInitExpression, insertCommand);
                    var sqlBuilder = new StringBuilder(innerSelect.Length * 2);

                    sqlBuilder.Append("INSERT INTO ");
                    sqlBuilder.AppendLine(entityMap.TableName);
                    sqlBuilder.Append(" (");

                    sqlBuilder.Append(String.Join(", ", memberInitExpression.Bindings
                        .Select(x => entityMap.PropertyMaps
                            .Where(p => p.PropertyName == x.Member.Name)
                            .Select(p => p.ColumnName)
                            .FirstOrDefault())
                        .Union(entityMap.PropertyMaps.OfType<ConstantPropertyMap>().Select(x => x.ColumnName))));

                    sqlBuilder.AppendLine(") ");
                    sqlBuilder.AppendLine(innerSelect);
                    insertCommand.CommandText = sqlBuilder.ToString();

                    int result = insertCommand.ExecuteNonQuery();
                    transaction.Complete();
                    return result;
                }
            }
            finally
            {
                if (insertCommand != null)
                    insertCommand.Dispose();
                if (insertConnection != null && !existingConnection)
                    insertConnection.Close();
            }
        }

        private static int BulkInsert<TEntity>(ObjectContext objectContext, EntityMap entityMap,
            IEnumerable<object> records, Expression<Func<TEntity, TEntity>> insertExpression)
            where TEntity : class
        {
            DbConnection insertConnection = null;
            DbCommand insertCommand = null;
            bool existingConnection = true;

            try
            {
                using (TransactionScope transaction = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions() { IsolationLevel = System.Transactions.IsolationLevel.ReadCommitted, Timeout = new TimeSpan(0, 5, 0) }))
                {
                    insertConnection = GetStoreConnection(objectContext);
                    if (insertConnection.State != System.Data.ConnectionState.Open)
                    {
                        existingConnection = false;
                        insertConnection.Open();
                    }
                    insertCommand = insertConnection.CreateCommand();
                    if (objectContext.CommandTimeout.HasValue)
                        insertCommand.CommandTimeout = objectContext.CommandTimeout.Value;

                    List<PropertyMap> propertyMaps = entityMap.PropertyMaps;
                    if (insertExpression != null)
                    {
                        var memberInitExpression = insertExpression.Body as MemberInitExpression;
                        if (memberInitExpression != null)
                        {
                            propertyMaps = memberInitExpression.Bindings
                                .Select(x => entityMap.PropertyMaps
                                    .Where(p => p.PropertyName == x.Member.Name)
                                    .FirstOrDefault()
                                ).ToList();
                        }
                    }

                    var insertLine = new StringBuilder();
                    insertLine.Append("INSERT INTO ");
                    insertLine.AppendLine(entityMap.TableName);
                    insertLine.Append(" (");
                    insertLine.Append(String.Join(", ", propertyMaps
                        .Select(x => x.ColumnName)));
                    insertLine.AppendLine(") VALUES ");
                    var rows = new StringBuilder();
                    List<DbParameter> parameters = new List<DbParameter>();
                    Type entityType = records.FirstOrDefault().GetType();
                    PropertyInfo[] properties = entityType.GetProperties();
                    int i = 0;
                    int result = 0;
                    int maxRows = 2000 / propertyMaps.Count();

                    foreach (var record in records)
                    {
                        int propCount = parameters.Count();
                        if (propCount > 0)
                        {
                            rows.AppendLine(",");
                        }
                        rows.Append("(");
                        rows.Append(String.Join(", ", propertyMaps
                            .Select(x => String.Format("@{0}", propCount++))));
                        rows.Append(")");
                        foreach (PropertyMap propMap in propertyMaps)
                        {
                            object value = null;
                            DbParameter dbParam = insertCommand.CreateParameter();
                            dbParam.ParameterName = String.Format("@{0}", parameters.Count);
                            if (propMap is ConstantPropertyMap)
                            {
                                value = ((ConstantPropertyMap)propMap).Value;
                            }
                            else
                            {
                                PropertyInfo prop = properties.SingleOrDefault(x => x.Name == propMap.PropertyName);
                                if (prop != null)
                                {
                                    value = prop.GetValue(record, new object[0]);
                                    dbParam.DbType = DbTypeConversion.ToDbType(prop.PropertyType);
                                }
                            }
                            dbParam.Value = value ?? DBNull.Value;
                            parameters.Add(dbParam);
                        }
                        if (++i >= maxRows)
                        {
                            result += CommitBulk(insertCommand, insertLine, rows, parameters);
                            i = 0;
                            rows.Clear();
                            parameters.Clear();
                        }
                    }
                    if (rows.Length > 0)
                    {
                        result += CommitBulk(insertCommand, insertLine, rows, parameters);
                    }
                    transaction.Complete();
                    return result;
                }
            }
            finally
            {
                if (insertCommand != null)
                    insertCommand.Dispose();
                if (insertConnection != null && !existingConnection)
                    insertConnection.Close();
            }
        }

        private static int CommitBulk(DbCommand command, StringBuilder insertLine, StringBuilder rows, List<DbParameter> parameters)
        {
            string commandText = insertLine.ToString() + rows.ToString();
            int parameterCount = 0;
            if (command.CommandText != commandText)
            {
                command.CommandText = commandText;
                command.Parameters.Clear();
                command.Parameters.AddRange(parameters.ToArray());
            }
            else
            {
                parameters.ForEach(x => command.Parameters[parameterCount++].Value = x.Value);
            }
            return command.ExecuteNonQuery();
        }

        private static DbConnection GetStoreConnection(ObjectContext objectContext)
        {
            DbConnection dbConnection = objectContext.Connection;
            var entityConnection = dbConnection as EntityConnection;

            // by-pass entity connection
            var connection = entityConnection == null
                ? dbConnection
                : entityConnection.StoreConnection;

            return connection;
        }

        private static string GetInsertSelectSql<TSource>(ObjectQuery<TSource> query, EntityMap entityMap, MemberInitExpression memberInitExpression, DbCommand command)
            where TSource : class
        {
            var selector = new StringBuilder(50);
            int i = 0;
            var constantProperties = entityMap.PropertyMaps.OfType<ConstantPropertyMap>();
            selector.Append("new(");
            selector.Append(String.Join(", ", memberInitExpression.Bindings
                .Select(x => ((x as MemberAssignment).Expression as MemberExpression).Member.Name)
                .Union(constantProperties.Select(x => String.Format("@{0} as {1}", i++, x.PropertyName))))); //, x.PropertyName
            selector.Append(")");

            var selectQuery = DynamicQueryable.Select(query, selector.ToString(), constantProperties.Select(x => x.Value).ToArray());
            var objectQuery = selectQuery as ObjectQuery;

            if (objectQuery == null)
                throw new ArgumentException("The query must be of type ObjectQuery.", "query");
            objectQuery.EnablePlanCaching = true;
            string innerJoinSql = EFQueryUtils.GetLimitedQuery(objectQuery);
            // create parameters
            foreach (var objectParameter in objectQuery.Parameters)
            {
                var parameter = command.CreateParameter();
                parameter.ParameterName = objectParameter.Name;
                parameter.Value = objectParameter.Value ?? DBNull.Value;

                command.Parameters.Add(parameter);
            }

            return innerJoinSql;
        }

        private static string GetSelectSql<TEntity>(ObjectQuery<TEntity> query, EntityMap entityMap, DbCommand command)
            where TEntity : class
        {
            // changing query to only select keys
            var selector = new StringBuilder(50);
            selector.Append("new(");
            foreach (var propertyMap in entityMap.KeyMaps)
            {
                if (selector.Length > 4)
                    selector.Append((", "));

                selector.Append(propertyMap.PropertyName);
            }
            selector.Append(")");

            var selectQuery = DynamicQueryable.Select(query, selector.ToString());
            var objectQuery = selectQuery as ObjectQuery;

            if (objectQuery == null)
                throw new ArgumentException("The query must be of type ObjectQuery.", "query");

            string innerJoinSql = objectQuery.ToTraceString();

            // create parameters
            foreach (var objectParameter in objectQuery.Parameters)
            {
                var parameter = command.CreateParameter();
                parameter.ParameterName = objectParameter.Name;
                parameter.Value = objectParameter.Value ?? DBNull.Value;

                command.Parameters.Add(parameter);
            }

            return innerJoinSql;
        }

    }
}
