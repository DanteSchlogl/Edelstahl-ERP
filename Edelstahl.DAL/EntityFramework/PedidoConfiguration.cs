using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.Infrastructure.Annotations;
using System.Data.Entity.ModelConfiguration;
using Edelstahl.Domain.Comercial;

namespace Edelstahl.DAL.EntityFramework
{
    public class PedidoConfiguration
        : EntityTypeConfiguration<Pedido>
    {
        public PedidoConfiguration()
        {
            ToTable("Pedidos");

            HasKey(pedido => pedido.Id);

            Property(pedido => pedido.Id)
                .HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);

            Property(pedido => pedido.Numero)
                .IsRequired()
                .HasMaxLength(50)
                .HasColumnAnnotation(
                    IndexAnnotation.AnnotationName,
                    new IndexAnnotation(
                        new IndexAttribute("UQ_Pedidos_Numero")
                        {
                            IsUnique = true
                        }));

            Property(pedido => pedido.ClienteId)
                .HasColumnAnnotation(
                    IndexAnnotation.AnnotationName,
                    new IndexAnnotation(
                        new IndexAttribute("IX_Pedidos_ClienteId")));

            Property(pedido => pedido.PresupuestoId)
                .HasColumnAnnotation(
                    IndexAnnotation.AnnotationName,
                    new IndexAnnotation(
                        new IndexAttribute("UQ_Pedidos_PresupuestoId")
                        {
                            IsUnique = true
                        }));

            Property(pedido => pedido.FechaPedido)
                .HasColumnType("datetime2");

            Property(pedido => pedido.TipoCambio)
                .HasPrecision(18, 4);

            Property(pedido => pedido.PorcentajeIVA)
                .HasPrecision(18, 4);

            Property(pedido => pedido.PorcentajeRecargo)
                .HasPrecision(18, 4);

            Property(pedido => pedido.PorcentajeDescuentoGeneral)
                .HasPrecision(18, 4);

            Property(pedido => pedido.PorcentajeAnticipo)
                .HasPrecision(18, 4);

            Property(pedido => pedido.ImporteAnticipo)
                .HasPrecision(18, 4);

            Property(pedido => pedido.MedioPagoAnticipo)
                .IsRequired()
                .HasMaxLength(100);

            Property(pedido => pedido.ComprobanteAnticipo)
                .IsRequired()
                .HasMaxLength(100);

            Property(pedido => pedido.FechaAnticipo)
                .HasColumnType("datetime2");

            Property(pedido => pedido.CondicionPago)
                .IsRequired()
                .HasMaxLength(250);

            Property(pedido => pedido.PlazoEntrega)
                .IsRequired()
                .HasMaxLength(250);

            Property(pedido => pedido.Observaciones)
                .IsRequired()
                .HasMaxLength(1000);

            HasMany(pedido => pedido.Detalles)
                .WithRequired()
                .HasForeignKey(detalle => detalle.PedidoId)
                .WillCascadeOnDelete(true);
        }
    }
}
