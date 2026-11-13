using System;
using System.Drawing;
using System.Windows.Forms;
using Edelstahl.BLL.Services;
using Edelstahl.Domain.Security;
using Edelstahl.WinApp.Ui;

namespace Edelstahl.WinApp
{
    /// <summary>
    /// Mi perfil: datos del usuario, cambio de contrasena, pregunta de seguridad
    /// y datos de contacto del administrador.
    /// </summary>
    public sealed class FrmPerfil : Form
    {
        private Label lblPregunta;
        private Label lblUltimoAcceso;

        public FrmPerfil()
        {
            Usuario usuario = new UsuarioService().ObtenerPorId(SesionActual.Usuario.Id);

            Text = Tr.T("My profile");
            ClientSize = new Size(560, 470);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            BackColor = Estilo.Superficie;
            Font = Estilo.Normal;
            Marca.AplicarIcono(this);

            Label titulo = new Label
            {
                Text = Tr.T("My profile"),
                Font = Estilo.Titulo,
                ForeColor = Estilo.Principal,
                AutoSize = true,
                Location = new Point(20, 14)
            };

            Controls.Add(titulo);

            int y = 62;
            Fila("Full name", usuario.NombreCompleto, ref y);
            Fila("User name", usuario.NombreUsuario, ref y);
            Fila("Email", usuario.Email, ref y);
            Fila("Role", usuario.Rol == null ? string.Empty : usuario.Rol.Nombre, ref y);
            lblUltimoAcceso = Fila("Last access",
                usuario.UltimoAcceso.HasValue ? usuario.UltimoAcceso.Value.ToString("dd/MM/yyyy HH:mm") : "-", ref y);
            lblPregunta = Fila("Security question", string.Empty, ref y);
            ActualizarPregunta(usuario);

            Button cambiarClave = Estilo.Boton("Change password");
            cambiarClave.Location = new Point(20, y + 8);
            cambiarClave.Click += (s, e) => DialogosUsuario.CambiarPassword(this);
            Controls.Add(cambiarClave);

            Button cambiarPregunta = Estilo.Boton("Security question");
            cambiarPregunta.Location = new Point(cambiarClave.Right + 12, y + 8);
            cambiarPregunta.Click += (s, e) =>
            {
                if (DialogosUsuario.CambiarPregunta(this))
                {
                    ActualizarPregunta(new UsuarioService().ObtenerPorId(SesionActual.Usuario.Id));
                }
            };
            Controls.Add(cambiarPregunta);

            Label contactoTitulo = new Label
            {
                Text = Tr.T("Your administrator"),
                Font = Estilo.Negrita,
                ForeColor = Estilo.Principal,
                AutoSize = true,
                Location = new Point(20, y + 62)
            };

            Controls.Add(contactoTitulo);

            Label contacto = new Label
            {
                Text = ContactoAdministrador.Texto(),
                AutoSize = true,
                Location = new Point(20, y + 84)
            };

            Controls.Add(contacto);

            Button cerrar = Estilo.Boton("Close");
            cerrar.Location = new Point(ClientSize.Width - cerrar.PreferredSize.Width - 36, ClientSize.Height - 50);
            cerrar.DialogResult = DialogResult.OK;
            Controls.Add(cerrar);
            AcceptButton = cerrar;
            CancelButton = cerrar;
        }

        private Label Fila(string etiqueta, string valor, ref int y)
        {
            Controls.Add(new Label
            {
                Text = Tr.T(etiqueta),
                Font = Estilo.Negrita,
                AutoSize = false,
                Size = new Size(140, 22),
                Location = new Point(20, y)
            });

            Label contenido = new Label
            {
                Text = valor,
                AutoSize = false,
                Size = new Size(380, 36),
                Location = new Point(165, y)
            };

            Controls.Add(contenido);
            y += 36;
            return contenido;
        }

        private void ActualizarPregunta(Usuario usuario)
        {
            lblPregunta.Text = usuario.TienePreguntaSeguridad()
                ? Tr.T(usuario.PreguntaSeguridad)
                : Tr.T("Not set. Choose one so you can recover your password.");
        }
    }
}
