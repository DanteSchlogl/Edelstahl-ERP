using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Edelstahl.BLL.Infrastructure;
using Edelstahl.DAL.Factory;
using Edelstahl.DAL.Interfaces;
using Edelstahl.Domain.Exceptions;
using Edelstahl.Domain.Integrity;
using Edelstahl.Domain.Security;

namespace Edelstahl.BLL.Services
{
    /// <summary>
    /// Crea, lista y restaura respaldos de las bases de negocio y servicios.
    ///
    /// Un respaldo se marca como sano solo si, justo antes de hacerlo, los
    /// dígitos verificadores de los datos eran correctos. El respaldo
    /// automático no se hace cuando hay una alteración, de modo que los
    /// respaldos sanos nunca se reemplazan por datos dañados.
    ///
    /// Solo la base de negocio se restaura desde aquí: la de servicios guarda
    /// los usuarios, la bitácora y este mismo historial.
    /// </summary>
    public class RespaldoService
    {
        private readonly IRespaldoDatabase _database;
        private readonly BitacoraService _bitacora;
        private readonly IRespaldoRepository _repositorio;
        private readonly IntegridadService _integridad;
        private readonly ConfiguracionRespaldo _configuracion;

        public RespaldoService()
            : this(
                FactoryDataAccess.RespaldoDatabase,
                new BitacoraService(),
                FactoryDataAccess.RespaldoRepository,
                new IntegridadService(),
                new ConfiguracionRespaldo())
        {
        }

        public RespaldoService(
            IRespaldoDatabase database,
            BitacoraService bitacora,
            IRespaldoRepository repositorio,
            IntegridadService integridad,
            ConfiguracionRespaldo configuracion)
        {
            _database = database ?? throw new ArgumentNullException(nameof(database));
            _bitacora = bitacora ?? throw new ArgumentNullException(nameof(bitacora));
            _repositorio = repositorio ?? throw new ArgumentNullException(nameof(repositorio));
            _integridad = integridad ?? throw new ArgumentNullException(nameof(integridad));
            _configuracion = configuracion ?? throw new ArgumentNullException(nameof(configuracion));
        }

        public ConfiguracionRespaldo Configuracion
        {
            get { return _configuracion; }
        }

        /// <summary>
        /// Carpeta de respaldos propia de SQL Server (donde el servicio siempre puede escribir).
        /// </summary>
        public string ObtenerCarpetaRespaldos()
        {
            return _database.ObtenerCarpetaPredeterminada();
        }

        /// <summary>
        /// Carpeta donde se dejan los respaldos: la elegida o, si no hay, la de SQL Server.
        /// </summary>
        public string ObtenerCarpetaActual()
        {
            return string.IsNullOrWhiteSpace(_configuracion.Carpeta)
                ? ObtenerCarpetaRespaldos()
                : _configuracion.Carpeta;
        }

        // ------------------------------------------------------------------
        // Crear
        // ------------------------------------------------------------------

        /// <summary>
        /// Respaldo pedido por un administrador desde el programa.
        /// </summary>
        public List<RegistroRespaldo> CrearRespaldoManual()
        {
            Autorizacion.Exigir(Permiso.RespaldoAdministrar);

            return CrearRespaldo(RegistroRespaldo.OrigenManual, SesionActual.NombreUsuario);
        }

        /// <summary>
        /// Respaldo automático: se hace como máximo uno cada tantas horas y
        /// solo si los datos están sanos. Un fallo no interrumpe el arranque.
        /// Devuelve verdadero si se creó un respaldo.
        /// </summary>
        public bool CrearRespaldoAutomaticoSiCorresponde()
        {
            if (!_configuracion.Automatico)
            {
                return false;
            }

            try
            {
                RegistroRespaldo ultimo = _repositorio
                    .GetByBaseDatos(_database.BaseNegocio)
                    .FirstOrDefault(r => r.Origen != RegistroRespaldo.OrigenAntesDeRestaurar);

                if (ultimo != null &&
                    DateTime.Now - ultimo.FechaHora < TimeSpan.FromHours(_configuracion.HorasEntreRespaldos))
                {
                    return false;
                }

                if (!_integridad.Verificar().Correcto)
                {
                    return false;
                }

                CrearRespaldo(RegistroRespaldo.OrigenAutomatico, "sistema");
                ConservarSoloLosUltimos();
                return true;
            }
            catch (Exception ex)
            {
                _bitacora.RegistrarError(
                    "Respaldo",
                    "Respaldo automatico",
                    ex,
                    "No fue posible crear el respaldo automatico.");
                return false;
            }
        }

        private List<RegistroRespaldo> CrearRespaldo(string origen, string usuario)
        {
            bool sano = _integridad.Verificar().Correcto;
            DateTime fecha = DateTime.Now;
            string carpetaSql = ObtenerCarpetaRespaldos();
            string carpetaElegida = _configuracion.Carpeta;
            List<RegistroRespaldo> creados = new List<RegistroRespaldo>();

            try
            {
                foreach (string baseDatos in new[]
                {
                    _database.BaseNegocio,
                    _database.BaseServicios
                })
                {
                    RegistroRespaldo registro = CrearArchivo(baseDatos, fecha, carpetaSql, carpetaElegida);
                    registro.Origen = origen;
                    registro.Sano = sano;
                    registro.Usuario = usuario ?? string.Empty;
                    _repositorio.Add(registro);
                    creados.Add(registro);
                }

                _bitacora.Registrar(
                    "Respaldo",
                    "Crear respaldo completo",
                    "Bases de datos",
                    null,
                    "Se crearon los respaldos de " + _database.BaseNegocio + " y " +
                    _database.BaseServicios + " (" + origen + ", " +
                    (sano ? "datos sanos" : "los datos NO pasaron la verificacion de integridad") +
                    ") en " + (string.IsNullOrWhiteSpace(carpetaElegida) ? carpetaSql : carpetaElegida) + ".",
                    sano ? "Correcto" : "Advertencia",
                    "Auditoria",
                    string.Empty,
                    usuario);

                return creados;
            }
            catch (Exception ex)
            {
                _bitacora.Registrar(
                    "Respaldo",
                    "Crear respaldo completo",
                    "Bases de datos",
                    null,
                    "No fue posible completar el respaldo de las bases de datos.",
                    "Fallido",
                    "Error",
                    ex.ToString(),
                    usuario);

                throw;
            }
        }

        /// <summary>
        /// Escribe el archivo de una base. Si hay una carpeta elegida, SQL
        /// Server intenta escribir ahí; si su cuenta no tiene permiso, escribe
        /// en su carpeta y el programa copia el archivo con los permisos del
        /// usuario de Windows.
        /// </summary>
        private RegistroRespaldo CrearArchivo(
            string baseDatos,
            DateTime fecha,
            string carpetaSql,
            string carpetaElegida)
        {
            ResultadoRespaldo resultado;
            string copia = string.Empty;

            if (string.IsNullOrWhiteSpace(carpetaElegida))
            {
                resultado = _database.CrearRespaldo(baseDatos, carpetaSql, fecha);
            }
            else
            {
                try
                {
                    resultado = _database.CrearRespaldo(baseDatos, carpetaElegida, fecha);
                }
                catch (Exception ex) when (_database.EsErrorDeArchivo(ex))
                {
                    resultado = _database.CrearRespaldo(baseDatos, carpetaSql, fecha);
                    copia = _database.CopiarArchivo(resultado.RutaArchivo, carpetaElegida);
                }
            }

            return new RegistroRespaldo
            {
                FechaHora = fecha,
                BaseDatos = baseDatos,
                RutaSql = resultado.RutaArchivo,
                RutaCopia = copia,
                TamanioBytes = resultado.TamanioBytes
            };
        }

        /// <summary>
        /// Borra los respaldos automáticos más viejos y deja los últimos N
        /// por base. Los manuales y las copias previas a restaurar no se tocan.
        /// </summary>
        private void ConservarSoloLosUltimos()
        {
            foreach (string baseDatos in new[]
            {
                _database.BaseNegocio,
                _database.BaseServicios
            })
            {
                List<RegistroRespaldo> sobrantes = _repositorio
                    .GetByBaseDatos(baseDatos)
                    .Where(r => r.Origen == RegistroRespaldo.OrigenAutomatico)
                    .Skip(_configuracion.Conservar)
                    .ToList();

                foreach (RegistroRespaldo viejo in sobrantes)
                {
                    BorrarArchivo(viejo.RutaSql);
                    BorrarArchivo(viejo.RutaCopia);
                    _repositorio.Delete(viejo.Id);
                }
            }
        }

        private static void BorrarArchivo(string ruta)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(ruta) && File.Exists(ruta))
                {
                    File.Delete(ruta);
                }
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                // Un archivo que no se puede borrar no impide seguir.
            }
        }

        // ------------------------------------------------------------------
        // Listar
        // ------------------------------------------------------------------

        /// <summary>
        /// Respaldos de la base de negocio (los que se pueden restaurar), del más nuevo al más antiguo.
        /// </summary>
        public List<RegistroRespaldo> ObtenerRespaldos()
        {
            return _repositorio.GetByBaseDatos(_database.BaseNegocio);
        }

        // ------------------------------------------------------------------
        // Restaurar
        // ------------------------------------------------------------------

        public ResultadoIntegridad Restaurar(RegistroRespaldo respaldo)
        {
            Autorizacion.Exigir(Permiso.RespaldoAdministrar);

            return RestaurarRespaldo(respaldo, SesionActual.NombreUsuario);
        }

        public ResultadoIntegridad RestaurarDesdeArchivo(string ruta)
        {
            Autorizacion.Exigir(Permiso.RespaldoAdministrar);

            return RestaurarArchivo(ruta, SesionActual.NombreUsuario);
        }

        /// <summary>
        /// Restaura un respaldo ya autorizado por quien llama (la sesión del
        /// programa o la pantalla de recuperación). Devuelve el resultado de
        /// verificar los dígitos después de restaurar.
        /// </summary>
        internal ResultadoIntegridad RestaurarRespaldo(RegistroRespaldo respaldo, string usuario)
        {
            if (respaldo == null)
            {
                throw new ArgumentNullException(nameof(respaldo));
            }

            if (!string.Equals(respaldo.BaseDatos, _database.BaseNegocio, StringComparison.Ordinal))
            {
                throw Mensajes.Regla("Only the business database can be restored from here.");
            }

            List<string> rutas = new List<string>();

            if (!string.IsNullOrWhiteSpace(respaldo.RutaSql))
            {
                rutas.Add(respaldo.RutaSql);
            }

            if (!string.IsNullOrWhiteSpace(respaldo.RutaCopia))
            {
                rutas.Add(respaldo.RutaCopia);
            }

            return Restaurar(
                rutas,
                usuario,
                "respaldo del " + respaldo.FechaHora.ToString("dd/MM/yyyy HH:mm:ss") +
                (respaldo.Sano ? " (sano)" : " (sin garantia de integridad)"));
        }

        internal ResultadoIntegridad RestaurarArchivo(string ruta, string usuario)
        {
            if (string.IsNullOrWhiteSpace(ruta))
            {
                throw Mensajes.Regla("Choose the backup file to restore.");
            }

            string baseDelArchivo;

            try
            {
                baseDelArchivo = _database.ObtenerBaseDelArchivo(ruta);
            }
            catch (AccesoADatosException ex)
            {
                // SQL Server no pudo abrir el archivo en su ubicacion: se prueba desde su carpeta.
                if (!_database.EsErrorDeArchivo(ex))
                {
                    throw;
                }

                string copiaLocal = _database.CopiarArchivo(ruta, ObtenerCarpetaRespaldos());
                baseDelArchivo = _database.ObtenerBaseDelArchivo(copiaLocal);
                ruta = copiaLocal;
            }

            if (!string.Equals(baseDelArchivo, _database.BaseNegocio, StringComparison.OrdinalIgnoreCase))
            {
                throw Mensajes.Regla("The selected file is not a backup of the business database.");
            }

            return Restaurar(new List<string> { ruta }, usuario, "archivo " + Path.GetFileName(ruta));
        }

        private ResultadoIntegridad Restaurar(List<string> rutas, string usuario, string origen)
        {
            if (rutas.Count == 0)
            {
                throw Mensajes.Regla("The backup file was not found.");
            }

            try
            {
                CrearCopiaPrevia(usuario);

                RestaurarPrimeraRutaDisponible(rutas);

                // Un respaldo viejo puede no tener las tablas o columnas nuevas.
                FactoryDataAccess.PrepararBaseDeDatos();

                ResultadoIntegridad resultado = _integridad.Verificar();

                _bitacora.Registrar(
                    "Respaldo",
                    "Restaurar respaldo",
                    "Bases de datos",
                    null,
                    "Se restauro la base " + _database.BaseNegocio + " desde el " + origen + ". " +
                    (resultado.Correcto
                        ? "Los digitos verificadores son correctos."
                        : "Atencion: los datos restaurados siguen sin pasar la verificacion de integridad."),
                    resultado.Correcto ? "Correcto" : "Advertencia",
                    "Auditoria",
                    string.Empty,
                    usuario);

                return resultado;
            }
            catch (Exception ex)
            {
                _bitacora.Registrar(
                    "Respaldo",
                    "Restaurar respaldo",
                    "Bases de datos",
                    null,
                    "No fue posible restaurar la base " + _database.BaseNegocio + " desde el " + origen + ".",
                    "Fallido",
                    "Error",
                    ex.ToString(),
                    usuario);

                throw;
            }
        }

        /// <summary>
        /// Prueba cada ruta (la de SQL Server y la copia) hasta que una
        /// restaure. Si SQL Server no puede abrir el archivo donde está, se
        /// reintenta con una copia dentro de su propia carpeta.
        /// </summary>
        private void RestaurarPrimeraRutaDisponible(List<string> rutas)
        {
            Exception ultimoError = null;
            string carpetaSql = ObtenerCarpetaRespaldos();

            foreach (string ruta in rutas)
            {
                if (IntentarRestaurar(ruta, ref ultimoError))
                {
                    return;
                }

                if (!_database.EsErrorDeArchivo(ultimoError))
                {
                    continue;
                }

                string copia;

                try
                {
                    copia = _database.CopiarArchivo(ruta, carpetaSql);
                }
                catch (RespaldoException ex)
                {
                    ultimoError = ex;
                    continue;
                }

                if (IntentarRestaurar(copia, ref ultimoError))
                {
                    return;
                }
            }

            throw new RespaldoException(
                Mensajes.T("The backup could not be restored: {0}",
                    ultimoError == null ? string.Empty : ultimoError.Message),
                ultimoError);
        }

        private bool IntentarRestaurar(string ruta, ref Exception error)
        {
            try
            {
                _database.VerificarArchivo(ruta);
                _database.Restaurar(_database.BaseNegocio, ruta);
                return true;
            }
            catch (Exception ex) when (ex is RespaldoException || ex is AccesoADatosException)
            {
                error = ex;
                return false;
            }
        }

        /// <summary>
        /// Antes de reemplazar la base se guarda una copia de cómo estaba, por
        /// si hiciera falta revisar qué se alteró. No se ofrece como sana. Si
        /// no se puede hacer, la restauración sigue (es lo que el usuario pidió).
        /// </summary>
        private void CrearCopiaPrevia(string usuario)
        {
            try
            {
                string carpetaSql = ObtenerCarpetaRespaldos();
                DateTime fecha = DateTime.Now;
                ResultadoRespaldo resultado = _database.CrearRespaldo(
                    _database.BaseNegocio, carpetaSql, fecha);

                _repositorio.Add(new RegistroRespaldo
                {
                    FechaHora = fecha,
                    BaseDatos = _database.BaseNegocio,
                    RutaSql = resultado.RutaArchivo,
                    TamanioBytes = resultado.TamanioBytes,
                    Origen = RegistroRespaldo.OrigenAntesDeRestaurar,
                    Sano = false,
                    Usuario = usuario ?? string.Empty,
                    Observaciones = "Copia del estado previo a una restauracion."
                });
            }
            catch (Exception ex)
            {
                _bitacora.Registrar(
                    "Respaldo",
                    "Copia previa a restaurar",
                    "Bases de datos",
                    null,
                    "No se pudo guardar una copia del estado previo a la restauracion; se continua con la restauracion.",
                    "Advertencia",
                    "Error",
                    ex.ToString(),
                    usuario);
            }
        }
    }
}
