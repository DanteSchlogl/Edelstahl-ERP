using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using Edelstahl.DAL.EntityFramework;
using Edelstahl.Domain.Common;

namespace Edelstahl.DAL.Store
{
    /// <summary>
    /// Almacén que persiste en SQL Server mediante Entity Framework.
    /// Cada ejecución usa su propio contexto y una única transacción.
    /// </summary>
    public sealed class EfNegocioStore : INegocioStore
    {
        public TResultado Ejecutar<TResultado>(Func<IUnidadDeTrabajo, TResultado> trabajo)
        {
            if (trabajo == null)
            {
                throw new ArgumentNullException(nameof(trabajo));
            }

            using (EdelstahlNegocioContext contexto = new EdelstahlNegocioContext())
            using (DbContextTransaction transaccion = contexto.Database.BeginTransaction())
            {
                try
                {
                    TResultado resultado = trabajo(new UnidadEf(contexto));
                    contexto.SaveChanges();
                    transaccion.Commit();
                    return resultado;
                }
                catch
                {
                    transaccion.Rollback();
                    throw;
                }
            }
        }

        public void Ejecutar(Action<IUnidadDeTrabajo> trabajo)
        {
            if (trabajo == null)
            {
                throw new ArgumentNullException(nameof(trabajo));
            }

            Ejecutar<bool>(unidad =>
            {
                trabajo(unidad);
                return true;
            });
        }

        private sealed class UnidadEf : IUnidadDeTrabajo
        {
            private readonly EdelstahlNegocioContext _contexto;

            public UnidadEf(EdelstahlNegocioContext contexto)
            {
                _contexto = contexto;
            }

            public IConjunto<T> Conjunto<T>() where T : Entity
            {
                return new ConjuntoEf<T>(_contexto.Set<T>(), _contexto.Set<T>());
            }

            public void Guardar()
            {
                _contexto.SaveChanges();
            }
        }

        private sealed class ConjuntoEf<T> : IConjunto<T> where T : Entity
        {
            private readonly DbSet<T> _conjunto;
            private readonly IQueryable<T> _consulta;

            public ConjuntoEf(DbSet<T> conjunto, IQueryable<T> consulta)
            {
                _conjunto = conjunto;
                _consulta = consulta;
            }

            public Type ElementType
            {
                get { return _consulta.ElementType; }
            }

            public Expression Expression
            {
                get { return _consulta.Expression; }
            }

            public IQueryProvider Provider
            {
                get { return _consulta.Provider; }
            }

            public IEnumerator<T> GetEnumerator()
            {
                return _consulta.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return _consulta.GetEnumerator();
            }

            public void Agregar(T entidad)
            {
                _conjunto.Add(entidad);
            }

            public void Quitar(T entidad)
            {
                _conjunto.Remove(entidad);
            }

            public IConjunto<T> Incluir(string ruta)
            {
                return new ConjuntoEf<T>(_conjunto, _consulta.Include(ruta));
            }
        }
    }
}
