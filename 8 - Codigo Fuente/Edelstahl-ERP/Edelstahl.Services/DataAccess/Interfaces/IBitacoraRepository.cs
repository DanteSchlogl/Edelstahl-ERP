using System;
using System.Collections.Generic;
using Edelstahl.Services.DomainModel.Security;

namespace Edelstahl.Services.DataAccess.Interfaces
{
    public interface IBitacoraRepository
        : IGenericRepository<RegistroBitacora>
    {
        List<RegistroBitacora> GetByUsuarioId(Guid usuarioId);
        List<RegistroBitacora> GetByFecha(DateTime desde, DateTime hasta);
        List<RegistroBitacora> GetByModulo(string modulo);
        List<RegistroBitacora> GetByTipoEvento(string tipoEvento);
    }
}
