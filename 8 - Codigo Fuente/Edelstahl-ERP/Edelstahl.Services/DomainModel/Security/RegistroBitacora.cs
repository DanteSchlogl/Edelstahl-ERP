using System;


namespace Edelstahl.Services.DomainModel.Security
{
    public class RegistroBitacora
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public DateTime FechaHora { get; set; }
        public Guid? UsuarioId { get; set; }
        public string NombreUsuario { get; set; }
        public string Modulo { get; set; }
        public string Accion { get; set; }
        public string Entidad { get; set; }
        public Guid? EntidadId { get; set; }
        public string Descripcion { get; set; }
        public string Resultado { get; set; }
        public string DireccionEquipo { get; set; }
        public string NombreEquipo { get; set; }
        public string TipoEvento { get; set; }
        public string DetalleTecnico { get; set; }

        public RegistroBitacora()
        {
            FechaHora = DateTime.Now;
            NombreUsuario = string.Empty;
            Modulo = string.Empty;
            Accion = string.Empty;
            Entidad = string.Empty;
            Descripcion = string.Empty;
            Resultado = "Correcto";
            DireccionEquipo = string.Empty;
            NombreEquipo = string.Empty;
            TipoEvento = "Informacion";
            DetalleTecnico = string.Empty;
        }
    }
}
