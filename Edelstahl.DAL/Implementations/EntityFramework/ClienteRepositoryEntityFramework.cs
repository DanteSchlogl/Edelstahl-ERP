using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Security.Cryptography;
using Edelstahl.DAL.EntityFramework;
using Edelstahl.DAL.Factory;
using Edelstahl.DAL.Interfaces;
using Edelstahl.Domain.Comercial;
using Edelstahl.Domain.Integrity;
using Edelstahl.Domain.Security;
using Edelstahl.Services.Cryptography;
using Edelstahl.Services.Integrity;

namespace Edelstahl.DAL.Implementations.EntityFramework
{
    /// <summary>
    /// Persistencia de clientes mediante Entity Framework 6.
    ///
    /// El CUIT se cifra mediante AES antes de almacenarse.
    /// El DVH protege la integridad de cada cliente.
    /// El DVV protege la integridad del conjunto completo.
    /// </summary>
    public class ClienteRepositoryEntityFramework
        : IClienteRepository
    {
        public void Add(
            Cliente entity)
        {
            ValidarCliente(
                entity);

            string cuitNormalizado =
                NormalizarCUIT(
                    entity.CUIT);

            if (ExistsByCUIT(
                cuitNormalizado))
            {
                throw new InvalidOperationException(
                    "Ya existe un cliente con el CUIT ingresado.");
            }

            if (entity.Id == Guid.Empty)
            {
                entity.Id =
                    Guid.NewGuid();
            }

            if (entity.FechaAlta ==
                default(DateTime))
            {
                entity.FechaAlta =
                    DateTime.Now;
            }

            Cliente clientePersistente =
                CrearCopiaParaPersistencia(
                    entity);

            /*
             * El CUIT se cifra antes de calcular el DVH.
             * El hash protege exactamente el valor que
             * será almacenado en SQL Server.
             */
            clientePersistente.CUIT =
                CifrarCUIT(
                    cuitNormalizado);

            ClienteDvhService.Asignar(
                clientePersistente);

            using (EdelstahlNegocioContext context =
                new EdelstahlNegocioContext())
            {
                using (var transaction =
                    context.Database.BeginTransaction())
                {
                    try
                    {
                        VerificarDvvActual(
                            context);

                        context.Clientes.Add(
                            clientePersistente);

                        GuardarClientesYActualizarDvv(
                            context);

                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();

                        throw;
                    }
                }
            }

            entity.CUIT =
                cuitNormalizado;

            entity.DVH =
                clientePersistente.DVH;
        }

        public void Update(
            Cliente entity)
        {
            ValidarCliente(
                entity);

            if (entity.Id == Guid.Empty)
            {
                throw new ArgumentException(
                    "El identificador del cliente no es válido.",
                    nameof(entity));
            }

            string cuitNormalizado =
                NormalizarCUIT(
                    entity.CUIT);

            Cliente clienteConMismoCUIT =
                ObtenerPorCUITInterno(
                    cuitNormalizado,
                    entity.Id);

            if (clienteConMismoCUIT != null)
            {
                throw new InvalidOperationException(
                    "Ya existe otro cliente con el CUIT ingresado.");
            }

            using (EdelstahlNegocioContext context =
                new EdelstahlNegocioContext())
            {
                using (var transaction =
                    context.Database.BeginTransaction())
                {
                    try
                    {
                        VerificarDvvActual(
                            context);

                        Cliente existente =
                            context.Clientes.Find(
                                entity.Id);

                        if (existente == null)
                        {
                            throw new InvalidOperationException(
                                "No se encontró el cliente " +
                                "que se desea modificar.");
                        }

                        VerificarDvhObligatorio(
                            existente);

                        CopiarDatosParaPersistencia(
                            entity,
                            existente,
                            cuitNormalizado);

                        /*
                         * La modificación genera un nuevo
                         * paquete AES y, por tanto, requiere
                         * recalcular el DVH.
                         */
                        ClienteDvhService.Asignar(
                            existente);

                        GuardarClientesYActualizarDvv(
                            context);

                        transaction.Commit();

                        entity.DVH =
                            existente.DVH;
                    }
                    catch
                    {
                        transaction.Rollback();

                        throw;
                    }
                }
            }

            entity.CUIT =
                cuitNormalizado;
        }

        public void Delete(
            Guid id)
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentException(
                    "El identificador del cliente no es válido.",
                    nameof(id));
            }

            using (EdelstahlNegocioContext context =
                new EdelstahlNegocioContext())
            {
                using (var transaction =
                    context.Database.BeginTransaction())
                {
                    try
                    {
                        VerificarDvvActual(
                            context);

                        Cliente existente =
                            context.Clientes.Find(
                                id);

                        if (existente == null)
                        {
                            throw new InvalidOperationException(
                                "No se encontró el cliente " +
                                "que se desea eliminar.");
                        }

                        VerificarDvhObligatorio(
                            existente);

                        /*
                         * La eliminación es lógica.
                         * Como cambia Activo, también debe
                         * recalcularse el DVH.
                         */
                        existente.Activo =
                            false;

                        ClienteDvhService.Asignar(
                            existente);

                        GuardarClientesYActualizarDvv(
                            context);

                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();

                        throw;
                    }
                }
            }
        }

        public Cliente GetById(
            Guid id)
        {
            if (id == Guid.Empty)
            {
                return null;
            }

            using (EdelstahlNegocioContext context =
                new EdelstahlNegocioContext())
            {
                VerificarDvvActual(
                    context);

                Cliente cliente =
                    context.Clientes
                        .AsNoTracking()
                        .FirstOrDefault(
                            item =>
                                item.Id == id);

                PrepararClienteRecuperado(
                    cliente);

                return cliente;
            }
        }

        public List<Cliente> GetAll()
        {
            using (EdelstahlNegocioContext context =
                new EdelstahlNegocioContext())
            {
                VerificarDvvActual(
                    context);

                List<Cliente> clientes =
                    context.Clientes
                        .AsNoTracking()
                        .OrderBy(
                            cliente =>
                                cliente.RazonSocial)
                        .ToList();

                List<Cliente> legibles =
                    new List<Cliente>();

                foreach (Cliente cliente
                    in clientes)
                {
                    try
                    {
                        PrepararClienteRecuperado(
                            cliente);

                        legibles.Add(
                            cliente);
                    }
                    catch (CryptographicException ex)
                    {
                        /*
                         * Un registro alterado o ilegible no debe impedir
                         * el uso del resto de los clientes: se omite del
                         * listado y se deja constancia en la bitácora.
                         */
                        RegistrarClienteIlegible(
                            cliente,
                            ex);
                    }
                }

                return legibles;
            }
        }

        private static void RegistrarClienteIlegible(
            Cliente cliente,
            Exception error)
        {
            try
            {
                FactoryDataAccess.BitacoraRepository.Add(
                    new RegistroBitacora
                    {
                        Id = Guid.NewGuid(),
                        FechaHora = DateTime.Now,
                        NombreUsuario = "SISTEMA",
                        Modulo = "Clientes",
                        Accion = "Registro omitido por integridad",
                        Entidad = "Cliente",
                        EntidadId = cliente.Id,
                        Descripcion = "El cliente '" + cliente.RazonSocial +
                            "' no superó la verificación de integridad y se omitió del listado.",
                        Resultado = "Fallido",
                        TipoEvento = "Seguridad",
                        NombreEquipo = Environment.MachineName,
                        DireccionEquipo = string.Empty,
                        DetalleTecnico = error.Message
                    });
            }
            catch (Exception)
            {
                // La bitácora no debe impedir el listado.
            }
        }

        public Cliente GetByCUIT(
            string cuit)
        {
            string cuitNormalizado =
                NormalizarCUIT(
                    cuit);

            if (string.IsNullOrWhiteSpace(
                cuitNormalizado))
            {
                return null;
            }

            return ObtenerPorCUITInterno(
                cuitNormalizado,
                null);
        }

        public bool ExistsByCUIT(
            string cuit)
        {
            string cuitNormalizado =
                NormalizarCUIT(
                    cuit);

            if (string.IsNullOrWhiteSpace(
                cuitNormalizado))
            {
                return false;
            }

            return ObtenerPorCUITInterno(
                cuitNormalizado,
                null) != null;
        }

        private static Cliente ObtenerPorCUITInterno(
            string cuitNormalizado,
            Guid? idExcluido)
        {
            using (EdelstahlNegocioContext context =
                new EdelstahlNegocioContext())
            {
                VerificarDvvActual(
                    context);

                List<Cliente> clientes =
                    context.Clientes
                        .AsNoTracking()
                        .ToList();

                foreach (Cliente cliente
                    in clientes)
                {
                    VerificarDvhObligatorio(
                        cliente);

                    string cuitLegible =
                        ObtenerCUITLegible(
                            cliente.CUIT);

                    string cuitClienteNormalizado =
                        NormalizarCUIT(
                            cuitLegible);

                    bool mismoCUIT =
                        string.Equals(
                            cuitClienteNormalizado,
                            cuitNormalizado,
                            StringComparison.Ordinal);

                    bool registroPermitido =
                        !idExcluido.HasValue ||
                        cliente.Id !=
                        idExcluido.Value;

                    if (mismoCUIT &&
                        registroPermitido)
                    {
                        cliente.CUIT =
                            cuitLegible;

                        return cliente;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Guarda las modificaciones legítimas sobre
        /// clientes y actualiza el DVV dentro de la
        /// misma transacción.
        /// </summary>
        private static void GuardarClientesYActualizarDvv(
            EdelstahlNegocioContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(
                    nameof(context));
            }

            /*
             * Primero se guardan los cambios y DVH
             * individuales para que la consulta del
             * conjunto refleje el estado definitivo.
             */
            context.SaveChanges();

            List<Cliente> clientes =
                context.Clientes
                    .AsNoTracking()
                    .OrderBy(
                        cliente =>
                            cliente.Id)
                    .ToList();

            foreach (Cliente cliente
                in clientes)
            {
                VerificarDvhObligatorio(
                    cliente);
            }

            string nuevoDvv =
                ClienteDvvService.Calcular(
                    clientes);

            IntegridadTabla integridad =
                context.IntegridadTablas
                    .SingleOrDefault(
                        registro =>
                            registro.NombreTabla ==
                            ClienteDvvService.NombreTabla);

            if (integridad == null)
            {
                integridad =
                    new IntegridadTabla
                    {
                        NombreTabla =
                            ClienteDvvService.NombreTabla,

                        DVV =
                            nuevoDvv,

                        FechaActualizacion =
                            DateTime.Now
                    };

                context.IntegridadTablas.Add(
                    integridad);
            }
            else
            {
                integridad.DVV =
                    nuevoDvv;

                integridad.FechaActualizacion =
                    DateTime.Now;
            }

            context.SaveChanges();
        }

        /// <summary>
        /// Comprueba que el conjunto de clientes no
        /// haya sido alterado fuera de Edelstahl ERP.
        /// </summary>
        private static void VerificarDvvActual(
            EdelstahlNegocioContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(
                    nameof(context));
            }

            IntegridadTabla integridad =
                context.IntegridadTablas
                    .AsNoTracking()
                    .SingleOrDefault(
                        registro =>
                            registro.NombreTabla ==
                            ClienteDvvService.NombreTabla);

            if (integridad == null ||
                string.IsNullOrWhiteSpace(
                    integridad.DVV))
            {
                throw new CryptographicException(
                    "No se encontró un DVV válido para " +
                    "la tabla Clientes.");
            }

            List<Cliente> clientes =
                context.Clientes
                    .AsNoTracking()
                    .OrderBy(
                        cliente =>
                            cliente.Id)
                    .ToList();

            foreach (Cliente cliente
                in clientes)
            {
                VerificarDvhObligatorio(
                    cliente);
            }

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

        private static Cliente CrearCopiaParaPersistencia(
            Cliente origen)
        {
            return new Cliente
            {
                Id =
                    origen.Id,

                CUIT =
                    origen.CUIT,

                Tipo =
                    origen.Tipo,

                RazonSocial =
                    Limpiar(
                        origen.RazonSocial),

                DireccionFacturacion =
                    Limpiar(
                        origen.DireccionFacturacion),

                DireccionEntrega =
                    Limpiar(
                        origen.DireccionEntrega),

                Localidad =
                    Limpiar(
                        origen.Localidad),

                Provincia =
                    Limpiar(
                        origen.Provincia),

                CodigoPostal =
                    Limpiar(
                        origen.CodigoPostal),

                Email =
                    Limpiar(
                        origen.Email),

                Telefono =
                    Limpiar(
                        origen.Telefono),

                LimiteCredito =
                    origen.LimiteCredito,

                DeudaActual =
                    origen.DeudaActual,

                Activo =
                    origen.Activo,

                AutorizadoACredito =
                    origen.AutorizadoACredito,

                FechaAlta =
                    origen.FechaAlta,

                DVH =
                    string.Empty
            };
        }

        private static void CopiarDatosParaPersistencia(
            Cliente origen,
            Cliente destino,
            string cuitNormalizado)
        {
            destino.CUIT =
                CifrarCUIT(
                    cuitNormalizado);

            destino.Tipo =
                origen.Tipo;

            destino.RazonSocial =
                Limpiar(
                    origen.RazonSocial);

            destino.DireccionFacturacion =
                Limpiar(
                    origen.DireccionFacturacion);

            destino.DireccionEntrega =
                Limpiar(
                    origen.DireccionEntrega);

            destino.Localidad =
                Limpiar(
                    origen.Localidad);

            destino.Provincia =
                Limpiar(
                    origen.Provincia);

            destino.CodigoPostal =
                Limpiar(
                    origen.CodigoPostal);

            destino.Email =
                Limpiar(
                    origen.Email);

            destino.Telefono =
                Limpiar(
                    origen.Telefono);

            destino.LimiteCredito =
                origen.LimiteCredito;

            destino.DeudaActual =
                origen.DeudaActual;

            destino.Activo =
                origen.Activo;

            destino.AutorizadoACredito =
                origen.AutorizadoACredito;

            destino.FechaAlta =
                origen.FechaAlta;
        }

        private static void PrepararClienteRecuperado(
            Cliente cliente)
        {
            if (cliente == null)
            {
                return;
            }

            /*
             * El DVH se verifica antes de reemplazar
             * el CUIT cifrado por su valor legible.
             */
            VerificarDvhObligatorio(
                cliente);

            cliente.CUIT =
                ObtenerCUITLegible(
                    cliente.CUIT);
        }

        private static void VerificarDvhObligatorio(
            Cliente cliente)
        {
            if (cliente == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(
                cliente.DVH))
            {
                throw new CryptographicException(
                    "El cliente '" +
                    cliente.RazonSocial +
                    "' no posee un DVH válido.");
            }

            if (!ClienteDvhService.EsValido(
                cliente))
            {
                throw new CryptographicException(
                    "Se detectó una alteración en los " +
                    "datos del cliente '" +
                    cliente.RazonSocial +
                    "'. El DVH almacenado no coincide " +
                    "con el valor calculado.");
            }
        }

        private static string CifrarCUIT(
            string cuitNormalizado)
        {
            if (string.IsNullOrWhiteSpace(
                cuitNormalizado))
            {
                return string.Empty;
            }

            return CryptographyService.Encrypt(
                cuitNormalizado);
        }

        private static string ObtenerCUITLegible(
            string valorAlmacenado)
        {
            if (string.IsNullOrWhiteSpace(
                valorAlmacenado))
            {
                return string.Empty;
            }

            /*
             * Compatibilidad temporal con CUIT
             * almacenados antes de incorporar AES.
             */
            if (EsCUITEnTextoPlano(
                valorAlmacenado))
            {
                return NormalizarCUIT(
                    valorAlmacenado);
            }

            try
            {
                return CryptographyService.Decrypt(
                    valorAlmacenado);
            }
            catch (CryptographicException ex)
            {
                throw new CryptographicException(
                    "No fue posible verificar o " +
                    "descifrar el CUIT almacenado.",
                    ex);
            }
        }

        private static bool EsCUITEnTextoPlano(
            string valor)
        {
            string normalizado =
                NormalizarCUIT(
                    valor);

            if (normalizado.Length != 11)
            {
                return false;
            }

            foreach (char caracter
                in normalizado)
            {
                if (!char.IsDigit(
                    caracter))
                {
                    return false;
                }
            }

            return true;
        }

        private static void ValidarCliente(
            Cliente cliente)
        {
            if (cliente == null)
            {
                throw new ArgumentNullException(
                    nameof(cliente));
            }

            if (string.IsNullOrWhiteSpace(
                cliente.CUIT))
            {
                throw new InvalidOperationException(
                    "El CUIT es obligatorio.");
            }

            if (string.IsNullOrWhiteSpace(
                cliente.RazonSocial))
            {
                throw new InvalidOperationException(
                    "La razón social es obligatoria.");
            }

            if (cliente.LimiteCredito < 0m ||
                cliente.DeudaActual < 0m)
            {
                throw new InvalidOperationException(
                    "El límite de crédito y la deuda " +
                    "no pueden ser negativos.");
            }
        }

        private static string NormalizarCUIT(
            string cuit)
        {
            return string.IsNullOrWhiteSpace(
                cuit)
                ? string.Empty
                : cuit
                    .Trim()
                    .Replace(
                        "-",
                        string.Empty)
                    .Replace(
                        " ",
                        string.Empty)
                    .Replace(
                        ".",
                        string.Empty);
        }

        private static string Limpiar(
            string valor)
        {
            return string.IsNullOrWhiteSpace(
                valor)
                ? string.Empty
                : valor.Trim();
        }
    }
}