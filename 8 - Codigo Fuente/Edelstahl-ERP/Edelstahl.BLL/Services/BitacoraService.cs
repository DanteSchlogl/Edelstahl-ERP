using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Edelstahl.BLL.Infrastructure;
using Edelstahl.DAL.Factory;
using Edelstahl.DAL.Interfaces;
using Edelstahl.Domain.Security;

namespace Edelstahl.BLL.Services
{
    public class BitacoraService
    {
        private readonly IBitacoraRepository _repository;

        public BitacoraService()
        {
            _repository = FactoryDataAccess.BitacoraRepository;
        }

        public BitacoraService(IBitacoraRepository repository)
        {
            _repository = repository
                ?? throw new ArgumentNullException(nameof(repository));
        }

        public void Registrar(
            string modulo,
            string accion,
            string entidad,
            Guid? entidadId,
            string descripcion,
            string resultado,
            string tipoEvento,
            string detalleTecnico = "",
            string nombreUsuarioAlternativo = "")
        {
            try
            {
                RegistroBitacora registro = new RegistroBitacora
                {
                    Id = Guid.NewGuid(),
                    FechaHora = DateTime.Now,
                    UsuarioId = SesionActual.EstaIniciada
                        ? (Guid?)SesionActual.Usuario.Id : null,
                    NombreUsuario = SesionActual.EstaIniciada
                        ? SesionActual.Usuario.NombreUsuario
                        : NormalizarUsuario(nombreUsuarioAlternativo),
                    Modulo = Limitar(modulo, 100),
                    Accion = Limitar(accion, 150),
                    Entidad = Limitar(entidad, 100),
                    EntidadId = entidadId,
                    Descripcion = Limitar(descripcion, 1000),
                    Resultado = resultado,
                    DireccionEquipo = ObtenerDireccionEquipo(),
                    NombreEquipo = Limitar(Environment.MachineName, 150),
                    TipoEvento = tipoEvento,
                    DetalleTecnico = detalleTecnico ?? string.Empty
                };

                _repository.Add(registro);
            }
            catch
            {
                // La bitácora nunca debe interrumpir la operación principal.
            }
        }

        public void RegistrarInformacion(
            string modulo,
            string accion,
            string descripcion)
        {
            Registrar(modulo, accion, string.Empty, null, descripcion,
                "Correcto", "Informacion");
        }

        public void RegistrarAuditoria(
            string modulo,
            string accion,
            string entidad,
            Guid? entidadId,
            string descripcion)
        {
            Registrar(modulo, accion, entidad, entidadId, descripcion,
                "Correcto", "Auditoria");
        }

        public void RegistrarSeguridad(
            string accion,
            string descripcion,
            bool correcto,
            string nombreUsuario = "")
        {
            Registrar("Seguridad", accion, "Usuario", null, descripcion,
                correcto ? "Correcto" : "Fallido",
                "Seguridad", string.Empty, nombreUsuario);
        }

        public void RegistrarError(
            string modulo,
            string accion,
            Exception exception,
            string descripcion = "")
        {
            Registrar(modulo, accion, string.Empty, null,
                string.IsNullOrWhiteSpace(descripcion)
                    ? "Se produjo un error en la operación." : descripcion,
                "Fallido", "Error",
                exception == null ? string.Empty : exception.ToString());
        }

        /// <summary>Registros de la bitacora de una entidad (orden, remito...), mas antiguos primero.</summary>
        public List<RegistroBitacora> ObtenerDeEntidad(Guid entidadId)
        {
            return _repository.GetAll().Where(r => r.EntidadId == entidadId).OrderBy(r => r.FechaHora).ToList();
        }

        public List<RegistroBitacora> ObtenerTodos()
        {
            return _repository.GetAll();
        }

        public List<RegistroBitacora> ObtenerPorFecha(
            DateTime desde,
            DateTime hasta)
        {
            return _repository.GetByFecha(desde, hasta);
        }

        /// <summary>
        /// Consulta de la bitácora con filtros opcionales. Requiere permiso de
        /// lectura y la propia consulta queda registrada.
        /// </summary>
        public List<RegistroBitacora> Consultar(
            DateTime? desde,
            DateTime? hasta,
            string usuario,
            string modulo,
            string tipoEvento,
            string texto)
        {
            Autorizacion.Exigir(Permiso.BitacoraVer);

            IEnumerable<RegistroBitacora> consulta = _repository.GetAll();

            if (desde.HasValue)
            {
                consulta = consulta.Where(r => r.FechaHora >= desde.Value.Date);
            }

            if (hasta.HasValue)
            {
                DateTime fin = hasta.Value.Date.AddDays(1);
                consulta = consulta.Where(r => r.FechaHora < fin);
            }

            if (!string.IsNullOrWhiteSpace(usuario))
            {
                consulta = consulta.Where(r => string.Equals(
                    r.NombreUsuario, usuario.Trim(), StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(modulo))
            {
                consulta = consulta.Where(r => string.Equals(
                    r.Modulo, modulo.Trim(), StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(tipoEvento))
            {
                consulta = consulta.Where(r => string.Equals(
                    r.TipoEvento, tipoEvento.Trim(), StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(texto))
            {
                string buscado = texto.Trim().ToLowerInvariant();
                consulta = consulta.Where(r =>
                    (r.Descripcion ?? string.Empty).ToLowerInvariant().Contains(buscado)
                    || (r.Accion ?? string.Empty).ToLowerInvariant().Contains(buscado)
                    || (r.Entidad ?? string.Empty).ToLowerInvariant().Contains(buscado));
            }

            return consulta.OrderByDescending(r => r.FechaHora).ToList();
        }

        public List<string> ObtenerModulos()
        {
            return _repository.GetAll()
                .Select(r => r.Modulo)
                .Where(m => !string.IsNullOrWhiteSpace(m))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(m => m)
                .ToList();
        }

        public List<string> ObtenerUsuarios()
        {
            return _repository.GetAll()
                .Select(r => r.NombreUsuario)
                .Where(u => !string.IsNullOrWhiteSpace(u))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(u => u)
                .ToList();
        }

        private static string ObtenerDireccionEquipo()
        {
            try
            {
                IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (IPAddress direccion in host.AddressList)
                {
                    if (direccion.AddressFamily == AddressFamily.InterNetwork)
                    {
                        return direccion.ToString();
                    }
                }
            }
            catch
            {
            }

            return "No disponible";
        }

        private static string NormalizarUsuario(string usuario)
        {
            return string.IsNullOrWhiteSpace(usuario)
                ? "ANONIMO"
                : Limitar(usuario.Trim().ToLowerInvariant(), 100);
        }

        private static string Limitar(string valor, int maximo)
        {
            string limpio = string.IsNullOrWhiteSpace(valor)
                ? string.Empty
                : valor.Trim();

            return limpio.Length <= maximo
                ? limpio
                : limpio.Substring(0, maximo);
        }
    }
}
