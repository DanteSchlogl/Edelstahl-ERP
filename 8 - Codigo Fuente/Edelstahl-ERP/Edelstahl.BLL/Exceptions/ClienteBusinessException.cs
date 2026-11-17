using Edelstahl.Services.DomainModel.Exceptions;

namespace Edelstahl.BLL.Exceptions
{
    /// <summary>Base de las excepciones de negocio propias de Cliente.</summary>
    public abstract class ClienteBusinessException : BusinessRuleException
    {
        protected ClienteBusinessException(string mensaje, string codigo)
            : base(mensaje, codigo)
        {
        }
    }
}
