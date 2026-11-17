using System;
using Edelstahl.Services.Logic.DTOs;
using Edelstahl.Services.Logic;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using Edelstahl.BLL.DTOs;
using Edelstahl.BLL.Services;
using Edelstahl.Services.DomainModel.Security;

namespace Edelstahl.WinApp.Ui
{
    /// <summary>
    /// Familias (roles y grupos de permisos) de un usuario. Solo lo abre el administrador.
    /// A la izquierda están las familias que el usuario todavía no tiene; a la derecha, las que tiene.
    /// Abajo se ve el árbol completo de lo que recibe: cada familia con sus familias hijas y permisos.
    /// La distribución de los controles está en FrmFamiliasUsuario.Designer.cs.
    /// </summary>
    public sealed partial class FrmFamiliasUsuario : Form
    {
        private AdministracionService _admin;
        private Guid _usuarioId;

        // Constructor sin parámetros: lo usa el diseñador de Visual Studio.
        public FrmFamiliasUsuario()
        {
            InitializeComponent();
        }

        public FrmFamiliasUsuario(Guid usuarioId, string usuario, string rol) : this()
        {
            _admin = new AdministracionService();
            _usuarioId = usuarioId;

            bool esAdministrador = string.Equals(rol, NombresRol.Administrador, StringComparison.OrdinalIgnoreCase);

            Text = Tr.T("Families of {0}", usuario);
            Marca.AplicarIcono(this);

            lblEncabezado.Text = Tr.T("Families of {0}", usuario) + Environment.NewLine
                + Tr.T("Role: {0}", rol)
                + (esAdministrador ? Environment.NewLine + Tr.T("The permissions of an administrator cannot be changed.") : string.Empty);

            lblDisponibles.Text = Tr.T("Available families");
            lblAsignadas.Text = Tr.T("Assigned families");
            lblArbol.Text = Tr.T("Permissions this user receives through families and direct permissions");
            btnAsignar.Text = Tr.T("Assign");
            btnQuitar.Text = Tr.T("Remove");
            btnCerrar.Text = Tr.T("Close");

            btnAsignar.Enabled = !esAdministrador;
            btnQuitar.Enabled = !esAdministrador;
            lstDisponibles.Enabled = !esAdministrador;
            lstAsignadas.Enabled = !esAdministrador;

            btnAsignar.Click += (s, e) => Asignar();
            btnQuitar.Click += (s, e) => Quitar();
            lstDisponibles.DoubleClick += (s, e) => Asignar();
            lstAsignadas.DoubleClick += (s, e) => Quitar();

            Cargar();
        }

        private void Cargar()
        {
            List<FamiliaDto> todas = null;
            List<FamiliaDto> asignadas = null;
            List<NodoPermisoDto> arbol = null;

            if (!Estilo.Ejecutar(this, () =>
            {
                todas = _admin.ObtenerFamilias();
                asignadas = _admin.ObtenerFamiliasDeUsuario(_usuarioId);
                arbol = _admin.ObtenerArbolDeUsuario(_usuarioId);
            }))
            {
                return;
            }

            HashSet<Guid> tiene = new HashSet<Guid>(asignadas.Select(f => f.Id));

            lstDisponibles.Items.Clear();
            lstAsignadas.Items.Clear();

            foreach (FamiliaDto familia in todas)
            {
                (tiene.Contains(familia.Id) ? lstAsignadas : lstDisponibles).Items.Add(new FamiliaItem(familia));
            }

            tvPermisos.BeginUpdate();
            tvPermisos.Nodes.Clear();
            AgregarNodos(tvPermisos.Nodes, arbol);
            tvPermisos.ExpandAll();
            tvPermisos.EndUpdate();
        }

        private static void AgregarNodos(TreeNodeCollection destino, IEnumerable<NodoPermisoDto> nodos)
        {
            foreach (NodoPermisoDto nodo in nodos)
            {
                string texto = nodo.EsFamilia ? nodo.Nombre : Tr.T(Nombres.Permiso(nodo.Clave));
                TreeNode hijo = destino.Add(texto);

                if (nodo.EsFamilia)
                {
                    hijo.NodeFont = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
                    AgregarNodos(hijo.Nodes, nodo.Hijos);
                }
            }
        }

        private void Asignar()
        {
            FamiliaItem elegida = lstDisponibles.SelectedItem as FamiliaItem;

            if (elegida == null)
            {
                Estilo.Avisar(this, Tr.T("Select a family first."));
                return;
            }

            if (Estilo.Ejecutar(this, () => _admin.AsignarFamilia(_usuarioId, elegida.Familia.Id)))
            {
                Cargar();
            }
        }

        private void Quitar()
        {
            FamiliaItem elegida = lstAsignadas.SelectedItem as FamiliaItem;

            if (elegida == null)
            {
                Estilo.Avisar(this, Tr.T("Select a family first."));
                return;
            }

            if (Estilo.Ejecutar(this, () => _admin.QuitarFamilia(_usuarioId, elegida.Familia.Id)))
            {
                Cargar();
            }
        }

        private sealed class FamiliaItem
        {
            public FamiliaDto Familia { get; private set; }

            public FamiliaItem(FamiliaDto familia)
            {
                Familia = familia;
            }

            public override string ToString()
            {
                return Familia.Nombre + "  (" + Familia.Permisos + ")";
            }
        }
    }
}
