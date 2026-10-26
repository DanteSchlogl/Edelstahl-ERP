using System;

namespace Edelstahl.BLL.Exceptions.Base
{
    /// <summary>
    /// Se produce cuando una operación incumple una regla del negocio.
    /// El mensaje ya está traducido al idioma activo.
    /// </summary>
    public sealed class ReglaDeNegocioException : BusinessRuleException
    {
        public ReglaDeNegocioException(string mensaje)
            : base(mensaje, "REGLA_NEGOCIO")
        {
        }

        public ReglaDeNegocioException(string mensaje, string codigo)
            : base(mensaje, codigo)
        {
        }
    }

    /// <summary>
    /// Se produce cuando el usuario no tiene permiso para la operación.
    /// </summary>
    public sealed class AccesoNoAutorizadoException : BusinessRuleException
    {
        public AccesoNoAutorizadoException(string mensaje)
            : base(mensaje, "ACCESO_NO_AUTORIZADO")
        {
        }
    }
}
