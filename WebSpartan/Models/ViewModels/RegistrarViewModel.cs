using System.ComponentModel.DataAnnotations;

namespace WebSpartan.Models.ViewModels
{
    public class RegistrarViewModel
    {
        [Required(ErrorMessage = "Informe seu nome completo")]
        public string NomeCompleto { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [MinLength(4)]
        public string Senha { get; set; }

        [DataType(DataType.Password)]
        [Compare("Senha", ErrorMessage = "As senhas não conferem")]
        public string ConfirmarSenha { get; set; }

        [Phone]
        [Required(ErrorMessage = "Informe seu número de telefone")]
        public string Telefone { get; set; }

        [Required(ErrorMessage = "Informe seu CPF")]
        [Length(14,14)]
        public string CPF { get; set; }
    }
}
