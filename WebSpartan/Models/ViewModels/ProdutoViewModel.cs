using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace WebSpartan.Models.ViewModels
{
    public class ProdutoViewModel
    {
        
        public int Id { get; set; }

        [Required(ErrorMessage = "O nome é obrigatório")]
        public string Nome { get; set; }

        public string Descricao { get; set; }

        public string DescricaoCompleta { get; set; }

        [Required(ErrorMessage = "O preço é obrigatório")]
        [Range(0.01, double.MaxValue, ErrorMessage = "O preço deve ser maior que zero")]
        public decimal Preco { get; set; }

        [Display(Name = "Imagem")]
        // Arquivo para upload da nova imagem
        public IFormFile? ImagemUrl { get; set; }

        public string? ImagemAtual { get; set; } // caminho da imagem atual (se já existe)
        public decimal CashbackPercentual { get; set; }
        public IEnumerable<SelectListItem> CashbackOpcoes { get; set; } = Enumerable
                    .Range(1, 6) // 1..6 → 5,10,15,20,25,30
                    .Select(i => new SelectListItem
                    {
                        Text = $"{i * 5}%",  // Exibido
                        Value = (i * 5).ToString() // Enviado (5,10,15...)
                    })
                    .ToList();
    }
}
