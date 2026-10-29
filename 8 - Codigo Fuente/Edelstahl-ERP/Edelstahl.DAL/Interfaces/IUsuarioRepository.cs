using System;
using System.Collections.Generic;
using Edelstahl.Domain.Security;

namespace Edelstahl.DAL.Interfaces
{
    /// <summary>
    /// Define las operaciones de persistencia relacionadas con usuarios.
    /// </summary>
    public interface IUsuarioRepository
        : IGenericRepository<Usuario>
    {
        Usuario GetByNombreUsuario(string nombreUsuario);

        Usuario GetByEmail(string email);

        List<Usuario> GetByRolId(Guid rolId);

        bool ExistsByNombreUsuario(string nombreUsuario);

        bool ExistsByEmail(string email);
    }
}
