using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Edelstahl.Domain.Common;

namespace Edelstahl.Domain.Comercial
{
    /// <summary>
    /// Representa una cotización comercial emitida para un cliente.
    /// Puede contener productos, servicios y fabricaciones especiales.
    /// </summary>
    public class Presupuesto : Entity
    {
        public string Numero { get; set; }

        public Guid ClienteId { get; set; }

        public DateTime FechaEmision { get; set; }

        public DateTime FechaVencimiento { get; set; }

        public Moneda Moneda { get; set; }

        public decimal TipoCambio { get; set; }

        public decimal PorcentajeIVA { get; set; }

        public decimal PorcentajeRecargo { get; set; }

        public decimal PorcentajeDescuentoGeneral { get; set; }

        public decimal PorcentajeAnticipo { get; set; }

        public string CondicionPago { get; set; }

        public string PlazoEntrega { get; set; }

        public bool EntregaIncluida { get; set; }

        public EstadoPresupuesto Estado { get; set; }

        public string Observaciones { get; set; }

        public List<DetallePresupuesto> Detalles { get; set; }

        public Presupuesto()
        {
            Numero = string.Empty;
            FechaEmision = DateTime.Now;
            FechaVencimiento = DateTime.Now.AddDays(15);

            Moneda = Moneda.PesosArgentinos;
            TipoCambio = 1m;
            PorcentajeIVA = 21m;
            PorcentajeRecargo = 0m;
            PorcentajeDescuentoGeneral = 0m;
            PorcentajeAnticipo = 0m;

            CondicionPago = string.Empty;
            PlazoEntrega = string.Empty;
            EntregaIncluida = false;

            Estado = EstadoPresupuesto.Enviado;
            Observaciones = string.Empty;

            Detalles = new List<DetallePresupuesto>();
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

        /// <summary>Porcentaje minimo de sena que exige toda venta antes de convertirse en pedido.</summary>
        public const decimal AnticipoMinimoPorcentaje = 30m;

        /// <summary>
        /// Porcentaje de sena que se aplica: el del presupuesto y, si incluye productos a fabricar,
        /// nunca menos que el minimo. Los productos de stock no exigen sena (se pagan al 100% antes de remitir).
        /// </summary>
        public decimal PorcentajeAnticipoEfectivo()
        {
            bool aFabricar = Detalles != null
                && Detalles.Any(d => d.TipoItem == TipoItemPresupuesto.FabricacionEspecial);

            return aFabricar && PorcentajeAnticipo < AnticipoMinimoPorcentaje
                ? AnticipoMinimoPorcentaje
                : PorcentajeAnticipo;
        }

        public decimal CalcularAnticipo()
        {
            return CalcularTotal()
                * PorcentajeAnticipoEfectivo()
                / 100m;
        }

        public decimal CalcularSaldoPendiente()
        {
            return CalcularTotal()
                - CalcularAnticipo();
        }

        public decimal CalcularTotalEnPesos()
        {
            if (Moneda == Moneda.PesosArgentinos)
            {
                return CalcularTotal();
            }

            return CalcularTotal() * TipoCambio;
        }

        public bool EstaVigente()
        {
            return FechaVencimiento.Date >= DateTime.Today
                && Estado != EstadoPresupuesto.Vencido
                && Estado != EstadoPresupuesto.Rechazado
                && Estado != EstadoPresupuesto.ConvertidoEnPedido;
        }

        public bool PuedeConfirmarse()
        {
            return EstaVigente()
                && Estado == EstadoPresupuesto.Aceptado
                && Detalles.Count > 0
                && CalcularTotal() > 0m;
        }

        public bool RequiereAnticipo()
        {
            return PorcentajeAnticipoEfectivo() > 0m;
        }
    }
}
