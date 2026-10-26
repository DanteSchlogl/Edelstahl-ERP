using System;
using System.Collections.Generic;
using Edelstahl.BLL.Exceptions.Commercial;
using Edelstahl.BLL.Validators;
using Edelstahl.DAL.Factory;
using Edelstahl.DAL.Interfaces;
using Edelstahl.Domain.Comercial;
using Edelstahl.BLL.Infrastructure;

namespace Edelstahl.BLL.Services
{
    /// <summary>
    /// Coordina las operaciones y reglas de negocio
    /// relacionadas con los clientes.
    /// </summary>
    public class ClienteService
    {
        private readonly IClienteRepository _clienteRepository;

        public ClienteService()
        {
            _clienteRepository =
                FactoryDataAccess.ClienteRepository;
        }

        public ClienteService(
            IClienteRepository clienteRepository)
        {
            _clienteRepository = clienteRepository
                ?? throw new ArgumentNullException(
                    nameof(clienteRepository));
        }

        public Cliente Registrar(Cliente cliente)
        {
            ClienteValidator.Validate(cliente);

            cliente.CUIT =
                ClienteValidator.NormalizeCUIT(cliente.CUIT);

            if (_clienteRepository.ExistsByCUIT(cliente.CUIT))
            {
                throw new ClienteDuplicadoException(cliente.CUIT);
            }

            cliente.RazonSocial =
                cliente.RazonSocial.Trim();

            cliente.Email =
                string.IsNullOrWhiteSpace(cliente.Email)
                    ? string.Empty
                    : cliente.Email.Trim();

            cliente.Telefono =
                string.IsNullOrWhiteSpace(cliente.Telefono)
                    ? string.Empty
                    : cliente.Telefono.Trim();

            cliente.Activo = true;
            cliente.DeudaActual = 0m;

            if (cliente.FechaAlta == default(DateTime))
            {
                cliente.FechaAlta = DateTime.Now;
            }

            _clienteRepository.Add(cliente);

            return cliente;
        }

        public Cliente ObtenerPorId(Guid id)
        {
            if (id == Guid.Empty)
            {
                throw Mensajes.Regla("The customer identifier is not valid.");
            }

            return _clienteRepository.GetById(id);
        }

        public Cliente ObtenerPorCUIT(string cuit)
        {
            ClienteValidator.ValidateCUIT(cuit);

            string cuitNormalizado =
                ClienteValidator.NormalizeCUIT(cuit);

            return _clienteRepository.GetByCUIT(
                cuitNormalizado);
        }

        public List<Cliente> ObtenerTodos()
        {
            return _clienteRepository.GetAll();
        }

        public bool ExistePorCUIT(string cuit)
        {
            ClienteValidator.ValidateCUIT(cuit);

            string cuitNormalizado =
                ClienteValidator.NormalizeCUIT(cuit);

            return _clienteRepository.ExistsByCUIT(
                cuitNormalizado);
        }

        public void Modificar(Cliente cliente)
        {
            ClienteValidator.Validate(cliente);

            cliente.CUIT =
                ClienteValidator.NormalizeCUIT(cliente.CUIT);

            Cliente clienteConMismoCUIT =
                _clienteRepository.GetByCUIT(cliente.CUIT);

            if (clienteConMismoCUIT != null &&
                clienteConMismoCUIT.Id != cliente.Id)
            {
                throw new ClienteDuplicadoException(
                    cliente.CUIT);
            }

            _clienteRepository.Update(cliente);
        }

        public void Eliminar(Guid id)
        {
            if (id == Guid.Empty)
            {
                throw Mensajes.Regla("The customer identifier is not valid.");
            }

            _clienteRepository.Delete(id);
        }
    }
}
