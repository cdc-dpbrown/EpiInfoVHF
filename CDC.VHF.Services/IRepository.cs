using System;

namespace CDC.VHF.Services
{
    public interface IRepository<T> where T : Record
    {
        string Create(T obj);
        T Retrieve(string key);
        void Update(T obj);
        void Delete(string key);
    }
}

//using System;
//using System.Linq.Expressions;

//namespace CDC.VHF.Services
//{
//    public interface IRepository : IDisposable
//    {
//        T Single<T>(Expression<Func<T, bool>> expression) where T : class, new();
//        System.Linq.IQueryable<T> All<T>() where T : class, new();
//        void Add<T>(T item) where T : class, new();
//        void Update<T>(T item) where T : class, new();
//        void Delete<T>(T item) where T : class, new();
//        void Delete<T>(Expression<Func<T, bool>> expression) where T : class, new();
//        void DeleteAll<T>() where T : class, IEntity, new();
//        void CommitChanges();
//    }
//}

