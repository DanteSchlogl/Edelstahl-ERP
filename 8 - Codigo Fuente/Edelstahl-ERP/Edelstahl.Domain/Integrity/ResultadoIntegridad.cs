using System;
using System.Collections.Generic;
using System.Linq;

namespace Edelstahl.Domain.Integrity
{
    /// <summary>
    /// Qué dígito verificador no coincide: el de un registro (horizontal)
    /// o el del conjunto de la tabla (vertical).
    /// </summary>
    public enum TipoDigito
    {
        Horizontal,
        Vertical
    }

    /// <summary>
    /// Una diferencia encontrada al verificar los dígitos verificadores.
    /// </summary>
    public class InconsistenciaIntegridad
    {
        public string Tabla { get; set; }

        public TipoDigito Tipo { get; set; }

        /// <summary>
        /// Registro afectado; es null cuando la diferencia es del DVV de la tabla.
        /// </summary>
        public Guid? RegistroId { get; set; }

        /// <summary>
        /// Texto que identifica el registro para quien lee el informe
        /// (por ejemplo, la razón social del cliente).
        /// </summary>
        public string Referencia { get; set; }

        public string Detalle { get; set; }

        public InconsistenciaIntegridad()
        {
            Tabla = string.Empty;
            Referencia = string.Empty;
            Detalle = string.Empty;
        }
    }

    /// <summary>
    /// Resultado de verificar los dígitos verificadores de las tablas protegidas.
    /// </summary>
    public class ResultadoIntegridad
    {
        private readonly List<InconsistenciaIntegridad> _inconsistencias =
            new List<InconsistenciaIntegridad>();

        public DateTime FechaVerificacion { get; set; }

        /// <summary>
        /// Cantidad de registros revisados (uno por cada DVH comprobado).
        /// </summary>
        public int RegistrosRevisados { get; set; }

        public IList<InconsistenciaIntegridad> Inconsistencias
        {
            get { return _inconsistencias; }
        }

        public bool Correcto
        {
            get { return _inconsistencias.Count == 0; }
        }

        public int RegistrosAlterados
        {
            get
            {
                return _inconsistencias
                    .Where(i => i.Tipo == TipoDigito.Horizontal)
                    .Select(i => i.RegistroId)
                    .Distinct()
                    .Count();
            }
        }

        public bool AlteradaLaTabla
        {
            get { return _inconsistencias.Any(i => i.Tipo == TipoDigito.Vertical); }
        }

        public ResultadoIntegridad()
        {
            FechaVerificacion = DateTime.Now;
        }

        public void Agregar(InconsistenciaIntegridad inconsistencia)
        {
            if (inconsistencia == null)
            {
                throw new ArgumentNullException(nameof(inconsistencia));
            }

            _inconsistencias.Add(inconsistencia);
        }
    }
}
