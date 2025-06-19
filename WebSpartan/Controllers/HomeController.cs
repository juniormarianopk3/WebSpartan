using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Text;
using WebSpartan.Models;
using WebSpartan.Models.ViewModels;
namespace WebSpartan.Controllers
{


    public class HomeController : Controller
    {

        private readonly AppDbContext _context;

        // Injeção de dependência do contexto
        public HomeController(AppDbContext context)
        {
            _context = context;
        }


        private int ObterTotalItensCarrinho()
        {
            var carrinho = HttpContext.Session.GetObjectFromJson<List<ItemCarrinho>>("Carrinho") ?? new List<ItemCarrinho>();
            return carrinho.Sum(i => i.Quantidade);
        }




        public async Task<ActionResult> Index()
        {
            ViewBag.TotalItensCarrinho = ObterTotalItensCarrinho();
            var produtos = await _context.Produtos.ToListAsync();
            var totalItens = HttpContext.Session.GetObjectFromJson<List<ItemCarrinho>>("Carrinho") ?? new();


            var viewModel = new ProdutoIndexViewModel
            {
                Produtos = produtos,
                TotalItensCarrinho = totalItens.Sum(i => i.Quantidade)
            };

            return View(viewModel);
        }


        [HttpPost]
        public ActionResult AdicionarAoCarrinho(int produtoId, int quantidade)
        {
            var produto = _context.Produtos.Find(produtoId);
            if (produto != null)
            {
                var carrinho = HttpContext.Session.GetObjectFromJson<List<ItemCarrinho>>("Carrinho") ?? new();

                var itemExistente = carrinho.Find(i => i.Produto.Id == produtoId);
                if (itemExistente != null)
                    itemExistente.Quantidade += quantidade;
                else
                    carrinho.Add(new ItemCarrinho { Produto = produto, Quantidade = quantidade });

                HttpContext.Session.SetObjectAsJson("Carrinho", carrinho);
            }

            return RedirectToAction(nameof(Index), new { adicionado = true });
        }

        [HttpPost]
        public IActionResult RemoverDoCarrinho(int produtoId)
        {
            var carrinho = HttpContext.Session.GetObjectFromJson<List<ItemCarrinho>>("Carrinho") ?? new();

            var item = carrinho.FirstOrDefault(i => i.Produto.Id == produtoId);
            if (item != null)
            {
                carrinho.Remove(item);
                HttpContext.Session.SetObjectAsJson("Carrinho", carrinho);
            }

            return RedirectToAction("Carrinho");
        }

        public ActionResult Carrinho()
        {
            var numeroWpp = _context.Configuracoes.FirstOrDefault(c => c.Chave == "WhatsAppNum")?.Valor
                   ?? "5511980748056"; // valor padrão se não achar

            ViewBag.WhatsAppNum = numeroWpp;
            ViewBag.TotalItensCarrinho = ObterTotalItensCarrinho();
            var carrinho = HttpContext.Session.GetObjectFromJson<List<ItemCarrinho>>("Carrinho") ?? new List<ItemCarrinho>();

            var viewModel = new CarrinhoViewModel
            {
                Itens = carrinho,
                Cliente = new DadosCliente()
            };
            return View(viewModel);
        }

        

    }

}
