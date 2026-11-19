using System;
using Edelstahl.Services.DataAccess.Interfaces;
using Edelstahl.Services.DomainModel.Exceptions;
using Edelstahl.Services.DomainModel.Integrity;
using Edelstahl.Services.DomainModel.Security;
using Edelstahl.Services.Logic.Infrastructure;

namespace Edelstahl.DAL.Implementations.Memory
{
    /// <summary>
    /// Verificador de integridad del modo demostración: los datos están en
    /// memoria y no tienen dígitos verificadores, así que siempre son correctos.
    /// </summary>
    internal sealed class VerificadorIntegridadMemory : IVerificadorIntegridad
    {
        public ResultadoIntegridad Verificar()
        {
            return new ResultadoIntegridad();
        }

        public int Recalcular()
        {
            return 0;
        }
    }
}
