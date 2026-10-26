using System;

namespace Edelstahl.Domain.Common
{
    /// <summary>
    /// Clase base para las entidades persistentes del sistema Edelstahl.
    /// </summary>
    public abstract class Entity
    {
        /// <summary>
        /// Identificador único de la entidad.
        /// </summary>
        public Guid Id { get; set; }

        protected Entity()
        {
            Id = Guid.NewGuid();
        }
    }
}
