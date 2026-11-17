using Edelstahl.Services.DomainModel.Security;

namespace Edelstahl.Services.DataAccess.Interfaces
{
    /// <summary>
    /// Define las operaciones de persistencia relacionadas con roles.
    /// </summary>
    public interface IRolRepository
        : IGenericRepository<Rol>
    {
        Rol GetByNombre(string nombre);

        bool ExistsByNombre(string nombre);
    }
}
