using System;
using System.Linq;
using Edelstahl.Domain.Common;

namespace Edelstahl.DAL.Store
{
    /// <summary>
    /// Conjunto de entidades de un tipo dentro de una unidad de trabajo.
    /// Se consulta con LINQ y se modifica mediante Agregar y Quitar.
    /// </summary>
    public interface IConjunto<T> : IQueryable<T> where T : Entity
    {
        void Agregar(T entidad);

        void Quitar(T entidad);

        /// <summary>
        /// Carga una propiedad de navegación (Detalles, Materiales, etc.).
        /// En el almacén en memoria las colecciones ya están cargadas.
        /// </summary>
        IConjunto<T> Incluir(string ruta);
    }

    /// <summary>
    /// Agrupa las operaciones que deben confirmarse o revertirse juntas.
    /// </summary>
    public interface IUnidadDeTrabajo
    {
        IConjunto<T> Conjunto<T>() where T : Entity;

        /// <summary>
        /// Envía a la base los cambios pendientes sin confirmar la transacción.
        /// Se usa cuando una entidad debe existir antes de ser referenciada.
        /// </summary>
        void Guardar();
    }

    /// <summary>
    /// Punto único de acceso a los datos de los módulos de negocio.
    /// La implementación depende del modo de ejecución.
    /// </summary>
    public interface INegocioStore
    {
        TResultado Ejecutar<TResultado>(Func<IUnidadDeTrabajo, TResultado> trabajo);

        void Ejecutar(Action<IUnidadDeTrabajo> trabajo);
    }
}
