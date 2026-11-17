using System;
using Edelstahl.Services.Logic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using Edelstahl.BLL.Services;
using Edelstahl.Services.DomainModel.Security;
using Edelstahl.WinApp.Ui;

namespace Edelstahl.WinApp
{
    /// <summary>
    /// Mi perfil: datos del usuario, cambio de contrasena, pregunta de seguridad
    /// y datos de contacto del administrador.
    /// La distribución de los controles está en FrmPerfil.Designer.cs (editable
    /// en el diseñador); aquí solo se cargan los datos y las traducciones.
    /// </summary>
    public sealed partial class FrmPerfil : Form
    {
        public FrmPerfil()
        {
            InitializeComponent();

            // En el diseñador de Visual Studio no hay sesión ni base de datos:
            // se muestra la distribución sin cargar datos.
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime)
            {
                return;
            }

            Usuario usuario = new UsuarioService().ObtenerPorId(SesionActual.Usuario.Id);

            Text = Tr.T("My profile");
            Marca.AplicarIcono(this);

            lblTitulo.Text = Tr.T("My profile");

            lblNombreCaption.Text = Tr.T("Full name");
            lblNombre.Text = usuario.NombreCompleto;
            lblUsuarioCaption.Text = Tr.T("User name");
            lblUsuario.Text = usuario.NombreUsuario;
            lblEmailCaption.Text = Tr.T("Email");
            lblEmail.Text = usuario.Email;
            lblRolCaption.Text = Tr.T("Role");
            lblRol.Text = usuario.Rol == null ? string.Empty : usuario.Rol.Nombre;
            lblUltimoAccesoCaption.Text = Tr.T("Last access");
            lblUltimoAcceso.Text = usuario.UltimoAcceso.HasValue
                ? usuario.UltimoAcceso.Value.ToString("dd/MM/yyyy HH:mm")
                : "-";
            lblPreguntaCaption.Text = Tr.T("Security question");
            ActualizarPregunta(usuario);

            btnCambiarClave.Text = Tr.T("Change password");
            btnCambiarClave.Click += (s, e) => DialogosUsuario.CambiarPassword(this);

            btnCambiarPregunta.Text = Tr.T("Security question");
            btnCambiarPregunta.Location = new Point(btnCambiarClave.Right + 12, btnCambiarPregunta.Top);
            btnCambiarPregunta.Click += (s, e) =>
            {
                if (DialogosUsuario.CambiarPregunta(this))
                {
                    ActualizarPregunta(new UsuarioService().ObtenerPorId(SesionActual.Usuario.Id));
                }
            };

            lblAdminTitulo.Text = Tr.T("Your administrator");
            lblContacto.Text = ContactoAdministrador.Texto();

            btnCerrar.Text = Tr.T("Close");
            btnCerrar.Location = new Point(
                ClientSize.Width - btnCerrar.PreferredSize.Width - 36,
                ClientSize.Height - 50);
        }

        private void ActualizarPregunta(Usuario usuario)
        {
            lblPregunta.Text = usuario.TienePreguntaSeguridad()
                ? Tr.T(usuario.PreguntaSeguridad)
                : Tr.T("Not set. Choose one so you can recover your password.");
        }
    }
}
