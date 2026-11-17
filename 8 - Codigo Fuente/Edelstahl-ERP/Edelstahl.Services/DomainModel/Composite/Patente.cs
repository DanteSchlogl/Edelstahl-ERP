using System;
using System.Collections.Generic;
using System.Text;
using Edelstahl.Services.DomainModel.Security;

namespace Edelstahl.Services.DomainModel.Composite
{
    /// <summary>Tipo de recurso que protege una patente.</summary>
    public enum TipoAcceso
    {
        Pantalla,
        CasoUso,
        Servicio,
        SP,
        Tabla
    }

    /// <summary>
    /// Hoja del árbol de permisos: una patente es un permiso suelto.
    /// DataKey es la clave con la que el sistema la reconoce (por ejemplo "ClientesVer"
    /// o el nombre de una pantalla).
    /// </summary>
    public sealed class Patente : Component
    {
        public string DataKey { get; set; }

        public TipoAcceso TipoAcceso { get; set; }

        /// <summary>Constructor por defecto: lo necesita la capa de datos al reconstruir el objeto.</summary>
        public Patente()
        {
            DataKey = string.Empty;
            TipoAcceso = TipoAcceso.CasoUso;
        }

        /// <summary>Patente de un permiso del programa (su DataKey es el nombre del permiso).</summary>
        public Patente(Permiso permiso)
        {
            DataKey = permiso.ToString();
            Nombre = DataKey;
            TipoAcceso = TipoAcceso.CasoUso;
        }

        /// <summary>El permiso del programa que representa, o null si el DataKey no es uno de ellos.</summary>
        public Permiso? PermisoAsociado
        {
            get
            {
                Permiso permiso;

                return Enum.TryParse(DataKey, out permiso) && Enum.IsDefined(typeof(Permiso), permiso)
                    ? (Permiso?)permiso
                    : null;
            }
        }

        public override void Add(Component component)
        {
            throw new InvalidOperationException("No se pueden agregar elementos en un hijo tipo hoja.");
        }

        public override void Remove(Component component)
        {
            throw new InvalidOperationException("No se pueden eliminar elementos en un hijo tipo hoja.");
        }

        public override bool Incluye(Permiso permiso)
        {
            return PermisoAsociado == permiso;
        }

        public override List<Patente> Patentes()
        {
            return new List<Patente> { this };
        }

        internal override void Escribir(StringBuilder texto, int nivel)
        {
            texto.Append(' ', nivel * 2).Append("- ").AppendLine(string.IsNullOrEmpty(Nombre) ? DataKey : Nombre);
        }

        internal override bool Contiene(Component otro)
        {
            return EsElMismo(this, otro);
        }
    }
}
