using System;
using System.Collections.Generic;
using System.Linq;
using Edelstahl.Services.DomainModel.Security;

namespace Edelstahl.Services.DomainModel.Composite
{
    /// <summary>
    /// Lo que un usuario puede hacer, según el Composite: una lista de Component (Familias y/o
    /// Patentes sueltas) a partir de la cual se recorre el árbol completo.
    /// </summary>
    public class UsuarioPrivilegios
    {
        public Guid IdUsuario { get; set; }

        public string NombreUsuario { get; set; }

        /// <summary>Para gestionar el patrón Composite se usa una lista de Component.</summary>
        public List<Component> Privilegios { get; set; }

        public UsuarioPrivilegios()
        {
            NombreUsuario = string.Empty;
            Privilegios = new List<Component>();
        }

        /// <summary>Recorre el árbol y devuelve todas las patentes accesibles, sin repetidos.</summary>
        public List<Patente> Patentes
        {
            get
            {
                List<Patente> resultado = new List<Patente>();

                foreach (Component privilegio in Privilegios)
                {
                    foreach (Patente patente in privilegio.Patentes())
                    {
                        if (!resultado.Any(p => string.Equals(p.DataKey, patente.DataKey, StringComparison.OrdinalIgnoreCase)))
                        {
                            resultado.Add(patente);
                        }
                    }
                }

                return resultado;
            }
        }

        /// <summary>Recorre el árbol y devuelve todas las familias accesibles, sin repetidos.</summary>
        public List<Familia> Familias
        {
            get
            {
                List<Familia> resultado = new List<Familia>();

                foreach (Familia familia in Privilegios.OfType<Familia>())
                {
                    if (!resultado.Any(f => Component.EsElMismo(f, familia)))
                    {
                        resultado.Add(familia);
                    }

                    foreach (Familia hija in familia.Familias())
                    {
                        if (!resultado.Any(f => Component.EsElMismo(f, hija)))
                        {
                            resultado.Add(hija);
                        }
                    }
                }

                return resultado;
            }
        }

        public bool Tiene(Permiso permiso)
        {
            return Privilegios.Any(p => p.Incluye(permiso));
        }
    }
}
