using System;
using Edelstahl.Services.Logic.DTOs;
using Edelstahl.Services.Logic;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Edelstahl.BLL.DTOs;
using Edelstahl.BLL.Services;
using Edelstahl.Services.DomainModel.Security;

namespace Edelstahl.WinApp.Ui
{
    /// <summary>
    /// Permisos de un usuario puntual. Solo lo abre el administrador. Los permisos marcados son los que
    /// tiene el usuario: los de su rol más los que el administrador le agregó, menos los que le quitó.
    /// La distribución de los controles está en FrmPermisosUsuario.Designer.cs.
    /// </summary>
    public partial class FrmPermisosUsuario : Form
    {
        private AdministracionService _admin;
        private Guid _usuarioId;
        private bool _esAdministrador;

        private List<PermisoDeUsuarioDto> _permisos = new List<PermisoDeUsuarioDto>();
        private bool _cargando;

        // Constructor sin parámetros: lo usa el diseñador de Visual Studio.
        public FrmPermisosUsuario()
        {
            InitializeComponent();
        }

        public FrmPermisosUsuario(Guid usuarioId, string usuario, string rol) : this()
        {
            _admin = new AdministracionService();
            _usuarioId = usuarioId;
            _esAdministrador = string.Equals(rol, NombresRol.Administrador, StringComparison.OrdinalIgnoreCase);

            Text = Tr.T("Permissions of {0}", usuario);
            Marca.AplicarIcono(this);

            lblEncabezado.Text = Tr.T("Permissions of {0}", usuario) + Environment.NewLine
                + Tr.T("Role: {0}", rol) + Environment.NewLine
                + (_esAdministrador
                    ? Tr.T("The permissions of an administrator cannot be changed.")
                    : Tr.T("Checked permissions are granted to this user. A change from the role's permissions is saved as an individual permission and recorded in the audit log."));

            btnGuardar.Text = Tr.T("Save");
            btnRestablecer.Text = Tr.T("Restore role permissions");
            btnCerrar.Text = Tr.T("Close");
            btnGuardar.Enabled = !_esAdministrador;
            btnRestablecer.Enabled = !_esAdministrador;

            btnGuardar.Click += (s, e) => Guardar();
            btnRestablecer.Click += (s, e) => Restablecer();
            btnCerrar.Click += (s, e) => Close();

            lstPermisos.ItemCheck += Lista_ItemCheck;
            lstPermisos.Enabled = !_esAdministrador;

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
                lstPermisos.Items.Clear();

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

                    lstPermisos.Items.Add(texto, p.Efectivo);
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
                deseados[_permisos[i].Permiso] = lstPermisos.GetItemChecked(i);
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
