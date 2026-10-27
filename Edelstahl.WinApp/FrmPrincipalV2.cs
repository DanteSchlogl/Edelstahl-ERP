using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Edelstahl.BLL.Infrastructure;
using Edelstahl.BLL.Services;
using Edelstahl.Domain.Security;
using Edelstahl.Services.Localization;
using Edelstahl.WinApp.Modulos;
using Edelstahl.WinApp.Ui;

namespace Edelstahl.WinApp
{
    /// <summary>
    /// Ventana principal: menu lateral por modulos (segun permisos), idioma y aviso de stock.
    /// </summary>
    public partial class FrmPrincipalV2 : Form
    {
        private sealed class EntradaMenu
        {
            public string Clave;
            public Permiso Permiso;
            public Func<Control> Crear;
            public Button Boton;
        }

        private readonly List<EntradaMenu> _menu = new List<EntradaMenu>();
        private readonly MonitorDeStock _monitor = new MonitorDeStock(TimeSpan.FromSeconds(60));

        private Panel pnlSidebar;
        private Panel pnlHeader;
        private Panel pnlWorkspace;
        private Label lblTituloAplicacion;
        private Label lblModulo;
        private Label lblIdioma;
        private Label lblUsuario;
        private Label lblAlertas;
        private ComboBox cboIdioma;
        private EntradaMenu _actual;
        private bool _cambiandoIdioma;

        public FrmPrincipalV2()
        {
            InitializeComponent();

            ConfigurarFormulario();
            CrearWorkspace();
            CrearHeader();
            CrearSidebar();

            // El panel central debe quedar al frente del orden de apilado para que
            // el acoplamiento (Fill) lo calcule despues del encabezado y del menu.
            pnlWorkspace.BringToFront();

            CargarIdiomas();
            AplicarIdioma();

            LanguageService.IdiomaCambiado += LanguageService_IdiomaCambiado;
            FormClosed += FrmPrincipalV2_FormClosed;
            Shown += FrmPrincipalV2_Shown;

            if (_menu.Count > 0)
            {
                Mostrar(_menu[0]);
            }
        }

        private void ConfigurarFormulario()
        {
            Text = "Edelstahl ERP";
            WindowState = FormWindowState.Maximized;
            BackColor = Estilo.Fondo;
            StartPosition = FormStartPosition.CenterScreen;
            Edelstahl.WinApp.Ui.Marca.AplicarIcono(this);
            MinimumSize = new Size(1100, 700);
            Font = Estilo.Normal;
        }

        private void CrearSidebar()
        {
            pnlSidebar = new Panel { Dock = DockStyle.Left, Width = 250, BackColor = Estilo.Lateral };
            Controls.Add(pnlSidebar);

            PictureBox emblema = new PictureBox { Image = Marca.Emblema(56), SizeMode = PictureBoxSizeMode.Zoom, Size = new Size(56, 56), Location = new Point(16, 14), BackColor = Color.Transparent };
            lblTituloAplicacion = new Label
            {
                Text = "EDELSTAHL",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 15, FontStyle.Bold),
                AutoSize = true,
                BackColor = Color.Transparent,
                Location = new Point(78, 18)
            };
            Label lema = new Label
            {
                Text = "ERP  |  STAINLESS STEEL",
                ForeColor = Color.FromArgb(214, 224, 247),
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                AutoSize = true,
                BackColor = Color.Transparent,
                Location = new Point(80, 46)
            };

            pnlSidebar.Controls.Add(emblema);
            pnlSidebar.Controls.Add(lblTituloAplicacion);
            pnlSidebar.Controls.Add(lema);
            pnlSidebar.Controls.Add(new PanelAcero { Dock = DockStyle.Bottom, Height = 300 });

            Agregar("Dashboard", Permiso.ReportesVer, () => new ModuloReportes());
            Agregar("Sales", Permiso.PedidosVer, () => new ModuloComercial());
            Agregar("Production", Permiso.ProduccionVer, () => new ModuloProduccion());
            Agregar("Purchasing and stock", Permiso.InventarioVer, () => new ModuloInventario());
            Agregar("Logistics and returns", Permiso.LogisticaVer, () => new ModuloLogistica());
            Agregar("Finance", Permiso.CuentaCorrienteVer, () => new ModuloFinanzas());
            Agregar("View audit log", Permiso.BitacoraVer, () => new VisorBitacoraPagina());
            Agregar("Administration", Permiso.UsuariosAdministrar, () => new ModuloAdministracion());

            int y = 92;

            foreach (EntradaMenu entrada in _menu)
            {
                entrada.Boton.Location = new Point(14, y);
                y += 52;
            }
        }

        private void Agregar(string clave, Permiso permiso, Func<Control> crear)
        {
            if (!Autorizacion.Tiene(permiso))
            {
                return;
            }

            EntradaMenu entrada = new EntradaMenu { Clave = clave, Permiso = permiso, Crear = crear };

            Button boton = new Button
            {
                Width = 222,
                Height = 44,
                FlatStyle = FlatStyle.Flat,
                BackColor = Estilo.Lateral,
                ForeColor = Color.White,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 10),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0),
                Tag = entrada
            };

            boton.FlatAppearance.BorderSize = 0;
            boton.Click += (remitente, argumentos) => Mostrar(entrada);
            boton.MouseEnter += (remitente, argumentos) => { if (_actual != entrada) boton.BackColor = Color.FromArgb(128, 146, 196); };
            boton.MouseLeave += (remitente, argumentos) => { if (_actual != entrada) boton.BackColor = Estilo.Lateral; };

            entrada.Boton = boton;
            pnlSidebar.Controls.Add(boton);
            _menu.Add(entrada);
        }

        private void Mostrar(EntradaMenu entrada)
        {
            _actual = entrada;

            foreach (EntradaMenu otra in _menu)
            {
                otra.Boton.BackColor = otra == entrada ? Estilo.LateralActivo : Estilo.Lateral;
                otra.Boton.ForeColor = otra == entrada ? Estilo.Principal : Color.White;
            }

            List<Control> anteriores = new List<Control>();

            foreach (Control control in pnlWorkspace.Controls)
            {
                anteriores.Add(control);
            }

            pnlWorkspace.Controls.Clear();

            foreach (Control control in anteriores)
            {
                control.Dispose();
            }

            Control modulo = null;

            try
            {
                modulo = entrada.Crear();
            }
            catch (Exception ex)
            {
                Infrastructure.GlobalExceptionHandler.ProcesarErrorControlado(
                    ex, "Interfaz", "Abrir modulo " + entrada.Clave, Tr.T("The module could not be opened."));
            }

            if (modulo != null)
            {
                modulo.Dock = DockStyle.Fill;
                pnlWorkspace.Controls.Add(modulo);
            }

            lblModulo.Text = Tr.T(entrada.Clave);
        }

        private void CrearHeader()
        {
            pnlHeader = new PanelEncabezado { Dock = DockStyle.Top, Height = 74, BackColor = Estilo.Superficie };
            Controls.Add(pnlHeader);
            pnlHeader.BringToFront();

            lblModulo = new Label
            {
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(84, 20),
                BackColor = Color.Transparent
            };

            lblUsuario = new Label
            {
                ForeColor = Color.FromArgb(222, 232, 252),
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 9),
                AutoSize = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };

            lblAlertas = new Label
            {
                ForeColor = Color.FromArgb(255, 220, 110),
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                AutoSize = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Visible = false
            };

            lblIdioma = new Label
            {
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                AutoSize = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };

            cboIdioma = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                DrawMode = DrawMode.OwnerDrawFixed,
                ItemHeight = 24,
                Font = new Font("Segoe UI", 9),
                Size = new Size(230, 30),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };

            cboIdioma.DrawItem += CboIdioma_DrawItem;
            cboIdioma.SelectedIndexChanged += CboIdioma_SelectedIndexChanged;

            pnlHeader.Controls.Add(lblModulo);
            pnlHeader.Controls.Add(lblUsuario);
            pnlHeader.Controls.Add(lblAlertas);
            pnlHeader.Controls.Add(lblIdioma);
            pnlHeader.Controls.Add(cboIdioma);
            pnlHeader.Resize += (remitente, argumentos) => Distribuir();
        }

        private void Distribuir()
        {
            if (pnlHeader == null || cboIdioma == null)
            {
                return;
            }

            cboIdioma.Location = new Point(pnlHeader.ClientSize.Width - cboIdioma.Width - 24, 12);
            lblIdioma.Location = new Point(cboIdioma.Left - lblIdioma.Width - 10, 18);
            lblUsuario.Location = new Point(pnlHeader.ClientSize.Width - lblUsuario.Width - 24, 46);
            lblAlertas.Location = new Point(Math.Max(lblModulo.Right + 20, lblUsuario.Left - lblAlertas.Width - 30), 46);
        }

        private void CrearWorkspace()
        {
            pnlWorkspace = new Panel { Dock = DockStyle.Fill, BackColor = Estilo.Fondo };
            Controls.Add(pnlWorkspace);
            pnlWorkspace.BringToFront();
        }

        private void CboIdioma_DrawItem(object sender, DrawItemEventArgs e)
        {
            e.DrawBackground();

            if (e.Index < 0 || e.Index >= cboIdioma.Items.Count)
            {
                return;
            }

            IdiomaItem idioma = cboIdioma.Items[e.Index] as IdiomaItem;

            if (idioma == null)
            {
                return;
            }

            if (idioma.Bandera != null)
            {
                e.Graphics.DrawImage(idioma.Bandera, new Rectangle(e.Bounds.Left + 6, e.Bounds.Top + 4, 24, 16));
            }

            Color colorTexto = (e.State & DrawItemState.Selected) != 0 ? SystemColors.HighlightText : cboIdioma.ForeColor;

            TextRenderer.DrawText(e.Graphics, idioma.Nombre, cboIdioma.Font,
                new Rectangle(e.Bounds.Left + 38, e.Bounds.Top, e.Bounds.Width - 40, e.Bounds.Height),
                colorTexto, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);

            e.DrawFocusRectangle();
        }

        private void CargarIdiomas()
        {
            _cambiandoIdioma = true;
            cboIdioma.Items.Clear();

            cboIdioma.Items.Add(new IdiomaItem("en-US", "English (United States)", "flag_us.png"));
            cboIdioma.Items.Add(new IdiomaItem("en-GB", "English (United Kingdom)", "flag_gb.png"));
            cboIdioma.Items.Add(new IdiomaItem("es-AR", "Español (Argentina)", "flag_ar.png"));
            cboIdioma.Items.Add(new IdiomaItem("es-ES", "Español (España)", "flag_es.png"));
            cboIdioma.Items.Add(new IdiomaItem("pt-BR", "Português (Brasil)", "flag_br.png"));
            cboIdioma.Items.Add(new IdiomaItem("de-DE", "Deutsch", "flag_de.png"));
            cboIdioma.Items.Add(new IdiomaItem("ru-RU", "Русский", "flag_ru.png"));

            SeleccionarIdiomaActual();
            _cambiandoIdioma = false;
        }

        private void SeleccionarIdiomaActual()
        {
            for (int indice = 0; indice < cboIdioma.Items.Count; indice++)
            {
                IdiomaItem idioma = cboIdioma.Items[indice] as IdiomaItem;

                if (idioma != null && string.Equals(idioma.Codigo, LanguageService.IdiomaActual, StringComparison.OrdinalIgnoreCase))
                {
                    cboIdioma.SelectedIndex = indice;
                    return;
                }
            }

            cboIdioma.SelectedIndex = 0;
        }

        private void CboIdioma_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_cambiandoIdioma)
            {
                return;
            }

            IdiomaItem idioma = cboIdioma.SelectedItem as IdiomaItem;

            if (idioma == null)
            {
                return;
            }

            try
            {
                LanguageService.CambiarIdioma(idioma.Codigo);
            }
            catch (Exception ex)
            {
                Estilo.Avisar(this, Tr.T(ex.Message));
                _cambiandoIdioma = true;
                SeleccionarIdiomaActual();
                _cambiandoIdioma = false;
            }
        }

        private void LanguageService_IdiomaCambiado(object sender, EventArgs e)
        {
            if (IsDisposed)
            {
                return;
            }

            if (InvokeRequired)
            {
                BeginInvoke((MethodInvoker)(() => LanguageService_IdiomaCambiado(sender, e)));
                return;
            }

            AplicarIdioma();

            if (_actual != null)
            {
                Mostrar(_actual);
            }
        }

        private void AplicarIdioma()
        {
            Text = Tr.T("Edelstahl ERP");
            lblTituloAplicacion.Text = "EDELSTAHL";
            lblIdioma.Text = Tr.T("Language") + ":";

            foreach (EntradaMenu entrada in _menu)
            {
                entrada.Boton.Text = Tr.T(entrada.Clave);
            }

            lblUsuario.Text = SesionActual.EstaIniciada
                ? Tr.T("User: {0} ({1})", SesionActual.NombreUsuario, SesionActual.NombreRol)
                : string.Empty;

            Distribuir();
        }

        private void FrmPrincipalV2_Shown(object sender, EventArgs e)
        {
            if (Autorizacion.Tiene(Permiso.InventarioVer))
            {
                _monitor.AlertasActualizadas += Monitor_AlertasActualizadas;
                _monitor.Iniciar();
            }
        }

        private void Monitor_AlertasActualizadas(object sender, AlertasStockEventArgs e)
        {
            if (IsDisposed || !IsHandleCreated)
            {
                return;
            }

            try
            {
                BeginInvoke((MethodInvoker)(() =>
                {
                    lblAlertas.Visible = e.Alertas.Count > 0;
                    lblAlertas.Text = Tr.T("Stock alerts: {0}", e.Alertas.Count);
                    Distribuir();
                }));
            }
            catch (InvalidOperationException)
            {
            }
        }

        private void FrmPrincipalV2_FormClosed(object sender, FormClosedEventArgs e)
        {
            LanguageService.IdiomaCambiado -= LanguageService_IdiomaCambiado;
            _monitor.AlertasActualizadas -= Monitor_AlertasActualizadas;
            _monitor.Dispose();

            foreach (object elemento in cboIdioma.Items)
            {
                IdiomaItem idioma = elemento as IdiomaItem;

                if (idioma != null)
                {
                    idioma.Dispose();
                }
            }
        }

        private sealed class IdiomaItem : IDisposable
        {
            public string Codigo { get; private set; }
            public string Nombre { get; private set; }
            public Image Bandera { get; private set; }

            public IdiomaItem(string codigo, string nombre, string nombreArchivo)
            {
                Codigo = codigo;
                Nombre = nombre;
                Bandera = CargarBandera(nombreArchivo);
            }

            private static Image CargarBandera(string nombreArchivo)
            {
                string ruta = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Flags", nombreArchivo);

                if (!File.Exists(ruta))
                {
                    return null;
                }

                using (Image temporal = Image.FromFile(ruta))
                {
                    return new Bitmap(temporal);
                }
            }

            public void Dispose()
            {
                if (Bandera != null)
                {
                    Bandera.Dispose();
                    Bandera = null;
                }
            }

            public override string ToString()
            {
                return Nombre;
            }
        }
    }

    /// <summary>
    /// Pagina completa de la bitacora (acceso directo desde el menu principal).
    /// </summary>
    public class VisorBitacoraPagina : ModuloBase
    {
        public VisorBitacoraPagina()
        {
            AgregarPestana("Audit log", new VisorBitacora(), Permiso.BitacoraVer);
            CargarPestanaActual();
        }
    }
}
