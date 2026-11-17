using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Edelstahl.Services.DomainModel.Security;

namespace Edelstahl.Services.DomainModel.Composite
{
    /// <summary>
    /// Nodo compuesto del árbol de permisos: agrupa Patentes y otras Familias.
    /// Un rol (Vendedor, Supervisor, etc.) es una Familia.
    /// </summary>
    public sealed class Familia : Component
    {
        private readonly List<Component> _hijos = new List<Component>();

        /// <summary>Constructor por defecto: lo necesita la capa de datos al reconstruir el objeto.</summary>
        public Familia()
        {
        }

        public Familia(string nombre)
        {
            if (string.IsNullOrWhiteSpace(nombre))
            {
                throw new ArgumentException("La familia necesita un nombre.", "nombre");
            }

            Nombre = nombre.Trim();
        }

        /// <summary>Lista de hijos de la familia: Patentes y/u otras Familias.</summary>
        public List<Component> Hijos
        {
            get { return _hijos; }
        }

        public List<Component> GetHijos()
        {
            return _hijos;
        }

        /// <summary>Agrega un hijo. Rechaza los que formarían un ciclo.</summary>
        public override void Add(Component component)
        {
            if (component == null)
            {
                throw new ArgumentNullException("component");
            }

            if (EsElMismo(component, this) || component.Contiene(this))
            {
                throw new InvalidOperationException(
                    "No se puede agregar '" + component.Nombre + "' a '" + Nombre + "': formaría un ciclo.");
            }

            if (!_hijos.Any(h => EsElMismo(h, component)))
            {
                _hijos.Add(component);
            }
        }

        public void AddRange(IEnumerable<Component> components)
        {
            foreach (Component component in components)
            {
                Add(component);
            }
        }

        public override void Remove(Component component)
        {
            _hijos.RemoveAll(h => EsElMismo(h, component));
        }

        // ---- Variantes para armar el árbol en una sola línea ----

        public Familia Agregar(Component componente)
        {
            Add(componente);
            return this;
        }

        public Familia Agregar(params Permiso[] permisos)
        {
            foreach (Permiso permiso in permisos)
            {
                Add(new Patente(permiso));
            }

            return this;
        }

        public override bool Incluye(Permiso permiso)
        {
            return _hijos.Any(hijo => hijo.Incluye(permiso));
        }

        public override List<Patente> Patentes()
        {
            List<Patente> resultado = new List<Patente>();

            foreach (Component hijo in _hijos)
            {
                foreach (Patente patente in hijo.Patentes())
                {
                    if (!resultado.Any(p => string.Equals(p.DataKey, patente.DataKey, StringComparison.OrdinalIgnoreCase)))
                    {
                        resultado.Add(patente);
                    }
                }
            }

            return resultado;
        }

        /// <summary>Todas las familias que contiene, a cualquier nivel, sin repetidos.</summary>
        public List<Familia> Familias()
        {
            List<Familia> resultado = new List<Familia>();
            RecorrerFamilias(resultado);
            return resultado;
        }

        private void RecorrerFamilias(List<Familia> acumulado)
        {
            foreach (Familia hija in _hijos.OfType<Familia>())
            {
                if (!acumulado.Any(f => EsElMismo(f, hija)))
                {
                    acumulado.Add(hija);
                    hija.RecorrerFamilias(acumulado);
                }
            }
        }

        internal override void Escribir(StringBuilder texto, int nivel)
        {
            texto.Append(' ', nivel * 2).Append("+ ").AppendLine(Nombre);

            foreach (Component hijo in _hijos)
            {
                hijo.Escribir(texto, nivel + 1);
            }
        }

        internal override bool Contiene(Component otro)
        {
            return EsElMismo(this, otro) || _hijos.Any(hijo => hijo.Contiene(otro));
        }
    }
}
