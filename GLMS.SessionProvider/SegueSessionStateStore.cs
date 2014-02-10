using System;
using System.Configuration;
using System.Collections;
using System.Threading;
using System.IO;
using System.Web;
using System.Web.Caching;
using System.Web.Util;
using System.Data;
using System.Data.SqlClient;
using System.Data.Common;
using System.Text;
using System.Security.Principal;
using System.Xml;
using System.Collections.Specialized;
using System.Configuration.Provider;
using System.Globalization;
using System.Web.Management;
using System.Web.Hosting;
using System.Web.Configuration;
using System.Web.SessionState;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.Linq;
using System.Web.Mvc;
using System.Web.Routing;
using System.Reflection;
using GLMS.MVC.Extensions;
using GLMS.MVC.Extensions.jqGrid;
using GLMS.MVC.Extensions.jqAutoComplete;


//------------------------------------------------------------------------------
// <copyright file="SqlSessionStateStore.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

/*
 * SqlSessionStateStore.cs
 * 
 * Copyright (c) 1998-2000, Microsoft Corporation
 * 
 */

namespace GLMS.SessionProvider
{
    /*
     * Provides session state via SQL Server
     */
    public class SessionStateStore : SessionStateStoreProviderBase
    {

        internal enum SupportFlags : uint
        {
            None = 0x00000000,
            GetLockAge = 0x00000001,
            Uninitialized = 0xFFFFFFFF
        }

#pragma warning disable 0649
        //        static ReadWriteSpinLock    s_lock;
#pragma warning restore 0649
        static int s_commandTimeout;
        static bool s_oneTimeInited;
        static bool s_usePartition = false;
        static EventHandler s_onAppDomainUnload;


        // We keep these info because we don't want to hold on to the config object.
        static string s_configPartitionResolverType = null;
        static string s_configSqlConnectionFileName;
        static int s_configSqlConnectionLineNumber;
        static bool s_configAllowCustomSqlDatabase = true;
        static string s_sqlConnectionString;
        static SqlPartitionInfo s_singlePartitionInfo;

        // Per request info        
        HttpContext _rqContext;
        int _rqOrigStreamLen;
        //IPartitionResolver  _partitionResolver = null;
        SqlPartitionInfo _partitionInfo;

        const int ITEM_SHORT_LENGTH = 7000;
        const int SQL_ERROR_PRIMARY_KEY_VIOLATION = 2627;
        const int SQL_LOGIN_FAILED = 18456;
        const int SQL_LOGIN_FAILED_2 = 18452;
        const int SQL_LOGIN_FAILED_3 = 18450;
        const int APP_SUFFIX_LENGTH = 8;

        static int ID_LENGTH = SessionIDManager.SessionIDMaxLength + APP_SUFFIX_LENGTH;
        internal const int SQL_COMMAND_TIMEOUT_DEFAULT = 30;        // in sec

        public SessionStateStore()
        {
        }

        /*
        internal override void Initialize(string name, NameValueCollection config, IPartitionResolver partitionResolver) {
            _partitionResolver = partitionResolver;
            Initialize(name, config);
        }
         */

#if DBG
        SessionStateModule  _module;

        internal void SetModule(SessionStateModule module) {
            _module = module;
        }
#endif
        /*
        public static void BounceSession(HttpContext Context)
        {
            System.Web.SessionState.SessionIDManager manager = new System.Web.SessionState.SessionIDManager();
            string oldId = manager.GetSessionID(Context);
            string newId = manager.CreateSessionID(Context);
            bool isAdd = false, isRedir = false;
            manager.SaveSessionID(Context, newId, out isRedir, out isAdd);
            SessionStateModule ssm = (SessionStateModule)HttpContext.Current.ApplicationInstance.Modules.Get("Session");
            System.Reflection.FieldInfo[] fields = ssm.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
            SessionStateStoreProviderBase store = null;
            System.Reflection.FieldInfo rqIdField = null, rqLockIdField = null, rqStateNotFoundField = null;
            foreach (System.Reflection.FieldInfo field in fields)
            {
                if (field.Name.Equals("_store")) store = (SessionStateStoreProviderBase)field.GetValue(ssm);
                else if (field.Name.Equals("_rqId")) rqIdField = field;
                else if (field.Name.Equals("_rqLockId")) rqLockIdField = field;
                else if (field.Name.Equals("_rqSessionStateNotFound")) rqStateNotFoundField = field;
            }
            object lockId = rqLockIdField.GetValue(ssm);
            if ((lockId != null) && (oldId != null))
            {
                store.RemoveItem(Context, oldId, lockId, null);
            }
            rqStateNotFoundField.SetValue(ssm, true);
            rqIdField.SetValue(ssm, newId);
        }
        */

        public override void Initialize(string name, NameValueCollection config)
        {
            if (String.IsNullOrEmpty(name))
                name = "SQL Server Session State Provider";

            base.Initialize(name, config);

            if (!s_oneTimeInited)
            {
                //                s_lock.AcquireWriterLock();
                try
                {
                    if (!s_oneTimeInited)
                    {
                        OneTimeInit();
                    }
                }
                finally
                {
                    //                    s_lock.ReleaseWriterLock();
                }
            }

            _partitionInfo = s_singlePartitionInfo;
        }

        void OneTimeInit()
        {
            SessionStateSection config = (SessionStateSection)WebConfigurationManager.GetSection("system.web/sessionState");

            s_configSqlConnectionFileName = config.ElementInformation.Properties["sqlConnectionString"].Source;
            s_configSqlConnectionLineNumber = config.ElementInformation.Properties["sqlConnectionString"].LineNumber;
            s_configAllowCustomSqlDatabase = config.AllowCustomSqlDatabase;

            s_sqlConnectionString = config.SqlConnectionString;
            if (WebConfigurationManager.ConnectionStrings[s_sqlConnectionString] != null)
            {
                s_sqlConnectionString = WebConfigurationManager.ConnectionStrings[s_sqlConnectionString].ConnectionString;
            }
            if (String.IsNullOrEmpty(s_sqlConnectionString)) throw new Exception("No connection string specified");
            s_singlePartitionInfo = CreatePartitionInfo(s_sqlConnectionString);

            s_commandTimeout = (int)config.SqlCommandTimeout.TotalSeconds;

            // We only need to do this in one instance
            s_onAppDomainUnload = new EventHandler(OnAppDomainUnload);
            Thread.GetDomain().DomainUnload += s_onAppDomainUnload;

            // Last thing to set.
            s_oneTimeInited = true;
        }

        void OnAppDomainUnload(Object unusedObject, EventArgs unusedEventArgs)
        {
            //Debug.Trace("SessionStateStore", "OnAppDomainUnload called");

            Thread.GetDomain().DomainUnload -= s_onAppDomainUnload;
        }

        internal SqlPartitionInfo CreatePartitionInfo(string sqlConnectionString)
        {
            /*
             * Parse the connection string for errors. We want to ensure
             * that the user's connection string doesn't contain an
             * Initial Catalog entry, so we must first create a dummy connection.
             */
            SqlConnection dummyConnection;
            string attachDBFilename = null;

            try
            {
                dummyConnection = new SqlConnection(sqlConnectionString);
            }
            catch (Exception e)
            {
                throw new Exception(
                    SR.GetString(SR.Error_parsing_session_sqlConnectionString, e.Message),
                    e);
            }

            // Search for both Database and AttachDbFileName.  Don't append our
            // database name if either of them exists.
            string database = dummyConnection.Database;
            SqlConnectionStringBuilder scsb = new SqlConnectionStringBuilder(sqlConnectionString);

            if (String.IsNullOrEmpty(database))
            {
                database = scsb.AttachDBFilename;
                attachDBFilename = database;
            }

            if (!String.IsNullOrEmpty(database))
            {
                if (!s_configAllowCustomSqlDatabase)
                {
                    throw new Exception(SR.GetString(SR.No_database_allowed_in_sqlConnectionString));
                }

                if (attachDBFilename != null)
                {
                    //                    HttpRuntime.CheckFilePermission(attachDBFilename, true);
                }
            }
            else
            {
                scsb.Add("Initial Catalog", "ASPState");
            }

            return new SqlPartitionInfo(new ResourcePool(new TimeSpan(0, 0, 5), int.MaxValue),
                                            scsb.IntegratedSecurity,
                                            scsb.ConnectionString);

        }

        public override bool SetItemExpireCallback(SessionStateItemExpireCallback expireCallback)
        {
            return false;
        }

        public override void Dispose()
        {
        }

        public override void InitializeRequest(HttpContext context)
        {
            ////Debug.Assert(context != null, "context != null");

            _rqContext = context;
            _rqOrigStreamLen = 0;

            /*            if (s_usePartition) {
                            // For multiple partition case, the connection info can change from request to request
                            _partitionInfo = null;
                        }
             * 
             * 
             */

        }

        public override void EndRequest(HttpContext context)
        {
            ////Debug.Assert(context != null, "context != null");
            _rqContext = null;
        }

        public bool KnowForSureNotUsingIntegratedSecurity
        {
            get
            {
                if (_partitionInfo == null)
                {
                    ////Debug.Assert(s_usePartition, "_partitionInfo can be null only if we're using paritioning and we haven't called GetConnection yet.");
                    // If we're using partitioning, we need the session id to figure out the connection
                    // string.  Without it, we can't know for sure.
                    return false;
                }
                else
                {
                    ////Debug.Assert(_partitionInfo != null);
                    return !_partitionInfo.UseIntegratedSecurity;
                }
            }
        }

        //
        // Regarding resource pool, we will turn it on if in <identity>:
        //  - User is not using integrated security
        //  - impersonation = "false"
        //  - impersonation = "true" and userName/password is NON-null
        //  - impersonation = "true" and IIS is using Anonymous
        //
        // Otherwise, the impersonated account will be dynamic and we have to turn
        // resource pooling off.
        //
        // Note:
        // In case 2. above, the user can specify different usernames in different 
        // web.config in different subdirs in the app.  In this case, we will just 
        // cache the connections in the resource pool based on the identity of the 
        // connection.  So in this specific scenario it is possible to have the 
        // resource pool filled with mixed identities.
        // 
        bool CanUsePooling()
        {
            bool ret = false;

            if (KnowForSureNotUsingIntegratedSecurity)
            {
                //Debug.Trace("SessionStatePooling", "CanUsePooling: not using integrated security");
                ret = true;
            }
            else if (_rqContext == null)
            {
                // One way this can happen is we hit an error on page compilation,
                // and SessionStateModule.OnEndRequest is called
                //Debug.Trace("SessionStatePooling", "CanUsePooling: no context");
                ret = false;
            }
            //            else if (!_rqContext.IsClientImpersonationConfigured) {
            //Debug.Trace("SessionStatePooling", "CanUsePooling: mode is None or Application");
            //ret = true;
            //            }
            //            else if (HttpRuntime.IsOnUNCShareInternal) {
            //Debug.Trace("SessionStatePooling", "CanUsePooling: mode is UNC");
            //                ret = false;
            //            }
            else
            {
                /*                string logon = _rqContext.WorkerRequest.GetServerVariable("LOGON_USER");

                                //Debug.Trace("SessionStatePooling", "LOGON_USER = '" + logon + "'; identity = '" + _rqContext.User.Identity.Name + "'; IsUNC = " + HttpRuntime.IsOnUNCShareInternal);

                                if (String.IsNullOrEmpty(logon)) {
                                    ret = true;
                                }
                                else {
                                    ret = false;
                                }
                 */
            }

            //Debug.Trace("SessionStatePooling", "CanUsePooling returns " + ret);
            return ret;
        }

        SqlStateConnection GetConnection(string id, ref bool usePooling)
        {
            SqlStateConnection conn = null;

            if (_partitionInfo == null)
            {
                //Debug.Assert(s_partitionManager != null);
                //Debug.Assert(_partitionResolver != null);

                //                _partitionInfo = (SqlPartitionInfo)s_partitionManager.GetPartition(_partitionResolver, id);
                _partitionInfo = s_singlePartitionInfo;
            }

            //Debug.Trace("SessionStatePooling", "Calling GetConnection under " + WindowsIdentity.GetCurrent().Name);
#if DBG
            //Debug.Assert(_module._rqChangeImpersonationRefCount != 0, 
                "SessionStateModule.ChangeImpersonation should have been called before making any call to SQL");
#endif

            usePooling = CanUsePooling();
            if (usePooling)
            {
                conn = (SqlStateConnection)_partitionInfo.RetrieveResource();
                if (conn != null && (conn.Connection.State & ConnectionState.Open) == 0)
                {
                    conn.Dispose();
                    conn = null;
                }
            }

            if (conn == null)
            {
                conn = new SqlStateConnection(_partitionInfo);
            }

            return conn;
        }

        void DisposeOrReuseConnection(ref SqlStateConnection conn, bool usePooling)
        {
            try
            {
                if (conn == null)
                {
                    return;
                }

                if (usePooling)
                {
                    _partitionInfo.StoreResource(conn);
                    conn = null;
                }
            }
            finally
            {
                if (conn != null)
                {
                    conn.Dispose();
                }
            }
        }

        internal static void ThrowSqlConnectionException(SqlConnection conn, Exception e)
        {
            if (s_usePartition)
            {
                throw new HttpException(
                    SR.GetString(SR.Cant_connect_sql_session_database_partition_resolver,
                                s_configPartitionResolverType, conn.DataSource, conn.Database));
            }
            else
            {
                throw new HttpException(
                    SR.GetString(SR.Cant_connect_sql_session_database),
                    e);
            }
        }

        SessionStateStoreData DoGet(HttpContext context, String id, bool getExclusive,
                                        out bool locked,
                                        out TimeSpan lockAge,
                                        out object lockId,
                                        out SessionStateActions actionFlags)
        {
            SqlDataReader reader;
            byte[] buf;
            MemoryStream stream = null;
            SessionStateStoreData item;
            bool useGetLockAge = false;
            SqlStateConnection conn = null;
            SqlCommand cmd = null;
            bool usePooling = true;

            //Debug.Assert(id.Length <= SessionIDManager.SESSION_ID_LENGTH_LIMIT, "id.Length <= SessionIDManager.SESSION_ID_LENGTH_LIMIT");
            //Debug.Assert(context != null, "context != null");

            // Set default return values
            locked = false;
            lockId = null;
            lockAge = TimeSpan.Zero;
            actionFlags = 0;

            buf = null;
            reader = null;

            conn = GetConnection(id, ref usePooling);

            //Debug.Assert(_partitionInfo != null, "_partitionInfo != null");
            //Debug.Assert(_partitionInfo.SupportFlags != SupportFlags.Uninitialized, "_partitionInfo.SupportFlags != SupportFlags.Uninitialized");

            //
            // In general, if we're talking to a SQL 2000 or above, we use LockAge; otherwise we use LockDate.
            // Below are the details:
            //
            // Version 1
            // ---------
            // In v1, the lockDate is generated and stored in SQL using local time, and we calculate the "lockage"
            // (i.e. how long the item is locked) by having the web server read lockDate from SQL and substract it 
            // from DateTime.Now.  But this approach introduced two problems:
            //  1. SQL server and web servers need to be in the same time zone.
            //  2. Daylight savings problem.
            //
            // Version 1.1
            // -----------
            // In v1.1, if using SQL 2000 we fixed the problem by calculating the "lockage" directly in SQL 
            // so that the SQL server and the web server don't have to be in the same time zone.  We also
            // use UTC date to store time in SQL so that the Daylight savings problem is solved.
            //
            // In summary, if using SQL 2000 we made the following changes to the SQL tables:
            //      i. The column Expires is using now UTC time
            //     ii. Add new SP TempGetStateItem2 and TempGetStateItemExclusive2 to return a lockage
            //         instead of a lockDate.
            //    iii. To support v1 web server, we still need to have TempGetStateItem and 
            //         TempGetStateItemExclusive.  However, we modify it a bit so that they use
            //         UTC time to update Expires column.
            //
            // If using SQL 7, we decided not to fix the problem, and the SQL scripts for SQL 7 remain pretty much 
            // the same. That means v1.1 web server will continue to call TempGetStateItem and 
            // TempGetStateItemExclusive and use v1 way to calculate the "lockage".
            //
            // Version 2.0
            // -----------
            // In v2.0 we added some new SP TempGetStateItem3 and TempGetStateItemExclusive3
            // because we added a new return value 'actionFlags'.  However, the principle remains the same
            // that we support lockAge only if talking to SQL 2000.
            //
            // (When one day MS stops supporting SQL 7 we can remove all the SQL7-specific scripts and
            //  stop all these craziness.)
            // 
            if ((_partitionInfo.SupportFlags & SupportFlags.GetLockAge) != 0)
            {
                useGetLockAge = true;
            }

            try
            {
                if (getExclusive)
                {
                    cmd = conn.TempGetExclusive;
                }
                else
                {
                    cmd = conn.TempGet;
                }

                cmd.Parameters[0].Value = id + _partitionInfo.AppSuffix; // @id
                cmd.Parameters[1].Value = Convert.DBNull;   // @itemShort
                cmd.Parameters[2].Value = Convert.DBNull;   // @locked
                cmd.Parameters[3].Value = Convert.DBNull;   // @lockDate or @lockAge
                cmd.Parameters[4].Value = Convert.DBNull;   // @lockCookie
                cmd.Parameters[5].Value = Convert.DBNull;   // @actionFlags

                try
                {
                    reader = cmd.ExecuteReader();

                    /* If the cmd returned data, we must read it all before getting out params */
                    if (reader != null)
                    {
                        try
                        {
                            if (reader.Read())
                            {
                                //Debug.Trace("SessionStateStore", "Sql Get returned long item");
                                buf = (byte[])reader[0];
                            }
                        }
                        finally
                        {
                            reader.Close();
                        }
                    }
                }
                catch (Exception e)
                {
                    ThrowSqlConnectionException(cmd.Connection, e);
                }

                /* Check if value was returned */
                if (Convert.IsDBNull(cmd.Parameters[2].Value))
                {
                    //Debug.Trace("SessionStateStore", "Sql Get returned null");
                    return null;
                }

                /* Check if item is locked */
                //Debug.Assert(!Convert.IsDBNull(cmd.Parameters[3].Value), "!Convert.IsDBNull(cmd.Parameters[3].Value)");
                //Debug.Assert(!Convert.IsDBNull(cmd.Parameters[4].Value), "!Convert.IsDBNull(cmd.Parameters[4].Value)");

                locked = (bool)cmd.Parameters[2].Value;
                lockId = (int)cmd.Parameters[4].Value;

                if (locked)
                {
                    //Debug.Trace("SessionStateStore", "Sql Get returned item that was locked");
                    //Debug.Assert(((int)cmd.Parameters[5].Value & (int)SessionStateActions.InitializeItem) == 0,
                    //   "(cmd.Parameters[5].Value & SessionStateActions.InitializeItem) == 0; uninit item shouldn't be locked");

                    if (useGetLockAge)
                    {
                        lockAge = new TimeSpan(0, 0, (int)cmd.Parameters[3].Value);
                    }
                    else
                    {
                        DateTime lockDate;
                        lockDate = (DateTime)cmd.Parameters[3].Value;
                        lockAge = DateTime.Now - lockDate;
                    }

                    //Debug.Trace("SessionStateStore", "LockAge = " + lockAge);

                    if (lockAge > new TimeSpan(0, 0, 30758400 /* one year */))
                    {
                        //Debug.Trace("SessionStateStore", "Lock age is more than 1 year!!!");
                        lockAge = TimeSpan.Zero;
                    }
                    return null;
                }

                actionFlags = (SessionStateActions)cmd.Parameters[5].Value;

                if (buf == null)
                {
                    /* Get short item */
                    //Debug.Assert(!Convert.IsDBNull(cmd.Parameters[1].Value), "!Convert.IsDBNull(cmd.Parameters[1].Value)");
                    //Debug.Trace("SessionStateStore", "Sql Get returned short item");
                    buf = (byte[])cmd.Parameters[1].Value;
                    //Debug.Assert(buf != null, "buf != null");
                }

                // Done with the connection.
                DisposeOrReuseConnection(ref conn, usePooling);

                try
                {
                    stream = new MemoryStream(buf);
                    item = Deserialize(context, stream);
                    _rqOrigStreamLen = (int)stream.Position;
                }
                finally
                {
                    if (stream != null)
                    {
                        stream.Close();
                    }
                }

                return item;
            }
            finally
            {
                DisposeOrReuseConnection(ref conn, usePooling);
            }
        }

        public override SessionStateStoreData GetItem(HttpContext context,
                                                        String id,
                                                        out bool locked,
                                                        out TimeSpan lockAge,
                                                        out object lockId,
                                                        out SessionStateActions actionFlags)
        {
            //Debug.Trace("SessionStateStore", "Calling Sql Get, id=" + id);

            //SessionIDManager.CheckIdLength(id, true /* throwOnFail */);
            return DoGet(context, id, false, out locked, out lockAge, out lockId, out actionFlags);
        }

        public override SessionStateStoreData GetItemExclusive(HttpContext context,
                                                String id,
                                                out bool locked,
                                                out TimeSpan lockAge,
                                                out object lockId,
                                                out SessionStateActions actionFlags)
        {
            //Debug.Trace("SessionStateStore", "Calling Sql GetExclusive, id=" + id);

            //SessionIDManager.CheckIdLength(id, true /* throwOnFail */);
            return DoGet(context, id, true, out locked, out lockAge, out lockId, out actionFlags);
        }

        // This will deserialize and return an item.
        // This version uses the default classes for SessionStateItemCollection, HttpStaticObjectsCollection
        // and SessionStateStoreData
        private static SessionStateStoreData Deserialize(HttpContext context, Stream stream)
        {

            int timeout;
            SessionStateItemCollection sessionItems;
            bool hasItems;
            bool hasStaticObjects;
            HttpStaticObjectsCollection staticObjects;
            Byte eof;

            //Debug.Assert(context != null);

            try
            {
                BinaryReader reader = new BinaryReader(stream);
                timeout = reader.ReadInt32();
                hasItems = reader.ReadBoolean();
                hasStaticObjects = reader.ReadBoolean();

                if (hasItems)
                {
                    sessionItems = SessionStateItemCollection.Deserialize(reader);
                }
                else
                {
                    sessionItems = new SessionStateItemCollection();
                }

                if (hasStaticObjects)
                {
                    staticObjects = HttpStaticObjectsCollection.Deserialize(reader);
                }
                else
                {
                    staticObjects = SessionStateUtility.GetSessionStaticObjects(context);
                }

                eof = reader.ReadByte();
                if (eof != 0xff)
                {
                    throw new HttpException(SR.GetString(SR.Invalid_session_state));
                }
            }
            catch (EndOfStreamException)
            {
                throw new HttpException(SR.GetString(SR.Invalid_session_state));
            }

            return new SessionStateStoreData(sessionItems, staticObjects, timeout);
        }

        private static SessionStateStoreData CreateLegitStoreData(HttpContext context,
                                                    ISessionStateItemCollection sessionItems,
                                                    HttpStaticObjectsCollection staticObjects,
                                                    int timeout)
        {
            if (sessionItems == null)
            {
                sessionItems = new SessionStateItemCollection();
            }

            if (staticObjects == null && context != null)
            {
                staticObjects = SessionStateUtility.GetSessionStaticObjects(context);
            }

            return new SessionStateStoreData(sessionItems, staticObjects, timeout);
        }

        static private void SerializeStoreData(SessionStateStoreData item, int initialStreamSize, out byte[] buf, out int length)
        {
            MemoryStream s = null;

            try
            {
                s = new MemoryStream(initialStreamSize);

                Serialize(item, s);
                buf = s.GetBuffer();
                length = (int)s.Length;
            }
            finally
            {
                if (s != null)
                {
                    s.Close();
                }
            }
        }

        // This method will take an item and serialize it
        private static void Serialize(SessionStateStoreData item, Stream stream)
        {
            bool hasItems = true;
            bool hasStaticObjects = true;

            BinaryWriter writer = new BinaryWriter(stream);
            writer.Write(item.Timeout);

            if (item.Items == null || item.Items.Count == 0)
            {
                hasItems = false;
            }
            writer.Write(hasItems);

            if (item.StaticObjects == null || item.StaticObjects.NeverAccessed)
            {
                hasStaticObjects = false;
            }
            writer.Write(hasStaticObjects);

            if (hasItems)
            {
                ((SessionStateItemCollection)item.Items).Serialize(writer);
            }

            if (hasStaticObjects)
            {
                item.StaticObjects.Serialize(writer);
            }

            // Prevent truncation of the stream
            writer.Write(unchecked((byte)0xff));
        }



        public override void ReleaseItemExclusive(HttpContext context,
                                String id,
                                object lockId)
        {
            //Debug.Trace("SessionStateStore", "Calling Sql ReleaseExclusive, id=" + id);
            //Debug.Assert(lockId != null, "lockId != null");
            //Debug.Assert(context != null, "context != null");

            bool usePooling = true;
            SqlStateConnection conn = null;
            int lockCookie = (int)lockId;

            try
            {
                //SessionIDManager.CheckIdLength(id, true /* throwOnFail */);

                conn = GetConnection(id, ref usePooling);
                try
                {
                    SqlCommand cmd = conn.TempReleaseExclusive;

                    cmd.Parameters[0].Value = id + _partitionInfo.AppSuffix;
                    cmd.Parameters[1].Value = lockCookie;
                    cmd.ExecuteNonQuery();
                }
                catch (Exception e)
                {
                    ThrowSqlConnectionException(conn.Connection, e);
                }

            }
            finally
            {
                DisposeOrReuseConnection(ref conn, usePooling);
            }
        }

        public override void SetAndReleaseItemExclusive(HttpContext context,
                                    String id,
                                    SessionStateStoreData item,
                                    object lockId,
                                    bool newItem)
        {
            byte[] buf;
            int length;
            SqlCommand cmd;
            bool usePooling = true;
            SqlStateConnection conn = null;
            int lockCookie;
            object sessionUser = Convert.DBNull;
            object sessionLodge = Convert.DBNull;
            object lastUrl = Convert.DBNull;
            if (context.Response.ContentType != "application/json")
            {
                if (context.CurrentHandler is MvcHandler)
                {
                    MvcHandler handler = (MvcHandler)context.CurrentHandler;
                    RouteData route = handler.RequestContext.RouteData;
                    string controller = (string)route.Values["controller"];
                    string area = (string)route.DataTokens["area"];
                    string action = (string)route.Values["action"];
                    lastUrl = String.Format("/{0}{1}{2}{3}{4}", area, String.IsNullOrEmpty(area) ? "" : "/", controller, String.IsNullOrEmpty(action) ? "" : "/", action);
                }
                else
                {
                    lastUrl = context.Request.Path ?? Convert.DBNull;
                }
            }

            //Debug.Assert(context != null, "context != null");

            try
            {
                try
                {
                    if (context.User.Identity.IsAuthenticated)
                    {
                        sessionUser = context.User.Identity.Name;
                    }
                }
                catch (Exception)
                {
                    sessionUser = "Unable to retrieve";
                }
                //Debug.Trace("SessionStateStore", "Calling Sql Set, id=" + id);

                //Debug.Assert(item.Items != null, "item.Items != null");
                //Debug.Assert(item.StaticObjects != null, "item.StaticObjects != null");

                //SessionIDManager.CheckIdLength(id, true /* throwOnFail */);

                try
                {
                    SerializeStoreData(item, ITEM_SHORT_LENGTH, out buf, out length);
                }
                catch
                {
                    if (!newItem)
                    {
                        ((SessionStateStoreProviderBase)this).ReleaseItemExclusive(context, id, lockId);
                    }
                    throw;
                }

                // Save it to the store

                if (lockId == null)
                {
                    lockCookie = 0;
                }
                else
                {
                    lockCookie = (int)lockId;
                }

                conn = GetConnection(id, ref usePooling);

                if (!newItem)
                {
                    //Debug.Assert(_rqOrigStreamLen > 0, "_rqOrigStreamLen > 0");
                    if (length <= ITEM_SHORT_LENGTH)
                    {
                        if (_rqOrigStreamLen <= ITEM_SHORT_LENGTH)
                        {
                            cmd = conn.TempUpdateShort;
                        }
                        else
                        {
                            cmd = conn.TempUpdateShortNullLong;
                        }
                    }
                    else
                    {
                        if (_rqOrigStreamLen <= ITEM_SHORT_LENGTH)
                        {
                            cmd = conn.TempUpdateLongNullShort;
                        }
                        else
                        {
                            cmd = conn.TempUpdateLong;
                        }
                    }

                }
                else
                {
                    if (length <= ITEM_SHORT_LENGTH)
                    {
                        cmd = conn.TempInsertShort;
                    }
                    else
                    {
                        cmd = conn.TempInsertLong;
                    }
                }

                cmd.Parameters[0].Value = id + _partitionInfo.AppSuffix;
                cmd.Parameters[1].Size = length;
                cmd.Parameters[1].Value = buf;
                cmd.Parameters[2].Value = item.Timeout;
                if (newItem)
                {
                    cmd.Parameters[3].Value = sessionUser;
                    cmd.Parameters[4].Value = sessionLodge;
                    cmd.Parameters[5].Value = lastUrl;
                }
                else
                {
                    cmd.Parameters[3].Value = lockCookie;
                    cmd.Parameters[4].Value = sessionUser;
                    cmd.Parameters[5].Value = sessionLodge;
                    cmd.Parameters[6].Value = lastUrl;
                }

                try
                {
                    cmd.ExecuteNonQuery();
                }
                catch (Exception e)
                {
                    HandleInsertException(conn.Connection, e, newItem, id);
                }
            }
            finally
            {
                DisposeOrReuseConnection(ref conn, usePooling);
            }
        }

        public override void RemoveItem(HttpContext context,
                                        String id,
                                        object lockId,
                                        SessionStateStoreData item)
        {
            //Debug.Trace("SessionStateStore", "Calling Sql Remove, id=" + id);
            //Debug.Assert(lockId != null, "lockId != null");
            //Debug.Assert(context != null, "context != null");

            bool usePooling = true;
            SqlStateConnection conn = null;
            int lockCookie = (int)lockId;

            try
            {
                //SessionIDManager.CheckIdLength(id, true /* throwOnFail */);

                conn = GetConnection(id, ref usePooling);
                try
                {
                    SqlCommand cmd = conn.TempRemove;
                    cmd.Parameters[0].Value = id + _partitionInfo.AppSuffix;
                    cmd.Parameters[1].Value = lockCookie;
                    cmd.ExecuteNonQuery();
                }
                catch (Exception e)
                {
                    ThrowSqlConnectionException(conn.Connection, e);
                }

            }
            finally
            {
                DisposeOrReuseConnection(ref conn, usePooling);
            }
        }

        public override void ResetItemTimeout(HttpContext context, String id)
        {
            //Debug.Trace("SessionStateStore", "Calling Sql ResetTimeout, id=" + id);
            //Debug.Assert(context != null, "context != null");

            bool usePooling = true;
            SqlStateConnection conn = null;

            try
            {
                //SessionIDManager.CheckIdLength(id, true /* throwOnFail */);

                conn = GetConnection(id, ref usePooling);
                try
                {
                    SqlCommand cmd = conn.TempResetTimeout;
                    cmd.Parameters[0].Value = id + _partitionInfo.AppSuffix;
                    cmd.ExecuteNonQuery();
                }
                catch (Exception e)
                {
                    ThrowSqlConnectionException(conn.Connection, e);
                }
            }
            finally
            {
                DisposeOrReuseConnection(ref conn, usePooling);
            }
        }

        public override SessionStateStoreData CreateNewStoreData(HttpContext context, int timeout)
        {
            //Debug.Assert(context != null, "context != null");
            return CreateLegitStoreData(context, null, null, timeout);
        }

        public override void CreateUninitializedItem(HttpContext context, String id, int timeout)
        {
            //Debug.Trace("SessionStateStore", "Calling Sql InsertUninitializedItem, id=" + id);
            //Debug.Assert(context != null, "context != null");

            bool usePooling = true;
            SqlStateConnection conn = null;
            byte[] buf;
            int length;

            try
            {
                //SessionIDManager.CheckIdLength(id, true /* throwOnFail */);

                // Store an empty data
                SerializeStoreData(CreateNewStoreData(context, timeout),
                                ITEM_SHORT_LENGTH, out buf, out length);

                conn = GetConnection(id, ref usePooling);

                try
                {
                    SqlCommand cmd = conn.TempInsertUninitializedItem;
                    cmd.Parameters[0].Value = id + _partitionInfo.AppSuffix;
                    cmd.Parameters[1].Size = length;
                    cmd.Parameters[1].Value = buf;
                    cmd.Parameters[2].Value = timeout;
                    cmd.ExecuteNonQuery();
                }
                catch (Exception e)
                {
                    HandleInsertException(conn.Connection, e, true, id);
                }
            }
            finally
            {
                DisposeOrReuseConnection(ref conn, usePooling);
            }
        }

        void HandleInsertException(SqlConnection conn, Exception e, bool newItem, string id)
        {
            SqlException sqlExpt = e as SqlException;
            if (sqlExpt != null &&
                sqlExpt.Number == SQL_ERROR_PRIMARY_KEY_VIOLATION &&
                newItem)
            {

                //Debug.Trace("SessionStateClientSet", 
                //                    "Insert failed because of primary key violation; just leave gracefully; id=" + id);

                // It's possible that two threads (from the same session) are creating the session
                // state, both failed to get it first, and now both tried to insert it.
                // One thread may lose with a Primary Key Violation error. If so, that thread will
                // just lose and exit gracefully.
            }
            else
            {
                ThrowSqlConnectionException(conn, e);
            }
        }

        public JQGridDataResult<SessionList> GetSessionList(JQGridDataRequest<SessionList> request)
        {
            bool usePooling = true;
            SqlStateConnection conn = null;
            try
            {
                conn = GetConnection(null, ref usePooling);
                var data = conn.DataContext.GetTable<SessionList>();
                return new JQGridDataResult<SessionList>(request, data);
            }
            finally
            {
                DisposeOrReuseConnection(ref conn, usePooling);
            }
        }

        public JQAutoCompleteResult<SessionList> GetSessionAutocomplete(JQAutoCompleteRequest<SessionList> request)
        {
            bool usePooling = true;
            SqlStateConnection conn = null;
            try
            {
                conn = GetConnection(null, ref usePooling);
                var data = conn.DataContext.GetTable<SessionList>();
                return new JQAutoCompleteResult<SessionList>(request, data);
            }
            finally
            {
                DisposeOrReuseConnection(ref conn, usePooling);
            }
        }

        internal class PartitionInfo : IDisposable
        {
            ResourcePool _rpool;

            internal PartitionInfo(ResourcePool rpool)
            {
                _rpool = rpool;
            }

            internal object RetrieveResource()
            {
                return _rpool.RetrieveResource();
            }

            internal void StoreResource(IDisposable o)
            {
                _rpool.StoreResource(o);
            }

            protected virtual string TracingPartitionString
            {
                get
                {
                    return String.Empty;
                }
            }

            string GetTracingPartitionString()
            {
                return TracingPartitionString;
            }

            public void Dispose()
            {
                if (_rpool == null)
                {
                    return;
                }

                lock (this)
                {
                    if (_rpool != null)
                    {
                        _rpool.Dispose();
                        _rpool = null;
                    }
                }
            }
        };

        internal class SqlPartitionInfo : PartitionInfo
        {
            bool _useIntegratedSecurity;
            string _sqlConnectionString;
            string _tracingPartitionString;
            SupportFlags _support = SupportFlags.Uninitialized;
            string _appSuffix;
            object _lock = new object();
            bool _sqlInfoInited;

            const string APP_SUFFIX_FORMAT = "x8";
            const int APPID_MAX = 280;
            const int SQL_2000_MAJ_VER = 8;

            internal SqlPartitionInfo(ResourcePool rpool, bool useIntegratedSecurity, string sqlConnectionString)
                : base(rpool)
            {
                _useIntegratedSecurity = useIntegratedSecurity;
                _sqlConnectionString = sqlConnectionString;
                //Debug.Trace("PartitionInfo", "Created a new info, sqlConnectionString=" + sqlConnectionString);
            }

            internal bool UseIntegratedSecurity
            {
                get { return _useIntegratedSecurity; }
            }

            internal string SqlConnectionString
            {
                get { return _sqlConnectionString; }
            }

            internal SupportFlags SupportFlags
            {
                get { return _support; }
                set { _support = value; }
            }

            protected override string TracingPartitionString
            {
                get
                {
                    if (_tracingPartitionString == null)
                    {
                        SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(_sqlConnectionString);
                        builder.Password = String.Empty;
                        builder.UserID = String.Empty;
                        _tracingPartitionString = builder.ConnectionString;
                    }
                    return _tracingPartitionString;
                }
            }

            internal string AppSuffix
            {
                get { return _appSuffix; }
            }

            void GetServerSupportOptions(SqlConnection sqlConnection)
            {
                //Debug.Assert(SupportFlags == SupportFlags.Uninitialized);

                SqlCommand cmd;
                SqlDataReader reader = null;
                SupportFlags flags = SupportFlags.None;
                bool v2 = false;
                SqlParameter p;

                // First, check if the SQL server is running Whidbey scripts
                cmd = new SqlCommand("Select name from sysobjects where type = 'P' and name = 'TempGetVersion'", sqlConnection);
                cmd.CommandType = CommandType.Text;

                try
                {
                    reader = cmd.ExecuteReader(CommandBehavior.SingleRow);
                    if (reader.Read())
                    {
                        // This function first appears in Whidbey (v2).  So we know it's
                        // at least 2.0 even without reading its content.
                        v2 = true;
                    }
                }
                catch (Exception e)
                {
                    SessionStateStore.ThrowSqlConnectionException(sqlConnection, e);
                }
                finally
                {
                    if (reader != null)
                    {
                        reader.Close();
                        reader = null;
                    }
                }

                if (!v2)
                {

                    if (s_usePartition)
                    {
                        throw new HttpException(
                                SR.GetString(SR.Need_v2_SQL_Server_partition_resolver,
                                            s_configPartitionResolverType, sqlConnection.DataSource, sqlConnection.Database));
                    }
                    else
                    {
                        throw new HttpException(
                            SR.GetString(SR.Need_v2_SQL_Server));
                    }
                }

                // Then, see if it's SQL 2000 or above

                cmd = new SqlCommand("dbo.GetMajorVersion", sqlConnection);
                cmd.CommandType = CommandType.StoredProcedure;
                p = cmd.Parameters.Add(new SqlParameter("@@ver", SqlDbType.Int));
                p.Direction = ParameterDirection.Output;

                try
                {
                    cmd.ExecuteNonQuery();
                    if ((int)p.Value >= SQL_2000_MAJ_VER)
                    {
                        // For details, see the extensive doc in DoGet method.
                        flags |= SupportFlags.GetLockAge;
                    }

                    //Debug.Trace("PartitionInfo", "SupportFlags initialized to " + flags);

                    SupportFlags = flags;
                }
                catch (Exception e)
                {
                    SessionStateStore.ThrowSqlConnectionException(sqlConnection, e);
                }

            }


            internal void InitSqlInfo(SqlConnection sqlConnection)
            {
                if (_sqlInfoInited)
                {
                    return;
                }

                lock (_lock)
                {
                    if (_sqlInfoInited)
                    {
                        return;
                    }

                    GetServerSupportOptions(sqlConnection);

                    // Get AppSuffix info

                    SqlParameter p;

                    SqlCommand cmdTempGetAppId = new SqlCommand("dbo.TempGetAppID", sqlConnection);
                    cmdTempGetAppId.CommandType = CommandType.StoredProcedure;
                    cmdTempGetAppId.CommandTimeout = s_commandTimeout;

                    // AppDomainAppIdInternal will contain the whole metabase path of the request's app
                    // e.g. /lm/w3svc/1/root/fxtest    
                    p = cmdTempGetAppId.Parameters.Add(new SqlParameter("@appName", SqlDbType.VarChar, APPID_MAX));
                    p.Value = HttpRuntime.AppDomainAppId;

                    p = cmdTempGetAppId.Parameters.Add(new SqlParameter("@appId", SqlDbType.Int));
                    p.Direction = ParameterDirection.Output;
                    p.Value = Convert.DBNull;

                    cmdTempGetAppId.ExecuteNonQuery();
                    //Debug.Assert(!Convert.IsDBNull(p), "!Convert.IsDBNull(p)");
                    int appId = (int)p.Value;
                    _appSuffix = (appId).ToString(APP_SUFFIX_FORMAT, CultureInfo.InvariantCulture);

                    _sqlInfoInited = true;
                }
            }
        };

        /*
            Here are all the sprocs created for session state and how they're used:
            
            CreateTempTables
            - Called during setup
            
            DeleteExpiredSessions
            - Called by SQL agent to remove expired sessions
            
            GetHashCode
            - Called by sproc TempGetAppID
            
            GetMajorVersion
            - Called during setup
            
            TempGetAppID
            - Called when an asp.net application starts up
            
            TempGetStateItem
            - Used for ReadOnly session state
            - Called by v1 asp.net
            - Called by v1.1 asp.net against SQL 7
            
            TempGetStateItem2
            - Used for ReadOnly session state
            - Called by v1.1 asp.net against SQL 2000
            
            TempGetStateItem3
            - Used for ReadOnly session state
            - Called by v2 asp.net
            
            TempGetStateItemExclusive
            - Called by v1 asp.net
            - Called by v1.1 asp.net against SQL 7
            
            TempGetStateItemExclusive2
            - Called by v1.1 asp.net against SQL 2000
            
            TempGetStateItemExclusive3
            - Called by v2 asp.net
            
            TempGetVersion
            - Called by v2 asp.net when an application starts up
            
            TempInsertStateItemLong
            - Used when creating a new session state with size > 7000 bytes
            
            TempInsertStateItemShort
            - Used when creating a new session state with size <= 7000 bytes
            
            TempInsertUninitializedItem
            - Used when creating a new uninitilized session state (cookieless="true" and regenerateExpiredSessionId="true" in config)
            
            TempReleaseStateItemExclusive
            - Used when a request that has acquired the session state (exclusively) hit an error during the page execution
            
            TempRemoveStateItem
            - Used when a session is abandoned
            
            TempResetTimeout
            - Used when a request (with an active session state) is handled by an HttpHandler which doesn't support IRequiresSessionState interface.
            
            TempUpdateStateItemLong
            - Used when updating a session state with size > 7000 bytes
            
            TempUpdateStateItemLongNullShort
            - Used when updating a session state where original size <= 7000 bytes but new size > 7000 bytes
            
            TempUpdateStateItemShort
            - Used when updating a session state with size <= 7000 bytes
            
            TempUpdateStateItemShortNullLong
            - Used when updating a session state where original size > 7000 bytes but new size <= 7000 bytes

        */
        class SqlStateConnection : IDisposable
        {
            SqlConnection _sqlConnection;
            DataContext _context;
            SqlCommand _cmdTempGet;
            SqlCommand _cmdTempGetExclusive;
            SqlCommand _cmdTempReleaseExclusive;
            SqlCommand _cmdTempInsertShort;
            SqlCommand _cmdTempInsertLong;
            SqlCommand _cmdTempUpdateShort;
            SqlCommand _cmdTempUpdateShortNullLong;
            SqlCommand _cmdTempUpdateLong;
            SqlCommand _cmdTempUpdateLongNullShort;
            SqlCommand _cmdTempRemove;
            SqlCommand _cmdTempResetTimeout;
            SqlCommand _cmdTempInsertUninitializedItem;

            SqlPartitionInfo _partitionInfo;

            internal SqlStateConnection(SqlPartitionInfo partitionInfo)
            {
                //Debug.Trace("SessionStateConnectionIdentity", "Connecting under " + WindowsIdentity.GetCurrent().Name);

                _partitionInfo = partitionInfo;
                _sqlConnection = new SqlConnection(_partitionInfo.SqlConnectionString);

                try
                {
                    _sqlConnection.Open();
                }
                catch (Exception e)
                {
                    SqlConnection connection = _sqlConnection;
                    SqlException sqlExpt = e as SqlException;

                    _sqlConnection = null;

                    if (sqlExpt != null &&
                        (sqlExpt.Number == SQL_LOGIN_FAILED ||
                         sqlExpt.Number == SQL_LOGIN_FAILED_2 ||
                         sqlExpt.Number == SQL_LOGIN_FAILED_3))
                    {
                        string user;

                        SqlConnectionStringBuilder scsb = new SqlConnectionStringBuilder(partitionInfo.SqlConnectionString);
                        if (scsb.IntegratedSecurity)
                        {
                            user = WindowsIdentity.GetCurrent().Name;
                        }
                        else
                        {
                            user = scsb.UserID;
                        }

                        HttpException outerException = new HttpException(
                                    SR.GetString(SR.Login_failed_sql_session_database, user), e);

                        e = outerException;
                    }

                    SessionStateStore.ThrowSqlConnectionException(connection, e);
                }

                try
                {

                    //PerfCounters.IncrementCounter(AppPerfCounter.SESSION_SQL_SERVER_CONNECTIONS);
                }
                catch
                {
                    Dispose();
                    throw;
                }
            }

            internal DataContext DataContext
            {
                get
                {
                    if (_context == null)
                    {
                        _context = new DataContext(_sqlConnection);
                    }
                    return _context;
                }
            }

            internal SqlCommand TempGet
            {
                get
                {
                    if (_cmdTempGet == null)
                    {
                        SqlParameter p;

                        _cmdTempGet = new SqlCommand("dbo.TempGetStateItem3", _sqlConnection);
                        _cmdTempGet.CommandType = CommandType.StoredProcedure;
                        _cmdTempGet.CommandTimeout = s_commandTimeout;

                        // Use a different set of parameters for the sprocs that support GetLockAge
                        if ((_partitionInfo.SupportFlags & SupportFlags.GetLockAge) != 0)
                        {
                            _cmdTempGet.Parameters.Add(new SqlParameter("@id", SqlDbType.NVarChar, ID_LENGTH));
                            p = _cmdTempGet.Parameters.Add(new SqlParameter("@itemShort", SqlDbType.VarBinary, ITEM_SHORT_LENGTH));
                            p.Direction = ParameterDirection.Output;
                            p = _cmdTempGet.Parameters.Add(new SqlParameter("@locked", SqlDbType.Bit));
                            p.Direction = ParameterDirection.Output;
                            p = _cmdTempGet.Parameters.Add(new SqlParameter("@lockAge", SqlDbType.Int));
                            p.Direction = ParameterDirection.Output;
                            p = _cmdTempGet.Parameters.Add(new SqlParameter("@lockCookie", SqlDbType.Int));
                            p.Direction = ParameterDirection.Output;
                            p = _cmdTempGet.Parameters.Add(new SqlParameter("@actionFlags", SqlDbType.Int));
                            p.Direction = ParameterDirection.Output;
                        }
                        else
                        {
                            _cmdTempGet.Parameters.Add(new SqlParameter("@id", SqlDbType.NVarChar, ID_LENGTH));
                            p = _cmdTempGet.Parameters.Add(new SqlParameter("@itemShort", SqlDbType.VarBinary, ITEM_SHORT_LENGTH));
                            p.Direction = ParameterDirection.Output;
                            p = _cmdTempGet.Parameters.Add(new SqlParameter("@locked", SqlDbType.Bit));
                            p.Direction = ParameterDirection.Output;
                            p = _cmdTempGet.Parameters.Add(new SqlParameter("@lockDate", SqlDbType.DateTime));
                            p.Direction = ParameterDirection.Output;
                            p = _cmdTempGet.Parameters.Add(new SqlParameter("@lockCookie", SqlDbType.Int));
                            p.Direction = ParameterDirection.Output;
                            p = _cmdTempGet.Parameters.Add(new SqlParameter("@actionFlags", SqlDbType.Int));
                            p.Direction = ParameterDirection.Output;
                        }
                    }

                    return _cmdTempGet;
                }
            }

            internal SqlCommand TempGetExclusive
            {
                get
                {
                    if (_cmdTempGetExclusive == null)
                    {
                        SqlParameter p;

                        _cmdTempGetExclusive = new SqlCommand("dbo.TempGetStateItemExclusive3", _sqlConnection);
                        _cmdTempGetExclusive.CommandType = CommandType.StoredProcedure;
                        _cmdTempGetExclusive.CommandTimeout = s_commandTimeout;

                        // Use a different set of parameters for the sprocs that support GetLockAge
                        if ((_partitionInfo.SupportFlags & SupportFlags.GetLockAge) != 0)
                        {
                            _cmdTempGetExclusive.Parameters.Add(new SqlParameter("@id", SqlDbType.NVarChar, ID_LENGTH));
                            p = _cmdTempGetExclusive.Parameters.Add(new SqlParameter("@itemShort", SqlDbType.VarBinary, ITEM_SHORT_LENGTH));
                            p.Direction = ParameterDirection.Output;
                            p = _cmdTempGetExclusive.Parameters.Add(new SqlParameter("@locked", SqlDbType.Bit));
                            p.Direction = ParameterDirection.Output;
                            p = _cmdTempGetExclusive.Parameters.Add(new SqlParameter("@lockAge", SqlDbType.Int));
                            p.Direction = ParameterDirection.Output;
                            p = _cmdTempGetExclusive.Parameters.Add(new SqlParameter("@lockCookie", SqlDbType.Int));
                            p.Direction = ParameterDirection.Output;
                            p = _cmdTempGetExclusive.Parameters.Add(new SqlParameter("@actionFlags", SqlDbType.Int));
                            p.Direction = ParameterDirection.Output;
                        }
                        else
                        {
                            _cmdTempGetExclusive.Parameters.Add(new SqlParameter("@id", SqlDbType.NVarChar, ID_LENGTH));
                            p = _cmdTempGetExclusive.Parameters.Add(new SqlParameter("@itemShort", SqlDbType.VarBinary, ITEM_SHORT_LENGTH));
                            p.Direction = ParameterDirection.Output;
                            p = _cmdTempGetExclusive.Parameters.Add(new SqlParameter("@locked", SqlDbType.Bit));
                            p.Direction = ParameterDirection.Output;
                            p = _cmdTempGetExclusive.Parameters.Add(new SqlParameter("@lockDate", SqlDbType.DateTime));
                            p.Direction = ParameterDirection.Output;
                            p = _cmdTempGetExclusive.Parameters.Add(new SqlParameter("@lockCookie", SqlDbType.Int));
                            p.Direction = ParameterDirection.Output;
                            p = _cmdTempGetExclusive.Parameters.Add(new SqlParameter("@actionFlags", SqlDbType.Int));
                            p.Direction = ParameterDirection.Output;
                        }
                    }

                    return _cmdTempGetExclusive;
                }
            }

            internal SqlCommand TempReleaseExclusive
            {
                get
                {
                    if (_cmdTempReleaseExclusive == null)
                    {
                        /* ReleaseExlusive */
                        _cmdTempReleaseExclusive = new SqlCommand("dbo.TempReleaseStateItemExclusive", _sqlConnection);
                        _cmdTempReleaseExclusive.CommandType = CommandType.StoredProcedure;
                        _cmdTempReleaseExclusive.CommandTimeout = s_commandTimeout;
                        _cmdTempReleaseExclusive.Parameters.Add(new SqlParameter("@id", SqlDbType.NVarChar, ID_LENGTH));
                        _cmdTempReleaseExclusive.Parameters.Add(new SqlParameter("@lockCookie", SqlDbType.Int));
                    }

                    return _cmdTempReleaseExclusive;
                }
            }

            internal SqlCommand TempInsertLong
            {
                get
                {
                    if (_cmdTempInsertLong == null)
                    {
                        _cmdTempInsertLong = new SqlCommand("dbo.TempInsertStateItemLong", _sqlConnection);
                        _cmdTempInsertLong.CommandType = CommandType.StoredProcedure;
                        _cmdTempInsertLong.CommandTimeout = s_commandTimeout;
                        _cmdTempInsertLong.Parameters.Add(new SqlParameter("@id", SqlDbType.NVarChar, ID_LENGTH));
                        _cmdTempInsertLong.Parameters.Add(new SqlParameter("@itemLong", SqlDbType.Image, 8000));
                        _cmdTempInsertLong.Parameters.Add(new SqlParameter("@timeout", SqlDbType.Int));
                        _cmdTempInsertLong.Parameters.Add(new SqlParameter("@sessionUser", SqlDbType.NVarChar, 128));
                        _cmdTempInsertLong.Parameters.Add(new SqlParameter("@sessionLodge", SqlDbType.NVarChar, 128));
                        _cmdTempInsertLong.Parameters.Add(new SqlParameter("@lastUrl", SqlDbType.NVarChar, 255));
                    }

                    return _cmdTempInsertLong;
                }
            }

            internal SqlCommand TempInsertShort
            {
                get
                {
                    /* Insert */
                    if (_cmdTempInsertShort == null)
                    {
                        _cmdTempInsertShort = new SqlCommand("dbo.TempInsertStateItemShort", _sqlConnection);
                        _cmdTempInsertShort.CommandType = CommandType.StoredProcedure;
                        _cmdTempInsertShort.CommandTimeout = s_commandTimeout;
                        _cmdTempInsertShort.Parameters.Add(new SqlParameter("@id", SqlDbType.NVarChar, ID_LENGTH));
                        _cmdTempInsertShort.Parameters.Add(new SqlParameter("@itemShort", SqlDbType.VarBinary, ITEM_SHORT_LENGTH));
                        _cmdTempInsertShort.Parameters.Add(new SqlParameter("@timeout", SqlDbType.Int));
                        _cmdTempInsertShort.Parameters.Add(new SqlParameter("@sessionUser", SqlDbType.NVarChar, 128));
                        _cmdTempInsertShort.Parameters.Add(new SqlParameter("@sessionLodge", SqlDbType.NVarChar, 128));
                        _cmdTempInsertShort.Parameters.Add(new SqlParameter("@lastUrl", SqlDbType.NVarChar, 255));
                    }

                    return _cmdTempInsertShort;
                }
            }

            internal SqlCommand TempUpdateLong
            {
                get
                {
                    if (_cmdTempUpdateLong == null)
                    {
                        _cmdTempUpdateLong = new SqlCommand("dbo.TempUpdateStateItemLong", _sqlConnection);
                        _cmdTempUpdateLong.CommandType = CommandType.StoredProcedure;
                        _cmdTempUpdateLong.CommandTimeout = s_commandTimeout;
                        _cmdTempUpdateLong.Parameters.Add(new SqlParameter("@id", SqlDbType.NVarChar, ID_LENGTH));
                        _cmdTempUpdateLong.Parameters.Add(new SqlParameter("@itemLong", SqlDbType.Image, 8000));
                        _cmdTempUpdateLong.Parameters.Add(new SqlParameter("@timeout", SqlDbType.Int));
                        _cmdTempUpdateLong.Parameters.Add(new SqlParameter("@lockCookie", SqlDbType.Int));
                        _cmdTempUpdateLong.Parameters.Add(new SqlParameter("@sessionUser", SqlDbType.NVarChar, 128));
                        _cmdTempUpdateLong.Parameters.Add(new SqlParameter("@sessionLodge", SqlDbType.NVarChar, 128));
                        _cmdTempUpdateLong.Parameters.Add(new SqlParameter("@lastUrl", SqlDbType.NVarChar, 255));
                    }

                    return _cmdTempUpdateLong;
                }
            }

            internal SqlCommand TempUpdateShort
            {
                get
                {
                    /* Update */
                    if (_cmdTempUpdateShort == null)
                    {
                        _cmdTempUpdateShort = new SqlCommand("dbo.TempUpdateStateItemShort", _sqlConnection);
                        _cmdTempUpdateShort.CommandType = CommandType.StoredProcedure;
                        _cmdTempUpdateShort.CommandTimeout = s_commandTimeout;
                        _cmdTempUpdateShort.Parameters.Add(new SqlParameter("@id", SqlDbType.NVarChar, ID_LENGTH));
                        _cmdTempUpdateShort.Parameters.Add(new SqlParameter("@itemShort", SqlDbType.VarBinary, ITEM_SHORT_LENGTH));
                        _cmdTempUpdateShort.Parameters.Add(new SqlParameter("@timeout", SqlDbType.Int));
                        _cmdTempUpdateShort.Parameters.Add(new SqlParameter("@lockCookie", SqlDbType.Int));
                        _cmdTempUpdateShort.Parameters.Add(new SqlParameter("@sessionUser", SqlDbType.NVarChar, 128));
                        _cmdTempUpdateShort.Parameters.Add(new SqlParameter("@sessionLodge", SqlDbType.NVarChar, 128));
                        _cmdTempUpdateShort.Parameters.Add(new SqlParameter("@lastUrl", SqlDbType.NVarChar, 255));
                    }

                    return _cmdTempUpdateShort;

                }
            }

            internal SqlCommand TempUpdateShortNullLong
            {
                get
                {
                    if (_cmdTempUpdateShortNullLong == null)
                    {
                        _cmdTempUpdateShortNullLong = new SqlCommand("dbo.TempUpdateStateItemShortNullLong", _sqlConnection);
                        _cmdTempUpdateShortNullLong.CommandType = CommandType.StoredProcedure;
                        _cmdTempUpdateShortNullLong.CommandTimeout = s_commandTimeout;
                        _cmdTempUpdateShortNullLong.Parameters.Add(new SqlParameter("@id", SqlDbType.NVarChar, ID_LENGTH));
                        _cmdTempUpdateShortNullLong.Parameters.Add(new SqlParameter("@itemShort", SqlDbType.VarBinary, ITEM_SHORT_LENGTH));
                        _cmdTempUpdateShortNullLong.Parameters.Add(new SqlParameter("@timeout", SqlDbType.Int));
                        _cmdTempUpdateShortNullLong.Parameters.Add(new SqlParameter("@lockCookie", SqlDbType.Int));
                        _cmdTempUpdateShortNullLong.Parameters.Add(new SqlParameter("@sessionUser", SqlDbType.NVarChar, 128));
                        _cmdTempUpdateShortNullLong.Parameters.Add(new SqlParameter("@sessionLodge", SqlDbType.NVarChar, 128));
                        _cmdTempUpdateShortNullLong.Parameters.Add(new SqlParameter("@lastUrl", SqlDbType.NVarChar, 255));
                    }

                    return _cmdTempUpdateShortNullLong;
                }
            }

            internal SqlCommand TempUpdateLongNullShort
            {
                get
                {
                    if (_cmdTempUpdateLongNullShort == null)
                    {
                        _cmdTempUpdateLongNullShort = new SqlCommand("dbo.TempUpdateStateItemLongNullShort", _sqlConnection);
                        _cmdTempUpdateLongNullShort.CommandType = CommandType.StoredProcedure;
                        _cmdTempUpdateLongNullShort.CommandTimeout = s_commandTimeout;
                        _cmdTempUpdateLongNullShort.Parameters.Add(new SqlParameter("@id", SqlDbType.NVarChar, ID_LENGTH));
                        _cmdTempUpdateLongNullShort.Parameters.Add(new SqlParameter("@itemLong", SqlDbType.Image, 8000));
                        _cmdTempUpdateLongNullShort.Parameters.Add(new SqlParameter("@timeout", SqlDbType.Int));
                        _cmdTempUpdateLongNullShort.Parameters.Add(new SqlParameter("@lockCookie", SqlDbType.Int));
                        _cmdTempUpdateLongNullShort.Parameters.Add(new SqlParameter("@sessionUser", SqlDbType.NVarChar, 128));
                        _cmdTempUpdateLongNullShort.Parameters.Add(new SqlParameter("@sessionLodge", SqlDbType.NVarChar, 128));
                        _cmdTempUpdateLongNullShort.Parameters.Add(new SqlParameter("@lastUrl", SqlDbType.NVarChar, 255));
                    }

                    return _cmdTempUpdateLongNullShort;
                }
            }

            internal SqlCommand TempRemove
            {
                get
                {
                    if (_cmdTempRemove == null)
                    {
                        /* Remove */
                        _cmdTempRemove = new SqlCommand("dbo.TempRemoveStateItem", _sqlConnection);
                        _cmdTempRemove.CommandType = CommandType.StoredProcedure;
                        _cmdTempRemove.CommandTimeout = s_commandTimeout;
                        _cmdTempRemove.Parameters.Add(new SqlParameter("@id", SqlDbType.NVarChar, ID_LENGTH));
                        _cmdTempRemove.Parameters.Add(new SqlParameter("@lockCookie", SqlDbType.Int));

                    }

                    return _cmdTempRemove;
                }
            }

            internal SqlCommand TempInsertUninitializedItem
            {
                get
                {
                    if (_cmdTempInsertUninitializedItem == null)
                    {
                        _cmdTempInsertUninitializedItem = new SqlCommand("dbo.TempInsertUninitializedItem", _sqlConnection);
                        _cmdTempInsertUninitializedItem.CommandType = CommandType.StoredProcedure;
                        _cmdTempInsertUninitializedItem.CommandTimeout = s_commandTimeout;
                        _cmdTempInsertUninitializedItem.Parameters.Add(new SqlParameter("@id", SqlDbType.NVarChar, ID_LENGTH));
                        _cmdTempInsertUninitializedItem.Parameters.Add(new SqlParameter("@itemShort", SqlDbType.VarBinary, ITEM_SHORT_LENGTH));
                        _cmdTempInsertUninitializedItem.Parameters.Add(new SqlParameter("@timeout", SqlDbType.Int));
                    }

                    return _cmdTempInsertUninitializedItem;
                }
            }

            internal SqlCommand TempResetTimeout
            {
                get
                {
                    if (_cmdTempResetTimeout == null)
                    {
                        /* ResetTimeout */
                        _cmdTempResetTimeout = new SqlCommand("dbo.TempResetTimeout", _sqlConnection);
                        _cmdTempResetTimeout.CommandType = CommandType.StoredProcedure;
                        _cmdTempResetTimeout.CommandTimeout = s_commandTimeout;
                        _cmdTempResetTimeout.Parameters.Add(new SqlParameter("@id", SqlDbType.NVarChar, ID_LENGTH));
                    }

                    return _cmdTempResetTimeout;
                }
            }

            public void Dispose()
            {
                //Debug.Trace("ResourcePool", "Disposing SqlStateConnection");
                if (_sqlConnection != null)
                {
                    if (_sqlConnection.State != ConnectionState.Closed)
                    {
                        try
                        {
                            _sqlConnection.Close();
                        }
                        catch (Exception ex)
                        {
                            // If we can't close the connection...  Log it somewhere, but don't die.
                            ex.Log();
                        }
                    }
                    _sqlConnection = null;
                    //PerfCounters.DecrementCounter(AppPerfCounter.SESSION_SQL_SERVER_CONNECTIONS);
                }
                if (_context != null)
                {
                    _context.Dispose();
                    _context = null;
                }
            }

            internal SqlConnection Connection
            {
                get { return _sqlConnection; }
            }
        }
    }

    [Table(Name = "SessionList")]
    public class SessionList
    {
        [Column()]
        public string SessionId { get; set; }
        [Column()]
        public DateTime Created { get { return _created.ToLocalTime(); } set { _created = value; } }
        private DateTime _created;
        [Column()]
        public int IdleTime { get; set; }
        [Column()]
        public string SessionUser { get; set; }
        [Column()]
        public string SessionLodge { get; set; }
        [Column()]
        public string LastUrl { get; set; }
    }

    internal static class SR
    {
        internal static string GetString(string strString)
        {
            return strString;
        }
        internal static string GetString(string strString, string param1)
        {
            return string.Format(strString, param1);
        }

        internal static string GetString(string strString, string param1, string param2)
        {
            return string.Format(strString, param1, param2);
        }
        internal static string GetString(string strString, string param1, string param2, string param3)
        {
            return string.Format(strString, param1, param2, param3);
        }

        internal const string Auth_rule_names_cant_contain_char = "Authorization rule names cannot contain the '{0}' character.";
        internal const string Connection_name_not_specified = "The attribute 'connectionStringName' is missing or empty.";
        internal const string Connection_string_not_found = "The connection name '{0}' was not found in the applications configuration or the connection string is empty.";
        internal const string Membership_AccountLockOut = "The user account has been locked out.";
        internal const string Membership_Custom_Password_Validation_Failure = "The custom password validation failed.";
        internal const string Membership_InvalidAnswer = "The password-answer supplied is invalid.";
        internal const string Membership_InvalidEmail = "The E-mail supplied is invalid.";
        internal const string Membership_InvalidPassword = "The password supplied is invalid.  Passwords must conform to the password strength requirements configured for the default provider.";
        internal const string Membership_InvalidProviderUserKey = "The provider user key supplied is invalid.  It must be of type System.Guid.";
        internal const string Membership_InvalidQuestion = "The password-question supplied is invalid.  Note that the current provider configuration requires a valid password question and answer.  As a result, a CreateUser overload that accepts question and answer parameters must also be used.";
        internal const string Membership_more_than_one_user_with_email = "More than one user has the specified e-mail address.";
        internal const string Membership_password_too_long = "The password is too long: it must not exceed 128 chars after encrypting.";
        internal const string Membership_PasswordRetrieval_not_supported = "This Membership Provider has not been configured to support password retrieval.";
        internal const string Membership_UserNotFound = "The user was not found.";
        internal const string Membership_WrongAnswer = "The password-answer supplied is wrong.";
        internal const string Membership_WrongPassword = "The password supplied is wrong.";
        internal const string PageIndex_bad = "The pageIndex must be greater than or equal to zero.";
        internal const string PageIndex_PageSize_bad = "The combination of pageIndex and pageSize cannot exceed the maximum value of System.Int32.";
        internal const string PageSize_bad = "The pageSize must be greater than zero.";
        internal const string Parameter_array_empty = "The array parameter '{0}' should not be empty.";
        internal const string Parameter_can_not_be_empty = "The parameter '{0}' must not be empty.";
        internal const string Parameter_can_not_contain_comma = "The parameter '{0}' must not contain commas.";
        internal const string Parameter_duplicate_array_element = "The array '{0}' should not contain duplicate values.";
        internal const string Parameter_too_long = "The parameter '{0}' is too long: it must not exceed {1} chars in length.";
        internal const string Password_does_not_match_regular_expression = "The parameter '{0}' does not match the regular expression specified in config file.";
        internal const string Password_need_more_non_alpha_numeric_chars = "Non alpha numeric characters in '{0}' needs to be greater than or equal to '{1}'.";
        internal const string Password_too_short = "The length of parameter '{0}' needs to be greater or equal to '{1}'.";
        internal const string PersonalizationProvider_ApplicationNameExceedMaxLength = "The ApplicationName cannot exceed character length {0}.";
        internal const string PersonalizationProvider_BadConnection = "The specified connectionStringName, '{0}', was not registered.";
        internal const string PersonalizationProvider_CantAccess = "A connection could not be made by the {0} personalization provider using the specified registration.";
        internal const string PersonalizationProvider_NoConnection = "The connectionStringName attribute must be specified when registering a personalization provider.";
        internal const string PersonalizationProvider_UnknownProp = "Invalid attribute '{0}', specified in the '{1}' personalization provider registration.";
        internal const string ProfileSqlProvider_description = "SQL profile provider.";
        internal const string Property_Had_Malformed_Url = "The '{0}' property had a malformed URL: {1}.";
        internal const string Provider_application_name_too_long = "The application name is too long.";
        internal const string Provider_bad_password_format = "Password format specified is invalid.";
        internal const string Provider_can_not_retrieve_hashed_password = "Configured settings are invalid: Hashed passwords cannot be retrieved. Either set the password format to different type, or set supportsPasswordRetrieval to false.";
        internal const string Provider_Error = "The Provider encountered an unknown error.";
        internal const string Provider_Not_Found = "Provider '{0}' was not found.";
        internal const string Provider_role_already_exists = "The role '{0}' already exists.";
        internal const string Provider_role_not_found = "The role '{0}' was not found.";
        internal const string Provider_Schema_Version_Not_Match = "The '{0}' requires a database schema compatible with schema version '{1}'.  However, the current database schema is not compatible with this version.  You may need to either install a compatible schema with aspnet_regsql.exe (available in the framework installation directory), or upgrade the provider to a newer version.";
        internal const string Provider_this_user_already_in_role = "The user '{0}' is already in role '{1}'.";
        internal const string Provider_this_user_not_found = "The user '{0}' was not found.";
        internal const string Provider_unknown_failure = "Stored procedure call failed.";
        internal const string Provider_unrecognized_attribute = "Attribute not recognized '{0}'";
        internal const string Provider_user_not_found = "The user was not found in the database.";
        internal const string Role_is_not_empty = "This role cannot be deleted because there are users present in it.";
        internal const string RoleSqlProvider_description = "SQL role provider.";
        internal const string SiteMapProvider_cannot_remove_root_node = "Root node cannot be removed from the providers, use RemoveProvider(string providerName) instead.";
        internal const string SqlError_Connection_String = "An error occurred while attempting to initialize a System.Data.SqlClient.SqlConnection object. The value that was provided for the connection string may be wrong, or it may contain an invalid syntax.";
        internal const string SqlExpress_file_not_found_in_connection_string = "SQL Express filename was not found in the connection string.";
        internal const string SqlPersonalizationProvider_Description = "Personalization provider that stores data in a SQL Server database.";
        internal const string Value_must_be_boolean = "The value must be boolean (true or false) for property '{0}'.";
        internal const string Value_must_be_non_negative_integer = "The value must be a non-negative 32-bit integer for property '{0}'.";
        internal const string Value_must_be_positive_integer = "The value must be a positive 32-bit integer for property '{0}'.";
        internal const string Value_too_big = "The value '{0}' can not be greater than '{1}'.";
        internal const string XmlSiteMapProvider_cannot_add_node = "SiteMapNode {0} cannot be found in current provider, only nodes in the same provider can be added.";
        internal const string XmlSiteMapProvider_Cannot_Be_Inited_Twice = "XmlSiteMapProvider cannot be initialized twice.";
        internal const string XmlSiteMapProvider_cannot_find_provider = "Provider {0} cannot be found inside XmlSiteMapProvider {1}.";
        internal const string XmlSiteMapProvider_cannot_remove_node = "SiteMapNode {0} does not exist in provider {1}, it must be removed from provider {2}.";
        internal const string XmlSiteMapProvider_Description = "SiteMap provider which reads in .sitemap XML files.";
        internal const string XmlSiteMapProvider_Error_loading_Config_file = "The XML sitemap config file {0} could not be loaded.  {1}";
        internal const string XmlSiteMapProvider_FileName_already_in_use = "The sitemap config file {0} is already used by other nodes or providers.";
        internal const string XmlSiteMapProvider_FileName_does_not_exist = "The file {0} required by XmlSiteMapProvider does not exist.";
        internal const string XmlSiteMapProvider_Invalid_Extension = "The file {0} has an invalid extension, only .sitemap files are allowed in XmlSiteMapProvider.";
        internal const string XmlSiteMapProvider_invalid_GetRootNodeCore = "GetRootNode is returning null from Provider {0}, this method must return a non-empty sitemap node.";
        internal const string XmlSiteMapProvider_invalid_resource_key = "Resource key {0} is not valid, it must contain a valid class name and key pair. For example, $resources:'className','key'";
        internal const string XmlSiteMapProvider_invalid_sitemapnode_returned = "Provider {0} must return a valid sitemap node.";
        internal const string XmlSiteMapProvider_missing_siteMapFile = "The {0} attribute must be specified on the XmlSiteMapProvider.";
        internal const string XmlSiteMapProvider_Multiple_Nodes_With_Identical_Key = "Multiple nodes with the same key '{0}' were found. XmlSiteMapProvider requires that sitemap nodes have unique keys.";
        internal const string XmlSiteMapProvider_Multiple_Nodes_With_Identical_Url = "Multiple nodes with the same URL '{0}' were found. XmlSiteMapProvider requires that sitemap nodes have unique URLs.";
        internal const string XmlSiteMapProvider_multiple_resource_definition = "Cannot have more than one resource binding on attribute '{0}'. Ensure that this attribute is not bound through an implicit expression, for example, {0}=\"$resources:key\".";
        internal const string XmlSiteMapProvider_Not_Initialized = "XmlSiteMapProvider is not initialized. Call Initialize() method first.";
        internal const string XmlSiteMapProvider_Only_One_SiteMapNode_Required_At_Top = "Exactly one <siteMapNode> element is required directly inside the <siteMap> element.";
        internal const string XmlSiteMapProvider_Only_SiteMapNode_Allowed = "Only <siteMapNode> elements are allowed at this location.";
        internal const string XmlSiteMapProvider_resourceKey_cannot_be_empty = "Resource key cannot be empty.";
        internal const string XmlSiteMapProvider_Top_Element_Must_Be_SiteMap = "Top element must be siteMap.";
        internal const string PersonalizationProviderHelper_TrimmedEmptyString = "Input parameter '{0}' cannot be an empty string.";
        internal const string StringUtil_Trimmed_String_Exceed_Maximum_Length = "Trimmed string value '{0}' of input parameter '{1}' cannot exceed character length {2}.";
        internal const string MembershipSqlProvider_description = "SQL membership provider.";
        internal const string MinRequiredNonalphanumericCharacters_can_not_be_more_than_MinRequiredPasswordLength = "The minRequiredNonalphanumericCharacters can not be greater than minRequiredPasswordLength.";
        internal const string PersonalizationProviderHelper_Empty_Collection = "Input parameter '{0}' cannot be an empty collection.";
        internal const string PersonalizationProviderHelper_Null_Or_Empty_String_Entries = "Input parameter '{0}' cannot contain null or empty string entries.";
        internal const string PersonalizationProviderHelper_CannotHaveCommaInString = "Input parameter '{0}' cannot have comma in string value '{1}'.";
        internal const string PersonalizationProviderHelper_Trimmed_Entry_Value_Exceed_Maximum_Length = "Trimmed entry value '{0}' of input parameter '{1}' cannot exceed character length {2}.";
        internal const string PersonalizationProviderHelper_More_Than_One_Path = "Input parameter '{0}' cannot contain more than one entry when '{1}' contains some entries.";
        internal const string PersonalizationProviderHelper_Negative_Integer = "The input parameter cannot be negative.";
        internal const string PersonalizationAdmin_UnexpectedPersonalizationProviderReturnValue = "The negative value '{0}' is returned when calling provider's '{1}' method.  The method should return non-negative integer.";
        internal const string PersonalizationProviderHelper_Null_Entries = "Input parameter '{0}' cannot contain null entries.";
        internal const string PersonalizationProviderHelper_Invalid_Less_Than_Parameter = "Input parameter '{0}' must be greater than or equal to {1}.";
        internal const string PersonalizationProviderHelper_No_Usernames_Set_In_Shared_Scope = "Input parameter '{0}' cannot be provided when '{1}' is set to '{2}'.";
        internal const string Provider_this_user_already_not_in_role = "The user '{0}' is already not in role '{1}'.";
        internal const string Not_configured_to_support_password_resets = "This provider is not configured to allow password resets. To enable password reset, set enablePasswordReset to \"true\" in the configuration file.";
        internal const string Parameter_collection_empty = "The collection parameter '{0}' should not be empty.";
        internal const string Provider_can_not_decode_hashed_password = "Hashed passwords cannot be decoded.";
        internal const string DbFileName_can_not_contain_invalid_chars = "The database filename can not contain the following 3 characters: [ (open square brace), ] (close square brace) and ' (single quote)";
        internal const string SQL_Services_Error_Deleting_Session_Job = "The attempt to remove the Session State expired sessions job from msdb did not succeed.  This can occur either because the job no longer exists, or because the job was originally created with a different user account than the account that is currently performing the uninstall.  You will need to manually delete the Session State expired sessions job if it still exists.";
        internal const string SQL_Services_Error_Executing_Command = "An error occurred during the execution of the SQL file '{0}'. The SQL error number is {1} and the SqlException message is: {2}";
        internal const string SQL_Services_Invalid_Feature = "An invalid feature is requested.";
        internal const string SQL_Services_Database_Empty_Or_Space_Only_Arg = "The database name cannot be empty or contain only white space characters.";
        internal const string SQL_Services_Database_contains_invalid_chars = "The custom database name cannot contain the following three characters: single quotation mark ('), left bracket ([) or right bracket (]).";
        internal const string SQL_Services_Error_Cant_Uninstall_Nonexisting_Database = "Cannot uninstall the specified feature(s) because the SQL database '{0}' does not exist.";
        internal const string SQL_Services_Error_Cant_Uninstall_Nonempty_Table = "Cannot uninstall the specified feature(s) because the SQL table '{0}' in the database '{1}' is not empty. You must first remove all rows from the table.";
        internal const string SQL_Services_Error_missing_custom_database = "The database name cannot be null or empty if the session state type is SessionStateType.Custom.";
        internal const string SQL_Services_Error_Cant_use_custom_database = "You cannot specify the database name because it is allowed only if the session state type is SessionStateType.Custom.";
        internal const string SQL_Services_Cant_connect_sql_database = "Unable to connect to SQL Server database.";
        internal const string Error_parsing_sql_partition_resolver_string = "Error parsing the SQL connection string returned by an instance of the IPartitionResolver type '{0}': {1}";
        internal const string Error_parsing_session_sqlConnectionString = "Error parsing <sessionState> sqlConnectionString attribute: {0}";
        internal const string No_database_allowed_in_sqlConnectionString = "The sqlConnectionString attribute or the connection string it refers to cannot contain the connection options 'Database', 'Initial Catalog' or 'AttachDbFileName'. In order to allow this, allowCustomSqlDatabase attribute must be set to true and the application needs to be granted unrestricted SqlClientPermission. Please check with your administrator if the application does not have this permission.";
        internal const string No_database_allowed_in_sql_partition_resolver_string = "The SQL connection string (server='{1}', database='{2}') returned by an instance of the IPartitionResolver type '{0}' cannot contain the connection options 'Database', 'Initial Catalog' or 'AttachDbFileName'. In order to allow this, allowCustomSqlDatabase attribute must be set to true and the application needs to be granted unrestricted SqlClientPermission. Please check with your administrator if the application does not have this permission.";
        internal const string Cant_connect_sql_session_database = "Unable to connect to SQL Server session database.";
        internal const string Cant_connect_sql_session_database_partition_resolver = "Unable to connect to SQL Server session database. The connection string (server='{1}', database='{2}') was returned by an instance of the IPartitionResolver type '{0}'.";
        internal const string Login_failed_sql_session_database = "Failed to login to session state SQL server for user '{0}'.";
        internal const string Need_v2_SQL_Server = "Unable to use SQL Server because ASP.NET version 2.0 Session State is not installed on the SQL server. Please install ASP.NET Session State SQL Server version 2.0 or above.";
        internal const string Need_v2_SQL_Server_partition_resolver = "Unable to use SQL Server because ASP.NET version 2.0 Session State is not installed on the SQL server. Please install ASP.NET Session State SQL Server version 2.0 or above. The connection string (server='{1}', database='{2}') was returned by an instance of the IPartitionResolver type '{0}'.";
        internal const string Invalid_session_state = "The session state information is invalid and might be corrupted.";

        internal const string Missing_required_attribute = "The '{0}' attribute must be specified on the '{1}' tag.";
        internal const string Invalid_boolean_attribute = "The '{0}' attribute must be set to 'true' or 'false'.";
        internal const string Empty_attribute = "The '{0}' attribute cannot be an empty string.";
        internal const string Config_base_unrecognized_attribute = "Unrecognized attribute '{0}'. Note that attribute names are case-sensitive.";
        internal const string Config_base_no_child_nodes = "Child nodes are not allowed.";
        internal const string Unexpected_provider_attribute = "The attribute '{0}' is unexpected in the configuration of the '{1}' provider.";
        internal const string Only_one_connection_string_allowed = "SqlWebEventProvider: Specify either a connectionString or connectionStringName, not both.";
        internal const string Cannot_use_integrated_security = "SqlWebEventProvider: connectionString can only contain connection strings that use Sql Server authentication.  Trusted Connection security is not supported.";
        internal const string Must_specify_connection_string_or_name = "SqlWebEventProvider: Either a connectionString or connectionStringName must be specified.";
        internal const string Invalid_max_event_details_length = "The value '{1}' specified for the maxEventDetailsLength attribute of the '{0}' provider is invalid. It should be between 0 and 1073741823.";
        internal const string Sql_webevent_provider_events_dropped = "{0} events were discarded since last notification was made at {1} because the event buffer capacity was exceeded.";
        internal const string Invalid_provider_positive_attributes = "The attribute '{0}' is invalid in the configuration of the '{1}' provider. The attribute must be set to a non-negative integer.";
    }
    class ResourcePool : IDisposable
    {
        ArrayList _resources;     // the resources
        int _iDisposable;   // resources below this index are candidates for disposal
        int _max;           // max number of resources
        Timer _timer;         // periodic timer
        TimerCallback _callback;      // callback delegate
        TimeSpan _interval;      // callback interval
        bool _disposed;

        internal ResourcePool(TimeSpan interval, int max)
        {
            _interval = interval;
            _resources = new ArrayList(4);
            _max = max;
            _callback = new TimerCallback(this.TimerProc);
        }

        ~ResourcePool()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
        }

        void Dispose(bool disposing)
        {
            lock (this)
            {
                if (!_disposed)
                {
                    if (_resources != null)
                    {
                        foreach (IDisposable resource in _resources)
                        {
                            resource.Dispose();
                        }

                        _resources.Clear();
                    }

                    if (_timer != null)
                    {
                        _timer.Dispose();
                    }

                    _disposed = true;

                    // Suppress finalization of this disposed instance.
                    if (disposing)
                    {
                        GC.SuppressFinalize(this);
                    }
                }
            }
        }

        internal object RetrieveResource()
        {
            object result = null;

            // avoid lock in common case
            if (_resources.Count != 0)
            {
                lock (this)
                {
                    if (!_disposed)
                    {
                        if (_resources.Count == 0)
                        {
                            result = null;
                        }
                        else
                        {
                            result = _resources[_resources.Count - 1];
                            _resources.RemoveAt(_resources.Count - 1);
                            if (_resources.Count < _iDisposable)
                            {
                                _iDisposable = _resources.Count;
                            }
                        }
                    }
                }
            }

            return result;
        }

        internal void StoreResource(IDisposable o)
        {

            lock (this)
            {
                if (!_disposed)
                {
                    if (_resources.Count < _max)
                    {
                        _resources.Add(o);
                        o = null;
                        if (_timer == null)
                        {

#if DBG
                            if (!Debug.IsTagPresent("Timer") || Debug.IsTagEnabled("Timer"))
#endif
                            {
                                _timer = new Timer(_callback, null, _interval, _interval);
                            }
                        }
                    }
                }
            }

            if (o != null)
            {
                o.Dispose();
            }
        }

        void TimerProc(Object userData)
        {
            IDisposable[] a = null;

            lock (this)
            {
                if (!_disposed)
                {
                    if (_resources.Count == 0)
                    {
                        if (_timer != null)
                        {
                            _timer.Dispose();
                            _timer = null;
                        }

                        return;
                    }

                    a = new IDisposable[_iDisposable];
                    _resources.CopyTo(0, a, 0, _iDisposable);
                    _resources.RemoveRange(0, _iDisposable);

                    // It means that whatever remain in _resources will be disposed 
                    // next time the timer proc is called.
                    _iDisposable = _resources.Count;
                }
            }

            if (a != null)
            {
                for (int i = 0; i < a.Length; i++)
                {
                    try
                    {
                        a[i].Dispose();
                    }
                    catch
                    {
                        // ignore all errors
                    }
                }
            }
        }

#if DBG
        internal void DebugValidate() {
            Debug.CheckValid(_resources != null, "_resources != null");

            Debug.CheckValid(0 <= _iDisposable && _iDisposable <= _resources.Count,
                             "0 <= _iDisposable && _iDisposable <= _resources.Count" +
                             ";_iDisposable=" + _iDisposable +
                             ";_resources.Count=" + _resources.Count);

            Debug.CheckValid(_interval > TimeSpan.Zero, "_interval > TimeSpan.Zero" +
                             ";_interval=" + _interval);
        }
#endif
    }
}
