using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Security.Cryptography;
using Edelstahl.Domain.Comercial;
using Edelstahl.Domain.Integrity;
using Edelstahl.Services.Cryptography;
using Edelstahl.Services.Integrity;

namespace Edelstahl.DAL.EntityFramework
{
    /// <summary>
    /// Inicializa la conexión con la base de datos
    /// comercial administrada mediante Entity Framework 6.
    ///
    /// También inicializa y verifica los controles
    /// de integridad DVH y DVV de la tabla Clientes.
    /// </summary>
    public static class InicializadorNegocio
    {
        public static void Inicializar()
        {
            /*
             * La estructura no la administra Entity Framework: la crea
             * EsquemaNegocio, que agrega las tablas que falten sin tocar
             * las existentes.
             */
            Database.SetInitializer<EdelstahlNegocioContext>(null);

            EsquemaServicios.Instalar();
            EsquemaNegocio.Instalar();

            /*
             * Se comprueba que la base sea accesible.
             */
            using (EdelstahlNegocioContext contexto = new EdelstahlNegocioContext())
            {
                contexto.Database.Initialize(false);
            }

            /*
             * Primero se generan los DVH históricos.
             *
             * El DVV depende de que todos los clientes posean un DVH válido.
             */
            MigrarDvhClientesExistentes();

            /*
             * Los CUIT guardados con el formato de cifrado anterior se
             * vuelven a cifrar con el formato actual, mucho más rápido.
             */
            MigrarCifradoClientes();

            /*
             * Después se crea o verifica el DVV del conjunto completo de clientes.
             */
            InicializarOVerificarDvvClientes();
        }

        /// <summary>
        /// Reemplaza el formato de cifrado anterior del CUIT por el actual
        /// y actualiza DVH y DVV dentro de la misma transacción. Solo actúa
        /// cuando la integridad previa es válida; si no lo es, no modifica
        /// nada y la verificación posterior informa la alteración.
        /// </summary>
        private static void MigrarCifradoClientes()
        {
            using (EdelstahlNegocioContext contexto = new EdelstahlNegocioContext())
            using (DbContextTransaction transaccion = contexto.Database.BeginTransaction())
            {
                try
                {
                    List<Cliente> clientes = contexto.Clientes
                        .OrderBy(cliente => cliente.Id)
                        .ToList();

                    List<Cliente> pendientes = clientes
                        .Where(cliente =>
                            !string.IsNullOrWhiteSpace(cliente.CUIT) &&
                            !cliente.CUIT.StartsWith(
                                AesEncryptionService.PrefijoV2,
                                StringComparison.Ordinal))
                        .ToList();

                    if (pendientes.Count == 0)
                    {
                        transaccion.Rollback();
                        return;
                    }

                    IntegridadTabla integridad = contexto.IntegridadTablas
                        .SingleOrDefault(registro =>
                            registro.NombreTabla == ClienteDvvService.NombreTabla);

                    if (integridad == null ||
                        !ClienteDvvService.EsValido(clientes, integridad.DVV))
                    {
                        transaccion.Rollback();
                        return;
                    }

                    foreach (Cliente cliente in pendientes)
                    {
                        string plano = ObtenerCuitPlano(cliente.CUIT);

                        if (plano == null)
                        {
                            transaccion.Rollback();
                            return;
                        }

                        cliente.CUIT = CryptographyService.Encrypt(plano);
                        ClienteDhvAsignar(cliente);
                    }

                    contexto.SaveChanges();

                    integridad.DVV = ClienteDvvService.Calcular(clientes);
                    integridad.FechaActualizacion = DateTime.Now;
                    contexto.SaveChanges();

                    transaccion.Commit();
                }
                catch
                {
                    transaccion.Rollback();
                    throw;
                }
            }
        }

        private static void ClienteDhvAsignar(Cliente cliente)
        {
            ClienteDvhService.Asignar(cliente);
        }

        /// <summary>
        /// Devuelve el CUIT legible de un valor guardado o null si no puede
        /// descifrarse con la clave actual.
        /// </summary>
        private static string ObtenerCuitPlano(string almacenado)
        {
            string normalizado = almacenado
                .Replace("-", string.Empty)
                .Replace(" ", string.Empty);

            if (normalizado.Length == 11 && normalizado.All(char.IsDigit))
            {
                return normalizado;
            }

            try
            {
                return CryptographyService.Decrypt(almacenado);
            }
            catch (CryptographicException)
            {
                return null;
            }
        }

        /// <summary>
        /// Genera el DVH de los clientes creados antes
        /// de incorporar el control de integridad.
        /// </summary>
        private static void MigrarDvhClientesExistentes()
        {
            using (EdelstahlNegocioContext contexto =
                new EdelstahlNegocioContext())
            {
                List<Cliente> clientesSinDvh =
                    contexto.Clientes
                        .Where(
                            cliente =>
                                cliente.DVH == null ||
                                cliente.DVH == string.Empty)
                        .ToList();

                if (clientesSinDvh.Count == 0)
                {
                    return;
                }

                foreach (Cliente cliente
                    in clientesSinDvh)
                {
                    ClienteDvhService.Asignar(
                        cliente);
                }

                contexto.SaveChanges();
            }
        }

        /// <summary>
        /// Crea el primer DVV de Clientes cuando todavía
        /// no existe o verifica el valor ya almacenado.
        /// </summary>
        private static void InicializarOVerificarDvvClientes()
        {
            using (EdelstahlNegocioContext contexto =
                new EdelstahlNegocioContext())
            {
                List<Cliente> clientes =
                    contexto.Clientes
                        .AsNoTracking()
                        .OrderBy(
                            cliente =>
                                cliente.Id)
                        .ToList();

                string dvvCalculado =
                    ClienteDvvService.Calcular(
                        clientes);

                IntegridadTabla integridad =
                    contexto.IntegridadTablas
                        .SingleOrDefault(
                            registro =>
                                registro.NombreTabla ==
                                ClienteDvvService.NombreTabla);

                /*
                 * Si todavía no existe un DVV almacenado,
                 * se crea el registro inicial.
                 */
                if (integridad == null)
                {
                    integridad =
                        new IntegridadTabla
                        {
                            NombreTabla =
                                ClienteDvvService.NombreTabla,

                            DVV =
                                dvvCalculado,

                            FechaActualizacion =
                                DateTime.Now
                        };

                    contexto.IntegridadTablas.Add(
                        integridad);

                    contexto.SaveChanges();

                    return;
                }

                /*
                 * En los siguientes inicios, el valor
                 * calculado debe coincidir con el DVV
                 * almacenado.
                 */
                bool integridadValida =
                    ClienteDvvService.EsValido(
                        clientes,
                        integridad.DVV);

                if (!integridadValida)
                {
                    throw new CryptographicException(
                        "Se detectó una alteración en el " +
                        "conjunto de registros de la tabla " +
                        "Clientes. El DVV almacenado no " +
                        "coincide con el valor calculado.");
                }
            }
        }
    }
}
