using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Edelstahl.Domain.Common;

namespace Edelstahl.DAL.Store
{
    /// <summary>
    /// Almacén temporal usado en el modo demostración. Los datos
    /// viven mientras la aplicación está abierta.
    ///
    /// El acceso se serializa con un candado, de modo que varios hilos
    /// (por ejemplo el monitor de stock) no vean estados intermedios.
    /// Si una operación falla, las altas y bajas de registros se revierten.
    /// </summary>
    public sealed class MemoryNegocioStore : INegocioStore
    {
        private static readonly Dictionary<Type, PropertyInfo[]> Colecciones =
            new Dictionary<Type, PropertyInfo[]>();

        private readonly object _candado = new object();
        private readonly Dictionary<Type, object> _listas = new Dictionary<Type, object>();

        public TResultado Ejecutar<TResultado>(Func<IUnidadDeTrabajo, TResultado> trabajo)
        {
            if (trabajo == null)
            {
                throw new ArgumentNullException(nameof(trabajo));
            }

            lock (_candado)
            {
                Dictionary<Type, object> copia = TomarCopia();

                try
                {
                    TResultado resultado = trabajo(new UnidadMemoria(this));
                    SincronizarHijos();
                    return resultado;
                }
                catch
                {
                    Restaurar(copia);
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

        private List<T> Lista<T>()
        {
            return (List<T>)ListaDe(typeof(T));
        }

        private object ListaDe(Type tipo)
        {
            object lista;

            if (!_listas.TryGetValue(tipo, out lista))
            {
                lista = Activator.CreateInstance(typeof(List<>).MakeGenericType(tipo));
                _listas[tipo] = lista;
            }

            return lista;
        }

        private static PropertyInfo[] ColeccionesDe(Type tipo)
        {
            lock (Colecciones)
            {
                PropertyInfo[] propiedades;

                if (!Colecciones.TryGetValue(tipo, out propiedades))
                {
                    propiedades = tipo.GetProperties()
                        .Where(p => p.PropertyType.IsGenericType
                            && p.PropertyType.GetGenericTypeDefinition() == typeof(List<>)
                            && typeof(Entity).IsAssignableFrom(p.PropertyType.GetGenericArguments()[0]))
                        .ToArray();

                    Colecciones[tipo] = propiedades;
                }

                return propiedades;
            }
        }

        /// <summary>
        /// Registra, como conjuntos propios, los registros hijos que cuelgan de
        /// una entidad (detalles, ubicaciones, materiales). Así se pueden
        /// consultar igual que en la base de datos.
        /// </summary>
        private void RegistrarHijos(object entidad)
        {
            foreach (PropertyInfo propiedad in ColeccionesDe(entidad.GetType()))
            {
                IList hijos = propiedad.GetValue(entidad, null) as IList;

                if (hijos == null)
                {
                    continue;
                }

                IList lista = (IList)ListaDe(propiedad.PropertyType.GetGenericArguments()[0]);

                foreach (object hijo in hijos)
                {
                    if (!lista.Contains(hijo))
                    {
                        lista.Add(hijo);
                    }

                    RegistrarHijos(hijo);
                }
            }
        }

        private void SincronizarHijos()
        {
            foreach (KeyValuePair<Type, object> par in _listas.ToList())
            {
                if (ColeccionesDe(par.Key).Length == 0)
                {
                    continue;
                }

                foreach (object entidad in ((IEnumerable)par.Value).Cast<object>().ToList())
                {
                    RegistrarHijos(entidad);
                }
            }
        }

        /// <summary>
        /// Guarda la composición de cada lista para poder revertir el
        /// alta o la baja de entidades si la operación falla.
        /// </summary>
        private Dictionary<Type, object> TomarCopia()
        {
            Dictionary<Type, object> copia = new Dictionary<Type, object>();

            foreach (KeyValuePair<Type, object> par in _listas)
            {
                IEnumerable origen = (IEnumerable)par.Value;
                copia[par.Key] = Activator.CreateInstance(par.Value.GetType(), origen);
            }

            return copia;
        }

        private void Restaurar(Dictionary<Type, object> copia)
        {
            _listas.Clear();

            foreach (KeyValuePair<Type, object> par in copia)
            {
                _listas[par.Key] = par.Value;
            }
        }

        private sealed class UnidadMemoria : IUnidadDeTrabajo
        {
            private readonly MemoryNegocioStore _almacen;

            public UnidadMemoria(MemoryNegocioStore almacen)
            {
                _almacen = almacen;
            }

            public IConjunto<T> Conjunto<T>() where T : Entity
            {
                return new ConjuntoMemoria<T>(_almacen, _almacen.Lista<T>());
            }

            public void Guardar()
            {
                _almacen.SincronizarHijos();
            }
        }

        private sealed class ConjuntoMemoria<T> : IConjunto<T> where T : Entity
        {
            private readonly MemoryNegocioStore _almacen;
            private readonly List<T> _lista;
            private readonly IQueryable<T> _consulta;

            public ConjuntoMemoria(MemoryNegocioStore almacen, List<T> lista)
            {
                _almacen = almacen;
                _lista = lista;
                _consulta = lista.AsQueryable();
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
                if (_lista.Any(existente => existente.Id == entidad.Id))
                {
                    throw new InvalidOperationException(
                        "Ya existe un registro con el mismo identificador.");
                }

                _lista.Add(entidad);
                _almacen.RegistrarHijos(entidad);
            }

            public void Quitar(T entidad)
            {
                _lista.Remove(entidad);
            }

            public IConjunto<T> Incluir(string ruta)
            {
                return this;
            }
        }
    }
}
