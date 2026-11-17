using System;
using Edelstahl.Services.Logic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using Edelstahl.BLL.Services;
using Edelstahl.WinApp.Ui;

namespace Edelstahl.WinApp
{
    /// <summary>
    /// Recuperar la contrasena desde el inicio de sesion: el usuario ve su pregunta de seguridad,
    /// la responde y elige una contrasena nueva. Si no puede, se muestran los datos del administrador.
    /// La distribución de los controles está en FrmRecuperarPassword.Designer.cs.
    /// </summary>
    public sealed partial class FrmRecuperarPassword : Form
    {
        private readonly UsuarioService _usuarios;

        // Constructor sin parámetros: lo usa el diseñador de Visual Studio.
        public FrmRecuperarPassword()
        {
            InitializeComponent();
        }

        public FrmRecuperarPassword(string usuarioInicial) : this()
        {
            _usuarios = new UsuarioService();

            Text = Tr.T("Recover password");
            Marca.AplicarIcono(this);

            lblTitulo.Text = Tr.T("Recover password");
            lblUsuarioCaption.Text = Tr.T("User name");
            txtUsuario.Text = usuarioInicial ?? string.Empty;
            btnVerPregunta.Text = Tr.T("View security question");
            btnVerPregunta.Click += BtnVerPregunta_Click;

            lblRespuestaCaption.Text = Tr.T("Answer");
            lblNuevaCaption.Text = Tr.T("New password");
            lblConfirmarCaption.Text = Tr.T("Confirm password");

            chkMostrar.Text = Tr.T("Show password");
            chkMostrar.CheckedChanged += (s, e) =>
            {
                txtRespuesta.UseSystemPasswordChar = !chkMostrar.Checked;
                txtNueva.UseSystemPasswordChar = !chkMostrar.Checked;
                txtConfirmar.UseSystemPasswordChar = !chkMostrar.Checked;
            };

            btnCambiar.Text = Tr.T("Change password");
            btnCambiar.Click += BtnCambiar_Click;
            btnCancelar.Text = Tr.T("Cancel");

            lblContactoTitulo.Text = Tr.T("If you cannot recover it, contact the administrator:");
            lblContacto.Text = ContactoAdministrador.Texto();

            HabilitarRespuesta(false);
        }

        private void HabilitarRespuesta(bool habilitar)
        {
            txtRespuesta.Enabled = habilitar;
            txtNueva.Enabled = habilitar;
            txtConfirmar.Enabled = habilitar;
            btnCambiar.Enabled = habilitar;
        }

        private void BtnVerPregunta_Click(object sender, EventArgs e)
        {
            lblPregunta.Text = string.Empty;
            HabilitarRespuesta(false);

            string pregunta = null;

            if (!Estilo.Ejecutar(this, () => pregunta = _usuarios.ObtenerPreguntaSeguridad(txtUsuario.Text)))
            {
                return;
            }

            lblPregunta.Text = Tr.T(pregunta);
            HabilitarRespuesta(true);
            AcceptButton = btnCambiar;
            txtRespuesta.Focus();
        }

        private void BtnCambiar_Click(object sender, EventArgs e)
        {
            if (txtNueva.Text != txtConfirmar.Text)
            {
                Estilo.Avisar(this, Tr.T("The passwords do not match."));
                return;
            }

            bool listo = Estilo.Ejecutar(this, () =>
                _usuarios.RecuperarConPregunta(txtUsuario.Text, txtRespuesta.Text, txtNueva.Text));

            if (!listo)
            {
                txtRespuesta.Clear();
                txtRespuesta.Focus();
                return;
            }

            Estilo.Informar(this, Tr.T("Your password was changed. You can sign in now."));
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
