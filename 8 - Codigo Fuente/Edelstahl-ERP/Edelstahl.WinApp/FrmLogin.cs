using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Edelstahl.BLL.Services;
using Edelstahl.Services.Configuration;
using Edelstahl.Services.Localization;
using Edelstahl.WinApp.Ui;

namespace Edelstahl.WinApp
{
    public sealed class FrmLogin : Form
    {
        private readonly UsuarioService _usuarioService;

        private readonly DemoAuthenticationService
            _demoAuthenticationService;

        private Label lblMarca;
        private Label lblTitulo;
        private Label lblSubtitulo;
        private Label lblUsuario;
        private Label lblPassword;
        private Label lblModo;

        private TextBox txtUsuario;
        private TextBox txtPassword;

        private Button btnIngresar;

        private CheckBox chkMostrarPassword;

        private LinkLabel lnkOlvido;

        public FrmLogin()
        {
            _usuarioService =
                ApplicationMode.UsesSqlServer
                    ? new UsuarioService()
                    : null;

            _demoAuthenticationService =
                ApplicationMode.IsDemo
                    ? new DemoAuthenticationService()
                    : null;

            LanguageService.Inicializar();

            CrearInterfaz();
            ConfigurarEventosIdioma();
            AplicarIdioma();
            PrepararModoActual();
        }

        private void CrearInterfaz()
        {
            ClientSize =
                new Size(900, 570);

            StartPosition =
                FormStartPosition.CenterScreen;

            Edelstahl.WinApp.Ui.Marca.AplicarIcono(this);

            FormBorderStyle =
                FormBorderStyle.FixedDialog;

            MaximizeBox =
                false;

            MinimizeBox =
                false;

            BackColor =
                Color.White;

            Panel panelImagen =
                new Panel
                {
                    Dock =
                        DockStyle.Left,

                    Width =
                        450,

                    BackColor =
                        Color.FromArgb(
                            103,
                            120,
                            166)
                };

            Controls.Add(
                panelImagen);

            PictureBox logo =
                new PictureBox
                {
                    Location =
                        new Point(30, 45),

                    Size =
                        new Size(390, 390),

                    SizeMode =
                        PictureBoxSizeMode.Zoom,

                    Image =
                        CargarLogo()
                };

            panelImagen.Controls.Add(
                logo);

            lblMarca =
                new Label
                {
                    ForeColor =
                        Color.White,

                    Font =
                        new Font(
                            "Segoe UI",
                            18,
                            FontStyle.Bold),

                    AutoSize =
                        true,

                    Location =
                        new Point(120, 475)
                };

            panelImagen.Controls.Add(
                lblMarca);

            lblTitulo =
                new Label
                {
                    ForeColor =
                        Color.FromArgb(
                            54,
                            69,
                            98),

                    Font =
                        new Font(
                            "Segoe UI",
                            20,
                            FontStyle.Bold),

                    AutoSize =
                        true,

                    Location =
                        new Point(510, 55)
                };

            Controls.Add(
                lblTitulo);

            lblSubtitulo =
                new Label
                {
                    ForeColor =
                        Color.DimGray,

                    AutoSize =
                        true,

                    Location =
                        new Point(513, 100)
                };

            Controls.Add(
                lblSubtitulo);

            lblModo =
                new Label
                {
                    Font =
                        new Font(
                            "Segoe UI",
                            9,
                            FontStyle.Bold),

                    ForeColor =
                        Color.FromArgb(
                            70,
                            85,
                            115),

                    BackColor =
                        Color.FromArgb(
                            225,
                            231,
                            240),

                    TextAlign =
                        ContentAlignment.MiddleCenter,

                    Location =
                        new Point(515, 130),

                    Size =
                        new Size(320, 32)
                };

            Controls.Add(
                lblModo);

            lblUsuario =
                new Label
                {
                    Font =
                        new Font(
                            "Segoe UI",
                            9,
                            FontStyle.Bold),

                    AutoSize =
                        true,

                    Location =
                        new Point(515, 185)
                };

            Controls.Add(
                lblUsuario);

            txtUsuario =
                new TextBox
                {
                    Name =
                        "txtUsuario",

                    Location =
                        new Point(515, 210),

                    Size =
                        new Size(320, 32),

                    Font =
                        new Font(
                            "Segoe UI",
                            11)
                };

            Controls.Add(
                txtUsuario);

            lblPassword =
                new Label
                {
                    Font =
                        new Font(
                            "Segoe UI",
                            9,
                            FontStyle.Bold),

                    AutoSize =
                        true,

                    Location =
                        new Point(515, 265)
                };

            Controls.Add(
                lblPassword);

            txtPassword =
                new TextBox
                {
                    Name =
                        "txtPassword",

                    Location =
                        new Point(515, 290),

                    Size =
                        new Size(320, 32),

                    Font =
                        new Font(
                            "Segoe UI",
                            11),

                    UseSystemPasswordChar =
                        true
                };

            Controls.Add(
                txtPassword);

            chkMostrarPassword =
                new CheckBox
                {
                    Name =
                        "chkMostrarPassword",

                    AutoSize =
                        true,

                    Location =
                        new Point(515, 335)
                };

            chkMostrarPassword.CheckedChanged +=
                ChkMostrarPassword_CheckedChanged;

            Controls.Add(
                chkMostrarPassword);

            btnIngresar =
                new Button
                {
                    Name =
                        "btnIngresar",

                    Location =
                        new Point(515, 390),

                    Size =
                        new Size(320, 48),

                    BackColor =
                        Color.FromArgb(
                            103,
                            120,
                            166),

                    ForeColor =
                        Color.White,

                    FlatStyle =
                        FlatStyle.Flat,

                    Font =
                        new Font(
                            "Segoe UI",
                            10,
                            FontStyle.Bold),

                    Cursor =
                        Cursors.Hand
                };

            btnIngresar.FlatAppearance.BorderSize =
                0;

            btnIngresar.Click +=
                BtnIngresar_Click;

            Controls.Add(
                btnIngresar);

            lnkOlvido =
                new LinkLabel
                {
                    Name =
                        "lnkOlvido",

                    AutoSize =
                        true,

                    Location =
                        new Point(515, 455),

                    Cursor =
                        Cursors.Hand
                };

            lnkOlvido.LinkClicked +=
                LnkOlvido_LinkClicked;

            Controls.Add(
                lnkOlvido);

            AcceptButton =
                btnIngresar;

            Shown +=
                FrmLogin_Shown;
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
            string nombre =
                System.Configuration.ConfigurationManager.AppSettings["ContactoNombre"];

            string telefono =
                System.Configuration.ConfigurationManager.AppSettings["ContactoRecuperacion"];

            MessageBox.Show(
                Tr.T("Call this number to recover your password:") +
                Environment.NewLine + Environment.NewLine +
                (string.IsNullOrWhiteSpace(nombre) ? string.Empty : nombre.Trim() + Environment.NewLine) +
                "WhatsApp: " + (telefono ?? string.Empty).Trim() +
                Environment.NewLine + Environment.NewLine +
                Tr.T("You will receive a temporary password and the program will ask you to choose your own."),
                Tr.T("Recover password"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
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

                Domain.Security.Usuario usuario;

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
        private bool ElegirPasswordPropia(Domain.Security.Usuario usuario)
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