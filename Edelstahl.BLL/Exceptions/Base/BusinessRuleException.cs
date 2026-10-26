using System;

namespace Edelstahl.BLL.Exceptions.Base
{
    /// <summary>
    /// Excepción base para las reglas de negocio de Edelstahl.
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
