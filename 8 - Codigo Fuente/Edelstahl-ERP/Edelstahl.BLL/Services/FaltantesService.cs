using System;
using System.Collections.Generic;
using System.Linq;
using Edelstahl.BLL.DTOs;
using Edelstahl.BLL.Infrastructure;
using Edelstahl.DAL.Factory;
using Edelstahl.DAL.Store;
using Edelstahl.Domain.Catalogo;
using Edelstahl.Domain.Comercial;
using Edelstahl.Domain.Security;

namespace Edelstahl.BLL.Services
{
    /// <summary>
    /// Calcula que productos faltan para poder vender: compara lo que piden los presupuestos
    /// con las existencias disponibles (stock fisico menos lo reservado para otros pedidos).
    /// </summary>
    public class FaltantesService
    {
        private readonly INegocioStore _almacen;

        public FaltantesService()
            : this(FactoryDataAccess.NegocioStore)
        {
        }

        public FaltantesService(INegocioStore almacen)
        {
            _almacen = almacen ?? throw new ArgumentNullException(nameof(almacen));
        }

        /// <summary>
        /// Productos que faltan para confirmar un presupuesto en particular.
        /// </summary>
        public List<FaltanteDto> DePresupuesto(Guid presupuestoId)
        {
            Presupuesto presupuesto = FactoryDataAccess.PresupuestoRepository.GetById(presupuestoId);

            return presupuesto == null
                ? new List<FaltanteDto>()
                : Calcular(new List<Presupuesto> { presupuesto });
        }

        /// <summary>
        /// Productos que faltan para los presupuestos enviados o aceptados que siguen vigentes.
        /// </summary>
        public List<FaltanteDto> Generales()
        {
            Autorizacion.Exigir(Permiso.InventarioVer);

            List<Presupuesto> abiertos = new List<Presupuesto>();

            foreach (Presupuesto resumen in FactoryDataAccess.PresupuestoRepository.GetAll()
                .Where(p => (p.Estado == EstadoPresupuesto.Aceptado || p.Estado == EstadoPresupuesto.Enviado)
                    && p.FechaVencimiento.Date >= DateTime.Today))
            {
                Presupuesto completo = FactoryDataAccess.PresupuestoRepository.GetById(resumen.Id);

                if (completo != null)
                {
                    abiertos.Add(completo);
                }
            }

            return Calcular(abiertos);
        }

        private List<FaltanteDto> Calcular(List<Presupuesto> presupuestos)
        {
            return _almacen.Ejecutar(unidad =>
            {
                Dictionary<string, Producto> porCodigo = new Dictionary<string, Producto>(StringComparer.OrdinalIgnoreCase);

                foreach (Producto producto in unidad.Conjunto<Producto>().ToList())
                {
                    if (!string.IsNullOrEmpty(producto.CodigoSku) && !porCodigo.ContainsKey(producto.CodigoSku))
                    {
                        porCodigo[producto.CodigoSku] = producto;
                    }
                }

                Dictionary<Guid, decimal> requerido = new Dictionary<Guid, decimal>();
                Dictionary<Guid, List<string>> origen = new Dictionary<Guid, List<string>>();

                foreach (Presupuesto presupuesto in presupuestos)
                {
                    foreach (DetallePresupuesto linea in presupuesto.Detalles.Where(d => d.TipoItem == TipoItemPresupuesto.Producto))
                    {
                        Producto producto;

                        if (!porCodigo.TryGetValue(linea.Codigo ?? string.Empty, out producto))
                        {
                            continue;
                        }

                        decimal acumulado;
                        requerido.TryGetValue(producto.Id, out acumulado);
                        requerido[producto.Id] = acumulado + linea.Cantidad;

                        if (!origen.ContainsKey(producto.Id))
                        {
                            origen[producto.Id] = new List<string>();
                        }

                        if (!origen[producto.Id].Contains(presupuesto.Numero))
                        {
                            origen[producto.Id].Add(presupuesto.Numero);
                        }
                    }
                }

                List<FaltanteDto> resultado = new List<FaltanteDto>();

                foreach (KeyValuePair<Guid, decimal> par in requerido)
                {
                    Producto producto = porCodigo.Values.First(p => p.Id == par.Key);
                    decimal disponible = InventarioMotor.DisponibleTotal(unidad, par.Key);
                    decimal falta = par.Value - disponible;

                    if (falta > 0m)
                    {
                        resultado.Add(new FaltanteDto
                        {
                            ProductoId = producto.Id,
                            Sku = producto.CodigoSku,
                            Producto = producto.Nombre,
                            Unidad = producto.UnidadMedida,
                            Requerido = par.Value,
                            Disponible = disponible < 0m ? 0m : disponible,
                            Faltante = falta,
                            CostoActual = producto.CostoActual,
                            Comprable = producto.EsComprable,
                            Presupuestos = string.Join(", ", origen[producto.Id])
                        });
                    }
                }

                return resultado.OrderBy(f => f.Sku).ToList();
            });
        }
    }
}
