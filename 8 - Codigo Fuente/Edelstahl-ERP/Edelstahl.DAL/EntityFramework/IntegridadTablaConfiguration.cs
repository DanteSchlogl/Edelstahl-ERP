using System.Data.Entity.ModelConfiguration;
using Edelstahl.Domain.Integrity;

namespace Edelstahl.DAL.EntityFramework
{
    /// <summary>
    /// Configuración de Entity Framework para
    /// el control de integridad vertical.
    /// </summary>
    public class IntegridadTablaConfiguration
        : EntityTypeConfiguration<IntegridadTabla>
    {
        public IntegridadTablaConfiguration()
        {
            ToTable(
                "IntegridadTablas");

            HasKey(
                integridad =>
                    integridad.NombreTabla);

            Property(
                    integridad =>
                        integridad.NombreTabla)
                .IsRequired()
                .HasMaxLength(128);

            Property(
                    integridad =>
                        integridad.DVV)
                .IsRequired()
                .HasMaxLength(64);

            Property(
                    integridad =>
                        integridad.FechaActualizacion)
                .IsRequired()
                .HasColumnType(
                    "datetime2");
        }
    }
}