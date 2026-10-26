using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Edelstahl.BLL.Services;
using Edelstahl.Services.Configuration;
using Edelstahl.Services.Localization;

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