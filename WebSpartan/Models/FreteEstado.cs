using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebSpartan.Models
{
    public class FreteEstado
    {
        public int Id { get; set; }

        [Required]
        [StringLength(2, ErrorMessage = "Use a sigla do estado, por ex: RJ, SP, MG")]
        public string Estado { get; set; } = string.Empty; // UF

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        [Display(Name = "Valor do frete (R$)")]
        public decimal Valor { get; set; }
    }
}
