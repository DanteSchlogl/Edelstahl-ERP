using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using Edelstahl.DAL.EntityFramework;
using Edelstahl.DAL.Interfaces;
using Edelstahl.Domain.Comercial;
using Edelstahl.Domain.Exceptions;
using Edelstahl.Domain.Integrity;
using Edelstahl.Services.Integrity;
using Edelstahl.Services.Localization;

namespace Edelstahl.DAL.Integrity
{
    /// <summary>
    /// Comprueba los dígitos verificadores de la tabla Clientes y, cuando un
    /// administrador lo decide después de revisar los datos, los vuelve a
    /// calcular.
    ///
    /// DVH (horizontal): un valor por registro, calculado con todos sus
    /// campos. Detecta qué registro se cambió fuera del sistema.
    /// DVV (vertical): un valor por tabla, calculado con los DVH de todos
    /// los registros. Detecta además registros agregados o borrados.
    /// </summary>
    internal class VerificadorIntegridad : IVerificadorIntegridad
    {
        /// <summary>
        /// Revisa cada registro (DVH) y el conjunto de la tabla (DVV). Solo
        /// lee: no modifica nada.
        /// </summary>
        public ResultadoIntegridad Verificar()
        {
            ResultadoIntegridad resultado = new ResultadoIntegridad();

            using (EdelstahlNegocioContext contexto = new EdelstahlNegocioContext())
            {
                List<Cliente> clientes = contexto.Clientes
                    .AsNoTracking()
                    .OrderBy(cliente => cliente.Id)
                    .ToList();

                resultado.RegistrosRevisados = clientes.Count;

                bool hayDvhVacio = false;

                foreach (Cliente cliente in clientes)
                {
                    if (string.IsNullOrWhiteSpace(cliente.DVH))
                    {
                        hayDvhVacio = true;
                        resultado.Agregar(Horizontal(cliente,
                            "The record has no check digit (DVH)."));
                    }
                    else if (!ClienteDvhService.EsValido(cliente))
                    {
                        resultado.Agregar(Horizontal(cliente,
                            "The stored check digit (DVH) of the record does not match its data."));
                    }
                }

                IntegridadTabla integridad = contexto.IntegridadTablas
                    .AsNoTracking()
                    .SingleOrDefault(registro =>
                        registro.NombreTabla == ClienteDvvService.NombreTabla);

                if (integridad == null || string.IsNullOrWhiteSpace(integridad.DVV))
                {
                    if (clientes.Count > 0)
                    {
                        resultado.Agregar(Vertical(
                            "The table has no stored check digit (DVV)."));
                    }
                }
                else if (hayDvhVacio)
                {
                    resultado.Agregar(Vertical(
                        "The check digit of the table (DVV) could not be verified because a record has no DVH."));
                }
                else if (!ClienteDvvService.EsValido(clientes, integridad.DVV))
                {
                    resultado.Agregar(Vertical(
                        "The check digit of the table (DVV) does not match the set of records."));
                }
            }

            return resultado;
        }

        /// <summary>
        /// Vuelve a calcular el DVH de cada cliente y el DVV de la tabla con
        /// los datos que hay ahora. Devuelve cuántos registros se recalcularon.
        /// Solo debe usarse después de revisar que los datos son correctos.
        /// </summary>
        public int Recalcular()
        {
            using (EdelstahlNegocioContext contexto = new EdelstahlNegocioContext())
            using (DbContextTransaction transaccion = contexto.Database.BeginTransaction())
            {
                try
                {
                    List<Cliente> clientes = contexto.Clientes
                        .OrderBy(cliente => cliente.Id)
                        .ToList();

                    foreach (Cliente cliente in clientes)
                    {
                        ClienteDvhService.Asignar(cliente);
                    }

                    contexto.SaveChanges();

                    IntegridadTabla integridad = contexto.IntegridadTablas
                        .SingleOrDefault(registro =>
                            registro.NombreTabla == ClienteDvvService.NombreTabla);

                    if (integridad == null)
                    {
                        integridad = new IntegridadTabla
                        {
                            NombreTabla = ClienteDvvService.NombreTabla
                        };

                        contexto.IntegridadTablas.Add(integridad);
                    }

                    integridad.DVV = ClienteDvvService.Calcular(clientes);
                    integridad.FechaActualizacion = DateTime.Now;
                    contexto.SaveChanges();

                    transaccion.Commit();
                    return clientes.Count;
                }
                catch
                {
                    transaccion.Rollback();
                    throw;
                }
            }
        }

        private static InconsistenciaIntegridad Horizontal(Cliente cliente, string detalle)
        {
            return new InconsistenciaIntegridad
            {
                Tabla = ClienteDvvService.NombreTabla,
                Tipo = TipoDigito.Horizontal,
                RegistroId = cliente.Id,
                Referencia = cliente.RazonSocial ?? string.Empty,
                Detalle = LanguageService.Translate(detalle)
            };
        }

        private static InconsistenciaIntegridad Vertical(string detalle)
        {
            return new InconsistenciaIntegridad
            {
                Tabla = ClienteDvvService.NombreTabla,
                Tipo = TipoDigito.Vertical,
                Detalle = LanguageService.Translate(detalle)
            };
        }
    }
}
