using System;
using System.Linq;
using System.Text;
using Edelstahl.DAL.Factory;
using Edelstahl.DAL.Interfaces;
using Edelstahl.Domain.Integrity;

namespace Edelstahl.BLL.Services
{
    /// <summary>
    /// Verifica los dígitos verificadores (DVH por registro y DVV por tabla)
    /// y deja constancia en la bitácora cuando detecta una alteración.
    /// </summary>
    public class IntegridadService
    {
        private const int MaximoDeRegistrosEnBitacora = 50;

        private readonly IVerificadorIntegridad _verificador;
        private readonly BitacoraService _bitacora;

        public IntegridadService()
            : this(FactoryDataAccess.VerificadorIntegridad, new BitacoraService())
        {
        }

        public IntegridadService(IVerificadorIntegridad verificador, BitacoraService bitacora)
        {
            _verificador = verificador ?? throw new ArgumentNullException(nameof(verificador));
            _bitacora = bitacora ?? throw new ArgumentNullException(nameof(bitacora));
        }

        /// <summary>
        /// Solo comprueba; no escribe en la bitácora.
        /// </summary>
        public ResultadoIntegridad Verificar()
        {
            return _verificador.Verificar();
        }

        /// <summary>
        /// Comprueba y, si hay una alteración, la registra en la bitácora con
        /// la fecha y la hora en que se detectó y el detalle de los registros.
        /// </summary>
        /// <param name="momento">Cuándo se verificó, por ejemplo "inicio del sistema".</param>
        public ResultadoIntegridad VerificarYRegistrar(string momento)
        {
            ResultadoIntegridad resultado = _verificador.Verificar();

            if (!resultado.Correcto)
            {
                RegistrarAlteracion(resultado, momento);
            }

            return resultado;
        }

        /// <summary>
        /// Recalcula los dígitos con los datos actuales. Lo decide un
        /// administrador después de revisar los datos; queda registrado.
        /// </summary>
        public int Recalcular(string usuario)
        {
            int registros = _verificador.Recalcular();

            _bitacora.Registrar(
                "Integridad",
                "Digitos verificadores recalculados",
                "Clientes",
                null,
                "El administrador recalculo los digitos verificadores (DVH y DVV) de la tabla Clientes con " +
                "los datos actuales. Registros recalculados: " + registros + ".",
                "Advertencia",
                "Auditoria",
                string.Empty,
                usuario);

            return registros;
        }

        private void RegistrarAlteracion(ResultadoIntegridad resultado, string momento)
        {
            StringBuilder descripcion = new StringBuilder();

            descripcion.Append("Se detecto una alteracion de los datos de la tabla Clientes el ");
            descripcion.Append(resultado.FechaVerificacion.ToString("dd/MM/yyyy HH:mm:ss"));

            if (!string.IsNullOrWhiteSpace(momento))
            {
                descripcion.Append(" (" + momento + ")");
            }

            descripcion.Append(": ");
            descripcion.Append(resultado.RegistrosAlterados + " registro(s) con el DVH incorrecto");
            descripcion.Append(resultado.AlteradaLaTabla
                ? " y el DVV de la tabla no coincide."
                : ".");
            descripcion.Append(" El acceso quedo bloqueado hasta restaurar un respaldo sano o recalcular los digitos.");

            StringBuilder detalle = new StringBuilder();

            foreach (InconsistenciaIntegridad item in resultado.Inconsistencias.Take(MaximoDeRegistrosEnBitacora))
            {
                detalle.AppendLine(
                    item.Tabla + " | " +
                    (item.Tipo == TipoDigito.Horizontal ? "DVH" : "DVV") + " | " +
                    (item.RegistroId.HasValue ? item.RegistroId.Value.ToString() : "-") + " | " +
                    item.Referencia + " | " + item.Detalle);
            }

            if (resultado.Inconsistencias.Count > MaximoDeRegistrosEnBitacora)
            {
                detalle.AppendLine("... y " + (resultado.Inconsistencias.Count - MaximoDeRegistrosEnBitacora) + " mas.");
            }

            _bitacora.Registrar(
                "Integridad",
                "Alteracion detectada",
                "Clientes",
                null,
                descripcion.ToString(),
                "Fallido",
                "Seguridad",
                detalle.ToString(),
                "sistema");
        }
    }
}
