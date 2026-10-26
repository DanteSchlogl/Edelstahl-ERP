using System;
using System.Collections.Generic;
using System.Linq;
using Edelstahl.BLL.DTOs;
using Edelstahl.BLL.Infrastructure;
using Edelstahl.DAL.Factory;
using Edelstahl.DAL.Store;
using Edelstahl.Domain.Catalogo;
using Edelstahl.Domain.Comercial;
using Edelstahl.Domain.Logistica;
using Edelstahl.Domain.Produccion;
using Edelstahl.Domain.Security;

namespace Edelstahl.BLL.Services
{
    /// <summary>
    /// Deja un pedido en condiciones de despacharse: reserva el stock que falta,
    /// ingresa la mercadería faltante si se pide y completa los materiales de
    /// una orden de fabricación.
    /// </summary>
    public class PreparacionService
    {
        private readonly INegocioStore _almacen;

        public PreparacionService()
            : this(FactoryDataAccess.NegocioStore)
        {
        }

        public PreparacionService(INegocioStore almacen)
        {
            _almacen = almacen ?? throw new ArgumentNullException(nameof(almacen));
        }

        /// <summary>
        /// Ingresa existencias de un producto en su ubicación habitual como ajuste positivo.
        /// </summary>
        public void IngresarStock(Guid productoId, decimal cantidad, string motivo)
        {
            Autorizacion.Exigir(Permiso.InventarioMover);

            if (cantidad <= 0m)
            {
                throw Mensajes.Regla("The quantity must be greater than zero.");
            }

            if (string.IsNullOrWhiteSpace(motivo))
            {
                throw Mensajes.Regla("An adjustment must state the reason.");
            }

            string sku = string.Empty;

            _almacen.Ejecutar(unidad =>
            {
                Producto producto = InventarioMotor.ObtenerProducto(unidad, productoId);
                UbicacionDeposito ubicacion = InventarioMotor.UbicacionParaProducto(unidad, producto);
                sku = producto.CodigoSku;

                InventarioMotor.Mover(unidad, producto.Id, ubicacion.Id, TipoMovimientoStock.AjustePositivo,
                    cantidad, producto.CostoActual, motivo, null, null, null, motivo, false);
            });

            new BitacoraService().RegistrarAuditoria("Inventario", "Ingreso de stock", "Producto", productoId,
                "Ingreso de " + cantidad.ToString("0.###") + " de " + sku + ": " + motivo);
        }

        /// <summary>
        /// Reserva para el pedido lo que le falta. Si no hay stock suficiente y se indica,
        /// ingresa primero la cantidad faltante. Las líneas de fabricación especial no
        /// se tocan: se resuelven en Producción.
        /// </summary>
        public ResultadoPreparacion PrepararPedido(Guid pedidoId, bool ingresarFaltante)
        {
            Autorizacion.Exigir(Permiso.LogisticaGestionar);

            if (ingresarFaltante)
            {
                Autorizacion.Exigir(Permiso.InventarioMover);
            }

            Pedido pedido = FactoryDataAccess.PedidoRepository.GetById(pedidoId);

            if (pedido == null)
            {
                throw Mensajes.Regla("The order does not exist.");
            }

            if (pedido.Estado == EstadoPedido.Cancelado || pedido.Estado == EstadoPedido.Entregado)
            {
                throw Mensajes.Regla("The order is not in a state that allows manufacturing.");
            }

            ResultadoPreparacion resultado = new ResultadoPreparacion();

            _almacen.Ejecutar(unidad =>
            {
                Dictionary<Guid, decimal> remitidas = unidad.Conjunto<RemitoEntrega>().Incluir("Detalles").ToList()
                    .Where(r => r.PedidoId == pedidoId && r.Estado != EstadoRemito.Cancelado)
                    .SelectMany(r => r.Detalles)
                    .GroupBy(d => d.DetallePedidoId)
                    .ToDictionary(g => g.Key, g => g.Sum(d => d.Cantidad));

                foreach (DetallePedido linea in pedido.Detalles)
                {
                    if (linea.TipoItem == TipoItemPresupuesto.Servicio)
                    {
                        continue;
                    }

                    decimal remitida;
                    remitidas.TryGetValue(linea.Id, out remitida);
                    decimal falta = linea.Cantidad - remitida;

                    if (falta <= 0m)
                    {
                        continue;
                    }

                    decimal reservado = InventarioMotor.ReservasActivasDeDetalle(unidad, linea.Id).Sum(r => r.CantidadPendiente);

                    if (reservado >= falta)
                    {
                        continue;
                    }

                    if (linea.TipoItem == TipoItemPresupuesto.FabricacionEspecial)
                    {
                        resultado.EnProduccion.Add(linea.Codigo);
                        continue;
                    }

                    // Producto de catálogo (también las líneas antiguas sin tipo definido).
                    string sku = linea.Codigo;
                    Producto producto = unidad.Conjunto<Producto>().FirstOrDefault(p => p.CodigoSku == sku);

                    if (producto == null)
                    {
                        throw Mensajes.Regla("The product {0} of the order is not in the catalog.", linea.Codigo);
                    }

                    decimal necesita = falta - reservado;
                    decimal disponible = InventarioMotor.DisponibleTotal(unidad, producto.Id);

                    if (disponible < necesita)
                    {
                        if (!ingresarFaltante)
                        {
                            resultado.Faltantes.Add(linea.Codigo + " (" + (necesita - disponible).ToString("0.###") + ")");
                            continue;
                        }

                        decimal ingreso = necesita - disponible;
                        UbicacionDeposito ubicacion = InventarioMotor.UbicacionParaProducto(unidad, producto);

                        InventarioMotor.Mover(unidad, producto.Id, ubicacion.Id, TipoMovimientoStock.AjustePositivo,
                            ingreso, producto.CostoActual, pedido.Numero, null, null, null,
                            "Ingreso para completar el pedido " + pedido.Numero, false);

                        resultado.Ingresados.Add(linea.Codigo + " (" + ingreso.ToString("0.###") + ")");
                    }

                    InventarioMotor.Reservar(unidad, producto.Id, necesita, linea.Id, null, pedido.Numero);
                    resultado.Reservados.Add(linea.Codigo);
                }
            });

            new BitacoraService().RegistrarAuditoria("Logística", "Preparar pedido", "Pedido", pedidoId,
                "Pedido " + pedido.Numero + ": reservados " + resultado.Reservados.Count + ", ingresados "
                + resultado.Ingresados.Count + ", en producción " + resultado.EnProduccion.Count);

            return resultado;
        }

        /// <summary>
        /// Reserva los materiales de una orden de fabricación e ingresa los que falten.
        /// Devuelve los materiales que todavía no se pudieron cubrir (vacío si está todo).
        /// </summary>
        public List<FaltanteMaterialDto> CompletarMateriales(Guid ordenId, bool ingresarFaltante)
        {
            Autorizacion.Exigir(Permiso.ProduccionGestionar);

            ProduccionService produccion = new ProduccionService(_almacen);
            List<FaltanteMaterialDto> faltantes = produccion.ReservarMateriales(ordenId);

            if (faltantes.Count == 0 || !ingresarFaltante)
            {
                return faltantes;
            }

            foreach (FaltanteMaterialDto faltante in faltantes)
            {
                IngresarStock(faltante.ProductoId, faltante.Faltante, "Ingreso de materiales para la orden de fabricación");
            }

            return produccion.ReservarMateriales(ordenId);
        }

        /// <summary>
        /// Recorre todo el ciclo de una orden de fabricación: materiales, inicio, avance al 100%,
        /// consumo, control de calidad aprobado y entrada del producto terminado.
        /// </summary>
        public void FabricarCompleto(Guid ordenId)
        {
            Autorizacion.Exigir(Permiso.ProduccionGestionar);
            Autorizacion.Exigir(Permiso.CalidadRegistrar);

            ProduccionService produccion = new ProduccionService(_almacen);

            if (CompletarMateriales(ordenId, true).Count > 0)
            {
                throw Mensajes.Regla("Manufacturing cannot start without all the materials reserved.");
            }

            produccion.IniciarProduccion(ordenId);
            produccion.RegistrarAvance(ordenId, Mensajes.T("Manufacturing"), 100m, string.Empty);

            foreach (MaterialOrdenDto material in produccion.ObtenerMateriales(ordenId).Where(m => m.PorConsumir > 0m))
            {
                produccion.RegistrarConsumo(ordenId, material.Id, material.PorConsumir, 0m, string.Empty);
            }

            produccion.EnviarACalidad(ordenId);
            produccion.RegistrarControlCalidad(ordenId, ResultadoCalidad.Aprobado, Mensajes.T("Approved in quick manufacturing."));
        }
    }
}
