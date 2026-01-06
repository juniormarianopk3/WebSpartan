namespace WebSpartan.Models
{
	public class Produto
	{
		public int Id { get; set; }
		public string Nome { get; set; }
		public string Descricao { get; set; }
        public string DescricaoCompleta { get; set; }
        public decimal Preco { get; set; }
		public string ImagemUrl { get; set; }

		// Regra: percentual padrão de cashback para esse produto
		public decimal? CashbackPercentual { get; set; } // ex: 0.05m = 5%
    }

}
