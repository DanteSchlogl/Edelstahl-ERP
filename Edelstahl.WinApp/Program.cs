using System;
using System.Windows.Forms;
using Edelstahl.BLL.Services;
using Edelstahl.DAL.EntityFramework;
using Edelstahl.DAL.Factory;
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
                        InicializadorNegocio.Inicializar();
                    }

                    new CargaInicialService().AsegurarDatosBase();
                    CryptographyService.Precalentar();
                    return string.Empty;
                });

                if (ApplicationMode.UsesSqlServer)
                {
                    VerificarAdministradorInicial();
                }

                if (!MostrarLogin())
                {
                    return;
                }

                LanguageService.AplicarIdiomaPreferido();
                OfrecerDatosDeEjemplo();

                Application.Run(new FrmPrincipalV2());
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
