using System;
using System.Drawing;
using System.Windows.Forms;
using Edelstahl.BLL.Services;
using Edelstahl.WinApp.Ui;

namespace Edelstahl.WinApp
{
    /// <summary>
    /// Recuperar la contrasena desde el inicio de sesion: el usuario ve su pregunta de seguridad,
    /// la responde y elige una contrasena nueva. Si no puede, se muestran los datos del administrador.
    /// </summary>
    public sealed class FrmRecuperarPassword : Form
    {
        private readonly UsuarioService _usuarios = new UsuarioService();

        private TextBox txtUsuario;
        private Button btnVerPregunta;
        private Label lblPregunta;
        private TextBox txtRespuesta;
        private TextBox txtNueva;
        private TextBox txtConfirmar;
        private CheckBox chkMostrar;
        private Button btnCambiar;

        public FrmRecuperarPassword(string usuarioInicial)
        {
            Text = Tr.T("Recover password");
            ClientSize = new Size(560, 600);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            BackColor = Color.White;
            Font = new Font("Segoe UI", 9f);
            Marca.AplicarIcono(this);

            Controls.Add(new Label
            {
                Text = Tr.T("Recover password"),
                Font = new Font("Segoe UI", 16f, FontStyle.Bold),
                ForeColor = Estilo.Principal,
                AutoSize = true,
                Location = new Point(24, 16)
            });

            Controls.Add(Etiqueta("User name", 64));
            txtUsuario = new TextBox { Location = new Point(24, 86), Size = new Size(330, 26), Font = new Font("Segoe UI", 11f), Text = usuarioInicial ?? string.Empty };
            Controls.Add(txtUsuario);

            btnVerPregunta = Boton("View security question", new Point(366, 84), 170);
            btnVerPregunta.Click += BtnVerPregunta_Click;
            Controls.Add(btnVerPregunta);

            lblPregunta = new Label
            {
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Estilo.Principal,
                AutoSize = false,
                Size = new Size(512, 40),
                Location = new Point(24, 126)
            };
            Controls.Add(lblPregunta);

            Controls.Add(Etiqueta("Answer", 176));
            txtRespuesta = Campo(198);

            Controls.Add(Etiqueta("New password", 244));
            txtNueva = Campo(266);

            Controls.Add(Etiqueta("Confirm password", 312));
            txtConfirmar = Campo(334);

            chkMostrar = new CheckBox { Text = Tr.T("Show password"), AutoSize = true, Location = new Point(24, 372) };
            chkMostrar.CheckedChanged += (s, e) =>
            {
                txtRespuesta.UseSystemPasswordChar = !chkMostrar.Checked;
                txtNueva.UseSystemPasswordChar = !chkMostrar.Checked;
                txtConfirmar.UseSystemPasswordChar = !chkMostrar.Checked;
            };
            Controls.Add(chkMostrar);

            btnCambiar = Boton("Change password", new Point(24, 412), 190);
            btnCambiar.Click += BtnCambiar_Click;
            Controls.Add(btnCambiar);

            Button cancelar = Boton("Cancel", new Point(226, 412), 110);
            cancelar.BackColor = Color.FromArgb(120, 125, 135);
            cancelar.DialogResult = DialogResult.Cancel;
            Controls.Add(cancelar);
            CancelButton = cancelar;

            Panel linea = new Panel { BackColor = Color.FromArgb(215, 220, 230), Location = new Point(24, 462), Size = new Size(512, 1) };
            Controls.Add(linea);

            Controls.Add(new Label
            {
                Text = Tr.T("If you cannot recover it, contact the administrator:"),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(24, 476)
            });

            Controls.Add(new Label
            {
                Text = ContactoAdministrador.Texto(),
                AutoSize = true,
                Location = new Point(24, 500)
            });

            HabilitarRespuesta(false);
            AcceptButton = btnVerPregunta;
        }

        private static Label Etiqueta(string clave, int y)
        {
            return new Label { Text = Tr.T(clave), Font = new Font("Segoe UI", 9f, FontStyle.Bold), AutoSize = true, Location = new Point(24, y) };
        }

        private TextBox Campo(int y)
        {
            TextBox cuadro = new TextBox
            {
                Location = new Point(24, y),
                Size = new Size(512, 26),
                Font = new Font("Segoe UI", 11f),
                UseSystemPasswordChar = true
            };

            Controls.Add(cuadro);
            return cuadro;
        }

        private static Button Boton(string clave, Point lugar, int ancho)
        {
            Button boton = new Button
            {
                Text = Tr.T(clave),
                Location = lugar,
                Size = new Size(ancho, 34),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(103, 120, 166),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };

            boton.FlatAppearance.BorderSize = 0;
            return boton;
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
