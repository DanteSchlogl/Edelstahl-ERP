using System.Collections.Generic;
using Edelstahl.Domain.Security;

namespace Edelstahl.DAL.Interfaces
{
    /// <summary>
    /// Historial de los respaldos creados. Vive en la base de servicios, que
    /// sigue sana aunque la base de negocio resulte alterada.
    /// </summary>
    public interface IRespaldoRepository
        : IGenericRepository<RegistroRespaldo>
    {
        /// <summary>
        /// Respaldos de una base, del más nuevo al más antiguo.
        /// </summary>
        List<RegistroRespaldo> GetByBaseDatos(string baseDatos);
    }
}
