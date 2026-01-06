using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;

namespace WebSpartan.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string NomeCompleto { get; set; }
        public decimal SaldoCashback { get; set; } = 0;
        public string CPF { get; set; }



        // Relacionamentos futuros
        public List<UserAddress> Enderecos { get; set; } = new();
       // public List<CashbackTransaction> Cashbacks { get; set; } = new();
    }
}
