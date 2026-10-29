using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.Infrastructure.Annotations;
using System.Data.Entity.ModelConfiguration;
using Edelstahl.Domain.Comercial;

namespace Edelstahl.DAL.EntityFramework
{
    /// <summary>
    /// Configuración de Entity Framework para la
    /// entidad Cliente.
    /// </summary>
    public class ClienteConfiguration
        : EntityTypeConfiguration<Cliente>
    {
        public ClienteConfiguration()
        {
            ToTable("Clientes");

            HasKey(
                cliente => cliente.Id);

            Property(
                    cliente => cliente.Id)
                .HasDatabaseGeneratedOption(
                    DatabaseGeneratedOption.None);

            /*
             * El CUIT se almacena cifrado mediante AES.
             * La longitud se amplía para admitir el
             * contenido criptográfico codificado
             * mediante Base64.
             */
            Property(
                    cliente => cliente.CUIT)
                .IsRequired()
                .HasMaxLength(300)
                .HasColumnAnnotation(
                    IndexAnnotation.AnnotationName,
                    new IndexAnnotation(
                        new IndexAttribute(
                            "UQ_Clientes_CUIT")
                        {
                            IsUnique = true
                        }));

            Property(
                    cliente => cliente.Tipo)
                .IsRequired();

            Property(
                    cliente => cliente.RazonSocial)
                .IsRequired()
                .HasMaxLength(250);

            Property(
                    cliente =>
                        cliente.DireccionFacturacion)
                .IsRequired()
                .HasMaxLength(500);

            Property(
                    cliente =>
                        cliente.DireccionEntrega)
                .IsRequired()
                .HasMaxLength(500);

            Property(
                    cliente => cliente.Localidad)
                .IsRequired()
                .HasMaxLength(150);

            Property(
                    cliente => cliente.Provincia)
                .IsRequired()
                .HasMaxLength(150);

            Property(
                    cliente => cliente.CodigoPostal)
                .IsRequired()
                .HasMaxLength(20);

            Property(
                    cliente => cliente.Email)
                .IsRequired()
                .HasMaxLength(250);

            Property(
                    cliente => cliente.Telefono)
                .IsRequired()
                .HasMaxLength(100);

            Property(
                    cliente => cliente.LimiteCredito)
                .HasPrecision(18, 2);

            Property(
                    cliente => cliente.DeudaActual)
                .HasPrecision(18, 2);

            Property(
                    cliente => cliente.FechaAlta)
                .HasColumnType("datetime2");

                     /*
                * El DVH es obligatorio después de completar
                  * la migración de los clientes históricos.
                   
                   * Contiene un hash SHA-256 representado mediante
                  * 64 caracteres hexadecimales.
                */
            


            Property(
        cliente => cliente.DVH)
    .IsRequired()
    .HasMaxLength(64);
        }
    }
}