using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Edelstahl.BLL.DTOs;
using Edelstahl.BLL.Services;
using Edelstahl.Domain.Security;

namespace Edelstahl.WinApp.Ui
{
    /// <summary>
    /// Permisos de un usuario puntual. Solo lo abre el administrador. Los permisos marcados son los que
    /// tiene el usuario: los de su rol más los que el administrador le agregó, menos los que le quitó.
    /// </summary>
    public class FrmPermisosUsuario : Form
    {
        private readonly AdministracionService _admin = new AdministracionService();
        private readonly Guid _usuarioId;
        private readonly bool _esAdministrador;
        private readonly CheckedListBox _lista = new CheckedListBox
        {
            Dock = DockStyle.Fill,
            CheckOnClick = true,
            IntegralHeight = false,
            Font = Estilo.Normal,
            BorderStyle = BorderStyle.FixedSingle
        };

        private List<PermisoDeUsuarioDto> _permisos = new List<PermisoDeUsuarioDto>();
        private bool _cargando;

        public FrmPermisosUsuario(Guid usuarioId, string usuario, string rol)
        {
            _usuarioId = usuarioId;
            _esAdministrador = string.Equals(rol, NombresRol.Administrador, StringComparison.OrdinalIgnoreCase);

            Text = Tr.T("Permissions of {0}", usuario);
            Size = new Size(560, 640);
            StartPosition = FormStartPosition.CenterParent;
            Marca.AplicarIcono(this);
            Font = Estilo.Normal;
            BackColor = Estilo.Superficie;
            MinimizeBox = false;
            ShowInTaskbar = false;

            Label encabezado = new Label
            {
                Dock = DockStyle.Top,
                Height = 78,
                Padding = new Padding(12, 8, 12, 0),
                ForeColor = Estilo.Principal,
                BackColor = Color.FromArgb(226, 234, 247),
                Text = Tr.T("Permissions of {0}", usuario) + Environment.NewLine
                    + Tr.T("Role: {0}", rol) + Environment.NewLine
                    + (_esAdministrador
                        ? Tr.T("The permissions of an administrator cannot be changed.")
                        : Tr.T("Checked permissions are granted to this user. A change from the role's permissions is saved as an individual permission and recorded in the audit log."))
            };

            FlowLayoutPanel botones = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 48,
                Padding = new Padding(12, 8, 12, 0),
                FlowDirection = FlowDirection.LeftToRight
            };

            Button guardar = Estilo.Boton("Save");
            Button restablecer = Estilo.Boton("Restore role permissions");
            Button cerrar = Estilo.Boton("Close");
            guardar.Enabled = !_esAdministrador;
            restablecer.Enabled = !_esAdministrador;

            guardar.Click += (s, e) => Guardar();
            restablecer.Click += (s, e) => Restablecer();
            cerrar.Click += (s, e) => Close();

            botones.Controls.Add(guardar);
            botones.Controls.Add(restablecer);
            botones.Controls.Add(cerrar);

            Panel centro = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12) };
            centro.Controls.Add(_lista);

            Controls.Add(centro);
            Controls.Add(botones);
            Controls.Add(encabezado);

            _lista.ItemCheck += Lista_ItemCheck;
            _lista.Enabled = !_esAdministrador;

            Cargar();
        }

        private void Cargar()
        {
            _cargando = true;

            try
            {
                if (!Estilo.Ejecutar(this, () => _permisos = _admin.ObtenerPermisosDeUsuario(_usuarioId)))
                {
                    return;
                }

                _permisos = _permisos.OrderBy(p => Tr.T(Nombres.Permiso(p.Permiso.ToString())), StringComparer.CurrentCultureIgnoreCase).ToList();
                _lista.Items.Clear();

                foreach (PermisoDeUsuarioDto p in _permisos)
                {
                    string texto = Tr.T(Nombres.Permiso(p.Permiso.ToString()));

                    if (p.SoloAdministrador)
                    {
                        texto += "   (" + Tr.T("administrators only") + ")";
                    }
                    else if (p.Individual == true)
                    {
                        texto += "   (" + Tr.T("added by the administrator") + ")";
                    }
                    else if (p.Individual == false)
                    {
                        texto += "   (" + Tr.T("removed by the administrator") + ")";
                    }

                    _lista.Items.Add(texto, p.Efectivo);
                }
            }
            finally
            {
                _cargando = false;
            }
        }

        private void Lista_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            if (_cargando || e.Index < 0 || e.Index >= _permisos.Count)
            {
                return;
            }

            if (_permisos[e.Index].SoloAdministrador)
            {
                e.NewValue = e.CurrentValue;
                Estilo.Avisar(this, Tr.T("This permission can only belong to administrators."));
            }
        }

        private void Guardar()
        {
            Dictionary<Permiso, bool> deseados = new Dictionary<Permiso, bool>();

            for (int i = 0; i < _permisos.Count; i++)
            {
                deseados[_permisos[i].Permiso] = _lista.GetItemChecked(i);
            }

            int cambios = 0;

            if (Estilo.Ejecutar(this, () => cambios = _admin.CambiarPermisosDeUsuario(_usuarioId, deseados)))
            {
                Estilo.Informar(this, Tr.T("Permissions saved: {0} change(s).", cambios));
                Cargar();
            }
        }

        private void Restablecer()
        {
            if (!Estilo.Confirmar(this, Tr.T("Restore the permissions of this user to those of their role?")))
            {
                return;
            }

            if (Estilo.Ejecutar(this, () => _admin.RestablecerPermisos(_usuarioId)))
            {
                Estilo.Informar(this, Tr.T("The permissions were restored to those of the role."));
                Cargar();
            }
        }
    }
}
