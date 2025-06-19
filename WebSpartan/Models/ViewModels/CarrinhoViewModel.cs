using System.ComponentModel.DataAnnotations;

namespace WebSpartan.Models.ViewModels
{
    public class CarrinhoViewModel
    {
        public List<ItemCarrinho> Itens { get; set; }
        public DadosCliente Cliente { get; set; }
        public decimal Frete { get; set; } // <-- Adicione aqui

        [Required(ErrorMessage ="Adicione o metodo de pagamento")]
        public string metodoPagamento { get; set; } // <-- Adicione aqui

    }
}
