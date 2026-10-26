using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Edelstahl.BLL.DTOs;
using Edelstahl.Domain.Comercial;
using Edelstahl.BLL.Infrastructure;



namespace Edelstahl.BLL.Services
{
    /// <summary>
    /// Coordina el proceso completo de confirmación de un pedido.
    /// Mantiene las selecciones realizadas durante el asistente.
    /// </summary>
    public class ConfirmarPedidoService
    {
        private readonly ClienteService _clienteService;
        private readonly PresupuestoService _presupuestoService;

        public Cliente ClienteSeleccionado { get; private set; }

        public Presupuesto PresupuestoSeleccionado { get; private set; }

        public ConfirmarPedidoService()
        {
            _clienteService = new ClienteService();
            _presupuestoService = new PresupuestoService();
        }

        public List<ClienteSeleccionDto> BuscarClientes(
            string filtro)
        {
            List<Cliente> clientes =
                _clienteService.ObtenerTodos();

            if (!string.IsNullOrWhiteSpace(filtro))
            {
                string filtroTexto =
                    filtro.Trim().ToLowerInvariant();

                string filtroCUIT =
                    NormalizarCUIT(filtro);

                clientes = clientes
                    .Where(cliente =>
                    {
                        string clienteCUIT =
                            NormalizarCUIT(cliente.CUIT);

                        string razonSocial =
                            (cliente.RazonSocial ?? string.Empty)
                                .ToLowerInvariant();

                        return clienteCUIT.Contains(filtroCUIT)
                            || razonSocial.Contains(filtroTexto);
                    })
                    .ToList();
            }

            return clientes
                .Select(cliente => new ClienteSeleccionDto
                {
                    Id = cliente.Id,
                    CUIT = cliente.CUIT,
                    RazonSocial = cliente.RazonSocial,
                    Localidad = cliente.Localidad,
                    CreditoDisponible =
                        cliente.CalcularCreditoDisponible(),
                    Activo = cliente.Activo
                })
                .ToList();
        }

        public Cliente SeleccionarCliente(Guid clienteId)
        {
            if (clienteId == Guid.Empty)
            {
                throw Mensajes.Regla("The customer identifier is not valid.");
            }

            Cliente cliente =
                _clienteService.ObtenerPorId(clienteId);

            if (cliente == null)
            {
                throw Mensajes.Regla("The selected customer was not found.");
            }

            if (!cliente.Activo)
            {
                throw Mensajes.Regla("The selected customer is not active.");
            }

            ClienteSeleccionado = cliente;
            PresupuestoSeleccionado = null;

            if (Edelstahl.Services.Configuration.ApplicationMode.IsDemo)
            {
                _presupuestoService.CrearPresupuestosDemostracion(
                    cliente.Id);
            }

            return ClienteSeleccionado;
        }

        public List<PresupuestoSeleccionDto>
            BuscarPresupuestos(string filtro)
        {
            ValidarClienteSeleccionado();

            List<Presupuesto> presupuestos =
                _presupuestoService.BuscarPorCliente(
                    ClienteSeleccionado.Id,
                    filtro);

            return presupuestos
                .Select(presupuesto =>
                    new PresupuestoSeleccionDto
                    {
                        Id = presupuesto.Id,
                        Numero = presupuesto.Numero,
                        FechaEmision =
                            presupuesto.FechaEmision,
                        FechaVencimiento =
                            presupuesto.FechaVencimiento,

                        Moneda =
                            presupuesto.Moneda ==
                            Moneda.DolaresEstadounidenses
                                ? "USD"
                                : "ARS",

                        Total =
                            presupuesto.CalcularTotal(),

                        Anticipo =
                            presupuesto.CalcularAnticipo(),

                        Estado =
                            presupuesto.Estado.ToString(),

                        Vigente =
                            presupuesto.EstaVigente(),

                        PuedeConfirmarse =
                            presupuesto.PuedeConfirmarse()
                    })
                .ToList();
        }

        public Presupuesto SeleccionarPresupuesto(
            Guid presupuestoId)
        {
            ValidarClienteSeleccionado();

            if (presupuestoId == Guid.Empty)
            {
                throw Mensajes.Regla("The quotation identifier is not valid.");
            }

            Presupuesto presupuesto =
                _presupuestoService.ObtenerPorId(
                    presupuestoId);

            if (presupuesto == null)
            {
                throw Mensajes.Regla("The selected quotation was not found.");
            }

            if (presupuesto.ClienteId !=
                ClienteSeleccionado.Id)
            {
                throw Mensajes.Regla("The quotation does not belong to the selected customer.");
            }

            PresupuestoSeleccionado = presupuesto;

            return PresupuestoSeleccionado;
        }

        public void ValidarPasoCliente()
        {
            ValidarClienteSeleccionado();
        }

        public void ValidarPasoPresupuesto()
        {
            ValidarClienteSeleccionado();

            if (PresupuestoSeleccionado == null)
            {
                throw Mensajes.Regla("Select a quotation to continue.");
            }

            if (!PresupuestoSeleccionado.PuedeConfirmarse())
            {
                throw Mensajes.Regla("The selected quotation is not valid, was not accepted or has no items.");
            }
        }

        public void ReiniciarPresupuesto()
        {
            PresupuestoSeleccionado = null;
        }

        public void Reiniciar()
        {
            ClienteSeleccionado = null;
            PresupuestoSeleccionado = null;
        }

        private void ValidarClienteSeleccionado()
        {
            if (ClienteSeleccionado == null)
            {
                throw Mensajes.Regla("Select a customer to continue.");
            }

            if (!ClienteSeleccionado.Activo)
            {
                throw Mensajes.Regla("The selected customer is not active.");
            }
        }

        private static string NormalizarCUIT(
            string cuit)
        {
            if (string.IsNullOrWhiteSpace(cuit))
            {
                return string.Empty;
            }

            return cuit
                .Trim()
                .Replace("-", string.Empty)
                .Replace(" ", string.Empty);
        }
    }
}