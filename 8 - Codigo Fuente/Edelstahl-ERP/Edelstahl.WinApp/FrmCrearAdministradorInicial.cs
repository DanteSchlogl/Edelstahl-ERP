using System;
using Edelstahl.WinApp.Ui;
using System.Drawing;
using System.Windows.Forms;
using Edelstahl.BLL.Services;

namespace Edelstahl.WinApp
{
    public sealed class FrmCrearAdministradorInicial : Form
    {
        private readonly UsuarioService _usuarioService;
        private TextBox txtUsuario;
        private TextBox txtNombreCompleto;
        private TextBox txtEmail;
        private TextBox txtPassword;
        private TextBox txtRepetirPassword;
        private Button btnCrear;

        public FrmCrearAdministradorInicial()
        {
            _usuarioService = new UsuarioService();
            CrearInterfaz();
        }

        private void CrearInterfaz()
        {
            Text = Tr.T("Initial setup");
            ClientSize = new Size(520, 470);
            StartPosition = FormStartPosition.CenterScreen;
            Edelstahl.WinApp.Ui.Marca.AplicarIcono(this);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.FromArgb(235, 240, 245);

            Label titulo = new Label
            {
                Text = Tr.T("Create the initial administrator"),
                AutoSize = true,
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = Color.FromArgb(54, 69, 98),
                Location = new Point(45, 30)
            };
            Controls.Add(titulo);

            Label ayuda = new Label
            {
                Text = Tr.T("This user will have full access to the system."),
                AutoSize = true,
                ForeColor = Color.DimGray,
                Location = new Point(48, 72)
            };
            Controls.Add(ayuda);

            txtUsuario = CrearCampo(Tr.T("User name"), 110, false);
            txtNombreCompleto = CrearCampo(Tr.T("Full name"), 175, false);
            txtEmail = CrearCampo(Tr.T("Email"), 240, false);
            txtPassword = CrearCampo(Tr.T("Password"), 305, true);
            txtRepetirPassword = CrearCampo(Tr.T("Repeat password"), 370, true);

            btnCrear = new Button
            {
                Text = Tr.T("Create administrator").ToUpper(),
                Size = new Size(220, 45),
                Location = new Point(250, 415),
                BackColor = Color.FromArgb(111, 192, 145),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnCrear.FlatAppearance.BorderSize = 0;
            btnCrear.Click += BtnCrear_Click;
            Controls.Add(btnCrear);
            AcceptButton = btnCrear;
        }

        private TextBox CrearCampo(string etiqueta, int top, bool password)
        {
            Label label = new Label
            {
                Text = etiqueta,
                AutoSize = true,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Location = new Point(50, top)
            };
            Controls.Add(label);

            TextBox campo = new TextBox
            {
                Size = new Size(420, 30),
                Location = new Point(50, top + 24),
                Font = new Font("Segoe UI", 10),
                UseSystemPasswordChar = password
            };
            Controls.Add(campo);
            return campo;
        }

        private void BtnCrear_Click(object sender, EventArgs e)
        {
            try
            {
                if (txtPassword.Text != txtRepetirPassword.Text)
                {
                    throw new InvalidOperationException(
                        Tr.T("The passwords entered do not match."));
                }

                _usuarioService.CrearAdministradorInicial(
                    txtUsuario.Text,
                    txtNombreCompleto.Text,
                    txtEmail.Text,
                    txtPassword.Text);

                MessageBox.Show(
                    Tr.T("The initial administrator was created."),
                    Tr.T("Setup completed"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    Tr.T("The administrator could not be created"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }
    }
}
