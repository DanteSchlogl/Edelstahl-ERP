using Edelstahl.Domain.Exceptions;
using Edelstahl.Services.Logic.Infrastructure;

namespace Edelstahl.BLL.Infrastructure
{
    /// <summary>
    /// Mensajes de negocio de clientes: excepciones propias del negocio con su texto traducido.
    /// </summary>
    public static class MensajesCliente
    {
        public static CUITInvalidoException CuitInvalido(string cuit)
        {
            return new CUITInvalidoException(
                cuit,
                Mensajes.T("The CUIT entered is not valid. It must contain 11 digits."));
        }

        public static ClienteDuplicadoException ClienteDuplicado(string cuit)
        {
            return new ClienteDuplicadoException(
                cuit,
                Mensajes.T("There is already a customer registered with the CUIT {0}.", cuit));
        }
    }
}