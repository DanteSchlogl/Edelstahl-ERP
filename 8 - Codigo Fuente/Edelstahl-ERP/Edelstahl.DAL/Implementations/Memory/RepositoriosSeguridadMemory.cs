using System;
using System.Collections.Generic;
using System.Linq;
using Edelstahl.DAL.Interfaces;
using Edelstahl.Domain.Security;

namespace Edelstahl.DAL.Implementations.Memory
{
    /// <summary>
    /// Bitácora en memoria para el modo demostración. El acceso está
    /// protegido porque se escribe desde varios hilos.
    /// </summary>
    public sealed class BitacoraRepositoryMemory : IBitacoraRepository
    {
        private static readonly object Candado = new object();
        private static readonly List<RegistroBitacora> Registros = new List<RegistroBitacora>();

        public void Add(RegistroBitacora entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            lock (Candado)
            {
                Registros.Add(entity);
            }
        }

        public void Update(RegistroBitacora entity)
        {
            throw new InvalidOperationException("Los registros de la bitácora no pueden modificarse.");
        }

        public void Delete(Guid id)
        {
            throw new InvalidOperationException("Los registros de la bitácora no pueden eliminarse.");
        }

        public RegistroBitacora GetById(Guid id)
        {
            lock (Candado)
            {
                return Registros.FirstOrDefault(r => r.Id == id);
            }
        }

        public List<RegistroBitacora> GetAll()
        {
            lock (Candado)
            {
                return Registros.OrderByDescending(r => r.FechaHora).ToList();
            }
        }

        public List<RegistroBitacora> GetByUsuarioId(Guid usuarioId)
        {
            lock (Candado)
            {
                return Registros.Where(r => r.UsuarioId == usuarioId)
                    .OrderByDescending(r => r.FechaHora).ToList();
            }
        }

        public List<RegistroBitacora> GetByFecha(DateTime desde, DateTime hasta)
        {
            lock (Candado)
            {
                return Registros.Where(r => r.FechaHora >= desde && r.FechaHora <= hasta)
                    .OrderByDescending(r => r.FechaHora).ToList();
            }
        }

        public List<RegistroBitacora> GetByModulo(string modulo)
        {
            lock (Candado)
            {
                return Registros.Where(r => string.Equals(r.Modulo, modulo, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(r => r.FechaHora).ToList();
            }
        }

        public List<RegistroBitacora> GetByTipoEvento(string tipoEvento)
        {
            lock (Candado)
            {
                return Registros.Where(r => string.Equals(r.TipoEvento, tipoEvento, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(r => r.FechaHora).ToList();
            }
        }
    }

    public sealed class RolRepositoryMemory : IRolRepository
    {
        private static readonly object Candado = new object();
        private static readonly List<Rol> Roles = new List<Rol>();

        public void Add(Rol entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            lock (Candado)
            {
                if (Roles.Any(r => string.Equals(r.Nombre, entity.Nombre, StringComparison.OrdinalIgnoreCase)))
                {
                    throw new InvalidOperationException("Ya existe un rol con ese nombre.");
                }

                Roles.Add(entity);
            }
        }

        public void Update(Rol entity)
        {
            lock (Candado)
            {
                Rol actual = Roles.FirstOrDefault(r => r.Id == entity.Id);

                if (actual == null)
                {
                    throw new InvalidOperationException("No se encontró el rol.");
                }

                actual.Nombre = entity.Nombre;
                actual.Descripcion = entity.Descripcion;
                actual.Activo = entity.Activo;
            }
        }

        public void Delete(Guid id)
        {
            lock (Candado)
            {
                Rol actual = Roles.FirstOrDefault(r => r.Id == id);

                if (actual != null)
                {
                    actual.Activo = false;
                }
            }
        }

        public Rol GetById(Guid id)
        {
            lock (Candado)
            {
                return Roles.FirstOrDefault(r => r.Id == id);
            }
        }

        public List<Rol> GetAll()
        {
            lock (Candado)
            {
                return Roles.OrderBy(r => r.Nombre).ToList();
            }
        }

        public Rol GetByNombre(string nombre)
        {
            lock (Candado)
            {
                return Roles.FirstOrDefault(
                    r => string.Equals(r.Nombre, nombre, StringComparison.OrdinalIgnoreCase));
            }
        }

        public bool ExistsByNombre(string nombre)
        {
            return GetByNombre(nombre) != null;
        }
    }

    public sealed class UsuarioRepositoryMemory : IUsuarioRepository
    {
        private static readonly object Candado = new object();
        private static readonly List<Usuario> Usuarios = new List<Usuario>();

        public void Add(Usuario entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            lock (Candado)
            {
                if (Usuarios.Any(u => string.Equals(u.NombreUsuario, entity.NombreUsuario,
                    StringComparison.OrdinalIgnoreCase)))
                {
                    throw new InvalidOperationException("Ya existe un usuario con ese nombre.");
                }

                Usuarios.Add(entity);
            }
        }

        public void Update(Usuario entity)
        {
            lock (Candado)
            {
                Usuario actual = Usuarios.FirstOrDefault(u => u.Id == entity.Id);

                if (actual == null)
                {
                    throw new InvalidOperationException("No se encontró el usuario.");
                }

                actual.RolId = entity.RolId;
                actual.Rol = entity.Rol;
                actual.NombreCompleto = entity.NombreCompleto;
                actual.Email = entity.Email;
                actual.PasswordHash = entity.PasswordHash;
                actual.PasswordSalt = entity.PasswordSalt;
                actual.Iteraciones = entity.Iteraciones;
                actual.Activo = entity.Activo;
                actual.DebeCambiarPassword = entity.DebeCambiarPassword;
                actual.IntentosFallidos = entity.IntentosFallidos;
                actual.BloqueadoHasta = entity.BloqueadoHasta;
                actual.UltimoAcceso = entity.UltimoAcceso;
                actual.PreguntaSeguridad = entity.PreguntaSeguridad;
                actual.RespuestaHash = entity.RespuestaHash;
                actual.RespuestaSalt = entity.RespuestaSalt;
            }
        }

        public void Delete(Guid id)
        {
            lock (Candado)
            {
                Usuario actual = Usuarios.FirstOrDefault(u => u.Id == id);

                if (actual != null)
                {
                    actual.Activo = false;
                }
            }
        }

        public Usuario GetById(Guid id)
        {
            lock (Candado)
            {
                return Usuarios.FirstOrDefault(u => u.Id == id);
            }
        }

        public List<Usuario> GetAll()
        {
            lock (Candado)
            {
                return Usuarios.OrderBy(u => u.NombreUsuario).ToList();
            }
        }

        public Usuario GetByNombreUsuario(string nombreUsuario)
        {
            lock (Candado)
            {
                return Usuarios.FirstOrDefault(u => string.Equals(
                    u.NombreUsuario, nombreUsuario, StringComparison.OrdinalIgnoreCase));
            }
        }

        public Usuario GetByEmail(string email)
        {
            lock (Candado)
            {
                return Usuarios.FirstOrDefault(u => string.Equals(
                    u.Email, email, StringComparison.OrdinalIgnoreCase));
            }
        }

        public List<Usuario> GetByRolId(Guid rolId)
        {
            lock (Candado)
            {
                return Usuarios.Where(u => u.RolId == rolId).ToList();
            }
        }

        public bool ExistsByNombreUsuario(string nombreUsuario)
        {
            return GetByNombreUsuario(nombreUsuario) != null;
        }

        public bool ExistsByEmail(string email)
        {
            return !string.IsNullOrWhiteSpace(email) && GetByEmail(email) != null;
        }
    }
}
