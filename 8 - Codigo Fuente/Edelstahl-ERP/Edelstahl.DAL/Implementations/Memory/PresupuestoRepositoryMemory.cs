using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Edelstahl.DAL.Interfaces;
using Edelstahl.Domain.Comercial;

namespace Edelstahl.DAL.Implementations.Memory
{
    /// <summary>
    /// Implementación temporal del repositorio de presupuestos.
    /// Los datos se conservan mientras la aplicación está abierta.
    /// </summary>
    public sealed class PresupuestoRepositoryMemory
        : IPresupuestoRepository
    {
        private readonly List<Presupuesto> _presupuestos;

        public PresupuestoRepositoryMemory()
        {
            _presupuestos = new List<Presupuesto>();
        }

        public void Add(Presupuesto entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            _presupuestos.Add(entity);
        }

        public void Update(Presupuesto entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            Presupuesto presupuestoExistente =
                GetById(entity.Id);

            if (presupuestoExistente == null)
            {
                throw new InvalidOperationException(
                    "No se encontró el presupuesto que se desea modificar.");
            }

            presupuestoExistente.Numero = entity.Numero;
            presupuestoExistente.ClienteId = entity.ClienteId;
            presupuestoExistente.FechaEmision = entity.FechaEmision;
            presupuestoExistente.FechaVencimiento =
                entity.FechaVencimiento;
            presupuestoExistente.Moneda = entity.Moneda;
            presupuestoExistente.TipoCambio = entity.TipoCambio;
            presupuestoExistente.PorcentajeIVA =
                entity.PorcentajeIVA;
            presupuestoExistente.PorcentajeRecargo =
                entity.PorcentajeRecargo;
            presupuestoExistente.PorcentajeDescuentoGeneral =
                entity.PorcentajeDescuentoGeneral;
            presupuestoExistente.PorcentajeAnticipo =
                entity.PorcentajeAnticipo;
            presupuestoExistente.CondicionPago =
                entity.CondicionPago;
            presupuestoExistente.PlazoEntrega =
                entity.PlazoEntrega;
            presupuestoExistente.EntregaIncluida =
                entity.EntregaIncluida;
            presupuestoExistente.Estado = entity.Estado;
            presupuestoExistente.Observaciones =
                entity.Observaciones;

            presupuestoExistente.Detalles =
                new List<DetallePresupuesto>(entity.Detalles);
        }

        public void Delete(Guid id)
        {
            Presupuesto presupuestoExistente =
                GetById(id);

            if (presupuestoExistente == null)
            {
                throw new InvalidOperationException(
                    "No se encontró el presupuesto que se desea eliminar.");
            }

            _presupuestos.Remove(presupuestoExistente);
        }

        public Presupuesto GetById(Guid id)
        {
            return _presupuestos.FirstOrDefault(
                presupuesto => presupuesto.Id == id);
        }

        public List<Presupuesto> GetAll()
        {
            return new List<Presupuesto>(_presupuestos);
        }

        public List<Presupuesto> GetByClienteId(Guid clienteId)
        {
            return _presupuestos
                .Where(presupuesto =>
                    presupuesto.ClienteId == clienteId)
                .ToList();
        }

        public Presupuesto GetByNumero(string numero)
        {
            return _presupuestos.FirstOrDefault(
                presupuesto => string.Equals(
                    presupuesto.Numero,
                    numero,
                    StringComparison.OrdinalIgnoreCase));
        }

        public bool ExistsByNumero(string numero)
        {
            return _presupuestos.Any(
                presupuesto => string.Equals(
                    presupuesto.Numero,
                    numero,
                    StringComparison.OrdinalIgnoreCase));
        }
    }
}
