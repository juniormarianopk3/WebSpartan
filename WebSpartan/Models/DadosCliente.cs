using System.ComponentModel.DataAnnotations;

namespace WebSpartan.Models
{
    public class DadosCliente
    {
        // Models/DadosCliente.cs

        [Required(ErrorMessage = "Nome é obrigatório")]
        public string Nome { get; set; }

        [Required(ErrorMessage = "O CPF é obrigatório")]
        public string CPF { get; set; }

        [Required(ErrorMessage = "O celular é obrigatório")]
        public string Celular { get; set; }

    }

    }

