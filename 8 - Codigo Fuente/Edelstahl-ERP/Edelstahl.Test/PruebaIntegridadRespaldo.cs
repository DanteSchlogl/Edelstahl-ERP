using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Globalization;
using System.Linq;
using System.Resources;
using Edelstahl.BLL.Infrastructure;
using Edelstahl.BLL.Services;
using Edelstahl.DAL.Backup;
using Edelstahl.DAL.EntityFramework;
using Edelstahl.DAL.Factory;
using Edelstahl.DAL.Interfaces;
using Edelstahl.DAL.Tools;
using Edelstahl.Domain.Comercial;
using Edelstahl.Domain.Exceptions;
using Edelstahl.Domain.Integrity;
using Edelstahl.Domain.Security;
using Edelstahl.Services.Configuration;
using Edelstahl.Services.Localization;
using Edelstahl.WinApp.Ui;

namespace Edelstahl.Test
{
    /// <summary>
    /// Pruebas de las correcciones de arquitectura: Factory por reflexión,
    /// excepciones de negocio, SqlHelper, dígitos verificadores con
    /// recuperación, respaldo automático y restauración.
    /// </summary>
    public static class PruebaIntegridadRespaldo
    {
        // ---------------------------------------------------------------
        // Idiomas con recursos (.resx)
        // ---------------------------------------------------------------

        public static void Idiomas()
        {
            Verificador.Seccion("Idiomas con recursos (.resx)");

            string[] codigos = LanguageService.ObtenerIdiomasDisponibles();
            Verificador.Igual(7, codigos.Length, "Hay siete idiomas disponibles");

            const string clave = "Waiting for the invoice that records the deposit";

            Verificador.Igual("Atendido por", LanguageService.Translate("Handled by", "es-AR"), "es-AR: se lee del recurso del idioma");
            Verificador.Igual("Atendido por", LanguageService.Translate("Handled by", "es-ES"), "es-ES: se lee del recurso del idioma");
            Verificador.Igual("Wartet auf die Rechnung mit der Anzahlung", LanguageService.Translate(clave, "de-DE"), "de-DE: se lee del recurso del idioma");

            foreach (string codigo in new[] { "pt-BR", "ru-RU" })
            {
                Verificador.Verificar(LanguageService.Translate(clave, codigo) != clave, codigo + ": hay traduccion en el recurso del idioma");
            }

            Verificador.Igual(clave, LanguageService.Translate(clave, "en-US"), "en-US (neutro): el texto en ingles es la clave");
            Verificador.Igual("texto que no existe", LanguageService.Translate("texto que no existe", "es-AR"), "Sin traduccion se muestra la clave");
            Verificador.Igual("Edelstahl ERP", LanguageService.Translate("TituloAplicacion", "en-GB"), "en-GB tiene su recurso");

            foreach (string codigo in codigos)
            {
                Verificador.Verificar(LanguageService.ExisteIdioma(codigo), codigo + " esta disponible");
            }

            Verificador.Verificar(!LanguageService.ExisteIdioma("xx-XX"), "Un idioma que no existe se rechaza");
            Verificador.Igual("Total 5", LanguageService.Format("Total {0}", 5), "Format reemplaza los valores");
        }

        // ---------------------------------------------------------------
        // Todos los idiomas con todos los textos, y la bitacora en cada idioma
        // ---------------------------------------------------------------

        public static void CoberturaDeIdiomas()
        {
            Verificador.Seccion("Cobertura de idiomas");

            ResourceManager recursos = new ResourceManager("Edelstahl.Services.Localization.Textos", typeof(LanguageService).Assembly);

            Func<string, Dictionary<string, string>> leer = codigo =>
            {
                Dictionary<string, string> d = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                ResourceSet conjunto = recursos.GetResourceSet(new CultureInfo(codigo), true, false);

                if (conjunto != null)
                {
                    foreach (System.Collections.DictionaryEntry e in conjunto)
                    {
                        d[(string)e.Key] = e.Value as string;
                    }
                }

                return d;
            };

            Dictionary<string, string> maestro = leer("es-AR");
            Verificador.Verificar(maestro.Count > 1000, "El idioma de referencia (es-AR) tiene sus textos: " + maestro.Count);

            foreach (string codigo in new[] { "es-ES", "pt-BR", "de-DE", "ru-RU" })
            {
                Dictionary<string, string> idioma = leer(codigo);
                List<string> faltan = maestro.Keys.Where(k => !idioma.ContainsKey(k)).ToList();
                Verificador.Verificar(faltan.Count == 0, codigo + ": tiene los " + maestro.Count + " textos" + (faltan.Count == 0 ? "" : " (faltan " + faltan.Count + ", por ejemplo: " + faltan[0] + ")"));

                List<string> sinMarcas = maestro.Keys.Where(k => idioma.ContainsKey(k)
                    && System.Text.RegularExpressions.Regex.Matches(k, @"\{\d+\}").Count != System.Text.RegularExpressions.Regex.Matches(idioma[k] ?? "", @"\{\d+\}").Count).ToList();
                Verificador.Verificar(sinMarcas.Count == 0, codigo + ": cada traduccion conserva sus marcas {0}, {1}..." + (sinMarcas.Count == 0 ? "" : " (falla: " + sinMarcas[0] + ")"));

                List<string> vacios = idioma.Where(p => string.IsNullOrWhiteSpace(p.Value)).Select(p => p.Key).ToList();
                Verificador.Verificar(vacios.Count == 0, codigo + ": no hay traducciones vacias");
            }
        }

        public static void BitacoraEnCadaIdioma()
        {
            Verificador.Seccion("Bitacora traducida a cada idioma");

            string[,] muestras =
            {
                { "Se registro el usuario carlos con el rol Supervisor.", "carlos", "Supervisor" },
                { "Se restauro la base EdelstahlNegocio desde el respaldo del 05/11/2026 09:22:17 (sano). Los digitos verificadores son correctos.", "EdelstahlNegocio", "05/11/2026 09:22:17" },
                { "Se detecto una alteracion de los datos de la tabla Clientes el 05/11/2026 09:33:30 (inicio del sistema): 3 registro(s) con el DVH incorrecto. El acceso quedo bloqueado hasta restaurar un respaldo sano o recalcular los digitos.", "05/11/2026 09:33:30", "3" },
                { "FAC-0001 (FacturaA) por 1.234,50", "FAC-0001", "1.234,50" },
                { "PRE-0007 -> Aceptado", "PRE-0007", "->" },
                { "OF-0001: Fabricación 50%", "OF-0001", "50" },
                { "Ingreso de 5 de AECT-12: Ingreso de stock para el presupuesto PRE-0007", "AECT-12", "PRE-0007" },
                { "Pedido PED-0001: reservados 2, ingresados 1, en producción 3", "PED-0001", "3" },
                { "El administrador atendió el pedido de ayuda de carlos: The stock does not match.", "carlos", "." },
                { "Se crearon los respaldos de EdelstahlNegocio y EdelstahlServicios (Manual, datos sanos) en C:\\Respaldos.", "EdelstahlServicios", "C:\\Respaldos" },
                { "Se agregó el permiso ProveedoresGestionar a prueba.ventas", "prueba.ventas", "" },
                { "Se quitó el permiso PresupuestosGestionar a prueba.ventas", "prueba.ventas", "" },
                { "Se restablecieron los permisos de prueba.ventas a los de su rol.", "prueba.ventas", "" }
            };

            foreach (string codigo in new[] { "en-US", "es-AR", "pt-BR", "de-DE", "ru-RU" })
            {
                LanguageService.CambiarIdioma(codigo);

                bool todasSeTraducen = true;
                bool conservanLosDatos = true;
                bool sinMarcasSueltas = true;

                for (int i = 0; i < muestras.GetLength(0); i++)
                {
                    string original = muestras[i, 0];
                    string traducido = BitacoraTraduccion.Descripcion(original);

                    if (codigo != "es-AR" && traducido == original)
                    {
                        todasSeTraducen = false;
                        Console.WriteLine("    sin traducir en " + codigo + ": " + original);
                    }

                    if (!traducido.Contains(muestras[i, 1]) || !traducido.Contains(muestras[i, 2]))
                    {
                        conservanLosDatos = false;
                        Console.WriteLine("    pierde datos en " + codigo + ": " + traducido);
                    }

                    if (traducido.Contains("{0}") || traducido.Contains("{1}"))
                    {
                        sinMarcasSueltas = false;
                    }
                }

                Verificador.Verificar(todasSeTraducen, codigo + ": las descripciones de la bitacora se traducen");
                Verificador.Verificar(conservanLosDatos, codigo + ": la traduccion conserva nombres, numeros y fechas");
                Verificador.Verificar(sinMarcasSueltas, codigo + ": no quedan marcas {0} sin completar");

                if (codigo != "es-AR")
                {
                    Verificador.Verificar(BitacoraTraduccion.Accion("Inicio de sesion") != "Inicio de sesion" && BitacoraTraduccion.Resultado("Correcto") != "Correcto",
                        codigo + ": modulo '" + BitacoraTraduccion.Modulo("Seguridad") + "', accion '" + BitacoraTraduccion.Accion("Inicio de sesion") + "', resultado '" + BitacoraTraduccion.Resultado("Correcto") + "'");
                }
            }

            LanguageService.CambiarIdioma("en-US");
            Verificador.Igual("Sign-in successful.", BitacoraTraduccion.Descripcion("Inicio de sesion correcto."), "En ingles se lee la descripcion traducida");
            Verificador.Igual("Security", BitacoraTraduccion.Modulo("Seguridad"), "En ingles el modulo es Security");
            Verificador.Igual("Texto libre que nadie escribio", BitacoraTraduccion.Descripcion("Texto libre que nadie escribio"), "Un texto desconocido se muestra tal como esta guardado");
        }

        // ---------------------------------------------------------------
        // Factory por reflexión y excepciones de negocio (no necesitan SQL)
        // ---------------------------------------------------------------

        public static void FactoryYExcepciones()
        {
            Verificador.Seccion("Factory por reflexion y excepciones de negocio");

            const string memoria = "Edelstahl.DAL.Implementations.Memory.";

            IClienteRepository valido = FactoryDataAccess.CrearDesdeNombre<IClienteRepository>(memoria + "ClienteRepositoryMemory");
            Verificador.Verificar(valido != null && valido.GetType().Name == "ClienteRepositoryMemory",
                "La Factory crea por reflexion la clase indicada por su nombre");

            Verificador.Falla<ConfiguracionInvalidaException>(
                () => FactoryDataAccess.CrearDesdeNombre<IClienteRepository>(memoria + "NoExiste"),
                "Un nombre de clase que no existe se rechaza");

            Verificador.Falla<ConfiguracionInvalidaException>(
                () => FactoryDataAccess.CrearDesdeNombre<IClienteRepository>(memoria + "PedidoRepositoryMemory"),
                "Una clase que no implementa la interfaz pedida se rechaza");

            Verificador.Falla<ConfiguracionInvalidaException>(
                () => FactoryDataAccess.CrearDesdeNombre<IClienteRepository>("Edelstahl.DAL.Interfaces.IClienteRepository"),
                "Una interfaz (no es una clase concreta) se rechaza");

            Verificador.Falla<ConfiguracionInvalidaException>(
                () => FactoryDataAccess.CrearDesdeNombre<IClienteRepository>("   "),
                "Un nombre vacio se rechaza");

            Verificador.Falla<ConfiguracionInvalidaException>(
                () => FactoryDataAccess.Inicializar(ExecutionMode.None),
                "Un modo de ejecucion no valido se rechaza");

            // Las repositorios lanzan excepciones de negocio, no genericas.
            ApplicationMode.Reset();
            ApplicationMode.Select(ExecutionMode.Demo);
            FactoryDataAccess.Inicializar(ExecutionMode.Demo);

            Verificador.Falla<RegistroNoEncontradoException>(
                () => FactoryDataAccess.ClienteRepository.Update(new Cliente { Id = Guid.NewGuid(), CUIT = "20111111112", RazonSocial = "No existe" }),
                "Modificar un cliente inexistente da RegistroNoEncontradoException");

            Verificador.Falla<ReglaDeNegocioException>(
                () => FactoryDataAccess.BitacoraRepository.Delete(Guid.NewGuid()),
                "Los registros de la bitacora no se pueden borrar (ReglaDeNegocioException)");

            Pedido pedido = new Pedido();
            Verificador.Falla<ReglaDeNegocioException>(
                () => pedido.Confirmar(),
                "Confirmar un pedido que no cumple las condiciones da ReglaDeNegocioException");

            Verificador.Verificar(
                typeof(RegistroDuplicadoException).BaseType == typeof(BusinessRuleException)
                && typeof(AccesoADatosException).BaseType == typeof(BusinessRuleException),
                "Todas las excepciones de negocio derivan de BusinessRuleException");
        }

        // ---------------------------------------------------------------
        // Integridad (DVH/DVV), respaldo y restauracion (necesitan SQL Server)
        // ---------------------------------------------------------------

        public static void IntegridadYRespaldos()
        {
            Verificador.Seccion("Digitos verificadores, respaldo y restauracion");

            // La prueba restaura la base de negocio: nunca debe correr sobre la real.
            if (SqlServerConnection.NombreBaseNegocio == "EdelstahlNegocio")
            {
                Verificador.Verificar(true, "Omitida: defina EDELSTAHL_NEGOCIO_DB y EDELSTAHL_SERVICIOS_DB para no tocar las bases reales");
                return;
            }

            ApplicationMode.Reset();
            ApplicationMode.Select(ExecutionMode.SqlServer);
            FactoryDataAccess.Inicializar(ExecutionMode.SqlServer);
            FactoryDataAccess.PrepararBaseDeDatos();
            SesionActual.Iniciar(new Usuario
            {
                NombreUsuario = "prueba",
                NombreCompleto = "Usuario de prueba",
                Rol = new Rol { Nombre = NombresRol.Administrador }
            });
            new CargaInicialService().AsegurarDatosBase();

            // La carpeta de preferencias de las pruebas la fija Program (variable de entorno).
            string carpetaOriginal = Environment.GetEnvironmentVariable(LanguageService.VariableCarpetaDatos);
            string carpetaDatos = Path.Combine(carpetaOriginal, "respaldos-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(carpetaDatos);

            try
            {
                Ejecutar(carpetaDatos);
            }
            finally
            {
                Environment.SetEnvironmentVariable(LanguageService.VariableCarpetaDatos, carpetaOriginal);

                try
                {
                    Directory.Delete(carpetaDatos, true);
                }
                catch (IOException)
                {
                }
                catch (UnauthorizedAccessException)
                {
                }
            }
        }

        private static readonly Random Azar = new Random();

        private static string CuitNuevo()
        {
            // Once digitos que no coinciden con los clientes de ejemplo ni con los ya creados.
            return "2" + Azar.Next(0, 1000000000).ToString("D9") + Azar.Next(0, 10);
        }

        private static Cliente NuevoCliente(string nombre)
        {
            return new ClienteService().Registrar(new Cliente
            {
                Tipo = TipoPersona.Empresa,
                CUIT = CuitNuevo(),
                RazonSocial = nombre,
                DireccionFacturacion = "Calle 1 100",
                DireccionEntrega = "Calle 1 100",
                Localidad = "CABA",
                Provincia = "Buenos Aires",
                CodigoPostal = "1000",
                Email = nombre.Replace(" ", ".").ToLowerInvariant() + "@example.test",
                Telefono = "1155550000"
            });
        }

        private static void Ejecutar(string carpetaDatos)
        {
            // El archivo de preferencias de respaldo de esta prueba queda dentro de su carpeta temporal.
            Environment.SetEnvironmentVariable(ConfiguracionRespaldo.VariableCarpeta, carpetaDatos);

            IntegridadService integridad = new IntegridadService();
            RespaldoService respaldos = new RespaldoService();

            Cliente uno = NuevoCliente("Integridad Uno " + Guid.NewGuid().ToString("N").Substring(0, 6));
            Cliente dos = NuevoCliente("Integridad Dos " + Guid.NewGuid().ToString("N").Substring(0, 6));

            // --- SqlHelper
            Verificador.Falla<AccesoADatosException>(
                () => SqlHelper.EjecutarNoQuery(BaseDeDatos.Servicios, "SELECT * FROM TablaQueNoExiste"),
                "SqlHelper traduce un error de SQL Server a AccesoADatosException");

            // --- Datos sanos
            ResultadoIntegridad sana = integridad.Verificar();
            Verificador.Verificar(sana.Correcto, "Con los datos sin tocar, el DVH de cada registro y el DVV de la tabla son correctos");
            Verificador.Verificar(sana.RegistrosRevisados >= 2, "Se revisaron " + sana.RegistrosRevisados + " clientes");

            // --- Respaldo sano (con carpeta elegida)
            respaldos.Configuracion.Carpeta = Path.Combine(carpetaDatos, "elegida");
            respaldos.Configuracion.Guardar();

            List<RegistroRespaldo> primero = respaldos.CrearRespaldoManual();
            Verificador.Igual(2, primero.Count, "El respaldo manual crea los archivos de las dos bases");
            Verificador.Verificar(primero.All(r => r.Sano), "Con los datos sanos el respaldo queda marcado como sano");
            Verificador.Verificar(primero.All(r => r.TamanioBytes > 0), "Cada respaldo tiene tamaño");
            Verificador.Verificar(
                primero.All(r => File.Exists(r.RutaCopia) || File.Exists(r.RutaSql)),
                "El archivo quedo en la carpeta elegida (o en la de SQL Server)");

            RegistroRespaldo sano = respaldos.ObtenerRespaldos().First(r => r.Id == primero.First(p => p.BaseDatos == FactoryDataAccess.RespaldoDatabase.BaseNegocio).Id);

            // --- Alteracion de un registro (DVH)
            string nombreOriginal = uno.RazonSocial;
            SqlHelper.EjecutarNoQuery(
                BaseDeDatos.Negocio,
                "UPDATE dbo.Clientes SET RazonSocial = RazonSocial + N' ALTERADO' WHERE Id = @Id",
                SqlHelper.Parametro("@Id", SqlDbType.UniqueIdentifier, uno.Id));

            ResultadoIntegridad alterada = integridad.Verificar();
            Verificador.Verificar(!alterada.Correcto, "Cambiar un dato por fuera del sistema se detecta");
            Verificador.Igual(1, alterada.RegistrosAlterados, "El DVH indica cual es el registro alterado");
            Verificador.Verificar(alterada.Inconsistencias.Any(i => i.Tipo == TipoDigito.Horizontal && i.RegistroId == uno.Id),
                "El registro alterado es el que se modifico");
            Verificador.Verificar(!alterada.AlteradaLaTabla,
                "El DVV no cambia porque guarda los DVH, que no se tocaron");

            int antesBitacora = FactoryDataAccess.BitacoraRepository.GetByModulo("Integridad").Count;
            integridad.VerificarYRegistrar("prueba");
            List<RegistroBitacora> enBitacora = FactoryDataAccess.BitacoraRepository.GetByModulo("Integridad");
            Verificador.Verificar(enBitacora.Count > antesBitacora, "La alteracion queda registrada en la bitacora");
            RegistroBitacora ultimo = enBitacora.OrderByDescending(b => b.FechaHora).First();
            Verificador.Verificar(ultimo.TipoEvento == "Seguridad" && ultimo.Resultado == "Fallido"
                && ultimo.Descripcion.Contains("1 registro"), "El asiento es de seguridad, fallido y cuenta los registros");
            Verificador.Verificar(ultimo.DetalleTecnico.Contains(uno.Id.ToString()), "El detalle nombra el registro alterado");

            // --- Respaldo con datos alterados: no se ofrece como sano
            List<RegistroRespaldo> enfermo = respaldos.CrearRespaldoManual();
            Verificador.Verificar(enfermo.All(r => !r.Sano), "Un respaldo hecho con datos alterados no se marca como sano");

            ConfiguracionRespaldo sinEspera = new ConfiguracionRespaldo(Path.Combine(carpetaDatos, "auto.config"))
            {
                HorasEntreRespaldos = 0,
                Conservar = 1,
                Carpeta = string.Empty
            };
            RespaldoService automatico = new RespaldoService(
                FactoryDataAccess.RespaldoDatabase, new BitacoraService(), FactoryDataAccess.RespaldoRepository, integridad, sinEspera);
            Verificador.Verificar(!automatico.CrearRespaldoAutomaticoSiCorresponde(),
                "El respaldo automatico no se hace cuando los datos estan alterados");

            // --- Recuperacion: usuario, clave y permiso
            UsuarioService usuarios = new UsuarioService();
            string sufijo = Guid.NewGuid().ToString("N").Substring(0, 6);
            string adminUsuario = "adminrec" + sufijo;
            string vendedorUsuario = "vendrec" + sufijo;
            const string clave = "Clave-Segura-2026";
            usuarios.Registrar(adminUsuario, "Admin Recuperacion", adminUsuario + "@example.test", clave, NombresRol.Administrador, false);
            usuarios.Registrar(vendedorUsuario, "Vendedor Recuperacion", vendedorUsuario + "@example.test", clave, NombresRol.Vendedor, false);

            RecuperacionService recuperacion = new RecuperacionService();
            Verificador.Falla<AccesoNoAutorizadoException>(
                () => recuperacion.ObtenerRespaldos(),
                "Sin identificarse no se ven los respaldos");
            Verificador.Falla<ReglaDeNegocioException>(
                () => recuperacion.Autenticar(adminUsuario, "clave incorrecta"),
                "Una contraseña incorrecta no da acceso a la recuperacion");
            Verificador.Falla<AccesoNoAutorizadoException>(
                () => recuperacion.Autenticar(vendedorUsuario, clave),
                "Un usuario sin permiso de respaldos no entra a la recuperacion");
            Verificador.Verificar(!recuperacion.Autorizado, "Tras los intentos fallidos no hay nadie autorizado");

            recuperacion.Autenticar(adminUsuario, clave);
            Verificador.Verificar(recuperacion.Autorizado && recuperacion.NombreUsuario == adminUsuario,
                "Un administrador con usuario y clave correctos queda autorizado");

            List<RegistroRespaldo> lista = recuperacion.ObtenerRespaldos();
            Verificador.Verificar(lista.Any(r => r.Id == sano.Id) && lista.Any(r => !r.Sano),
                "Autorizado, ve todos los respaldos (sanos y no sanos)");
            Verificador.Verificar(lista.All(r => r.BaseDatos == FactoryDataAccess.RespaldoDatabase.BaseNegocio),
                "La lista es de la base de negocio, la que se restaura");

            // --- Restaurar el respaldo sano
            ResultadoIntegridad tras = recuperacion.RestaurarRespaldo(sano);
            Verificador.Verificar(tras.Correcto, "Restaurado el respaldo sano, los digitos vuelven a ser correctos");
            Verificador.Verificar(integridad.Verificar().Correcto, "Una nueva verificacion confirma que los datos estan sanos");
            Cliente restaurado = new ClienteService().ObtenerPorId(uno.Id);
            Verificador.Igual(nombreOriginal, restaurado.RazonSocial, "El dato alterado volvio a su valor original");
            Verificador.Verificar(
                recuperacion.ObtenerRespaldos().Any(r => r.Origen == RegistroRespaldo.OrigenAntesDeRestaurar && !r.Sano),
                "Antes de restaurar se guardo una copia del estado anterior");
            Verificador.Verificar(
                FactoryDataAccess.BitacoraRepository.GetByModulo("Respaldo").Any(b => b.Accion == "Restaurar respaldo" && b.Resultado == "Correcto"),
                "La restauracion queda en la bitacora");

            // --- Restaurar un archivo elegido por el usuario
            SqlHelper.EjecutarNoQuery(
                BaseDeDatos.Negocio,
                "UPDATE dbo.Clientes SET Telefono = N'999' WHERE Id = @Id",
                SqlHelper.Parametro("@Id", SqlDbType.UniqueIdentifier, dos.Id));
            Verificador.Verificar(!integridad.Verificar().Correcto, "Otra alteracion se detecta");

            string archivo = sano.RutaCopia.Length > 0 && File.Exists(sano.RutaCopia) ? sano.RutaCopia : sano.RutaSql;
            Verificador.Verificar(recuperacion.RestaurarArchivo(archivo).Correcto,
                "Tambien se puede restaurar un archivo .bak elegido a mano");

            string archivoServicios = primero.First(r => r.BaseDatos == FactoryDataAccess.RespaldoDatabase.BaseServicios).RutaSql;
            Verificador.Falla<ReglaDeNegocioException>(
                () => recuperacion.RestaurarArchivo(archivoServicios),
                "El respaldo de otra base no se acepta para restaurar el negocio");

            // --- Recalcular despues de revisar
            SqlHelper.EjecutarNoQuery(
                BaseDeDatos.Negocio,
                "UPDATE dbo.Clientes SET Telefono = N'888' WHERE Id = @Id",
                SqlHelper.Parametro("@Id", SqlDbType.UniqueIdentifier, dos.Id));
            Verificador.Verificar(!integridad.Verificar().Correcto, "Un cambio mas se detecta");
            Verificador.Verificar(recuperacion.RecalcularDigitos().Correcto, "Recalcular los digitos acepta los datos actuales");
            Verificador.Verificar(
                FactoryDataAccess.BitacoraRepository.GetByModulo("Integridad").Any(b => b.Accion == "Digitos verificadores recalculados"),
                "El recalculo queda en la bitacora");

            // --- DVV: un registro borrado por fuera
            Cliente tres = NuevoCliente("Integridad Tres " + Guid.NewGuid().ToString("N").Substring(0, 6));
            SqlHelper.EjecutarNoQuery(
                BaseDeDatos.Negocio,
                "DELETE FROM dbo.Clientes WHERE Id = @Id",
                SqlHelper.Parametro("@Id", SqlDbType.UniqueIdentifier, tres.Id));
            ResultadoIntegridad borrado = integridad.Verificar();
            Verificador.Verificar(!borrado.Correcto && borrado.AlteradaLaTabla && borrado.RegistrosAlterados == 0,
                "Un registro borrado por fuera lo detecta el DVV de la tabla (ningun DVH falla)");
            recuperacion.RecalcularDigitos();

            // --- Respaldo automatico con datos sanos y conservacion
            Verificador.Verificar(automatico.CrearRespaldoAutomaticoSiCorresponde(),
                "Con datos sanos se hace el respaldo automatico");
            Verificador.Verificar(automatico.CrearRespaldoAutomaticoSiCorresponde(),
                "(sin espera configurada) se hace otro");
            int automaticosNegocio = automatico.ObtenerRespaldos().Count(r => r.Origen == RegistroRespaldo.OrigenAutomatico);
            Verificador.Igual(1, automaticosNegocio, "Se conservan solo los ultimos respaldos automaticos pedidos");

            ConfiguracionRespaldo conEspera = new ConfiguracionRespaldo(Path.Combine(carpetaDatos, "auto2.config"))
            {
                HorasEntreRespaldos = 24
            };
            RespaldoService unPorDia = new RespaldoService(
                FactoryDataAccess.RespaldoDatabase, new BitacoraService(), FactoryDataAccess.RespaldoRepository, integridad, conEspera);
            Verificador.Verificar(!unPorDia.CrearRespaldoAutomaticoSiCorresponde(),
                "No se repite el respaldo automatico dentro de las horas configuradas");

            ConfiguracionRespaldo apagado = new ConfiguracionRespaldo(Path.Combine(carpetaDatos, "auto3.config")) { Automatico = false, HorasEntreRespaldos = 0 };
            Verificador.Verificar(!new RespaldoService(FactoryDataAccess.RespaldoDatabase, new BitacoraService(), FactoryDataAccess.RespaldoRepository, integridad, apagado).CrearRespaldoAutomaticoSiCorresponde(),
                "Con el respaldo automatico apagado no se hace");

            // --- Configuracion persistente
            ConfiguracionRespaldo guardada = new ConfiguracionRespaldo();
            Verificador.Igual(Path.Combine(carpetaDatos, "elegida"), guardada.Carpeta, "La carpeta elegida se conserva entre ejecuciones");
        }
    }
}
