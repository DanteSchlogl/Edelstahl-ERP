using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using Edelstahl.DAL.EntityFramework;
using Edelstahl.DAL.Interfaces;
using Edelstahl.Domain.Comercial;

namespace Edelstahl.DAL.Implementations.EntityFramework
{
    /// <summary>
    /// Persistencia de presupuestos y detalles mediante Entity Framework 6.
    /// </summary>
    public class PresupuestoRepositoryEntityFramework
        : IPresupuestoRepository
    {
        public void Add(Presupuesto entity)
        {
            ValidarPresupuesto(entity);

            if (entity.Id == Guid.Empty)
            {
                entity.Id = Guid.NewGuid();
            }

            PrepararDetalles(entity);

            using (EdelstahlNegocioContext context =
                new EdelstahlNegocioContext())
            using (DbContextTransaction transaction =
                context.Database.BeginTransaction())
            {
                try
                {
                    if (context.Presupuestos.Any(
                        presupuesto => presupuesto.Numero == entity.Numero))
                    {
                        throw new InvalidOperationException(
                            "Ya existe un presupuesto con el número ingresado.");
                    }

                    context.Presupuestos.Add(entity);
                    context.SaveChanges();
                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        public void Update(Presupuesto entity)
        {
            ValidarPresupuesto(entity);

            if (entity.Id == Guid.Empty)
            {
                throw new ArgumentException(
                    "El identificador del presupuesto no es válido.",
                    nameof(entity));
            }

            PrepararDetalles(entity);

            using (EdelstahlNegocioContext context =
                new EdelstahlNegocioContext())
            using (DbContextTransaction transaction =
                context.Database.BeginTransaction())
            {
                try
                {
                    bool numeroDuplicado = context.Presupuestos.Any(
                        presupuesto =>
                            presupuesto.Numero == entity.Numero &&
                            presupuesto.Id != entity.Id);

                    if (numeroDuplicado)
                    {
                        throw new InvalidOperationException(
                            "Ya existe otro presupuesto con ese número.");
                    }

                    Presupuesto existente = context.Presupuestos
                        .Include(presupuesto => presupuesto.Detalles)
                        .FirstOrDefault(
                            presupuesto => presupuesto.Id == entity.Id);

                    if (existente == null)
                    {
                        throw new InvalidOperationException(
                            "No se encontró el presupuesto que se desea modificar.");
                    }

                    context.DetallesPresupuesto.RemoveRange(
                        existente.Detalles);

                    CopiarDatos(entity, existente);
                    existente.Detalles = new List<DetallePresupuesto>();

                    foreach (DetallePresupuesto detalle in entity.Detalles)
                    {
                        detalle.PresupuestoId = existente.Id;
                        existente.Detalles.Add(detalle);
                    }

                    context.SaveChanges();
                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        public void Delete(Guid id)
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentException(
                    "El identificador del presupuesto no es válido.",
                    nameof(id));
            }

            using (EdelstahlNegocioContext context =
                new EdelstahlNegocioContext())
            {
                Presupuesto existente = context.Presupuestos.Find(id);

                if (existente == null)
                {
                    throw new InvalidOperationException(
                        "No se encontró el presupuesto que se desea eliminar.");
                }

                existente.Estado = EstadoPresupuesto.Rechazado;
                context.SaveChanges();
            }
        }

        public Presupuesto GetById(Guid id)
        {
            if (id == Guid.Empty)
            {
                return null;
            }

            using (EdelstahlNegocioContext context =
                new EdelstahlNegocioContext())
            {
                return context.Presupuestos
                    .AsNoTracking()
                    .Include(presupuesto => presupuesto.Detalles)
                    .FirstOrDefault(
                        presupuesto => presupuesto.Id == id);
            }
        }

        public List<Presupuesto> GetAll()
        {
            using (EdelstahlNegocioContext context =
                new EdelstahlNegocioContext())
            {
                return context.Presupuestos
                    .AsNoTracking()
                    .Include(presupuesto => presupuesto.Detalles)
                    .OrderByDescending(
                        presupuesto => presupuesto.FechaEmision)
                    .ToList();
            }
        }

        public List<Presupuesto> GetByClienteId(Guid clienteId)
        {
            if (clienteId == Guid.Empty)
            {
                return new List<Presupuesto>();
            }

            using (EdelstahlNegocioContext context =
                new EdelstahlNegocioContext())
            {
                return context.Presupuestos
                    .AsNoTracking()
                    .Include(presupuesto => presupuesto.Detalles)
                    .Where(
                        presupuesto => presupuesto.ClienteId == clienteId)
                    .OrderByDescending(
                        presupuesto => presupuesto.FechaEmision)
                    .ToList();
            }
        }

        public Presupuesto GetByNumero(string numero)
        {
            string normalizado = NormalizarNumero(numero);

            if (string.IsNullOrWhiteSpace(normalizado))
            {
                return null;
            }

            using (EdelstahlNegocioContext context =
                new EdelstahlNegocioContext())
            {
                return context.Presupuestos
                    .AsNoTracking()
                    .Include(presupuesto => presupuesto.Detalles)
                    .FirstOrDefault(
                        presupuesto => presupuesto.Numero == normalizado);
            }
        }

        public bool ExistsByNumero(string numero)
        {
            string normalizado = NormalizarNumero(numero);

            if (string.IsNullOrWhiteSpace(normalizado))
            {
                return false;
            }

            using (EdelstahlNegocioContext context =
                new EdelstahlNegocioContext())
            {
                return context.Presupuestos.Any(
                    presupuesto => presupuesto.Numero == normalizado);
            }
        }

        private static void PrepararDetalles(Presupuesto presupuesto)
        {
            presupuesto.Numero = NormalizarNumero(presupuesto.Numero);

            if (presupuesto.Detalles == null)
            {
                presupuesto.Detalles = new List<DetallePresupuesto>();
            }

            foreach (DetallePresupuesto detalle in presupuesto.Detalles)
            {
                if (detalle.Id == Guid.Empty)
                {
                    detalle.Id = Guid.NewGuid();
                }

                detalle.PresupuestoId = presupuesto.Id;
                detalle.Codigo = Limpiar(detalle.Codigo);
                detalle.Descripcion = Limpiar(detalle.Descripcion);
                detalle.DescripcionTecnica = Limpiar(
                    detalle.DescripcionTecnica);
            }
        }

        private static void CopiarDatos(
            Presupuesto origen,
            Presupuesto destino)
        {
            destino.Numero = origen.Numero;
            destino.ClienteId = origen.ClienteId;
            destino.FechaEmision = origen.FechaEmision;
            destino.FechaVencimiento = origen.FechaVencimiento;
            destino.Moneda = origen.Moneda;
            destino.TipoCambio = origen.TipoCambio;
            destino.PorcentajeIVA = origen.PorcentajeIVA;
            destino.PorcentajeRecargo = origen.PorcentajeRecargo;
            destino.PorcentajeDescuentoGeneral =
                origen.PorcentajeDescuentoGeneral;
            destino.PorcentajeAnticipo = origen.PorcentajeAnticipo;
            destino.CondicionPago = Limpiar(origen.CondicionPago);
            destino.PlazoEntrega = Limpiar(origen.PlazoEntrega);
            destino.EntregaIncluida = origen.EntregaIncluida;
            destino.Estado = origen.Estado;
            destino.Observaciones = Limpiar(origen.Observaciones);
        }

        private static void ValidarPresupuesto(Presupuesto presupuesto)
        {
            if (presupuesto == null)
            {
                throw new ArgumentNullException(nameof(presupuesto));
            }

            if (string.IsNullOrWhiteSpace(presupuesto.Numero))
            {
                throw new InvalidOperationException(
                    "El número del presupuesto es obligatorio.");
            }

            if (presupuesto.ClienteId == Guid.Empty)
            {
                throw new InvalidOperationException(
                    "El cliente del presupuesto no es válido.");
            }

            if (presupuesto.Detalles == null ||
                presupuesto.Detalles.Count == 0)
            {
                throw new InvalidOperationException(
                    "El presupuesto debe contener al menos un detalle.");
            }
        }

        private static string NormalizarNumero(string numero)
        {
            return string.IsNullOrWhiteSpace(numero)
                ? string.Empty
                : numero.Trim().ToUpperInvariant();
        }

        private static string Limpiar(string valor)
        {
            return string.IsNullOrWhiteSpace(valor)
                ? string.Empty
                : valor.Trim();
        }
    }
}
