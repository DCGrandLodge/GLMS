using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Migrations;
using System.Data.Entity.Validation;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using EntityFramework.Extensions;
using GLMS.BLL;
using GLMS.BLL.Entities;
using GLMS.DAL.EntityConfiguration;

namespace GLMS.DAL
{
    public class GLMSContext : DbContext, IGLMSContext
    {
        // TODO - Kevin - keep history of dues amounts?
        // TODO - Kevin - use 'Activities'?

        public DbSet<Degree> Degrees { get; set; }
        public DbSet<Office> Offices { get; set; }
        public DbSet<Lodge> Lodges { get; set; }
        public DbSet<Member> Members { get; set; }
        public DbSet<User> Users { get; set; }

        #region IGLMSContext
        IQueryable<Degree> IGLMSContext.Degrees { get { return Degrees; } }
        IQueryable<Office> IGLMSContext.Offices { get { return Offices; } }
        IQueryable<Lodge> IGLMSContext.Lodges { get { return Lodges; } }
        IQueryable<Member> IGLMSContext.Members { get { return Members; } }
        IQueryable<User> IGLMSContext.Users { get { return Users; } }
        #endregion
    
        public GLMSContext()
            : base("GLMSContext")
        {
            SetTimeout();
        }

        private static int IsMigrating = 0;
        public static void Migrate()
        {
            try
            {
                if (0 == System.Threading.Interlocked.Exchange(ref IsMigrating, 1))
                {
                    try
                    {
                        // Automatically migrate database to catch up.
                        var dbMigrator = new DbMigrator(new Migrations.Configuration());
                        var pendingMigrations = dbMigrator.GetPendingMigrations();
                        if (pendingMigrations.Any())
                        {
                            Elmah.ErrorLog.Log(new System.Exception("The database needs these code updates: " + string.Join(", ", pendingMigrations)));
                            dbMigrator.Update();
                        }
                    }
                    finally
                    {
                        System.Threading.Interlocked.Exchange(ref IsMigrating, 0);
                    }
                }
            }
            catch (System.Exception ex)
            {
                Elmah.ErrorLog.Log(ex);
            }
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // Entity Configurations
            modelBuilder.Configurations.Add(new DegreeConfig());
            modelBuilder.Configurations.Add(new LodgeConfig());
            modelBuilder.Configurations.Add(new LodgeMembershipConfig());
            modelBuilder.Configurations.Add(new LodgeOfficerConfig());
            modelBuilder.Configurations.Add(new MemberConfig());
            modelBuilder.Configurations.Add(new MemberDegreeConfig());
            modelBuilder.Configurations.Add(new OfficeConfig());
            modelBuilder.Configurations.Add(new UserConfig());

            // Complex Types
            modelBuilder.ComplexType<Password>();
            modelBuilder.Configurations.Add(new AddressConfig());
        }

        private void SetTimeout()
        {
            try
            {
                (this as IObjectContextAdapter).ObjectContext.CommandTimeout = Database.Connection.ConnectionTimeout;
            }
            catch (Exception)
            {
                // For now, do nothing...  not sure why this happens or what to do about it.
            }
        }

        public void Add<T>(T entity)
            where T : class
        {
            Set<T>().Add(entity);
        }
        public void Update<T>(T entity)
            where T : class
        {
            ((IObjectContextAdapter)this).ObjectContext.ObjectStateManager.ChangeObjectState(entity, EntityState.Modified);
        }
        public void Remove<T>(T entity)
            where T : class
        {
            Set<T>().Remove(entity);
        }
        public T Attach<T>(T entity)
            where T : class
        {
            if (entity != null)
            {
                ObjectContext context = ((IObjectContextAdapter)this).ObjectContext;
                var container = context.MetadataWorkspace.GetEntityContainer(context.DefaultContainerName,
                    DataSpace.CSpace);
                var typeName = entity.GetType().Name;
                string entitySetName = container.BaseEntitySets
                    .Where(x => x.ElementType.Name == typeName)
                    .Select(x => x.Name)
                    .First();
                var key = context.CreateEntityKey(String.Format("{0}.{1}", container.Name, entitySetName), entity);
                ObjectStateEntry entry;
                if (context.ObjectStateManager.TryGetObjectStateEntry(key, out entry))
                {
                    entity = entry.Entity as T;
                }
                else
                {
                    try
                    {
                        Set<T>().Attach(entity);
                    }
                    catch (InvalidOperationException ex)
                    {
                        if (ex.Message == "An object with the same key already exists in the ObjectStateManager. The ObjectStateManager cannot track multiple objects with the same key.")
                        {
                            if (context.ObjectStateManager.TryGetObjectStateEntry(key, out entry))
                            {
                                entity = entry.Entity as T;
                            }
                            else
                            {
                                entity = context.GetObjectByKey(key) as T;
                                if (entity == null)
                                {
                                    string keyName = String.Format("{0}.{1}.", key.EntityContainerName, key.EntitySetName);
                                    List<string> names = key.EntityKeyValues.Select(x => x.Value.ToString()).ToList();
                                    keyName += String.Join(".", names);
                                    throw new InvalidOperationException(
                                        String.Format("Attempted to attach an object with key {0} but the ObjectStateManager says it already exists, but we can't find it.", key), ex);
                                }
                            }
                        }
                        else
                        {
                            throw;
                        }
                    }
                }
            }
            return entity;
        }
        public T Create<T>()
            where T : class
        {
            return Set<T>().Create();
        }

        // EntityFramework.Extensions
        public int BulkInsert<T>(IEnumerable<T> records, Expression<Func<T, T>> insertExpression = null) where T : class
        {
            return Set<T>().BulkInsert(records, insertExpression);
        }

        public int InsertFrom<S, T>(IQueryable<S> query, Expression<Func<S, T>> insertExpression)
            where T : class
            where S : class
        {
            return Set<T>().InsertFrom(query, insertExpression);
        }

        public int Update<T>(IQueryable<T> query, Expression<Func<T, T>> updateExpression) where T : class
        {
            return Set<T>().Update(query, updateExpression);
        }

        public int Update<T>(Expression<Func<T, bool>> filterExpression, Expression<Func<T, T>> updateExpression) where T : class
        {
            return Set<T>().Update(filterExpression, updateExpression);
        }

        public int Delete<T>(IQueryable<T> query) where T : class
        {
            return Set<T>().Delete(query);
        }

        public int Delete<T>(Expression<Func<T, bool>> filterExpression) where T : class
        {
            return Set<T>().Delete(filterExpression);
        }

        void IGLMSContext.SaveChanges()
        {
            SaveChanges();
        }

        bool IGLMSContext.TrySaveChanges(ModelStateDictionary ModelState)
        {
            try
            {
                this.SaveChanges();
            }
            catch (DbEntityValidationException ex)
            {
                var errors = ex.EntityValidationErrors.First();
                foreach (var propertyError in errors.ValidationErrors)
                {
                    ModelState.AddModelError(propertyError.PropertyName, propertyError.ErrorMessage);
                }
                return false;
            }
            catch (Exception ex)
            {
                string errorID = Elmah.ErrorLog.Log(ex);
                ModelState.AddModelError("", String.Format("Error number {0} occured while saving your information.", errorID));
                return false;
            }
            return true;
        }

    }
}
