
namespace WebSpartan.Models
{
    public class PixOrder
    {
        public int Id { get; set; }

        // ID da transação no Mercado Pago (payment.Id)
        public string TransactionId { get; set; } = null!;

        // Se já processamos (marcamos pedido como pago, aplicamos cashback, etc.)
        public bool Processado { get; set; }

        public string Status { get; set; } = "pending";

        // Ligação com o pedido da loja
        public int PedidoId { get; set; }
        public Pedido Pedido { get; set; } = null!;
    }
}

