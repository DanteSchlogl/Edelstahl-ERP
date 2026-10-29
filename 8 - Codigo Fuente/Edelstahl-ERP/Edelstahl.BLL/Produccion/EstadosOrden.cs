using System;
using System.Linq;
using Edelstahl.BLL.Infrastructure;
using Edelstahl.Domain.Produccion;

namespace Edelstahl.BLL.Produccion
{
    /// <summary>
    /// Patrón State aplicado a la orden de fabricación: cada estado decide
    /// qué operaciones admite y a qué estado pasa la orden. Así las etapas
    /// respetan el orden definido y no pueden omitirse.
    /// </summary>
    public interface IEstadoOrden
    {
        EstadoProduccion Estado { get; }

        bool PermiteConsumos { get; }

        void Iniciar(OrdenFabricacion orden);

        void RegistrarAvance(OrdenFabricacion orden);

        void EnviarACalidad(OrdenFabricacion orden);

        void AplicarControl(OrdenFabricacion orden, ResultadoCalidad resultado);

        void Cancelar(OrdenFabricacion orden);
    }

    public abstract class EstadoOrdenBase : IEstadoOrden
    {
        public abstract EstadoProduccion Estado { get; }

        public virtual bool PermiteConsumos
        {
            get { return false; }
        }

        public virtual void Iniciar(OrdenFabricacion orden)
        {
            throw NoPermitida("start");
        }

        public virtual void RegistrarAvance(OrdenFabricacion orden)
        {
            throw NoPermitida("register progress");
        }

        public virtual void EnviarACalidad(OrdenFabricacion orden)
        {
            throw NoPermitida("send to quality control");
        }

        public virtual void AplicarControl(OrdenFabricacion orden, ResultadoCalidad resultado)
        {
            throw NoPermitida("register a quality control");
        }

        public virtual void Cancelar(OrdenFabricacion orden)
        {
            throw NoPermitida("cancel");
        }

        protected Exception NoPermitida(string operacion)
        {
            return Mensajes.Regla(
                "The operation ({0}) is not allowed while the order is {1}.",
                Mensajes.T(operacion),
                Etiquetas.De(Estado));
        }
    }

    public sealed class EstadoPendiente : EstadoOrdenBase
    {
        public override EstadoProduccion Estado
        {
            get { return EstadoProduccion.Pendiente; }
        }

        public override void Iniciar(OrdenFabricacion orden)
        {
            orden.Estado = EstadoProduccion.EnProceso;
            orden.FechaInicio = DateTime.Now;
        }

        public override void Cancelar(OrdenFabricacion orden)
        {
            orden.Estado = EstadoProduccion.Cancelada;
        }
    }

    public sealed class EstadoEnProceso : EstadoOrdenBase
    {
        public override EstadoProduccion Estado
        {
            get { return EstadoProduccion.EnProceso; }
        }

        public override bool PermiteConsumos
        {
            get { return true; }
        }

        public override void RegistrarAvance(OrdenFabricacion orden)
        {
        }

        public override void EnviarACalidad(OrdenFabricacion orden)
        {
            decimal avance = AvanceDe(orden);

            if (avance < 100m)
            {
                throw Mensajes.Regla(
                    "The order is at {0}% progress. It cannot be sent to quality control until manufacturing reaches 100%. Keep manufacturing or cancel the order.",
                    avance.ToString("0.##"));
            }

            orden.Estado = EstadoProduccion.ControlCalidad;
        }

        /// <summary>
        /// Avance de fabricación de la orden: el mayor porcentaje registrado (0 si todavía no se cargó ninguno).
        /// </summary>
        public static decimal AvanceDe(OrdenFabricacion orden)
        {
            return orden.Avances == null || orden.Avances.Count == 0 ? 0m : orden.Avances.Max(a => a.PorcentajeAvance);
        }

        public override void Cancelar(OrdenFabricacion orden)
        {
            orden.Estado = EstadoProduccion.Cancelada;
        }
    }

    public sealed class EstadoEnControlCalidad : EstadoOrdenBase
    {
        public override EstadoProduccion Estado
        {
            get { return EstadoProduccion.ControlCalidad; }
        }

        public override void AplicarControl(OrdenFabricacion orden, ResultadoCalidad resultado)
        {
            if (resultado == ResultadoCalidad.Aprobado)
            {
                orden.Estado = EstadoProduccion.Finalizada;
                orden.FechaFinReal = DateTime.Now;
            }
            else if (resultado == ResultadoCalidad.Reproceso)
            {
                orden.Estado = EstadoProduccion.EnProceso;
            }

            else if (resultado == ResultadoCalidad.Rechazado)
            {
                // El rechazo cierra la orden: el motivo ya quedo en el control de calidad.
                orden.Estado = EstadoProduccion.Cancelada;
            }
        }

        public override void Cancelar(OrdenFabricacion orden)
        {
            orden.Estado = EstadoProduccion.Cancelada;
        }
    }

    public sealed class EstadoFinalizada : EstadoOrdenBase
    {
        public override EstadoProduccion Estado
        {
            get { return EstadoProduccion.Finalizada; }
        }
    }

    public sealed class EstadoCancelada : EstadoOrdenBase
    {
        public override EstadoProduccion Estado
        {
            get { return EstadoProduccion.Cancelada; }
        }
    }

    public static class EstadosOrden
    {
        public static IEstadoOrden Para(EstadoProduccion estado)
        {
            switch (estado)
            {
                case EstadoProduccion.Pendiente:
                    return new EstadoPendiente();
                case EstadoProduccion.EnProceso:
                    return new EstadoEnProceso();
                case EstadoProduccion.ControlCalidad:
                    return new EstadoEnControlCalidad();
                case EstadoProduccion.Finalizada:
                    return new EstadoFinalizada();
                default:
                    return new EstadoCancelada();
            }
        }
    }
}
