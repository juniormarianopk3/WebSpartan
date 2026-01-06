namespace WebSpartan.Models.ViewModels
{
    public class CashbackExtratoViewModel
    {
        public decimal SaldoAtual { get; set; }
        public List<WebSpartan.Models.CashbackMovimento> Movimentos { get; set; } = new();
    }
}
