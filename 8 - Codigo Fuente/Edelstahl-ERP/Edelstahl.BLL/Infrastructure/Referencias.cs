using System;
using System.Collections.Generic;
using System.Linq;
using Edelstahl.DAL.Factory;
using Edelstahl.Domain.Comercial;

namespace Edelstahl.BLL.Infrastructure
{
    /// <summary>
    /// Datos de referencia (clientes y pedidos) que los módulos de
    /// producción, finanzas y logística muestran junto a sus propios registros.
    /// </summary>
    public static class Referencias
    {
        public static Dictionary<Guid, string> NombresClientes()
        {
            return FactoryDataAccess.ClienteRepository.GetAll()
                .ToDictionary(c => c.Id, c => c.RazonSocial);
        }

        public static Dictionary<Guid, Cliente> Clientes()
        {
            return FactoryDataAccess.ClienteRepository.GetAll().ToDictionary(c => c.Id);
        }

        public static Dictionary<Guid, Pedido> Pedidos()
        {
            return FactoryDataAccess.PedidoRepository.GetAll().ToDictionary(p => p.Id);
        }

        public static string Nombre(Dictionary<Guid, string> nombres, Guid id)
        {
            string nombre;
            return nombres.TryGetValue(id, out nombre) ? nombre : string.Empty;
        }
    }
}
