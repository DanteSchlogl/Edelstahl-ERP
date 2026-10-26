using Edelstahl.Domain.Comercial;

namespace Edelstahl.DAL.Interfaces
{
    /// <summary>
    /// Define las operaciones de persistencia específicas
    /// para la entidad Cliente.
    /// </summary>
    public interface IClienteRepository : IGenericRepository<Cliente>
    {
        Cliente GetByCUIT(string cuit);

        bool ExistsByCUIT(string cuit);
    }
}
