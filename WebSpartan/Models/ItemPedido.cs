using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebSpartan.Models
{
    public class ItemPedido
    {
        public int Id { get; set; }

        public int PedidoId { get; set; }

        public int ProdutoId { get; set; }

        public int Quantidade { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ValorUnitario { get; set; }

        [ForeignKey("PedidoId")]
        public Pedido Pedido { get; set; }

        [ForeignKey("ProdutoId")]
        public Produto Produto { get; set; }

        public decimal Subtotal => Quantidade * ValorUnitario;

        // 🔹 Cashback REAL gerado por este item
        public decimal CashbackValor { get; set; }

    }
}
