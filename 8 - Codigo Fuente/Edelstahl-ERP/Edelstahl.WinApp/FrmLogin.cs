using System;
using Edelstahl.Services.Logic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Edelstahl.BLL.Services;
using Edelstahl.Services.Configuration;
using Edelstahl.Services.Localization;
using Edelstahl.WinApp.Ui;

namespace Edelstahl.WinApp
{
    /// <summary>
    /// Inicio de sesión. La distribución de los controles está en FrmLogin.Designer.cs
    /// (editable en el diseñador); aquí solo hay textos traducidos y lógica.
    /// </summary>
    public sealed partial class FrmLogin : Form
    {
        private UsuarioService _usuarioService;

        private DemoAuthenticationService _demoAuthenticationService;

        public FrmLogin()
        {
            InitializeComponent();

            // En el diseñador de Visual Studio solo se muestra la distribución.
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime)
            {
                return;
            }

            _usuarioService = ApplicationMode.UsesSqlServer ? new UsuarioService() : null;

            _demoAuthenticationService = ApplicationMode.IsDemo ? new DemoAuthenticationService() : null;

            LanguageService.Inicializar();

            Edelstahl.WinApp.Ui.Marca.AplicarIcono(this);
            pbLogo.Image = CargarLogo();

            btnIngresar.Click += BtnIngresar_Click;
            chkMostrarPassword.CheckedChanged += ChkMostrarPassword_CheckedChanged;
            lnkOlvido.LinkClicked += LnkOlvido_LinkClicked;
            Shown += FrmLogin_Shown;

            ConfigurarEventosIdioma();
            AplicarIdioma();
            PrepararModoActual();
        }
        private void ConfigurarEventosIdioma()
        {
            LanguageService.IdiomaCambiado +=
                LanguageService_IdiomaCambiado;

            FormClosed +=
                FrmLogin_FormClosed;
        }

        private void PrepararModoActual()
        {
            if (ApplicationMode.IsDemo)
            {
                lblModo.BackColor =
                    Color.FromArgb(
                        233,
                        243,
                        229);

                lblModo.ForeColor =
                    Color.FromArgb(
                        55,
                        110,
                        70);

                txtUsuario.Text =
                    "demo";

                txtPassword.Clear();

                return;
            }

            lblModo.BackColor =
                Color.FromArgb(
                    221,
                    235,
                    247);

            lblModo.ForeColor =
                Color.FromArgb(
                    50,
                    80,
                    125);
        }

        private void AplicarIdioma()
        {
            Text =
                LanguageService.Translate(
                    "TituloAplicacion") +
                " - " +
                LanguageService.Translate(
                    "IniciarSesion");

            lblMarca.Text =
                LanguageService.Translate(
                    "TituloAplicacion")
                    .ToUpperInvariant();

            lblTitulo.Text =
                LanguageService.Translate(
                    "IniciarSesion");

            lblSubtitulo.Text =
                LanguageService.Translate(
                    "SubtituloLogin") +
                ".";

            lblUsuario.Text =
                LanguageService.Translate(
                    "NombreUsuario");

            lblPassword.Text =
                LanguageService.Translate(
                    "Contrasena");

            chkMostrarPassword.Text =
                LanguageService.Translate(
                    "MostrarContrasena");

            btnIngresar.Text =
                LanguageService.Translate(
                    "IniciarSesion")
                    .ToUpperInvariant();

            lnkOlvido.Text =
                Tr.T("Recover password");

            // En modo demostracion no hay usuarios reales cuya clave recuperar.
            lnkOlvido.Visible =
                !ApplicationMode.IsDemo;

            lblModo.Text =
                ApplicationMode.IsDemo
                    ? LanguageService.Translate(
                        "ModoDemostracion")
                    : LanguageService.Translate(
                        "IniciarConSqlServer");
        }

        private void LanguageService_IdiomaCambiado(
            object sender,
            EventArgs e)
        {
            AplicarIdioma();
        }

        private void FrmLogin_FormClosed(
            object sender,
            FormClosedEventArgs e)
        {
            LanguageService.IdiomaCambiado -=
                LanguageService_IdiomaCambiado;
        }

        private void FrmLogin_Shown(
            object sender,
            EventArgs e)
        {
            if (ApplicationMode.IsDemo)
            {
                txtPassword.Focus();
                return;
            }

            txtUsuario.Focus();
        }

        private void LnkOlvido_LinkClicked(
            object sender,
            LinkLabelLinkClickedEventArgs e)
        {
            using (FrmRecuperarPassword recuperar =
                new FrmRecuperarPassword(
                    txtUsuario.Text))
            {
                recuperar.ShowDialog(
                    this);
            }
        }

        private void ChkMostrarPassword_CheckedChanged(
            object sender,
            EventArgs e)
        {
            txtPassword.UseSystemPasswordChar =
                !chkMostrarPassword.Checked;
        }

        private void BtnIngresar_Click(
            object sender,
            EventArgs e)
        {
            try
            {
                btnIngresar.Enabled =
                    false;

                Edelstahl.Services.DomainModel.Security.Usuario usuario;

                if (ApplicationMode.IsDemo)
                {
                    usuario =
                        _demoAuthenticationService
                            .Autenticar(
                                txtUsuario.Text,
                                txtPassword.Text);
                }
                else
                {
                    usuario =
                        _usuarioService.Autenticar(
                            txtUsuario.Text,
                            txtPassword.Text);
                }

                SesionActual.Iniciar(
                    usuario);

                // Clave provisoria entregada por un administrador: hay que elegir una propia para entrar.
                if (ApplicationMode.UsesSqlServer &&
                    usuario.DebeCambiarPassword &&
                    !ElegirPasswordPropia(usuario))
                {
                    SesionActual.Cerrar();
                    txtPassword.Clear();
                    txtPassword.Focus();

                    MessageBox.Show(
                        Tr.T("You must choose a new password to sign in."),
                        LanguageService.Translate(
                            "AccesoDenegado"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);

                    return;
                }

                DialogResult =
                    DialogResult.OK;

                Close();
            }
            catch (Exception ex)
            {
                txtPassword.Clear();
                txtPassword.Focus();

                MessageBox.Show(
                    ex.Message,
                    LanguageService.Translate(
                        "AccesoDenegado"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
            finally
            {
                btnIngresar.Enabled =
                    true;
            }
        }

        /// <summary>
        /// Pide la clave nueva hasta que sea valida. Devuelve false si el usuario cancela.
        /// </summary>
        private bool ElegirPasswordPropia(Edelstahl.Services.DomainModel.Security.Usuario usuario)
        {
            while (true)
            {
                Respuesta datos = Dialogo.Pedir(this, "Choose a new password",
                    "An administrator gave you a temporary password. Choose your own to continue. It must have at least 8 characters.",
                    Campo.Clave("clave", "New password"),
                    Campo.Clave("clave2", "Confirm password").Confirmando("clave"));

                if (datos == null)
                {
                    return false;
                }

                try
                {
                    // La clave actual es la provisoria que se acaba de escribir en el login.
                    _usuarioService.CambiarPassword(
                        usuario.Id,
                        txtPassword.Text,
                        datos.Texto("clave"));

                    return true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        ex.Message,
                        Tr.T("Choose a new password"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
            }
        }

        private static Image CargarLogo()
        {
            string carpeta =
                Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "Resources",
                    "Login");

            if (!Directory.Exists(
                carpeta))
            {
                return null;
            }

            string[] archivos =
                Directory.GetFiles(
                    carpeta,
                    "logo_login.*");

            if (archivos.Length == 0)
            {
                return null;
            }

            using (Image temporal =
                Image.FromFile(
                    archivos[0]))
            {
                return new Bitmap(
                    temporal);
            }
        }
    }
}