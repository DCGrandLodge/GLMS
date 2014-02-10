using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using GLMS.BLL.Entities;

namespace GLMS.BLL
{
    public interface IGLMSContext : IDisposable
    {
        // Entities
        IQueryable<Degree> Degrees { get; }
        IQueryable<Lodge> Lodges { get; }
        IQueryable<Member> Members { get; }
        IQueryable<User> Users { get; }
        IQueryable<Office> Offices { get; }

        // CRUD
        void Add<T>(T entity) where T : class;
        void Update<T>(T entity) where T : class;
        void Remove<T>(T entity) where T : class;
        T Attach<T>(T entity) where T : class;
        T Create<T>() where T : class;

        // EntityFramework.Extensions
        int InsertFrom<S, T>(IQueryable<S> query, Expression<Func<S, T>> insertExpression) where T : class where S : class;
        int BulkInsert<T>(IEnumerable<T> records, Expression<Func<T, T>> insertExpression = null) where T : class;
        int Delete<T>(IQueryable<T> query) where T : class;
        int Delete<T>(Expression<Func<T, bool>> filterExpression) where T : class;
        int Update<T>(IQueryable<T> query, Expression<Func<T, T>> updateExpression) where T : class;
        int Update<T>(Expression<Func<T, bool>> filterExpression, Expression<Func<T, T>> updateExpression) where T : class;

        // Save Changes
        void SaveChanges();
        bool TrySaveChanges(ModelStateDictionary ModelState);
    
    }
}
