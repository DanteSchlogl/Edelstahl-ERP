using System;
using System.Collections.Generic;

namespace Edelstahl.DAL.Interfaces
{
    /// <summary>
    /// Define las operaciones básicas de persistencia
    /// para las entidades del sistema.
    /// </summary>
    public interface IGenericRepository<T>
    {
        void Add(T entity);

        void Update(T entity);

        void Delete(Guid id);

        T GetById(Guid id);

        List<T> GetAll();
    }
}