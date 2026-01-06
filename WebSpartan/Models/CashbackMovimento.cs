using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebSpartan.Models
{
    public class CashbackMovimento
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = null!;

        [ForeignKey(nameof(UserId))]
        public ApplicationUser? User { get; set; }

        public DateTime Data { get; set; } = DateTime.Now;

        // "CREDITO" ou "DEBITO"
        [Required]
        [MaxLength(20)]
        public string Tipo { get; set; } = "CREDITO";

        // Valor do movimento (use positivo sempre, o Tipo define se soma/subtrai)
        [Column(TypeName = "decimal(18,2)")]
        public decimal Valor { get; set; }

        // Saldo antes e depois (muito útil para auditoria)
        [Column(TypeName = "decimal(18,2)")]
        public decimal SaldoAntes { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal SaldoDepois { get; set; }

        [MaxLength(255)]
        public string? Descricao { get; set; }

        public int? PedidoId { get; set; }

        [ForeignKey(nameof(PedidoId))]
        public Pedido? Pedido { get; set; }
    }
}
