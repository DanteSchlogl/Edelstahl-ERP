using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Edelstahl.BLL.Infrastructure;
using Edelstahl.Domain.Security;

namespace Edelstahl.BLL.Services
{
    /// <summary>Un pedido de ayuda hecho por un usuario, con su estado (pendiente o atendido).</summary>
    public class PedidoAyuda
    {
        public Guid Id { get; set; }
        public DateTime FechaHora { get; set; }
        public string Usuario { get; set; }
        public string Perfil { get; set; }
        public string Tema { get; set; }
        public string Detalle { get; set; }
        public bool Atendido { get; set; }
        public DateTime? FechaAtencion { get; set; }
        public string AtendidoPor { get; set; }
    }

    /// <summary>
    /// Pedidos de ayuda al administrador: el usuario elige un tema segun su perfil
    /// y el pedido queda registrado en la bitacora para que el administrador lo vea.
    /// </summary>
    public class AyudaService
    {
        // OJO: la bitacora de SQL Server rechaza (en silencio) los registros cuyo resultado no sea
        // Correcto, Fallido o Advertencia, o cuyo tipo no sea Informacion, Seguridad, Error o Auditoria.
        private const string ModuloSoporte = "Soporte";
        private const string AccionPedido = "Pedido de ayuda";
        private const string AccionAtendido = "Pedido de ayuda atendido";

        /// <summary>Temas de ayuda que se ofrecen segun el perfil (texto en ingles; la pantalla los traduce).</summary>
        public static List<string> TemasPara(string rol)
        {
            List<string> temas = new List<string>();

            switch (rol)
            {
                case NombresRol.Vendedor:
                    temas.Add("I cannot create or confirm a quotation or an order");
                    temas.Add("A customer or a product is missing or wrong");
                    temas.Add("I cannot issue an invoice or register a collection");
                    break;
                case NombresRol.Produccion:
                    temas.Add("I cannot start or finish a manufacturing order");
                    temas.Add("Materials are missing for an order");
                    temas.Add("A quality check problem");
                    break;
                case NombresRol.Deposito:
                    temas.Add("The stock does not match");
                    temas.Add("I cannot receive a purchase or register a stock movement");
                    break;
                case NombresRol.Compras:
                    temas.Add("I cannot create a purchase order");
                    temas.Add("A supplier is missing or wrong");
                    break;
                case NombresRol.Logistica:
                    temas.Add("I cannot create a delivery note or a route");
                    temas.Add("A return needs approval");
                    break;
                case NombresRol.Supervisor:
                    temas.Add("I need a report or an access I do not have");
                    temas.Add("A number in a report looks wrong");
                    break;
                default:
                    temas.Add("I need a permission or a role change");
                    break;
            }

            temas.Add("I cannot sign in or my user is locked");
            temas.Add("Other");

            return temas;
        }

        /// <summary>Deja el pedido en la bitacora con el usuario, su perfil y el tema elegido.</summary>
        public void RegistrarPedido(string tema, string detalle)
        {
            if (!SesionActual.EstaIniciada)
            {
                throw Mensajes.Regla("You must sign in first.");
            }

            if (string.IsNullOrWhiteSpace(tema))
            {
                throw Mensajes.Regla("Choose a topic.");
            }

            string descripcion = "Pedido de ayuda de " + SesionActual.NombreUsuario +
                " (" + SesionActual.NombreRol + "): " + tema.Trim();

            if (!string.IsNullOrWhiteSpace(detalle))
            {
                descripcion += ". " + detalle.Trim();
            }

            new BitacoraService().Registrar(
                ModuloSoporte, AccionPedido, "Usuario", SesionActual.Usuario.Id,
                descripcion, "Correcto", "Informacion", string.Empty, SesionActual.NombreUsuario);
        }

        /// <summary>
        /// Todos los pedidos de ayuda, del mas nuevo al mas viejo. Un pedido esta atendido
        /// cuando existe un registro "Pedido de ayuda atendido" que apunta a el.
        /// </summary>
        public List<PedidoAyuda> ObtenerPedidos()
        {
            Autorizacion.Exigir(Permiso.UsuariosAdministrar);

            List<RegistroBitacora> registros = new BitacoraService().Consultar(null, null, null, ModuloSoporte, null, null);

            Dictionary<Guid, RegistroBitacora> atendidos = registros
                .Where(r => r.Accion == AccionAtendido && r.EntidadId.HasValue)
                .GroupBy(r => r.EntidadId.Value)
                .ToDictionary(g => g.Key, g => g.OrderBy(x => x.FechaHora).First());

            List<PedidoAyuda> pedidos = new List<PedidoAyuda>();

            foreach (RegistroBitacora r in registros.Where(x => x.Accion == AccionPedido))
            {
                // "Pedido de ayuda de <usuario> (<perfil>): <tema>. <detalle>"
                Match m = Regex.Match(r.Descripcion ?? string.Empty, @"^Pedido de ayuda de (.+?) \((.*?)\): (.*)$", RegexOptions.Singleline);
                string resto = m.Success ? m.Groups[3].Value : (r.Descripcion ?? string.Empty);
                int corte = resto.IndexOf(". ", StringComparison.Ordinal);

                RegistroBitacora atencion;
                bool atendido = atendidos.TryGetValue(r.Id, out atencion);

                pedidos.Add(new PedidoAyuda
                {
                    Id = r.Id,
                    FechaHora = r.FechaHora,
                    Usuario = m.Success ? m.Groups[1].Value : r.NombreUsuario,
                    Perfil = m.Success ? m.Groups[2].Value : string.Empty,
                    Tema = corte >= 0 ? resto.Substring(0, corte) : resto.TrimEnd('.'),
                    Detalle = corte >= 0 ? resto.Substring(corte + 2) : string.Empty,
                    Atendido = atendido,
                    FechaAtencion = atendido ? (DateTime?)atencion.FechaHora : null,
                    AtendidoPor = atendido ? atencion.NombreUsuario : string.Empty
                });
            }

            return pedidos.OrderByDescending(p => p.FechaHora).ToList();
        }

        /// <summary>Cantidad de pedidos sin atender (0 si el usuario no es administrador).</summary>
        public int ContarPendientes()
        {
            if (!SesionActual.Tiene(Permiso.UsuariosAdministrar))
            {
                return 0;
            }

            return ObtenerPedidos().Count(p => !p.Atendido);
        }

        /// <summary>El administrador marca un pedido como atendido; queda registrado en la bitacora.</summary>
        public void MarcarAtendido(Guid pedidoId)
        {
            Autorizacion.Exigir(Permiso.UsuariosAdministrar);

            PedidoAyuda pedido = ObtenerPedidos().FirstOrDefault(p => p.Id == pedidoId);

            if (pedido == null)
            {
                throw Mensajes.Regla("The help request was not found.");
            }

            if (pedido.Atendido)
            {
                throw Mensajes.Regla("The help request was already handled.");
            }

            new BitacoraService().Registrar(
                ModuloSoporte, AccionAtendido, "Pedido", pedidoId,
                "El administrador atendió el pedido de ayuda de " + pedido.Usuario + ": " + pedido.Tema + ".",
                "Correcto", "Auditoria", string.Empty, SesionActual.NombreUsuario);
        }
    }
}
