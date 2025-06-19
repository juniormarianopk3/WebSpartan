using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace WebSpartan.Models.ViewModels
{
    public class EditProdutoViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "O nome é obrigatório")]
        public string Nome { get; set; }

        public string Descricao { get; set; }

        [Required(ErrorMessage = "O preço é obrigatório")]
        [Range(0.01, double.MaxValue, ErrorMessage = "O preço deve ser maior que zero")]
        public decimal Preco { get; set; }

        [Display(Name = "Imagem")]
        // Arquivo para upload da nova imagem
        public IFormFile ImagemUrl { get; set; }

        public string ImagemAtual { get; set; } // caminho da imagem atual (se já existe)

    }
}
