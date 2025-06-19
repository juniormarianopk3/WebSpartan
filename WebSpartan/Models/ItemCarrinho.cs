namespace WebSpartan.Models
{
    public class ItemCarrinho
    {
        public int ProdutoId { get; set; }
        public Produto Produto { get; set; }
        public int Quantidade { get; set; }
    }

}
