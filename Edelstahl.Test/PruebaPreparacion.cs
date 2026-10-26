using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Edelstahl.BLL.DTOs;
using Edelstahl.BLL.Exceptions.Base;
using Edelstahl.BLL.Services;
using Edelstahl.DAL.EntityFramework;
using Edelstahl.DAL.Factory;
using Edelstahl.Domain.Catalogo;
using Edelstahl.Domain.Comercial;
using Edelstahl.Domain.Finanzas;
using Edelstahl.Domain.Produccion;
using Edelstahl.Domain.Security;
using Edelstahl.Services.Configuration;

namespace Edelstahl.Test
{
    /// <summary>
    /// Recorre las acciones que dejan un pedido listo para despachar: ingreso de stock,
    /// preparación del pedido y fabricación completa con materiales faltantes.
    /// </summary>
    public static class PruebaPreparacion
    {
        public static void Ejecutar(ExecutionMode modo)
        {
            Verificador.Seccion("Preparacion de pedidos en modo " + modo);

            ApplicationMode.Reset();
            ApplicationMode.Select(modo);
            FactoryDataAccess.Inicializar(modo);

            if (modo == ExecutionMode.SqlServer)
            {
                InicializadorNegocio.Inicializar();
            }

            SesionActual.Iniciar(new Usuario
            {
                NombreUsuario = "preparacion",
                NombreCompleto = "Prueba de preparacion",
                Rol = new Rol { Nombre = NombresRol.Administrador }
            });

            new CargaInicialService().AsegurarDatosBase();
            new CargaInicialService().CargarDatosDeEjemplo(null);

            CatalogoService catalogo = new CatalogoService();
            InventarioService inventario = new InventarioService();
            PresupuestoService presupuestos = new PresupuestoService();
            PreparacionService preparacion = new PreparacionService();
            LogisticaService logistica = new LogisticaService();
            Cliente cliente = new ClienteService().ObtenerTodos().First();

            // 1. Una línea con tipo inválido se rechaza.
            Producto estandar = catalogo.ObtenerPorSku("AECT-25");
            PresupuestoSolicitud invalida = Solicitud(cliente, 0m, new List<LineaPresupuestoSolicitud>
            {
                Linea(estandar, (TipoItemPresupuesto)0, 1m)
            });

            Verificador.Falla<ReglaDeNegocioException>(() => presupuestos.CrearPresupuesto(invalida), "Un tipo de linea invalido se rechaza");

            // 2. Producto sin stock: el pedido no se confirma hasta ingresar la mercadería.
            Presupuesto sinStock = presupuestos.CrearPresupuesto(Solicitud(cliente, 30m, new List<LineaPresupuestoSolicitud>
            {
                Linea(estandar, TipoItemPresupuesto.Producto, 2m)
            }));
            presupuestos.CambiarEstado(sinStock.Id, EstadoPresupuesto.Aceptado);
            decimal senaStock = presupuestos.ObtenerResumenes(cliente.Id, null).First(p => p.Id == sinStock.Id).Anticipo;

            Verificador.Falla<ReglaDeNegocioException>(
                () => new PedidoService().ConfirmarDesdePresupuesto(cliente.Id, sinStock.Id, senaStock, "Efectivo", "REC-1", DateTime.Today),
                "Sin stock el pedido no se confirma");

            preparacion.IngresarStock(estandar.Id, 5m, "Prueba de ingreso");

            Verificador.Falla<ReglaDeNegocioException>(
                () => new PedidoService().ConfirmarDesdePresupuesto(cliente.Id, sinStock.Id, 0m, "Efectivo", "REC-1", DateTime.Today),
                "Un presupuesto no se acepta como pedido sin la sena");
            Verificador.Falla<ReglaDeNegocioException>(
                () => presupuestos.CrearPresupuesto(Solicitud(cliente, 0m, new List<LineaPresupuestoSolicitud> { Linea(estandar, TipoItemPresupuesto.FabricacionEspecial, 1m) })),
                "Un producto a fabricar no se presupuesta sin la sena minima");

            Pedido pedido = new PedidoService().ConfirmarDesdePresupuesto(cliente.Id, sinStock.Id, senaStock, "Efectivo", "REC-1", DateTime.Today);
            Factura facturaPedido = new FacturacionService().EmitirFactura(pedido.Id, TipoComprobante.FacturaA);
            PedidoDespachoDto fila = logistica.ObtenerPedidosParaDespacho().First(p => p.PedidoId == pedido.Id);
            Verificador.Verificar(!fila.Habilitado && fila.Motivo.Contains("100%"), "Facturado pero sin cobrar el pedido no se puede remitir: " + fila.Motivo);

            Cliente deudor = new ClienteService().ObtenerPorId(cliente.Id);
            deudor.AutorizadoACredito = true;
            new ClienteService().Modificar(deudor);
            fila = logistica.ObtenerPedidosParaDespacho().First(p => p.PedidoId == pedido.Id);
            Verificador.Verificar(fila.Habilitado, "Un cliente autorizado puede recibir sin haber pagado el 100%: " + fila.Motivo);
            deudor.AutorizadoACredito = false;
            new ClienteService().Modificar(deudor);

            Pagar(facturaPedido);
            fila = logistica.ObtenerPedidosParaDespacho().First(p => p.PedidoId == pedido.Id);
            Verificador.Verificar(fila.Habilitado && fila.Facturado && !string.IsNullOrEmpty(fila.Factura),
                "Con el stock ingresado y facturado el pedido queda habilitado (factura " + fila.Factura + ")");

            // 3. Si la reserva se pierde, Preparar pedido la recupera (e ingresa lo que falte).
            foreach (ReservaDto reserva in inventario.ObtenerReservas(true).Where(r => r.Sku == estandar.CodigoSku))
            {
                inventario.LiberarReserva(reserva.Id, "Prueba de liberacion");
            }

            inventario.RegistrarMovimiento(new MovimientoSolicitud
            {
                ProductoId = estandar.Id,
                UbicacionId = inventario.ObtenerUbicaciones(true).First().Id,
                Tipo = TipoMovimientoStock.AjusteNegativo,
                Cantidad = inventario.ConsultarStock(estandar.CodigoSku, null, false).Sum(s => s.Disponible),
                Documento = "PRUEBA",
                Observaciones = "Vaciar para la prueba"
            });

            fila = logistica.ObtenerPedidosParaDespacho().First(p => p.PedidoId == pedido.Id);
            Verificador.Verificar(!fila.Habilitado && fila.Motivo.Contains(estandar.CodigoSku), "Sin reserva el motivo nombra el producto: " + fila.Motivo);

            ResultadoPreparacion sinIngresar = preparacion.PrepararPedido(pedido.Id, false);
            Verificador.Verificar(sinIngresar.Faltantes.Count == 1 && sinIngresar.Reservados.Count == 0, "Preparar sin ingresar informa lo que falta");

            ResultadoPreparacion conIngreso = preparacion.PrepararPedido(pedido.Id, true);
            Verificador.Verificar(conIngreso.Ingresados.Count == 1 && conIngreso.Reservados.Count == 1, "Preparar con ingreso agrega el stock y reserva");
            fila = logistica.ObtenerPedidosParaDespacho().First(p => p.PedidoId == pedido.Id);
            Verificador.Verificar(fila.Habilitado, "Despues de preparar el pedido queda habilitado");

            // 4. Fabricacion especial: materiales faltantes y ciclo completo.
            Producto filtro = catalogo.ObtenerPorSku("FILTRO-10");
            Producto material = catalogo.ObtenerProducto(catalogo.BuscarProductos(string.Empty, null, TipoProducto.MateriaPrima, true, false).First().Id);
            Presupuesto fabricacion = presupuestos.CrearPresupuesto(Solicitud(cliente, 30m, new List<LineaPresupuestoSolicitud>
            {
                Linea(filtro, TipoItemPresupuesto.FabricacionEspecial, 1m)
            }));
            presupuestos.CambiarEstado(fabricacion.Id, EstadoPresupuesto.Aceptado);
            decimal sena = presupuestos.ObtenerResumenes(cliente.Id, null).First(p => p.Id == fabricacion.Id).Anticipo;
            Pedido pedidoFab = new PedidoService().ConfirmarDesdePresupuesto(cliente.Id, fabricacion.Id, sena, "Efectivo", "REC-2", DateTime.Today);
            DetallePedido linea = pedidoFab.Detalles.First();

            ResultadoGeneracionOrden orden = new ProduccionService().GenerarOrden(pedidoFab.Id, linea.Id, PrioridadOrden.Normal, null,
                new List<MaterialRequerido> { new MaterialRequerido { ProductoId = material.Id, Cantidad = 7m } }, "Plano de prueba", string.Empty);

            List<FaltanteMaterialDto> restantes = preparacion.CompletarMateriales(orden.Orden.Id, true);
            Verificador.Verificar(restantes.Count == 0, "Los materiales faltantes se ingresan y se reservan");

            Verificador.Falla<ReglaDeNegocioException>(
                () => preparacion.FabricarCompleto(orden.Orden.Id), "Sin la factura de la sena no se manda a producir");
            Factura facturaFab = new FacturacionService().EmitirFactura(pedidoFab.Id, TipoComprobante.FacturaA);
            Verificador.Verificar(facturaFab.EstadoPago == EstadoPago.Parcial, "La factura asienta la sena abonada");

            preparacion.FabricarCompleto(orden.Orden.Id);
            OrdenTableroDto tablero = new ProduccionService().ObtenerTablero(true).First(o => o.Id == orden.Orden.Id);
            Verificador.Verificar(tablero.EstadoClave == EstadoProduccion.Finalizada, "La fabricacion completa deja la orden finalizada (" + tablero.Estado + ")");

            Pagar(facturaFab);
            fila = logistica.ObtenerPedidosParaDespacho().First(p => p.PedidoId == pedidoFab.Id);
            Verificador.Verificar(fila.Habilitado, "Fabricado y facturado el pedido queda listo para despachar");

            // 5. Un producto a medida no se fabrica sin la sena minima.
            Presupuesto sinSena = presupuestos.CrearPresupuesto(Solicitud(cliente, 30m, new List<LineaPresupuestoSolicitud>
            {
                Linea(filtro, TipoItemPresupuesto.FabricacionEspecial, 1m)
            }));
            presupuestos.CambiarEstado(sinSena.Id, EstadoPresupuesto.Aceptado);
            decimal senaOk = presupuestos.ObtenerResumenes(cliente.Id, null).First(p => p.Id == sinSena.Id).Anticipo;
            Pedido pedidoSinSena = new PedidoService().ConfirmarDesdePresupuesto(cliente.Id, sinSena.Id, senaOk, "Efectivo", "REC-3", DateTime.Today);
            Pedido sinCobrar = FactoryDataAccess.PedidoRepository.GetById(pedidoSinSena.Id);
            sinCobrar.ImporteAnticipo = 0m;
            FactoryDataAccess.PedidoRepository.Update(sinCobrar);

            Verificador.Falla<ReglaDeNegocioException>(
                () => new ProduccionService().GenerarOrden(pedidoSinSena.Id, pedidoSinSena.Detalles.First().Id, PrioridadOrden.Normal, null,
                    new List<MaterialRequerido> { new MaterialRequerido { ProductoId = material.Id, Cantidad = 1m } }, string.Empty, string.Empty),
                "No se crea la orden de fabricacion si no se abono la sena");

            sinCobrar.ImporteAnticipo = senaOk;
            FactoryDataAccess.PedidoRepository.Update(sinCobrar);
            ResultadoGeneracionOrden ordenRechazo = new ProduccionService().GenerarOrden(pedidoSinSena.Id, pedidoSinSena.Detalles.First().Id, PrioridadOrden.Normal, null,
                new List<MaterialRequerido> { new MaterialRequerido { ProductoId = material.Id, Cantidad = 1m } }, string.Empty, string.Empty);

            sinCobrar.ImporteAnticipo = 0m;
            FactoryDataAccess.PedidoRepository.Update(sinCobrar);
            Verificador.Falla<ReglaDeNegocioException>(
                () => new ProduccionService().IniciarProduccion(ordenRechazo.Orden.Id), "Tampoco se inicia la fabricacion sin la sena");

            // 6. Un rechazo en calidad cierra la orden: no queda bloqueada ni hay que cancelarla aparte.
            sinCobrar.ImporteAnticipo = senaOk;
            FactoryDataAccess.PedidoRepository.Update(sinCobrar);
            preparacion.CompletarMateriales(ordenRechazo.Orden.Id, true);
            ProduccionService produccionRechazo = new ProduccionService();
            Verificador.Falla<ReglaDeNegocioException>(
                () => produccionRechazo.IniciarProduccion(ordenRechazo.Orden.Id), "Con la sena pero sin factura tampoco se manda a producir");
            new FacturacionService().EmitirFactura(pedidoSinSena.Id, TipoComprobante.FacturaA);
            produccionRechazo.IniciarProduccion(ordenRechazo.Orden.Id);
            produccionRechazo.RegistrarAvance(ordenRechazo.Orden.Id, "Fabricacion", 100m, string.Empty);
            produccionRechazo.EnviarACalidad(ordenRechazo.Orden.Id);
            produccionRechazo.RegistrarControlCalidad(ordenRechazo.Orden.Id, ResultadoCalidad.Rechazado, "Mal el corte");
            Verificador.Igual(EstadoProduccion.Cancelada, produccionRechazo.ObtenerOrden(ordenRechazo.Orden.Id).Estado, "El rechazo en calidad cierra la orden");
            Verificador.Falla<ReglaDeNegocioException>(
                () => produccionRechazo.CancelarOrden(ordenRechazo.Orden.Id, "otra vez"), "No se pide cancelar de nuevo una orden rechazada");

            // 7. Los productos de stock no exigen sena, pero nada se remite sin cobrar el 100%.
            preparacion.IngresarStock(estandar.Id, 5m, "Stock para la prueba");
            Presupuesto soloStock = presupuestos.CrearPresupuesto(Solicitud(cliente, 0m, new List<LineaPresupuestoSolicitud>
            {
                Linea(estandar, TipoItemPresupuesto.Producto, 1m)
            }));
            presupuestos.CambiarEstado(soloStock.Id, EstadoPresupuesto.Aceptado);
            Pedido pedidoStock = new PedidoService().ConfirmarDesdePresupuesto(cliente.Id, soloStock.Id, 0m, "Efectivo", "REC-9", DateTime.Today);
            Verificador.Verificar(pedidoStock != null, "Un pedido solo de productos de stock se confirma sin sena");

            Factura facturaStock = new FacturacionService().EmitirFactura(pedidoStock.Id, TipoComprobante.FacturaA);
            PedidoDespachoDto filaStock = logistica.ObtenerPedidosParaDespacho().First(p => p.PedidoId == pedidoStock.Id);
            Verificador.Verificar(!filaStock.Habilitado && filaStock.Motivo.Contains("100%"),
                "Sin abonar el 100% no se genera el remito: " + filaStock.Motivo);

            Pagar(facturaStock);
            filaStock = logistica.ObtenerPedidosParaDespacho().First(p => p.PedidoId == pedidoStock.Id);
            Verificador.Verificar(filaStock.Habilitado, "Abonado el 100% el pedido queda habilitado para el remito");
        }

        private static PresupuestoSolicitud Solicitud(Cliente cliente, decimal sena, List<LineaPresupuestoSolicitud> lineas)
        {
            return new PresupuestoSolicitud
            {
                ClienteId = cliente.Id,
                Moneda = Moneda.PesosArgentinos,
                TipoCambio = 1m,
                PorcentajeIva = 21m,
                PorcentajeAnticipo = sena,
                VigenciaDias = 7,
                CondicionPago = "Contado",
                PlazoEntrega = "7 dias",
                Enviar = true,
                Lineas = lineas
            };
        }

        private static LineaPresupuestoSolicitud Linea(Producto producto, TipoItemPresupuesto tipo, decimal cantidad)
        {
            return new LineaPresupuestoSolicitud
            {
                ProductoId = producto.Id,
                Codigo = producto.CodigoSku,
                Descripcion = producto.Nombre,
                DescripcionTecnica = tipo == TipoItemPresupuesto.FabricacionEspecial ? "Especificaciones tecnicas de prueba" : string.Empty,
                Tipo = tipo,
                Cantidad = cantidad,
                PrecioUnitario = producto.PrecioBase > 0m ? producto.PrecioBase : 100m
            };
        }

        private static void Pagar(Factura factura)
        {
            FinanzasService finanzas = new FinanzasService();
            MedioPago efectivo = finanzas.ObtenerMediosPago(true).First(m => !m.RequiereReferencia);
            finanzas.RegistrarCobro(factura.ClienteId, factura.Id, efectivo.Id, factura.SaldoPendiente, string.Empty, string.Empty, "Prueba");
        }
    }
}
