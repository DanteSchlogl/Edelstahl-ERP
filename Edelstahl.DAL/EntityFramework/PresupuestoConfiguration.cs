using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.Infrastructure.Annotations;
using System.Data.Entity.ModelConfiguration;
using Edelstahl.Domain.Comercial;

namespace Edelstahl.DAL.EntityFramework
{
    public class PresupuestoConfiguration
        : EntityTypeConfiguration<Presupuesto>
    {
        public PresupuestoConfiguration()
        {
            ToTable("Presupuestos");

            HasKey(presupuesto => presupuesto.Id);

            Property(presupuesto => presupuesto.Id)
                .HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);

            Property(presupuesto => presupuesto.Numero)
                .IsRequired()
                .HasMaxLength(50)
                .HasColumnAnnotation(
                    IndexAnnotation.AnnotationName,
                    new IndexAnnotation(
                        new IndexAttribute("UQ_Presupuestos_Numero")
                        {
                            IsUnique = true
                        }));

            Property(presupuesto => presupuesto.ClienteId)
                .HasColumnAnnotation(
                    IndexAnnotation.AnnotationName,
                    new IndexAnnotation(
                        new IndexAttribute("IX_Presupuestos_ClienteId")));

            Property(presupuesto => presupuesto.FechaEmision)
                .HasColumnType("datetime2");

            Property(presupuesto => presupuesto.FechaVencimiento)
                .HasColumnType("datetime2");

            Property(presupuesto => presupuesto.TipoCambio)
                .HasPrecision(18, 4);

            Property(presupuesto => presupuesto.PorcentajeIVA)
                .HasPrecision(18, 4);

            Property(presupuesto => presupuesto.PorcentajeRecargo)
                .HasPrecision(18, 4);

            Property(presupuesto => presupuesto.PorcentajeDescuentoGeneral)
                .HasPrecision(18, 4);

            Property(presupuesto => presupuesto.PorcentajeAnticipo)
                .HasPrecision(18, 4);

            Property(presupuesto => presupuesto.CondicionPago)
                .IsRequired()
                .HasMaxLength(250);

            Property(presupuesto => presupuesto.PlazoEntrega)
                .IsRequired()
                .HasMaxLength(250);

            Property(presupuesto => presupuesto.Observaciones)
                .IsRequired()
                .HasMaxLength(1000);

            HasMany(presupuesto => presupuesto.Detalles)
                .WithRequired()
                .HasForeignKey(detalle => detalle.PresupuestoId)
                .WillCascadeOnDelete(true);
        }
    }
}
