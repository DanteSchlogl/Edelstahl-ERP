using System;
using System.Collections.Generic;
using System.Linq;
using Edelstahl.DAL.Interfaces;
using Edelstahl.Domain.Comercial;

namespace Edelstahl.DAL.Implementations.Memory
{
    /// <summary>
    /// Implementación temporal del repositorio de clientes.
    /// Mantiene la información en memoria mientras la aplicación está abierta.
    /// </summary>
    public sealed class ClienteRepositoryMemory : IClienteRepository
    {
        private readonly List<Cliente> _clientes;

        public ClienteRepositoryMemory()
        {
            _clientes = new List<Cliente>();

            CargarClientesDemostracion();
        }


        private void CargarClientesDemostracion()
        {
            _clientes.Add(
                new Cliente
                {
                    CUIT = "30667788992",
                    RazonSocial = "Soluciones Sanitarias Norte SRL",
                    Email = "compras@sanitariasnorte.test",
                    Telefono = "1144556677",
                    Localidad = "Vicente Lopez",
                    Provincia = "Buenos Aires",
                    CodigoPostal = "1638",
                    DireccionFacturacion = "Av. del Libertador 2500",
                    DireccionEntrega = "Av. del Libertador 2500",
                    LimiteCredito = 1500000m,
                    DeudaActual = 0m,
                    Activo = true
                });

            _clientes.Add(
                new Cliente
                {
                    CUIT = "30334455662",
                    RazonSocial = "Automatizacion Sanitaria Central SRL",
                    Email = "compras@sanitariacentral.test",
                    Telefono = "1144226688",
                    Localidad = "San Martin",
                    Provincia = "Buenos Aires",
                    CodigoPostal = "1650",
                    DireccionFacturacion = "Calle Industrial 1450",
                    DireccionEntrega = "Parque Industrial San Martin",
                    LimiteCredito = 7000000m,
                    DeudaActual = 0m,
                    Activo = true
                });

            _clientes.Add(
                new Cliente
                {
                    CUIT = "30445566778",
                    RazonSocial = "Equipos Sanitarios Patagonicos SRL",
                    Email = "compras@equipospatagonicos.test",
                    Telefono = "1144558899",
                    Localidad = "Pilar",
                    Provincia = "Buenos Aires",
                    CodigoPostal = "1629",
                    DireccionFacturacion = "Ruta Industrial 8 Km 52",
                    DireccionEntrega = "Parque Industrial Pilar",
                    LimiteCredito = 7000000m,
                    DeudaActual = 0m,
                    Activo = true
                });

            _clientes.Add(
                new Cliente
                {
                    CUIT = "30889977663",
                    RazonSocial = "Ingenieria de Procesos del Sur SRL",
                    Email = "compras@procesosdelsur.test",
                    Telefono = "1155448899",
                    Localidad = "Pilar",
                    Provincia = "Buenos Aires",
                    CodigoPostal = "1629",
                    DireccionFacturacion = "Colectora Industrial 750",
                    DireccionEntrega = "Deposito Industrial 12",
                    LimiteCredito = 6000000m,
                    DeudaActual = 0m,
                    Activo = true
                });

            _clientes.Add(
    new Cliente
    {
        CUIT = "30221100998",
        RazonSocial = "Cliente con Credito Limitado SRL",
        Email = "pruebas@creditolimitado.test",
        Telefono = "1112340000",
        Localidad = "Tigre",
        Provincia = "Buenos Aires",
        CodigoPostal = "1648",
        DireccionFacturacion = "Domicilio de prueba",
        DireccionEntrega = "Deposito de prueba",
        LimiteCredito = 70000m,
        DeudaActual = 0m,
        Activo = true
    }
    
    
    );
        }   

        


        public void Add(Cliente entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            _clientes.Add(entity);
        }

        public void Update(Cliente entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            Cliente clienteExistente = GetById(entity.Id);

            if (clienteExistente == null)
            {
                throw new InvalidOperationException(
                    "No se encontró el cliente que se desea modificar.");
            }

            clienteExistente.CUIT = entity.CUIT;
            clienteExistente.Tipo = entity.Tipo;
            clienteExistente.RazonSocial = entity.RazonSocial;
            clienteExistente.DireccionFacturacion = entity.DireccionFacturacion;
            clienteExistente.DireccionEntrega = entity.DireccionEntrega;
            clienteExistente.Localidad = entity.Localidad;
            clienteExistente.Provincia = entity.Provincia;
            clienteExistente.CodigoPostal = entity.CodigoPostal;
            clienteExistente.Email = entity.Email;
            clienteExistente.Telefono = entity.Telefono;
            clienteExistente.LimiteCredito = entity.LimiteCredito;
            clienteExistente.DeudaActual = entity.DeudaActual;
            clienteExistente.Activo = entity.Activo;
            clienteExistente.AutorizadoACredito = entity.AutorizadoACredito;
        }

        public void Delete(Guid id)
        {
            Cliente clienteExistente = GetById(id);

            if (clienteExistente == null)
            {
                throw new InvalidOperationException(
                    "No se encontró el cliente que se desea eliminar.");
            }

            _clientes.Remove(clienteExistente);
        }

        public Cliente GetById(Guid id)
        {
            return _clientes.FirstOrDefault(cliente => cliente.Id == id);
        }

        public List<Cliente> GetAll()
        {
            return new List<Cliente>(_clientes);
        }

        public Cliente GetByCUIT(string cuit)
        {
            return _clientes.FirstOrDefault(
                cliente => string.Equals(
                    cliente.CUIT,
                    cuit,
                    StringComparison.OrdinalIgnoreCase));
        }

        public bool ExistsByCUIT(string cuit)
        {
            return _clientes.Any(
                cliente => string.Equals(
                    cliente.CUIT,
                    cuit,
                    StringComparison.OrdinalIgnoreCase));

        }


    }
}