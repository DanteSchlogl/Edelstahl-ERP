using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Edelstahl.BLL.DTOs;
using Edelstahl.BLL.Exceptions.Base;
using Edelstahl.BLL.Infrastructure;
using Edelstahl.DAL.Factory;
using Edelstahl.Domain.Catalogo;
using Edelstahl.Domain.Security;
using Edelstahl.DAL.Interfaces;
using Edelstahl.Domain.Comercial;

namespace Edelstahl.BLL.Services
{
    /// <summary>
    /// Coordina las operaciones relacionadas con presupuestos.
    /// </summary>
    public class PresupuestoService
    {
        private readonly IPresupuestoRepository _presupuestoRepository;

        public PresupuestoService()
        {
            _presupuestoRepository =
                FactoryDataAccess.PresupuestoRepository;
        }

        public PresupuestoService(
            IPresupuestoRepository presupuestoRepository)
        {
            _presupuestoRepository =
                presupuestoRepository
                ?? throw new ArgumentNullException(
                    nameof(presupuestoRepository));
        }

        public Presupuesto Registrar(Presupuesto presupuesto)
        {
            ValidarPresupuesto(presupuesto);

            presupuesto.Numero =
                presupuesto.Numero.Trim().ToUpperInvariant();

            if (_presupuestoRepository.ExistsByNumero(
                presupuesto.Numero))
            {
                throw Mensajes.Regla("There is already a quotation {0}.", presupuesto.Numero);
            }

            foreach (DetallePresupuesto detalle
                in presupuesto.Detalles)
            {
                detalle.PresupuestoId = presupuesto.Id;
            }

            _presupuestoRepository.Add(presupuesto);

            return presupuesto;
        }

        public Presupuesto ObtenerPorId(Guid id)
        {
            if (id == Guid.Empty)
            {
                throw Mensajes.Regla("The quotation identifier is not valid.");
            }

            return _presupuestoRepository.GetById(id);
        }

        public Presupuesto ObtenerPorNumero(string numero)
        {
            if (string.IsNullOrWhiteSpace(numero))
            {
                return null;
            }

            return _presupuestoRepository.GetByNumero(
                numero.Trim().ToUpperInvariant());
        }

        public List<Presupuesto> ObtenerPorCliente(
            Guid clienteId)
        {
            if (clienteId == Guid.Empty)
            {
                throw Mensajes.Regla("The customer identifier is not valid.");
            }

            return _presupuestoRepository
                .GetByClienteId(clienteId)
                .OrderByDescending(
                    presupuesto => presupuesto.FechaEmision)
                .ToList();
        }

        public List<Presupuesto> BuscarPorCliente(
            Guid clienteId,
            string filtro)
        {
            List<Presupuesto> presupuestos =
                ObtenerPorCliente(clienteId);

            if (string.IsNullOrWhiteSpace(filtro))
            {
                return presupuestos;
            }

            string filtroNormalizado =
                filtro.Trim().ToLowerInvariant();

            return presupuestos
                .Where(presupuesto =>
                    (presupuesto.Numero ?? string.Empty)
                        .ToLowerInvariant()
                        .Contains(filtroNormalizado)
                    ||
                    presupuesto.Estado
                        .ToString()
                        .ToLowerInvariant()
                        .Contains(filtroNormalizado))
                .ToList();
        }

        /// <summary>Seña mínima que exige el pedido de productos a medida.</summary>
        public const decimal SenaMinimaFabricacion = Presupuesto.AnticipoMinimoPorcentaje;

        /// <summary>Descuento que puede aplicar un vendedor sin autorización.</summary>
        public const decimal DescuentoMaximoSinAutorizacion = 10m;

        /// <summary>
        /// Genera un presupuesto a partir de líneas del catálogo o de servicios,
        /// aplicando las reglas comerciales (vigencia, descuentos y seña mínima).
        /// </summary>
        public Presupuesto CrearPresupuesto(PresupuestoSolicitud solicitud)
        {
            Autorizacion.Exigir(Permiso.PresupuestosGestionar);

            if (solicitud == null)
            {
                throw new ArgumentNullException(nameof(solicitud));
            }

            if (solicitud.Lineas == null || solicitud.Lineas.Count == 0)
            {
                throw Mensajes.Regla("The quotation must contain at least one item.");
            }

            if (solicitud.Lineas.Any(l => l.PrecioUnitario <= 0m))
            {
                throw Mensajes.Regla("Every item needs a unit price greater than zero.");
            }

            if (solicitud.VigenciaDias < 1 || solicitud.VigenciaDias > 90)
            {
                throw Mensajes.Regla("The validity must be between 1 and 90 days.");
            }

            if (solicitud.Moneda == Moneda.DolaresEstadounidenses && solicitud.TipoCambio <= 0m)
            {
                throw Mensajes.Regla("The exchange rate is not valid.");
            }

            decimal[] porcentajes =
            {
                solicitud.PorcentajeIva, solicitud.PorcentajeRecargo,
                solicitud.PorcentajeDescuentoGeneral, solicitud.PorcentajeAnticipo
            };

            if (porcentajes.Any(p => p < 0m || p > 100m))
            {
                throw Mensajes.Regla("The percentages must be between 0 and 100.");
            }

            Cliente cliente = FactoryDataAccess.ClienteRepository.GetById(solicitud.ClienteId);

            if (cliente == null)
            {
                throw Mensajes.Regla("The customer does not exist.");
            }

            if (!cliente.Activo)
            {
                throw Mensajes.Regla("The customer is not active.");
            }

            decimal descuentoMayor = Math.Max(
                solicitud.PorcentajeDescuentoGeneral,
                solicitud.Lineas.Max(l => l.Descuento));

            if (descuentoMayor > DescuentoMaximoSinAutorizacion && !Autorizacion.Tiene(Permiso.UsuariosAdministrar))
            {
                throw new AccesoNoAutorizadoException(
                    Mensajes.T("Discounts above {0}% require the approval of an administrator.",
                        DescuentoMaximoSinAutorizacion.ToString("0")));
            }

            CatalogoService catalogo = new CatalogoService();
            List<DetallePresupuesto> detalles = new List<DetallePresupuesto>();

            foreach (LineaPresupuestoSolicitud linea in solicitud.Lineas)
            {
                if (!Enum.IsDefined(typeof(TipoItemPresupuesto), linea.Tipo))
                {
                    throw Mensajes.Regla("The item type is not valid.");
                }

                if (linea.Cantidad <= 0m)
                {
                    throw Mensajes.Regla("The quantity must be greater than zero.");
                }

                if (linea.PrecioUnitario < 0m || linea.Descuento < 0m || linea.Descuento > 100m)
                {
                    throw Mensajes.Regla("The price or the discount is not valid.");
                }

                DetallePresupuesto detalle = new DetallePresupuesto
                {
                    TipoItem = linea.Tipo,
                    Cantidad = linea.Cantidad,
                    PrecioUnitario = linea.PrecioUnitario,
                    PorcentajeDescuento = linea.Descuento,
                    DescripcionTecnica = linea.DescripcionTecnica ?? string.Empty
                };

                if (linea.Tipo == TipoItemPresupuesto.Servicio)
                {
                    if (string.IsNullOrWhiteSpace(linea.Codigo) || string.IsNullOrWhiteSpace(linea.Descripcion))
                    {
                        throw Mensajes.Regla("A service line needs a code and a description.");
                    }

                    detalle.Codigo = linea.Codigo.Trim();
                    detalle.Descripcion = linea.Descripcion.Trim();
                }
                else
                {
                    if (!linea.ProductoId.HasValue)
                    {
                        throw Mensajes.Regla("Select a product for every product line.");
                    }

                    Producto producto = catalogo.ObtenerProducto(linea.ProductoId.Value);

                    if (producto == null || !producto.Activo || !producto.EsVendible)
                    {
                        throw Mensajes.Regla("Only active products that can be sold may be quoted.");
                    }

                    if (linea.Tipo == TipoItemPresupuesto.FabricacionEspecial)
                    {
                        if (!producto.EsFabricable)
                        {
                            throw Mensajes.Regla("The product {0} cannot be manufactured.", producto.CodigoSku);
                        }

                        if (string.IsNullOrWhiteSpace(linea.DescripcionTecnica))
                        {
                            throw Mensajes.Regla("Custom manufactured items need complete technical specifications.");
                        }
                    }

                    detalle.Codigo = producto.CodigoSku;
                    detalle.Descripcion = string.IsNullOrWhiteSpace(linea.Descripcion)
                        ? producto.Nombre
                        : linea.Descripcion.Trim();
                }

                detalles.Add(detalle);
            }

            if (detalles.Any(d => d.TipoItem == TipoItemPresupuesto.FabricacionEspecial)
                && solicitud.PorcentajeAnticipo < SenaMinimaFabricacion)
            {
                throw Mensajes.Regla(
                    "Custom manufactured items require a deposit of at least {0}%.",
                    SenaMinimaFabricacion.ToString("0"));
            }

            List<string> existentes = _presupuestoRepository.GetAll().Select(p => p.Numero).ToList();

            Presupuesto presupuesto = new Presupuesto
            {
                Numero = Numeracion.Siguiente("PRE", existentes),
                ClienteId = cliente.Id,
                FechaEmision = DateTime.Now,
                FechaVencimiento = DateTime.Today.AddDays(solicitud.VigenciaDias),
                Moneda = solicitud.Moneda,
                TipoCambio = solicitud.Moneda == Moneda.PesosArgentinos ? 1m : solicitud.TipoCambio,
                PorcentajeIVA = solicitud.PorcentajeIva,
                PorcentajeRecargo = solicitud.PorcentajeRecargo,
                PorcentajeDescuentoGeneral = solicitud.PorcentajeDescuentoGeneral,
                PorcentajeAnticipo = solicitud.PorcentajeAnticipo,
                CondicionPago = solicitud.CondicionPago ?? string.Empty,
                PlazoEntrega = solicitud.PlazoEntrega ?? string.Empty,
                EntregaIncluida = solicitud.EntregaIncluida,
                Observaciones = solicitud.Observaciones ?? string.Empty,
                Estado = solicitud.Enviar ? EstadoPresupuesto.Enviado : EstadoPresupuesto.Borrador,
                Detalles = detalles
            };

            Presupuesto registrado = Registrar(presupuesto);

            new BitacoraService().RegistrarAuditoria(
                "Ventas", "CrearPresupuesto", "Presupuesto", registrado.Id,
                registrado.Numero + " para " + cliente.RazonSocial);

            return registrado;
        }

        public List<PresupuestoResumenDto> ObtenerResumenes(Guid? clienteId, EstadoPresupuesto? estado)
        {
            ActualizarVencidos();

            Dictionary<Guid, string> clientes = Referencias.NombresClientes();

            return _presupuestoRepository.GetAll()
                .Where(p => (!clienteId.HasValue || p.ClienteId == clienteId.Value)
                    && (!estado.HasValue || p.Estado == estado.Value))
                .OrderByDescending(p => p.FechaEmision)
                .Select(p => new PresupuestoResumenDto
                {
                    Id = p.Id,
                    Numero = p.Numero,
                    ClienteId = p.ClienteId,
                    Cliente = Referencias.Nombre(clientes, p.ClienteId),
                    Emision = p.FechaEmision,
                    Vencimiento = p.FechaVencimiento,
                    Moneda = Etiquetas.De(p.Moneda),
                    Total = p.CalcularTotal(),
                    Anticipo = p.CalcularAnticipo(),
                    Estado = Etiquetas.De(p.Estado),
                    EstadoClave = p.Estado,
                    Items = p.Detalles.Count
                }).ToList();
        }

        /// <summary>
        /// Marca como vencidos los presupuestos abiertos cuya vigencia terminó.
        /// </summary>
        public int ActualizarVencidos()
        {
            int cantidad = 0;

            foreach (Presupuesto presupuesto in _presupuestoRepository.GetAll())
            {
                bool abierto = presupuesto.Estado == EstadoPresupuesto.Borrador
                    || presupuesto.Estado == EstadoPresupuesto.Enviado
                    || presupuesto.Estado == EstadoPresupuesto.Aceptado;

                if (abierto && presupuesto.FechaVencimiento.Date < DateTime.Today)
                {
                    presupuesto.Estado = EstadoPresupuesto.Vencido;
                    _presupuestoRepository.Update(presupuesto);
                    cantidad++;
                }
            }

            return cantidad;
        }

        public void CambiarEstado(Guid presupuestoId, EstadoPresupuesto nuevoEstado)
        {
            Autorizacion.Exigir(Permiso.PresupuestosGestionar);

            ActualizarVencidos();

            Presupuesto presupuesto = _presupuestoRepository.GetById(presupuestoId);

            if (presupuesto == null)
            {
                throw Mensajes.Regla("The quotation does not exist.");
            }

            bool permitido =
                (presupuesto.Estado == EstadoPresupuesto.Borrador
                    && (nuevoEstado == EstadoPresupuesto.Enviado || nuevoEstado == EstadoPresupuesto.Rechazado))
                || (presupuesto.Estado == EstadoPresupuesto.Enviado
                    && (nuevoEstado == EstadoPresupuesto.Aceptado || nuevoEstado == EstadoPresupuesto.Rechazado))
                // Aceptado todavia no es pedido: el cliente puede arrepentirse o puede haber un error de carga.
                || (presupuesto.Estado == EstadoPresupuesto.Aceptado && nuevoEstado == EstadoPresupuesto.Rechazado);

            if (!permitido)
            {
                throw Mensajes.Regla(
                    "A quotation that is {0} cannot change to {1}.",
                    Etiquetas.De(presupuesto.Estado),
                    Etiquetas.De(nuevoEstado));
            }

            presupuesto.Estado = nuevoEstado;
            _presupuestoRepository.Update(presupuesto);

            new BitacoraService().RegistrarAuditoria(
                "Ventas", "CambiarEstadoPresupuesto", "Presupuesto", presupuesto.Id,
                presupuesto.Numero + " -> " + nuevoEstado);
        }

        /// <summary>
        /// Renueva la vigencia de un presupuesto vencido o abierto para poder convertirlo.
        /// </summary>
        public void ExtenderVigencia(Guid presupuestoId, int dias)
        {
            Autorizacion.Exigir(Permiso.PresupuestosGestionar);

            if (dias < 1 || dias > 90)
            {
                throw Mensajes.Regla("The validity must be between 1 and 90 days.");
            }

            Presupuesto presupuesto = _presupuestoRepository.GetById(presupuestoId);

            if (presupuesto == null)
            {
                throw Mensajes.Regla("The quotation does not exist.");
            }

            if (presupuesto.Estado != EstadoPresupuesto.Vencido && presupuesto.Estado != EstadoPresupuesto.Borrador
                && presupuesto.Estado != EstadoPresupuesto.Enviado)
            {
                throw Mensajes.Regla("The validity of this quotation cannot be extended.");
            }

            presupuesto.FechaVencimiento = DateTime.Today.AddDays(dias);
            presupuesto.Estado = EstadoPresupuesto.Enviado;
            _presupuestoRepository.Update(presupuesto);

            new BitacoraService().RegistrarAuditoria(
                "Ventas", "ExtenderVigencia", "Presupuesto", presupuesto.Id, presupuesto.Numero);
        }

        public List<DetallePresupuesto> ObtenerDetalles(Guid presupuestoId)
        {
            Presupuesto presupuesto = _presupuestoRepository.GetById(presupuestoId);
            return presupuesto == null ? new List<DetallePresupuesto>() : presupuesto.Detalles;
        }

        public void CrearPresupuestosDemostracion(
            Guid clienteId)
        {
            if (clienteId == Guid.Empty)
            {
                throw Mensajes.Regla("The customer identifier is not valid.");
            }

            if (_presupuestoRepository
                .GetByClienteId(clienteId)
                .Any())
            {
                return;
            }

            Presupuesto ventaEstandar =
                CrearVentaEstandar(clienteId);

            Presupuesto fabricacionEspecial =
                CrearFabricacionEspecial(clienteId);

            Registrar(ventaEstandar);
            Registrar(fabricacionEspecial);
        }

        private static Presupuesto CrearVentaEstandar(
            Guid clienteId)
        {
            Presupuesto presupuesto = new Presupuesto
            {
                Numero = CrearNumero(
                    "VEN",
                    clienteId),

                ClienteId = clienteId,

                FechaEmision = DateTime.Today,

                FechaVencimiento =
                    DateTime.Today.AddDays(15),

                Moneda =
                    Moneda.DolaresEstadounidenses,

                TipoCambio = 1515m,

                PorcentajeIVA = 21m,

                PorcentajeAnticipo = 0m,

                CondicionPago = "Contado",

                PlazoEntrega =
                    "Entrega inmediata según disponibilidad",

                EntregaIncluida = false,

                Estado = EstadoPresupuesto.Aceptado,

                Observaciones =
                    "Cotización de venta estándar."
            };

            presupuesto.Detalles.Add(
                new DetallePresupuesto
                {
                    Codigo = "MP304-15",

                    Descripcion =
                        "Mirilla plana para soldar AISI 304",

                    TipoItem =
                        TipoItemPresupuesto.Producto,

                    Cantidad = 4m,

                    PrecioUnitario = 33.47m
                });

            presupuesto.Detalles.Add(
                new DetallePresupuesto
                {
                    Codigo = "MCPS304-15",

                    Descripcion =
                        "Manguito Clamp largo AISI 304",

                    TipoItem =
                        TipoItemPresupuesto.Producto,

                    Cantidad = 4m,

                    PrecioUnitario = 4.23m
                });

            presupuesto.Detalles.Add(
                new DetallePresupuesto
                {
                    Codigo = "SERV-SOLD",

                    Descripcion =
                        "Servicio de soldadura y pulido sanitario",

                    TipoItem =
                        TipoItemPresupuesto.Servicio,

                    Cantidad = 4m,

                    PrecioUnitario = 45m
                });

            return presupuesto;
        }

        private static Presupuesto CrearFabricacionEspecial(
            Guid clienteId)
        {
            Presupuesto presupuesto = new Presupuesto
            {
                Numero = CrearNumero(
                    "FAB",
                    clienteId),

                ClienteId = clienteId,

                FechaEmision = DateTime.Today,

                FechaVencimiento =
                    DateTime.Today.AddDays(30),

                Moneda =
                    Moneda.DolaresEstadounidenses,

                TipoCambio = 1515m,

                PorcentajeIVA = 10.5m,

                PorcentajeAnticipo = 50m,

                CondicionPago =
                    "50% de anticipo y 50% contra entrega",

                PlazoEntrega =
                    "45 a 60 días desde la recepción del anticipo",

                EntregaIncluida = false,

                Estado = EstadoPresupuesto.Aceptado,

                Observaciones =
                    "Cotización de fabricación especial."
            };

            presupuesto.Detalles.Add(
                new DetallePresupuesto
                {
                    Codigo = "FILTRO-10",

                    Descripcion =
                        "Filtro de ranura continua de 10 pulgadas",

                    DescripcionTecnica =
                        "Construcción sanitaria en acero inoxidable, " +
                        "configuración en L y conexión 226.",

                    TipoItem =
                        TipoItemPresupuesto.FabricacionEspecial,

                    Cantidad = 2m,

                    PrecioUnitario = 1240m
                });

            presupuesto.Detalles.Add(
                new DetallePresupuesto
                {
                    Codigo = "FILTRO-20",

                    Descripcion =
                        "Filtro de ranura continua de 20 pulgadas",

                    DescripcionTecnica =
                        "Construcción sanitaria en acero inoxidable, " +
                        "configuración en L y conexión 226.",

                    TipoItem =
                        TipoItemPresupuesto.FabricacionEspecial,

                    Cantidad = 2m,

                    PrecioUnitario = 1492m
                });

            return presupuesto;
        }

        private static string CrearNumero(
            string prefijo,
            Guid clienteId)
        {
            string identificadorCorto =
                clienteId
                    .ToString("N")
                    .Substring(0, 6)
                    .ToUpperInvariant();

            return string.Format(
                "PRE-{0}-{1}-{2}",
                DateTime.Today.Year,
                prefijo,
                identificadorCorto);
        }

        private static void ValidarPresupuesto(
            Presupuesto presupuesto)
        {
            if (presupuesto == null)
            {
                throw new ArgumentNullException(
                    nameof(presupuesto),
                    "El presupuesto no puede ser nulo.");
            
            }


        }
    }
}
