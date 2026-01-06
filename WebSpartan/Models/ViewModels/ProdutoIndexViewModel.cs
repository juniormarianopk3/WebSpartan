using System.Collections.Generic;
using WebSpartan.Models;

namespace WebSpartan.Models.ViewModels
{
    public class ProdutoIndexViewModel
    {
        public List<Produto> Produtos { get; set; }
        public int TotalItensCarrinho { get; set; }
        public decimal CashbackPercentual { get; set; }
    }
}
