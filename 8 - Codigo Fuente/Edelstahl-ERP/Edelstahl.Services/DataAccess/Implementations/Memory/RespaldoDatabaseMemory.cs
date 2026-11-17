using System;
using Edelstahl.Services.DataAccess.Interfaces;
using Edelstahl.Services.DomainModel.Exceptions;
using Edelstahl.Services.DomainModel.Security;
using Edelstahl.Services.Localization;

namespace Edelstahl.Services.DataAccess.Implementations.Memory
{
    /// <summary>
    /// Respaldo del modo demostración: no hay bases de datos que respaldar.
    /// </summary>
    internal sealed class RespaldoDatabaseMemory : IRespaldoDatabase
    {
        public string BaseNegocio
        {
            get { return "EdelstahlNegocio"; }
        }

        public string BaseServicios
        {
            get { return "EdelstahlServicios"; }
        }

        public string ObtenerCarpetaPredeterminada()
        {
            throw NoDisponible();
        }

        public ResultadoRespaldo CrearRespaldo(string baseDatos, string carpetaDestino, DateTime fechaHora)
        {
            throw NoDisponible();
        }

        public void VerificarArchivo(string ruta)
        {
            throw NoDisponible();
        }

        public string ObtenerBaseDelArchivo(string ruta)
        {
            throw NoDisponible();
        }

        public void Restaurar(string baseDatos, string ruta)
        {
            throw NoDisponible();
        }

        public string CopiarArchivo(string rutaOrigen, string carpetaDestino)
        {
            throw NoDisponible();
        }

        public bool EsErrorDeArchivo(Exception ex)
        {
            return false;
        }

        private static ReglaDeNegocioException NoDisponible()
        {
            return new ReglaDeNegocioException(
                LanguageService.Translate("Backups are not available in demonstration mode."));
        }
    }
}
