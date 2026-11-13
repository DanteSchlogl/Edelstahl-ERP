using System;
using System.Collections.Generic;
using Edelstahl.Domain.Common;

namespace Edelstahl.Domain.Produccion
{
    public enum EstadoProduccion
    {
        Pendiente = 1,
        EnProceso = 2,
        ControlCalidad = 3,
        Finalizada = 4,
        Cancelada = 5
    }

    public enum PrioridadOrden
    {
        Normal = 1,
        Alta = 2,
        Urgente = 3
    }

    public enum ResultadoCalidad
    {
        Aprobado = 1,
        Rechazado = 2,
        Reproceso = 3
    }

    public class OrdenFabricacion : Entity
    {
        public string Numero { get; set; }
        public Guid? PedidoId { get; set; }
        public Guid? DetallePedidoId { get; set; }
        public Guid ProductoId { get; set; }
        public string Origen { get; set; }
        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaFinEstimada { get; set; }
        public DateTime? FechaFinReal { get; set; }
        public EstadoProduccion Estado { get; set; }
        public PrioridadOrden Prioridad { get; set; }
        public decimal CantidadFabricar { get; set; }
        public string PlanoTecnicoUri { get; set; }
        public string Especificaciones { get; set; }
        public string Observaciones { get; set; }
        public List<ConsumoMaterial> Materiales { get; set; }
        public List<AvanceProduccion> Avances { get; set; }
        public List<ControlCalidad> Controles { get; set; }

        public OrdenFabricacion()
        {
            Numero = string.Empty;
            Origen = "PEDIDO";
            FechaCreacion = DateTime.Now;
            Estado = EstadoProduccion.Pendiente;
            Prioridad = PrioridadOrden.Normal;
            PlanoTecnicoUri = string.Empty;
            Especificaciones = string.Empty;
            Observaciones = string.Empty;
            Materiales = new List<ConsumoMaterial>();
            Avances = new List<AvanceProduccion>();
            Controles = new List<ControlCalidad>();
        }
    }

    public class ConsumoMaterial : Entity
    {
        public Guid OrdenFabricacionId { get; set; }
        public Guid ProductoMaterialId { get; set; }
        public Guid? UbicacionId { get; set; }
        public DateTime? FechaReserva { get; set; }
        public DateTime? FechaConsumo { get; set; }
        public decimal CantidadRequerida { get; set; }
        public decimal CantidadReservada { get; set; }
        public decimal CantidadConsumida { get; set; }
        public decimal CantidadMerma { get; set; }
        public decimal CostoUnitario { get; set; }
        public string Observaciones { get; set; }

        public ConsumoMaterial()
        {
            Observaciones = string.Empty;
        }
    }

    public class AvanceProduccion : Entity
    {
        public Guid OrdenFabricacionId { get; set; }
        public DateTime Fecha { get; set; }
        public string Usuario { get; set; }
        public string Etapa { get; set; }
        public decimal PorcentajeAvance { get; set; }
        public string Observaciones { get; set; }

        public AvanceProduccion()
        {
            Fecha = DateTime.Now;
            Usuario = string.Empty;
            Etapa = string.Empty;
            Observaciones = string.Empty;
        }
    }

    public class ControlCalidad : Entity
    {
        public Guid OrdenFabricacionId { get; set; }
        public string UsuarioControl { get; set; }
        public DateTime FechaControl { get; set; }
        public ResultadoCalidad Resultado { get; set; }
        public bool RequiereReproceso { get; set; }
        public int NumeroReproceso { get; set; }
        public DateTime? FechaReproceso { get; set; }
        public string Observaciones { get; set; }

        public ControlCalidad()
        {
            UsuarioControl = string.Empty;
            FechaControl = DateTime.Now;
            Observaciones = string.Empty;
        }
    }
}
