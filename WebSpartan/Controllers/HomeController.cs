using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebSpartan.Models;
using WebSpartan.Models.ViewModels;

namespace WebSpartan.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;

        public HomeController(AppDbContext context)
        {
            _context = context;
        }

        // ============================
        // CATÁLOGO DE PRODUTOS
        // ============================
        public async Task<IActionResult> Index()
        {
            var produtos = await _context.Produtos.ToListAsync();
            var carrinho = HttpContext.Session.GetObjectFromJson<List<ItemCarrinho>>("Carrinho") ?? new();
            var viewModel = new ProdutoIndexViewModel
            {
                Produtos = produtos,
                TotalItensCarrinho = carrinho.Sum(i => i.Quantidade)
            };
            ViewBag.TotalItensCarrinho = viewModel.TotalItensCarrinho;
            return View(viewModel);
        }

        public IActionResult Detalhes(int id)
        {
            var produto = _context.Produtos.FirstOrDefault(p => p.Id == id);
            if (produto == null) return NotFound();
            return View(produto);
        }

        // ============================
        // CARRINHO DE COMPRAS
        // ============================
        public IActionResult Carrinho()
        {
            var carrinho = HttpContext.Session.GetObjectFromJson<List<ItemCarrinho>>("Carrinho") ?? new();
            var viewModel = new CarrinhoViewModel
            {
                Itens = carrinho,
                Cliente = new DadosCliente()
            };
            ViewBag.TotalItensCarrinho = carrinho.Sum(i => i.Quantidade);
            return View(viewModel);
        }

        [HttpPost]
        public IActionResult AdicionarAoCarrinho(int produtoId, int quantidade)
        {
            var produto = _context.Produtos.Find(produtoId);
            if (produto == null) return NotFound();

            var carrinho = HttpContext.Session.GetObjectFromJson<List<ItemCarrinho>>("Carrinho") ?? new();
            var itemExistente = carrinho.FirstOrDefault(i => i.ProdutoId == produtoId);

            if (itemExistente != null)
            {
                itemExistente.Quantidade += quantidade;
            }
            else
            {
                carrinho.Add(new ItemCarrinho
                {
                    ProdutoId = produto.Id,
                    Produto = produto,
                    Quantidade = quantidade
                });
            }

            HttpContext.Session.SetObjectAsJson("Carrinho", carrinho);
            return RedirectToAction(nameof(Carrinho));
        }

        [HttpPost]
        public IActionResult RemoverItemCarrinho([FromBody] int produtoId)
        {
            var carrinho = HttpContext.Session.GetObjectFromJson<List<ItemCarrinho>>("Carrinho") ?? new();
            var item = carrinho.FirstOrDefault(i => i.Produto.Id == produtoId);

            if (item != null)
            {
                if (item.Quantidade > 1)
                    item.Quantidade--;
                else
                    carrinho.Remove(item);

                HttpContext.Session.SetObjectAsJson("Carrinho", carrinho);
            }

            decimal novoTotal = carrinho.Sum(i => i.Produto.Preco * i.Quantidade);

            return Json(new
            {
                sucesso = true,
                produtoId,
                novaQuantidade = item?.Quantidade ?? 0,
                novoTotal
            });
        }
    }
}
