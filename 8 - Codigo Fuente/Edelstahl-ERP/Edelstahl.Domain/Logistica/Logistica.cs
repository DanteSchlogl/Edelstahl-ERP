using System;
using System.Collections.Generic;
using Edelstahl.Domain.Common;

namespace Edelstahl.Domain.Logistica
{
    public class Vehiculo : Entity
    {
        public string Patente { get; set; }
        public string Marca { get; set; }
        public string Modelo { get; set; }
        public int Anio { get; set; }
        public decimal CapacidadKg { get; set; }
        public decimal? CapacidadM3 { get; set; }
        public DateTime? VencimientoSeguro { get; set; }
        public DateTime? VencimientoVtv { get; set; }
        public string Estado { get; set; }
        public bool Activo { get; set; }

        public Vehiculo()
        {
            Patente = string.Empty;
            Marca = string.Empty;
            Modelo = string.Empty;
            Estado = "DISPONIBLE";
            Activo = true;
        }

        public bool DocumentacionVigente(DateTime fecha)
        {
            return VencimientoSeguro.HasValue
                && VencimientoVtv.HasValue
                && VencimientoSeguro.Value.Date >= fecha.Date
                && VencimientoVtv.Value.Date >= fecha.Date;
        }
    }

    public class Chofer : Entity
    {
        public string Documento { get; set; }
        public string Apellido { get; set; }
        public string Nombre { get; set; }
        public string LicenciaConducir { get; set; }
        public string CategoriaLicencia { get; set; }
        public DateTime VencimientoLicencia { get; set; }
        public string Telefono { get; set; }
        public bool Activo { get; set; }

        public Chofer()
        {
            Documento = string.Empty;
            Apellido = string.Empty;
            Nombre = string.Empty;
            LicenciaConducir = string.Empty;
            CategoriaLicencia = string.Empty;
            Telefono = string.Empty;
            Activo = true;
        }

        public string NombreCompleto
        {
            get { return Apellido + ", " + Nombre; }
        }
    }

    public enum EstadoRemito
    {
        Pendiente = 1,
        Asignado = 2,
        EnTransito = 3,
        Entregado = 4,
        Rechazado = 5,
        Reprogramado = 6,
        Cancelado = 7
    }

    public class RemitoEntrega : Entity
    {
        public string Numero { get; set; }
        public Guid PedidoId { get; set; }
        public Guid ClienteId { get; set; }
        public DateTime FechaEmision { get; set; }
        public DateTime? FechaDespacho { get; set; }
        public DateTime? FechaEntrega { get; set; }
        public EstadoRemito Estado { get; set; }
        public string DireccionEntrega { get; set; }
        public string ReceptorNombre { get; set; }
        public string ReceptorDocumento { get; set; }
        public string Observaciones { get; set; }
        public List<DetalleRemito> Detalles { get; set; }

        public RemitoEntrega()
        {
            Numero = string.Empty;
            FechaEmision = DateTime.Now;
            Estado = EstadoRemito.Pendiente;
            DireccionEntrega = string.Empty;
            ReceptorNombre = string.Empty;
            ReceptorDocumento = string.Empty;
            Observaciones = string.Empty;
            Detalles = new List<DetalleRemito>();
        }
    }

    public class DetalleRemito : Entity
    {
        public Guid RemitoId { get; set; }
        public Guid DetallePedidoId { get; set; }
        public string Codigo { get; set; }
        public string Descripcion { get; set; }
        public decimal Cantidad { get; set; }

        public DetalleRemito()
        {
            Codigo = string.Empty;
            Descripcion = string.Empty;
        }
    }

    public enum EstadoHojaRuta
    {
        Planificada = 1,
        EnCurso = 2,
        Finalizada = 3,
        Cancelada = 4
    }

    public class HojaRuta : Entity
    {
        public string Numero { get; set; }
        public Guid VehiculoId { get; set; }
        public Guid ChoferId { get; set; }
        public DateTime FechaPlanificada { get; set; }
        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaFinalizacion { get; set; }
        public EstadoHojaRuta Estado { get; set; }
        public decimal? KilometrosIniciales { get; set; }
        public decimal? KilometrosFinales { get; set; }
        public string Observaciones { get; set; }
        public List<DetalleHojaRuta> Detalles { get; set; }

        public HojaRuta()
        {
            Numero = string.Empty;
            FechaPlanificada = DateTime.Today;
            Estado = EstadoHojaRuta.Planificada;
            Observaciones = string.Empty;
            Detalles = new List<DetalleHojaRuta>();
        }
    }

    public enum ResultadoEntrega
    {
        EntregadoConforme = 1,
        EntregaParcial = 2,
        Rechazado = 3,
        Ausente = 4,
        DomicilioIncorrecto = 5
    }

    public class DetalleHojaRuta : Entity
    {
        public Guid HojaRutaId { get; set; }
        public Guid RemitoId { get; set; }
        public int OrdenEntrega { get; set; }
        public TimeSpan? HoraEstimada { get; set; }
        public TimeSpan? HoraLlegada { get; set; }
        public string EstadoEntrega { get; set; }
        public ResultadoEntrega? Resultado { get; set; }
        public string MotivoNoEntrega { get; set; }
        public string Observaciones { get; set; }

        public DetalleHojaRuta()
        {
            EstadoEntrega = "PENDIENTE";
            MotivoNoEntrega = string.Empty;
            Observaciones = string.Empty;
        }
    }

    public class ComprobanteEntrega : Entity
    {
        public Guid DetalleHojaRutaId { get; set; }
        public DateTime FechaRegistro { get; set; }
        public ResultadoEntrega Resultado { get; set; }
        public string NombreReceptor { get; set; }
        public string DocumentoReceptor { get; set; }
        public string MotivoRechazo { get; set; }
        public string Observaciones { get; set; }

        public ComprobanteEntrega()
        {
            FechaRegistro = DateTime.Now;
            NombreReceptor = string.Empty;
            DocumentoReceptor = string.Empty;
            MotivoRechazo = string.Empty;
            Observaciones = string.Empty;
        }
    }

    public enum EstadoDevolucion
    {
        Solicitada = 1,
        EnTransito = 2,
        Recibida = 3,
        EnInspeccion = 4,
        Resuelta = 5,
        Rechazada = 6,
        Cancelada = 7
    }

    public enum AccionResuelta
    {
        SinAccion = 0,
        ReingresoStock = 1,
        Reproceso = 2,
        Reemplazo = 3,
        Descarte = 4
    }

    public class Devolucion : Entity
    {
        public string Numero { get; set; }
        public Guid RemitoId { get; set; }
        public Guid PedidoId { get; set; }
        public Guid ClienteId { get; set; }
        public DateTime FechaSolicitud { get; set; }
        public DateTime? FechaRecepcion { get; set; }
        public EstadoDevolucion Estado { get; set; }
        public bool EsTotal { get; set; }
        public string MotivoGeneral { get; set; }
        public string Observaciones { get; set; }
        public List<DetalleDevolucion> Detalles { get; set; }

        public Devolucion()
        {
            Numero = string.Empty;
            FechaSolicitud = DateTime.Now;
            Estado = EstadoDevolucion.Solicitada;
            MotivoGeneral = string.Empty;
            Observaciones = string.Empty;
            Detalles = new List<DetalleDevolucion>();
        }
    }

    public class DetalleDevolucion : Entity
    {
        public Guid DevolucionId { get; set; }
        public Guid DetallePedidoId { get; set; }
        public string Codigo { get; set; }
        public string Descripcion { get; set; }
        public decimal CantidadDevuelta { get; set; }
        public string Motivo { get; set; }
        public AccionResuelta Accion { get; set; }
        public decimal CantidadReingresada { get; set; }
        public decimal CantidadDescartada { get; set; }
        public string Estado { get; set; }
        public string Observaciones { get; set; }
        public List<InspeccionDevolucion> Inspecciones { get; set; }

        public DetalleDevolucion()
        {
            Codigo = string.Empty;
            Descripcion = string.Empty;
            Motivo = string.Empty;
            Accion = AccionResuelta.SinAccion;
            Estado = "PENDIENTE";
            Observaciones = string.Empty;
            Inspecciones = new List<InspeccionDevolucion>();
        }
    }

    public class InspeccionDevolucion : Entity
    {
        public Guid DetalleDevolucionId { get; set; }
        public string UsuarioInspector { get; set; }
        public DateTime FechaInspeccion { get; set; }
        public bool RequiereReproceso { get; set; }
        public bool RequiereReemplazo { get; set; }
        public bool AptoReingresoStock { get; set; }
        public string Resultado { get; set; }
        public string CondicionMaterial { get; set; }
        public string Observaciones { get; set; }

        public InspeccionDevolucion()
        {
            UsuarioInspector = string.Empty;
            FechaInspeccion = DateTime.Now;
            Resultado = string.Empty;
            CondicionMaterial = string.Empty;
            Observaciones = string.Empty;
        }
    }
}
