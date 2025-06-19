using System.ComponentModel.DataAnnotations;

namespace WebSpartan.Models.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Informe o usuário.")]
        [Display(Name = "Usuário")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Informe a senha.")]
        [DataType(DataType.Password)]
        [Display(Name = "Senha")]
        public string Password { get; set; }

        [Display(Name = "Lembrar-me")]
        public bool LembrarMe { get; set; }
    }
}

