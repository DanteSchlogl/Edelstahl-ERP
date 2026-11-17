namespace Edelstahl.BLL.Exceptions
{
    /// <summary>Se produce cuando el cliente ya existe con el CUIT informado.</summary>
    public sealed class ClienteDuplicadoException : ClienteBusinessException
    {
        public string CUIT { get; }

        public ClienteDuplicadoException(string cuit, string mensaje)
            : base(mensaje, "CLIENTE_DUPLICADO")
        {
            CUIT = cuit;
        }
    }
}
