using System;
using System.Collections.Generic;
using Edelstahl.Services.DataAccess.Interfaces;
using Edelstahl.Services.DomainModel.Composite;

namespace Edelstahl.Services.DataAccess.Implementations.Adapters
{
    /// <summary>Arma una Patente con los valores de su fila (IdPatente, DataKey, Nombre, TipoAcceso).</summary>
    internal class PatenteAdapter : IAdapter<Patente>
    {
        private static readonly PatenteAdapter _instance = new PatenteAdapter();

        public static PatenteAdapter Current
        {
            get { return _instance; }
        }

        private PatenteAdapter()
        {
        }

        public Patente Get(object[] valores)
        {
            return new Patente
            {
                Id = Guid.Parse(valores[0].ToString()),
                DataKey = valores[1].ToString(),
                Nombre = valores[2].ToString(),
                TipoAcceso = (TipoAcceso)Convert.ToInt32(valores[3])
            };
        }
    }

    /// <summary>
    /// Arma una Familia con los valores de su fila (IdFamilia, Nombre) y la hidrata: carga
    /// recursivamente sus familias hijas y sus patentes.
    /// </summary>
    internal class FamiliaAdapter : IAdapter<Familia>
    {
        private static readonly FamiliaAdapter _instance = new FamiliaAdapter();

        // Familias que se están cargando en este momento: corta la recursión si la base tuviera un ciclo.
        [ThreadStatic]
        private static HashSet<Guid> _enCarga;

        public static FamiliaAdapter Current
        {
            get { return _instance; }
        }

        private FamiliaAdapter()
        {
        }

        public Familia Get(object[] valores)
        {
            Familia familia = new Familia
            {
                Id = Guid.Parse(valores[0].ToString()),
                Nombre = valores[1].ToString()
            };

            if (_enCarga == null)
            {
                _enCarga = new HashSet<Guid>();
            }

            if (!_enCarga.Add(familia.Id))
            {
                return familia;
            }

            try
            {
                // Hidratación de los objetos hijos
                familia.AddRange(new FamiliaFamiliaRepository().GetByObject(familia));
                familia.AddRange(new FamiliaPatenteRepository().GetByObject(familia));
            }
            finally
            {
                _enCarga.Remove(familia.Id);
            }

            return familia;
        }
    }
}
