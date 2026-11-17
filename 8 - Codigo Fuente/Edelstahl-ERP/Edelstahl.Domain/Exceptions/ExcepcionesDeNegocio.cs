using System;
using Edelstahl.Services.DomainModel.Exceptions;

namespace Edelstahl.Domain.Exceptions
{
    /// <summary>
    /// Se produce cuando el cliente ya existe con el CUIT informado.
    /// </summary>
    public sealed class ClienteDuplicadoException : BusinessRuleException
    {
        public string CUIT { get; }

        public ClienteDuplicadoException(string cuit, string mensaje)
            : base(mensaje, "CLIENTE_DUPLICADO")
        {
            CUIT = cuit;
        }
    }

    /// <summary>
    /// Se produce cuando el CUIT ingresado no cumple las reglas establecidas.
    /// </summary>
    public sealed class CUITInvalidoException : BusinessRuleException
    {
        public string CUIT { get; }

        public CUITInvalidoException(string cuit, string mensaje)
            : base(mensaje, "CLIENTE_CUIT_INVALIDO")
        {
            CUIT = cuit;
        }
    }
}
