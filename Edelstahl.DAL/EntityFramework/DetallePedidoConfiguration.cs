using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.Infrastructure.Annotations;
using System.Data.Entity.ModelConfiguration;
using Edelstahl.Domain.Comercial;

namespace Edelstahl.DAL.EntityFramework
{
    public class DetallePedidoConfiguration
        : EntityTypeConfiguration<DetallePedido>
    {
        public DetallePedidoConfiguration()
        {
            ToTable("DetallesPedido");

            HasKey(detalle => detalle.Id);

            Property(detalle => detalle.Id)
                .HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);

            Property(detalle => detalle.PedidoId)
                .HasColumnAnnotation(
                    IndexAnnotation.AnnotationName,
                    new IndexAnnotation(
                        new IndexAttribute("IX_DetallesPedido_PedidoId")));

            Property(detalle => detalle.DetallePresupuestoId)
                .HasColumnAnnotation(
                    IndexAnnotation.AnnotationName,
                    new IndexAnnotation(
                        new IndexAttribute("IX_DetallesPedido_DetallePresupuestoId")));

            Property(detalle => detalle.Codigo)
                .IsRequired()
                .HasMaxLength(100);

            Property(detalle => detalle.Descripcion)
                .IsRequired()
                .HasMaxLength(500);

            Property(detalle => detalle.DescripcionTecnica)
                .IsRequired()
                .HasMaxLength(1000);

            Property(detalle => detalle.Cantidad)
                .HasPrecision(18, 4);

            Property(detalle => detalle.PrecioUnitario)
                .HasPrecision(18, 4);

            Property(detalle => detalle.PorcentajeDescuento)
                .HasPrecision(18, 4);
        }
    }
}
