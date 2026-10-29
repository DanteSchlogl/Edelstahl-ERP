using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Edelstahl.Domain.Common;

namespace Edelstahl.Domain.Comercial
{
    /// <summary>
    /// Representa un pedido comercial confirmado
    /// a partir de un presupuesto aceptado.
    /// </summary>
    public class Pedido : Entity
    {
        public string Numero { get; set; }

        public Guid ClienteId { get; set; }

        public Guid PresupuestoId { get; set; }

        public DateTime FechaPedido { get; set; }

        public EstadoPedido Estado { get; set; }

        public Moneda Moneda { get; set; }

        public decimal TipoCambio { get; set; }

        public decimal PorcentajeIVA { get; set; }

        public decimal PorcentajeRecargo { get; set; }

        public decimal PorcentajeDescuentoGeneral { get; set; }

        public decimal PorcentajeAnticipo { get; set; }

        public decimal ImporteAnticipo { get; set; }

        public string MedioPagoAnticipo { get; set; }

        public string ComprobanteAnticipo { get; set; }

        public DateTime? FechaAnticipo { get; set; }

        public string CondicionPago { get; set; }

        public string PlazoEntrega { get; set; }

        public bool EntregaIncluida { get; set; }

        public string Observaciones { get; set; }

        public List<DetallePedido> Detalles { get; set; }

        public Pedido()
        {
            Numero = string.Empty;

            FechaPedido = DateTime.Now;

            Estado = EstadoPedido.Pendiente;

            Moneda = Moneda.PesosArgentinos;
            TipoCambio = 1m;

            PorcentajeIVA = 21m;
            PorcentajeRecargo = 0m;
            PorcentajeDescuentoGeneral = 0m;
            PorcentajeAnticipo = 0m;
            ImporteAnticipo = 0m;

            MedioPagoAnticipo = string.Empty;
            ComprobanteAnticipo = string.Empty;
            FechaAnticipo = null;

            CondicionPago = string.Empty;
            PlazoEntrega = string.Empty;
            EntregaIncluida = false;
            Observaciones = string.Empty;

            Detalles = new List<DetallePedido>();
        }

        public decimal CalcularSubtotal()
        {
            return Detalles.Sum(
                detalle => detalle.CalcularSubtotal());
        }

        public decimal CalcularDescuentoGeneral()
        {
            return CalcularSubtotal()
                * PorcentajeDescuentoGeneral
                / 100m;
        }

        public decimal CalcularRecargo()
        {
            decimal subtotalNeto =
                CalcularSubtotal()
                - CalcularDescuentoGeneral();

            return subtotalNeto
                * PorcentajeRecargo
                / 100m;
        }

        public decimal CalcularNetoGravado()
        {
            return CalcularSubtotal()
                - CalcularDescuentoGeneral()
                + CalcularRecargo();
        }

        public decimal CalcularIVA()
        {
            return CalcularNetoGravado()
                * PorcentajeIVA
                / 100m;
        }

        public decimal CalcularTotal()
        {
            return CalcularNetoGravado()
                + CalcularIVA();
        }

        public decimal CalcularSaldoPendiente()
        {
            decimal saldo =
                CalcularTotal() - ImporteAnticipo;

            return saldo < 0m
                ? 0m
                : saldo;
        }

        public decimal CalcularTotalEnPesos()
        {
            if (Moneda == Moneda.PesosArgentinos)
            {
                return CalcularTotal();
            }

            return CalcularTotal() * TipoCambio;
        }

        public bool TieneDetalles()
        {
            return Detalles != null
                && Detalles.Count > 0;
        }

        public bool TieneAnticipoSuficiente()
        {
            if (PorcentajeAnticipo <= 0m)
            {
                return true;
            }

            decimal porcentaje = Detalles != null && Detalles.Any(d => d.RequiereFabricacion) && PorcentajeAnticipo < Presupuesto.AnticipoMinimoPorcentaje
                ? Presupuesto.AnticipoMinimoPorcentaje
                : PorcentajeAnticipo;

            decimal anticipoRequerido =
                CalcularTotal()
                * porcentaje
                / 100m;

            // La sena se carga con dos decimales: se tolera un centavo de redondeo.
            return ImporteAnticipo + 0.01m >= anticipoRequerido;
        }

        public bool PuedeConfirmarse()
        {
            return ClienteId != Guid.Empty
                && PresupuestoId != Guid.Empty
                && !string.IsNullOrWhiteSpace(Numero)
                && TieneDetalles()
                && CalcularTotal() > 0m
                && TieneAnticipoSuficiente()
                && Estado != EstadoPedido.Cancelado;
        }

        public void Confirmar()
        {
            if (!PuedeConfirmarse())
            {
                throw new InvalidOperationException(
                    "El pedido no cumple las condiciones necesarias " +
                    "para ser confirmado.");
            }

            Estado = EstadoPedido.Confirmado;
        }
    }
}