using System;
using System.Windows.Forms;
using Edelstahl.BLL.Infrastructure;
using Edelstahl.Domain.Security;
using Edelstahl.Services.Localization;

namespace Edelstahl.WinApp.Ui
{
    /// <summary>
    /// Modulo de la aplicacion: un conjunto de pestanas, cada una con su lista.
    /// Una pestana solo aparece si el usuario tiene el permiso requerido.
    /// </summary>
    public abstract class ModuloBase : UserControl
    {
        private readonly TabControl _pestanas;

        protected ModuloBase()
        {
            Dock = DockStyle.Fill;
            BackColor = Estilo.Fondo;

            _pestanas = new TabControl { Dock = DockStyle.Fill, Font = Estilo.Normal };
            _pestanas.SelectedIndexChanged += (remitente, argumentos) => CargarPestanaActual();

            // Hasta que la ventana existe el control de pestanas no tiene pestana
            // seleccionada, asi que la primera carga se hace al crearse.
            _pestanas.HandleCreated += (remitente, argumentos) =>
            {
                if (_pestanas.IsHandleCreated)
                {
                    _pestanas.BeginInvoke((MethodInvoker)CargarPestanaActual);
                }
            };
            Controls.Add(_pestanas);

            LanguageService.IdiomaCambiado += AlCambiarIdioma;
            Disposed += (remitente, argumentos) => LanguageService.IdiomaCambiado -= AlCambiarIdioma;
        }

        protected TabControl Pestanas
        {
            get { return _pestanas; }
        }

        protected TabPage AgregarPestana(string clave, Control contenido, Permiso? permiso)
        {
            if (permiso.HasValue && !Autorizacion.Tiene(permiso.Value))
            {
                return null;
            }

            TabPage pagina = new TabPage(Tr.T(clave)) { Tag = clave, BackColor = Estilo.Fondo };
            contenido.Dock = DockStyle.Fill;
            pagina.Controls.Add(contenido);
            _pestanas.TabPages.Add(pagina);
            return pagina;
        }

        public void CargarPestanaActual()
        {
            TabPage pagina = _pestanas.SelectedTab;

            if (pagina == null)
            {
                return;
            }

            foreach (Control control in pagina.Controls)
            {
                Recargar(control);
            }
        }

        private static void Recargar(Control control)
        {
            PanelLista lista = control as PanelLista;

            if (lista != null)
            {
                lista.Recargar();
                return;
            }

            IRecargable recargable = control as IRecargable;

            if (recargable != null)
            {
                recargable.Recargar();
            }
        }

        private void AlCambiarIdioma(object remitente, EventArgs argumentos)
        {
            if (IsDisposed)
            {
                return;
            }

            foreach (TabPage pagina in _pestanas.TabPages)
            {
                if (pagina.Tag is string)
                {
                    pagina.Text = Tr.T((string)pagina.Tag);
                }
            }
        }
    }

    public interface IRecargable
    {
        void Recargar();
    }
}
