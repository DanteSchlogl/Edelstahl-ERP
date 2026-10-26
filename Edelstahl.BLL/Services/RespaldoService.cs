using System;
using System.Collections.Generic;
using Edelstahl.DAL.Backup;
using Edelstahl.Domain.Security;

namespace Edelstahl.BLL.Services
{
    /// <summary>
    /// Coordina el respaldo de las bases de negocio y servicios.
    /// </summary>
    public class RespaldoService
    {
        private readonly RespaldoDatabaseService _databaseService;
        private readonly BitacoraService _bitacoraService;

        public RespaldoService()
        {
            _databaseService = new RespaldoDatabaseService();
            _bitacoraService = new BitacoraService();
        }

        public string ObtenerCarpetaRespaldos()
        {
            return _databaseService.ObtenerCarpetaPredeterminada();
        }

        public List<ResultadoRespaldo> CrearRespaldoCompleto()
        {
            DateTime fechaHora = DateTime.Now;
            string carpeta = ObtenerCarpetaRespaldos();
            List<ResultadoRespaldo> resultados =
                new List<ResultadoRespaldo>();

            try
            {
                resultados.Add(
                    _databaseService.CrearRespaldo(
                        "EdelstahlNegocio",
                        carpeta,
                        fechaHora));

                resultados.Add(
                    _databaseService.CrearRespaldo(
                        "EdelstahlServicios",
                        carpeta,
                        fechaHora));

                _bitacoraService.RegistrarAuditoria(
                    "Respaldo",
                    "Crear respaldo completo",
                    "Bases de datos",
                    null,
                    "Se crearon correctamente los respaldos de " +
                    "EdelstahlNegocio y EdelstahlServicios en " +
                    carpeta + ".");

                return resultados;
            }
            catch (Exception ex)
            {
                _bitacoraService.RegistrarError(
                    "Respaldo",
                    "Crear respaldo completo",
                    ex,
                    "No fue posible completar el respaldo de las bases de datos.");

                throw;
            }
        }
    }
}
