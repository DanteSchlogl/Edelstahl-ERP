using System;

namespace Edelstahl.Domain.Exceptions
{
    /// <summary>
    /// Excepción base de todos los errores de negocio de Edelstahl ERP.
    /// Su mensaje está pensado para mostrarse al usuario tal cual: la
    /// interfaz atrapa esta clase y no la trata como un error inesperado.
    /// </summary>
    public abstract class BusinessRuleException : Exception
    {
        public string Code { get; }

        protected BusinessRuleException(
            string message,
            string code)
            : base(message)
        {
            Code = code;
        }

        protected BusinessRuleException(
            string message,
            string code,
            Exception innerException)
            : base(message, innerException)
        {
            Code = code;
        }
    }
}
