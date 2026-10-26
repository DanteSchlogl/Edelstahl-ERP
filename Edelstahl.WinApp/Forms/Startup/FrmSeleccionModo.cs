using System;
using System.Drawing;
using System.Windows.Forms;
using Edelstahl.Services.Configuration;
using Edelstahl.Services.Localization;

namespace Edelstahl.WinApp.Forms.Startup
{
    public sealed class FrmSeleccionModo : Form
    {
        private Label lblMarca;
        private Label lblTitulo;
        private Label lblDescripcion;
        private Label lblSqlDescripcion;
        private Label lblDemoDescripcion;

        private Button btnSqlServer;
        private Button btnDemo;
        private Button btnSalir;

        public ExecutionMode SelectedMode
        {
            get;
            private set;
        }

        public FrmSeleccionModo()
        {
            LanguageService.Inicializar();

            SelectedMode =
                ExecutionMode.None;

            CrearInterfaz();
            AplicarIdioma();
        }

        private void CrearInterfaz()
        {
            Text =
                "Edelstahl ERP";

            ClientSize =
                new Size(
                    760,
                    560);

            StartPosition =
                FormStartPosition.CenterScreen;

            FormBorderStyle =
                FormBorderStyle.FixedDialog;

            MaximizeBox =
                false;

            MinimizeBox =
                false;

            ShowInTaskbar =
                true;

            BackColor =
                Color.FromArgb(
                    239,
                    243,
                    248);

            Panel pnlEncabezado =
                new Panel
                {
                    Dock =
                        DockStyle.Top,

                    Height =
                        105,

                    BackColor =
                        Color.FromArgb(
                            103,
                            120,
                            166)
                };

            Controls.Add(
                pnlEncabezado);

            lblMarca =
                new Label
                {
                    ForeColor =
                        Color.White,

                    Font =
                        new Font(
                            "Segoe UI",
                            22,
                            FontStyle.Bold),

                    AutoSize =
                        true,

                    Location =
                        new Point(
                            30,
                            28)
                };

            pnlEncabezado.Controls.Add(
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
                            18,
                            FontStyle.Bold),

                    AutoSize =
                        true,

                    Location =
                        new Point(
                            35,
                            135)
                };

            Controls.Add(
                lblTitulo);

            lblDescripcion =
                new Label
                {
                    ForeColor =
                        Color.DimGray,

                    Font =
                        new Font(
                            "Segoe UI",
                            10),

                    AutoSize =
                        true,

                    Location =
                        new Point(
                            38,
                            178)
                };

            Controls.Add(
                lblDescripcion);

            Panel pnlSql =
                CrearTarjeta(
                    new Point(
                        35,
                        220),

                    Color.FromArgb(
                        221,
                        235,
                        247));

            Controls.Add(
                pnlSql);

            btnSqlServer =
                CrearBotonPrincipal(
                    new Point(
                        20,
                        20),

                    Color.FromArgb(
                        92,
                        126,
                        215));

            btnSqlServer.Click +=
                BtnSqlServer_Click;

            pnlSql.Controls.Add(
                btnSqlServer);

            lblSqlDescripcion =
                CrearDescripcionTarjeta(
                    new Point(
                        20,
                        82));

            pnlSql.Controls.Add(
                lblSqlDescripcion);

            Panel pnlDemo =
                CrearTarjeta(
                    new Point(
                        395,
                        220),

                    Color.FromArgb(
                        233,
                        243,
                        229));

            Controls.Add(
                pnlDemo);

            btnDemo =
                CrearBotonPrincipal(
                    new Point(
                        20,
                        20),

                    Color.FromArgb(
                        111,
                        172,
                        135));

            btnDemo.Click +=
                BtnDemo_Click;

            pnlDemo.Controls.Add(
                btnDemo);

            lblDemoDescripcion =
                CrearDescripcionTarjeta(
                    new Point(
                        20,
                        82));

            pnlDemo.Controls.Add(
                lblDemoDescripcion);

            btnSalir =
                new Button
                {
                    Size =
                        new Size(
                            180,
                            42),

                    Location =
                        new Point(
                            290,
                            485),

                    BackColor =
                        Color.FromArgb(
                            210,
                            215,
                            225),

                    ForeColor =
                        Color.FromArgb(
                            50,
                            50,
                            50),

                    FlatStyle =
                        FlatStyle.Flat,

                    Font =
                        new Font(
                            "Segoe UI",
                            10),

                    Cursor =
                        Cursors.Hand
                };

            btnSalir.FlatAppearance.BorderSize =
                0;

            btnSalir.Click +=
                BtnSalir_Click;

            Controls.Add(
                btnSalir);

            CancelButton =
                btnSalir;
        }

        private static Panel CrearTarjeta(
            Point ubicacion,
            Color colorFondo)
        {
            return new Panel
            {
                Size =
                    new Size(
                        330,
                        225),

                Location =
                    ubicacion,

                BackColor =
                    colorFondo,

                BorderStyle =
                    BorderStyle.FixedSingle
            };
        }

        private static Button CrearBotonPrincipal(
            Point ubicacion,
            Color colorFondo)
        {
            Button boton =
                new Button
                {
                    Size =
                        new Size(
                            288,
                            50),

                    Location =
                        ubicacion,

                    BackColor =
                        colorFondo,

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

            boton.FlatAppearance.BorderSize =
                0;

            return boton;
        }

        private static Label CrearDescripcionTarjeta(
            Point ubicacion)
        {
            return new Label
            {
                Location =
                    ubicacion,

                Size =
                    new Size(
                        285,
                        115),

                ForeColor =
                    Color.FromArgb(
                        55,
                        65,
                        80),

                Font =
                    new Font(
                        "Segoe UI",
                        9),

                TextAlign =
                    ContentAlignment.TopLeft
            };
        }

        private void AplicarIdioma()
        {
            Text =
                LanguageService.Translate(
                    "TituloAplicacion");

            lblMarca.Text =
                LanguageService.Translate(
                    "TituloAplicacion");

            lblTitulo.Text =
                LanguageService.Translate(
                    "SeleccionarModoInicio");

            lblDescripcion.Text =
                LanguageService.Translate(
                    "DescripcionModoInicio");

            btnSqlServer.Text =
                LanguageService.Translate(
                    "IniciarConSqlServer")
                    .ToUpperInvariant();

            lblSqlDescripcion.Text =
                LanguageService.Translate(
                    "DescripcionModoSql");

            btnDemo.Text =
                LanguageService.Translate(
                    "IniciarModoDemostracion")
                    .ToUpperInvariant();

            lblDemoDescripcion.Text =
                LanguageService.Translate(
                    "DescripcionModoDemo");

            btnSalir.Text =
                LanguageService.Translate(
                    "SalirAplicacion");
        }

        private void BtnSqlServer_Click(
            object sender,
            EventArgs e)
        {
            SelectedMode =
                ExecutionMode.SqlServer;

            ApplicationMode.Select(
                SelectedMode);

            DialogResult =
                DialogResult.OK;

            Close();
        }

        private void BtnDemo_Click(
            object sender,
            EventArgs e)
        {
            SelectedMode =
                ExecutionMode.Demo;

            ApplicationMode.Select(
                SelectedMode);

            DialogResult =
                DialogResult.OK;

            Close();
        }

        private void BtnSalir_Click(
            object sender,
            EventArgs e)
        {
            SelectedMode =
                ExecutionMode.None;

            DialogResult =
                DialogResult.Cancel;

            Close();
        }
    }
}