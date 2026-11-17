namespace Edelstahl.BLL.Exceptions
{
    /// <summary>Se produce cuando el CUIT ingresado no cumple las reglas establecidas.</summary>
    public sealed class CUITInvalidoException : ClienteBusinessException
    {
        public string CUIT { get; }

        public CUITInvalidoException(string cuit, string mensaje)
            : base(mensaje, "CLIENTE_CUIT_INVALIDO")
        {
            CUIT = cuit;
        }
    }
}
