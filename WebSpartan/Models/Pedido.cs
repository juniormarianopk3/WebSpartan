using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebSpartan.Models
{
    public class Pedido
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }

        public DateTime Data { get; set; } = DateTime.Now;

        [Required]
        public string Status { get; set; } = "Pendente";

        [Column(TypeName = "decimal(18,2)")]
        public decimal Subtotal { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Frete { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Desconto { get; set; }
        public decimal CashbackGerado { get; set; } // quanto ele VAI GANHAR
        public decimal Total => Subtotal + Frete - Desconto;

        public string? MpPreferenceId { get; set; }
        public string? MpPaymentId { get; set; }

        public int? EnderecoEntregaId { get; set; }

        [ForeignKey("EnderecoEntregaId")]
        public UserAddress? EnderecoEntrega { get; set; }

        [ForeignKey("UserId")]
        public ApplicationUser User { get; set; }

        public ICollection<ItemPedido> Itens { get; set; } = new List<ItemPedido>();
        public bool CashbackDebitado { get; set; } = false; // ✅ novo
        public bool CashbackCreditado { get; set; } = false;
        public bool EntregaCombinada { get; set; } = false;
        public string? ObservacaoEntrega { get; set; } // opcional: "Cliente combinará via WhatsApp"

    }
}
