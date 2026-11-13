using System;
using System.Windows.Forms;
using Edelstahl.BLL.Services;
using Edelstahl.DAL.EntityFramework;
using Edelstahl.DAL.Factory;
using Edelstahl.Domain.Integrity;
using Edelstahl.Services.Configuration;
using Edelstahl.Services.Cryptography;
using Edelstahl.Services.Localization;
using Edelstahl.WinApp.Forms.Startup;
using Edelstahl.WinApp.Infrastructure;
using Edelstahl.WinApp.Ui;

namespace Edelstahl.WinApp
{
    internal static class Program
    {
        /// <summary>
        /// Punto de entrada principal de Edelstahl ERP.
        /// </summary>
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            GlobalExceptionHandler.Registrar();

            try
            {
                // La aplicacion siempre arranca en ingles; el idioma elegido
                // por el usuario se aplica despues de iniciar sesion.
                LanguageService.Inicializar();
                ApplicationMode.Reset();

                using (FrmSeleccionModo selectorModo = new FrmSeleccionModo())
                {
                    if (selectorModo.ShowDialog() != DialogResult.OK)
                    {
                        return;
                    }
                }

                FactoryDataAccess.Inicializar(ApplicationMode.CurrentMode);

                FrmEspera.Ejecutar("Preparing the database...", () =>
                {
                    if (ApplicationMode.UsesSqlServer)
                    {
                        FactoryDataAccess.PrepararBaseDeDatos();
                    }

                    new CargaInicialService().AsegurarDatosBase();
                    CryptographyService.Precalentar();
                    return string.Empty;
                });

                // Si los digitos verificadores indican una alteracion no se deja entrar
                // hasta repararla con la pantalla de recuperacion.
                if (ApplicationMode.UsesSqlServer && !VerificarIntegridad())
                {
                    return;
                }

                if (ApplicationMode.UsesSqlServer)
                {
                    VerificarAdministradorInicial();
                }

                // Cerrar sesion vuelve a la pantalla de inicio de sesion; cerrar la ventana termina el programa.
                bool primeraVez = true;

                while (true)
                {
                    if (!primeraVez)
                    {
                        SesionActual.Cerrar();
                        LanguageService.Inicializar();
                    }

                    primeraVez = false;

                    if (!MostrarLogin())
                    {
                        return;
                    }

                    LanguageService.AplicarIdiomaPreferido();
                    OfrecerDatosDeEjemplo();

                    using (FrmPrincipalV2 principal = new FrmPrincipalV2())
                    {
                        Application.Run(principal);

                        if (!principal.CerrarSesionSolicitada)
                        {
                            return;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                GlobalExceptionHandler.ProcesarErrorInicio(ex);
            }
            finally
            {
                SesionActual.Cerrar();
                ApplicationMode.Reset();
            }
        }

        /// <summary>
        /// Comprueba el DVH de cada cliente y el DVV de la tabla. Si todo esta bien
        /// hace el respaldo automatico del dia; si hay una alteracion la registra en
        /// la bitacora y abre la recuperacion. Devuelve false si el usuario se va sin repararla.
        /// </summary>
        private static bool VerificarIntegridad()
        {
            ResultadoIntegridad resultado = null;

            FrmEspera.Ejecutar("Checking the integrity of the data...", () =>
            {
                resultado = new IntegridadService().VerificarYRegistrar("inicio del sistema");

                // El respaldo automatico solo se hace con datos sanos.
                if (resultado.Correcto)
                {
                    new RespaldoService().CrearRespaldoAutomaticoSiCorresponde();
                }

                return string.Empty;
            });

            if (resultado.Correcto)
            {
                return true;
            }

            using (FrmRecuperacionIntegridad recuperacion = new FrmRecuperacionIntegridad(resultado))
            {
                return recuperacion.ShowDialog() == DialogResult.OK;
            }
        }

        private static void VerificarAdministradorInicial()
        {
            UsuarioService usuarioService = new UsuarioService();

            if (usuarioService.ExisteAdministrador())
            {
                return;
            }

            using (FrmCrearAdministradorInicial configuracion = new FrmCrearAdministradorInicial())
            {
                configuracion.ShowDialog();
            }
        }

        private static bool MostrarLogin()
        {
            using (FrmLogin login = new FrmLogin())
            {
                return login.ShowDialog() == DialogResult.OK;
            }
        }

        /// <summary>
        /// Si el catalogo esta vacio carga los datos de ejemplo: automaticamente
        /// en modo demostracion y con confirmacion cuando se usa SQL Server.
        /// </summary>
        private static void OfrecerDatosDeEjemplo()
        {
            CargaInicialService carga = new CargaInicialService();

            if (!SesionActual.Tiene(Edelstahl.Domain.Security.Permiso.CatalogoEditar) || carga.TieneCatalogo())
            {
                return;
            }

            bool cargar = !ApplicationMode.UsesSqlServer ||
                MessageBox.Show(
                    Tr.T("The catalog is empty. Load the sample customers and products?"),
                    Tr.T("Edelstahl ERP"),
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question) == DialogResult.Yes;

            if (!cargar)
            {
                return;
            }

            FrmEspera.Ejecutar("Loading sample customers and products...", () =>
            {
                carga.CargarDatosDeEjemplo(new Progress<int>(p => { }));
                return string.Empty;
            });
        }
    }
}
