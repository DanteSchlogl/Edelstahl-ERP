using Edelstahl.BLL.Exceptions.Base;

namespace Edelstahl.BLL.Exceptions.Commercial
{
    /// <summary>
    /// Se produce cuando ya existe un cliente
    /// registrado con el CUIT informado.
    /// </summary>
    public sealed class ClienteDuplicadoException
        : BusinessRuleException
    {
        public string CUIT { get; }

        public ClienteDuplicadoException(string cuit)
            : base(
                Edelstahl.BLL.Infrastructure.Mensajes.T("There is already a customer registered with the CUIT {0}.", cuit),
                "CLIENTE_DUPLICADO")
        {
            CUIT = cuit;
        }
    }
}
