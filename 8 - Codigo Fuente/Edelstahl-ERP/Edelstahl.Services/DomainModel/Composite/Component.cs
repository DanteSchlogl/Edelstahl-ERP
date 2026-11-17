using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Edelstahl.Services.DomainModel.Security;

namespace Edelstahl.Services.DomainModel.Composite
{
    /// <summary>
    /// Patrón Composite aplicado a roles y permisos (igual que en el ejemplo de la cátedra).
    ///
    ///   Component : lo que comparten una Patente y una Familia.
    ///   Patente   : hoja del árbol. Un permiso suelto (una pantalla, un caso de uso, etc.).
    ///   Familia   : nodo compuesto. Agrupa Patentes y/u otras Familias. Un ROL es una Familia.
    ///
    /// Preguntarle a una Familia si incluye un permiso es igual que preguntárselo a una
    /// Patente: quien pregunta no necesita saber si tiene delante un permiso o un grupo.
    /// </summary>
    public abstract class Component
    {
        public Guid Id { get; set; }

        public string Nombre { get; set; }

        protected Component()
        {
            Nombre = string.Empty;
        }

        public abstract void Add(Component component);

        public abstract void Remove(Component component);

        /// <summary>true si este componente (o alguno de sus hijos) otorga el permiso.</summary>
        public abstract bool Incluye(Permiso permiso);

        /// <summary>Todas las patentes que otorga, sin repetidos y con las familias ya desplegadas.</summary>
        public abstract List<Patente> Patentes();

        /// <summary>Los permisos del programa (enum Permiso) que otorga, sin repetidos.</summary>
        public IEnumerable<Permiso> Permisos()
        {
            return Patentes()
                .Where(p => p.PermisoAsociado.HasValue)
                .Select(p => p.PermisoAsociado.Value)
                .Distinct();
        }

        /// <summary>Texto con el árbol, con sangría por nivel (útil para mostrar o depurar).</summary>
        public string Arbol()
        {
            StringBuilder texto = new StringBuilder();
            Escribir(texto, 0);
            return texto.ToString();
        }

        internal abstract void Escribir(StringBuilder texto, int nivel);

        internal abstract bool Contiene(Component otro);

        /// <summary>Es el mismo componente: la misma instancia o el mismo Id guardado.</summary>
        internal static bool EsElMismo(Component a, Component b)
        {
            return ReferenceEquals(a, b)
                || (a != null && b != null && a.Id != Guid.Empty && a.Id == b.Id && a.GetType() == b.GetType());
        }
    }
}
