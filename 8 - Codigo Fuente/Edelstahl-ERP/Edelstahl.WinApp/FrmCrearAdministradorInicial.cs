using System;
using Edelstahl.Services.Logic;
using System.ComponentModel;
using Edelstahl.WinApp.Ui;
using System.Drawing;
using System.Windows.Forms;
using Edelstahl.BLL.Services;

namespace Edelstahl.WinApp
{
    /// <summary>
    /// La distribución de los controles está en FrmCrearAdministradorInicial.Designer.cs
    /// (editable en el diseñador); aquí solo hay textos traducidos y lógica.
    /// </summary>
    public sealed partial class FrmCrearAdministradorInicial : Form
    {
        private readonly UsuarioService _usuarioService;

        public FrmCrearAdministradorInicial()
        {
            InitializeComponent();

            // En el diseñador de Visual Studio solo se muestra la distribución.
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime)
            {
                return;
            }

            _usuarioService = new UsuarioService();
            AplicarTextos();
            btnCrear.Click += BtnCrear_Click;
        }

        private void AplicarTextos()
        {
            Text = Tr.T("Initial setup");
            Edelstahl.WinApp.Ui.Marca.AplicarIcono(this);

            lblTitulo.Text = Tr.T("Create the initial administrator");
            lblAyuda.Text = Tr.T("This user will have full access to the system.");
            lblUsuario.Text = Tr.T("User name");
            lblNombreCompleto.Text = Tr.T("Full name");
            lblEmail.Text = Tr.T("Email");
            lblPassword.Text = Tr.T("Password");
            lblRepetirPassword.Text = Tr.T("Repeat password");
            btnCrear.Text = Tr.T("Create administrator").ToUpper();
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
