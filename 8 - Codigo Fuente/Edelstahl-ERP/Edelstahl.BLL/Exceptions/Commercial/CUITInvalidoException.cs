using Edelstahl.BLL.Exceptions.Base;

namespace Edelstahl.BLL.Exceptions.Commercial
{
    /// <summary>
    /// Se produce cuando el CUIT ingresado no cumple
    /// con las reglas establecidas.
    /// </summary>
    public sealed class CUITInvalidoException
        : BusinessRuleException
    {
        public string CUIT { get; }

        public CUITInvalidoException(string cuit)
            : base(
                Edelstahl.BLL.Infrastructure.Mensajes.T("The CUIT entered is not valid. It must contain 11 digits."),
                "CLIENTE_CUIT_INVALIDO")
        {
            CUIT = cuit;
        }
    }
}