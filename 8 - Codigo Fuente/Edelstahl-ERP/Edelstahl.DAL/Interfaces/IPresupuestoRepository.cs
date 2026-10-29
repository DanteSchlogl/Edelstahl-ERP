using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Edelstahl.Domain.Comercial;

namespace Edelstahl.DAL.Interfaces
{
    /// <summary>
    /// Define las operaciones de persistencia
    /// relacionadas con los presupuestos.
    /// </summary>
    public interface IPresupuestoRepository
        : IGenericRepository<Presupuesto>
    {
        List<Presupuesto> GetByClienteId(Guid clienteId);

        Presupuesto GetByNumero(string numero);

        bool ExistsByNumero(string numero);
    }
}
