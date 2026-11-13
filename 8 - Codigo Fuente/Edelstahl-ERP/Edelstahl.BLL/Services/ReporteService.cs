using System;
using System.Collections.Generic;
using System.Linq;
using Edelstahl.BLL.DTOs;
using Edelstahl.BLL.Infrastructure;
using Edelstahl.DAL.Factory;
using Edelstahl.DAL.Store;
using Edelstahl.Domain.Catalogo;
using Edelstahl.Domain.Comercial;
using Edelstahl.Domain.Compras;
using Edelstahl.Domain.Finanzas;
using Edelstahl.Domain.Logistica;
using Edelstahl.Domain.Produccion;
using Edelstahl.Domain.Security;

namespace Edelstahl.BLL.Services
{
    /// <summary>
    /// Indicadores y reportes de gestión. Todos los importes se expresan en
    /// pesos; los pedidos en dólares se convierten con su tipo de cambio.
    /// </summary>
    public class ReporteService
    {
        private readonly INegocioStore _almacen;

        public ReporteService()
            : this(FactoryDataAccess.NegocioStore)
        {
        }

        public ReporteService(INegocioStore almacen)
        {
            _almacen = almacen ?? throw new ArgumentNullException(nameof(almacen));
        }

        private static bool Activo(EstadoPedido estado)
        {
            return estado != EstadoPedido.Cancelado && estado != EstadoPedido.Entregado && estado != EstadoPedido.Pendiente;
        }

        public IndicadoresDto ObtenerIndicadores()
        {
            Autorizacion.Exigir(Permiso.ReportesVer);

            DateTime inicioMes = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            List<Pedido> pedidos = FactoryDataAccess.PedidoRepository.GetAll();

            IndicadoresDto indicadores = new IndicadoresDto
            {
                VentasDelMes = pedidos
                    .Where(p => p.Estado != EstadoPedido.Cancelado && p.FechaPedido >= inicioMes)
                    .Sum(p => p.CalcularTotalEnPesos()),
                PedidosActivos = pedidos.Count(p => Activo(p.Estado)),
                ProductosEnAlerta = new InventarioService(_almacen).ObtenerAlertas().Count
            };

            _almacen.Ejecutar(unidad =>
            {
                indicadores.OrdenesEnProceso = unidad.Conjunto<OrdenFabricacion>()
                    .Count(o => o.Estado == EstadoProduccion.EnProceso || o.Estado == EstadoProduccion.ControlCalidad);

                List<Factura> abiertas = unidad.Conjunto<Factura>()
                    .Where(f => f.SaldoPendiente > 0m && f.EstadoPago != EstadoPago.Anulada)
                    .ToList();

                indicadores.FacturasPendientes = abiertas.Count;
                indicadores.CuentasPorCobrar = abiertas.Sum(f => f.SaldoPendiente);

                indicadores.CuentasPorPagar = unidad.Conjunto<OrdenCompra>()
                    .Where(o => o.Estado != EstadoOrdenCompra.Cancelada && o.Estado != EstadoOrdenCompra.Borrador)
                    .Select(o => (decimal?)o.SaldoPendiente)
                    .Sum() ?? 0m;

                indicadores.OrdenesDeCompraAbiertas = unidad.Conjunto<OrdenCompra>()
                    .Count(o => o.Estado == EstadoOrdenCompra.Emitida || o.Estado == EstadoOrdenCompra.ParcialmenteRecibida);

                indicadores.EntregasDelMes = unidad.Conjunto<RemitoEntrega>()
                    .Count(r => r.Estado == EstadoRemito.Entregado && r.FechaEntrega >= inicioMes);
            });

            return indicadores;
        }

        public List<VentaDto> VentasPorPeriodo(DateTime desde, DateTime hasta)
        {
            Autorizacion.Exigir(Permiso.ReportesGerenciales);

            Dictionary<Guid, string> clientes = Referencias.NombresClientes();
            DateTime fin = hasta.Date.AddDays(1);

            return FactoryDataAccess.PedidoRepository.GetAll()
                .Where(p => p.Estado != EstadoPedido.Cancelado && p.FechaPedido >= desde.Date && p.FechaPedido < fin)
                .OrderBy(p => p.FechaPedido)
                .Select(p => new VentaDto
                {
                    Fecha = p.FechaPedido,
                    Pedido = p.Numero,
                    Cliente = Referencias.Nombre(clientes, p.ClienteId),
                    Estado = Etiquetas.De(p.Estado),
                    Moneda = Etiquetas.De(p.Moneda),
                    Total = p.CalcularTotal(),
                    TotalEnPesos = p.CalcularTotalEnPesos()
                }).ToList();
        }

        public List<VentaClienteDto> VentasPorCliente(DateTime desde, DateTime hasta)
        {
            Autorizacion.Exigir(Permiso.ReportesGerenciales);

            Dictionary<Guid, string> clientes = Referencias.NombresClientes();
            DateTime fin = hasta.Date.AddDays(1);

            return FactoryDataAccess.PedidoRepository.GetAll()
                .Where(p => p.Estado != EstadoPedido.Cancelado && p.FechaPedido >= desde.Date && p.FechaPedido < fin)
                .GroupBy(p => p.ClienteId)
                .Select(g => new VentaClienteDto
                {
                    Cliente = Referencias.Nombre(clientes, g.Key),
                    Pedidos = g.Count(),
                    TotalEnPesos = g.Sum(p => p.CalcularTotalEnPesos())
                })
                .OrderByDescending(v => v.TotalEnPesos)
                .ToList();
        }

        public List<VentaProductoDto> VentasPorProducto(DateTime desde, DateTime hasta)
        {
            Autorizacion.Exigir(Permiso.ReportesGerenciales);

            DateTime fin = hasta.Date.AddDays(1);

            return FactoryDataAccess.PedidoRepository.GetAll()
                .Where(p => p.Estado != EstadoPedido.Cancelado && p.FechaPedido >= desde.Date && p.FechaPedido < fin)
                .SelectMany(p => p.Detalles.Select(d => new
                {
                    d.Codigo,
                    d.Descripcion,
                    d.Cantidad,
                    Importe = d.CalcularSubtotal() * (p.Moneda == Moneda.DolaresEstadounidenses ? p.TipoCambio : 1m)
                }))
                .GroupBy(x => x.Codigo)
                .Select(g => new VentaProductoDto
                {
                    Codigo = g.Key,
                    Descripcion = g.First().Descripcion,
                    Cantidad = g.Sum(x => x.Cantidad),
                    ImporteEnPesos = g.Sum(x => x.Importe)
                })
                .OrderByDescending(v => v.ImporteEnPesos)
                .ToList();
        }

        public List<StockValorizadoDto> StockValorizado()
        {
            Autorizacion.Exigir(Permiso.InventarioVer);

            return _almacen.Ejecutar(unidad =>
            {
                Dictionary<Guid, decimal> fisica = unidad.Conjunto<StockProducto>().ToList()
                    .GroupBy(s => s.ProductoId)
                    .ToDictionary(g => g.Key, g => g.Sum(s => s.CantidadFisica));

                return unidad.Conjunto<Producto>().ToList()
                    .Where(p => fisica.ContainsKey(p.Id) && fisica[p.Id] > 0m)
                    .Select(p => new StockValorizadoDto
                    {
                        Sku = p.CodigoSku,
                        Producto = p.Nombre,
                        Unidad = p.UnidadMedida,
                        Fisica = fisica[p.Id],
                        Costo = p.CostoActual,
                        Valor = Math.Round(fisica[p.Id] * p.CostoActual, 2)
                    })
                    .OrderByDescending(v => v.Valor)
                    .ToList();
            });
        }

        public List<CuentaPorCobrarDto> CuentasPorCobrar()
        {
            Autorizacion.Exigir(Permiso.ReportesGerenciales);

            Dictionary<Guid, string> clientes = Referencias.NombresClientes();

            return _almacen.Ejecutar(unidad =>
                unidad.Conjunto<Factura>().ToList()
                    .Where(f => f.SaldoPendiente > 0m && f.EstadoPago != EstadoPago.Anulada)
                    .OrderBy(f => f.FechaVencimiento)
                    .Select(f => new CuentaPorCobrarDto
                    {
                        Cliente = Referencias.Nombre(clientes, f.ClienteId),
                        Factura = f.Numero,
                        Emision = f.FechaEmision,
                        Vencimiento = f.FechaVencimiento,
                        DiasVencida = f.FechaVencimiento.HasValue && f.FechaVencimiento.Value.Date < DateTime.Today
                            ? (int)(DateTime.Today - f.FechaVencimiento.Value.Date).TotalDays
                            : 0,
                        Total = f.MontoTotal,
                        Saldo = f.SaldoPendiente
                    }).ToList());
        }

        public List<CuentaPorPagarDto> CuentasPorPagar()
        {
            Autorizacion.Exigir(Permiso.ReportesGerenciales);

            return _almacen.Ejecutar(unidad =>
            {
                Dictionary<Guid, Proveedor> proveedores = unidad.Conjunto<Proveedor>().ToList().ToDictionary(p => p.Id);

                return unidad.Conjunto<OrdenCompra>().ToList()
                    .Where(o => o.SaldoPendiente > 0m && o.Estado != EstadoOrdenCompra.Cancelada)
                    .OrderBy(o => o.FechaEmision)
                    .Select(o => new CuentaPorPagarDto
                    {
                        Proveedor = proveedores.ContainsKey(o.ProveedorId) ? proveedores[o.ProveedorId].RazonSocial : string.Empty,
                        OrdenCompra = o.Numero,
                        Fecha = o.FechaEmision,
                        Total = o.MontoTotal,
                        Saldo = o.SaldoPendiente
                    }).ToList();
            });
        }
    }
}
