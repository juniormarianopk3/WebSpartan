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

        [Required(ErrorMessage = "O CEP é obrigatório")]
        public string CEP { get; set; }

        [Required(ErrorMessage = "Rua é obrigatória")]
        public string Rua { get; set; }

        [Required(ErrorMessage = "Número é obrigatório")]
        public string Numero { get; set; }

        [Required(ErrorMessage = "Bairro é obrigatório")]
        public string Bairro { get; set; }

        public string? Complemento { get; set; }

        [Required(ErrorMessage = "Cidade é obrigatória")]
        public string Cidade { get; set; }

        [Required(ErrorMessage = "Estado é obrigatório")]
        public string Estado { get; set; }
    }

    }

