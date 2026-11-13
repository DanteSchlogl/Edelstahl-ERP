namespace Edelstahl.Domain.Comercial
{
    public enum EstadoPedido
    {
        Pendiente = 0,
        Confirmado = 1,
        EnPreparacion = 2,
        EnProduccion = 3,
        ListoParaEntrega = 4,
        Entregado = 5,
        Cancelado = 6,
        EnDistribucion = 7
    }
}
