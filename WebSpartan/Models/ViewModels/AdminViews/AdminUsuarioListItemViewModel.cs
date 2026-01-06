namespace WebSpartan.Models.ViewModels
{
    public class AdminUsuarioListItemViewModel
    {
        public string UserId { get; set; } = null!;
        public string? Nome { get; set; }
        public string? Email { get; set; }
        public string? Telefone { get; set; }
        public decimal SaldoCashback { get; set; }
        public int TotalPedidosPago { get; set; }
        public decimal? TotalGasto { get; set; }
        public DateTime? UltimaCompra { get; set; } // pode ser null
        public int? DiasSemComprar { get; set; }    // calculado depois
    }
}
