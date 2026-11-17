using System;
using System.Collections.Generic;
using Edelstahl.Services.DomainModel.Composite;

namespace Edelstahl.Services.DataAccess.Interfaces
{
    /// <summary>Arma un objeto del dominio a partir de los valores de una fila.</summary>
    internal interface IAdapter<T>
    {
        T Get(object[] valores);
    }

    public interface IGenericRepository<T>
    {
        void Add(T entity);

        void Update(T entity);

        void Delete(Guid id);

        T GetById(Guid id);

        List<T> GetAll();
    }

    internal interface IFamiliaRepository : IGenericRepository<Familia>
    {
        Familia GetByNombre(string nombre);
    }

    internal interface IPatenteRepository : IGenericRepository<Patente>
    {
        Patente GetByDataKey(string dataKey);
    }

    /// <summary>
    /// Relación entre dos tablas (Familia-Familia, Familia-Patente, Usuario-Familia, Usuario-Patente).
    /// T es el hijo y Y es el padre.
    /// </summary>
    internal interface IJoinRepository<T, Y>
    {
        List<T> GetByObject(Y padre);

        bool Existe(T hijo, Y padre);

        void Add(T hijo, Y padre);

        void AddRange(List<T> hijos, Y padre);

        void Delete(T hijo, Y padre);
    }
}
