using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using Edelstahl.Services.Configuration;
using Edelstahl.Services.Localization;

namespace Edelstahl.WinApp.Forms.Startup
{
    /// <summary>
    /// Primera ventana: se elige si la aplicación trabaja con SQL Server o en modo demostración.
    /// La distribución de los controles está en FrmSeleccionModo.Designer.cs.
    /// </summary>
    public sealed partial class FrmSeleccionModo : Form
    {
        public ExecutionMode SelectedMode
        {
            get;
            private set;
        }

        public FrmSeleccionModo()
        {
            InitializeComponent();

            // En el diseñador de Visual Studio solo se muestra la distribución.
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime)
            {
                return;
            }

            LanguageService.Inicializar();

            SelectedMode = ExecutionMode.None;

            Edelstahl.WinApp.Ui.Marca.AplicarIcono(this);

            btnSqlServer.Click += BtnSqlServer_Click;
            btnDemo.Click += BtnDemo_Click;
            btnSalir.Click += BtnSalir_Click;

            AplicarIdioma();
        }

        private void AplicarIdioma()
        {
            Text = LanguageService.Translate("TituloAplicacion");
            lblMarca.Text = LanguageService.Translate("TituloAplicacion");
            lblTitulo.Text = LanguageService.Translate("SeleccionarModoInicio");
            lblDescripcion.Text = LanguageService.Translate("DescripcionModoInicio");
            btnSqlServer.Text = LanguageService.Translate("IniciarConSqlServer").ToUpperInvariant();
            lblSqlDescripcion.Text = LanguageService.Translate("DescripcionModoSql");
            btnDemo.Text = LanguageService.Translate("IniciarModoDemostracion").ToUpperInvariant();
            lblDemoDescripcion.Text = LanguageService.Translate("DescripcionModoDemo");
            btnSalir.Text = LanguageService.Translate("SalirAplicacion");
        }

        private void BtnSqlServer_Click(object sender, EventArgs e)
        {
            SelectedMode = ExecutionMode.SqlServer;

            ApplicationMode.Select(SelectedMode);

            DialogResult = DialogResult.OK;

            Close();
        }

        private void BtnDemo_Click(object sender, EventArgs e)
        {
            SelectedMode = ExecutionMode.Demo;

            ApplicationMode.Select(SelectedMode);

            DialogResult = DialogResult.OK;

            Close();
        }

        private void BtnSalir_Click(object sender, EventArgs e)
        {
            SelectedMode = ExecutionMode.None;

            DialogResult = DialogResult.Cancel;

            Close();
        }
    }
}
