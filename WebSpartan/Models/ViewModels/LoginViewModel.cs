using System.ComponentModel.DataAnnotations;

namespace WebSpartan.Models.ViewModels
{
    public class LoginViewModel
    {
        [Required, EmailAddress]
        public string Email { get; set; }

        [Required, DataType(DataType.Password)]
        public string Senha { get; set; }

        [Display(Name = "Manter conectado?")]
        public bool LembrarMe { get; set; }
    }
}
