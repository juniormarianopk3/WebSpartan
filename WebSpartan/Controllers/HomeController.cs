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

                // Aqui deve buscar pelo ProdutoId corretamente
                var itemExistente = carrinho.FirstOrDefault(i => i.ProdutoId == produtoId);
                if (itemExistente != null)
                {
                    itemExistente.Quantidade += quantidade; // soma à quantidade existente
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
            }

            return RedirectToAction(nameof(Index), new { adicionado = true });
        }

       

        [HttpPost]
        public IActionResult RemoverItemCarrinho([FromBody] int produtoId)
        {
            var carrinho = HttpContext.Session.GetObjectFromJson<List<ItemCarrinho>>("Carrinho") ?? new();
            var item = carrinho.FirstOrDefault(i => i.Produto.Id == produtoId);

            int novaQuantidade = 0;
            if (item != null)
            {
                if (item.Quantidade > 1)
                {
                    item.Quantidade--;
                    novaQuantidade = item.Quantidade;
                }
                else
                {
                    carrinho.Remove(item);
                }

                HttpContext.Session.SetObjectAsJson("Carrinho", carrinho);
            }

            decimal novoTotal = carrinho.Sum(i => i.Produto.Preco * i.Quantidade);

            return Json(new
            {
                sucesso = true,
                produtoId,
                novaQuantidade,
                novoTotal
            });
        }



        public ActionResult Carrinho()
        {

            ViewBag.TotalItensCarrinho = ObterTotalItensCarrinho();
            var carrinho = HttpContext.Session.GetObjectFromJson<List<ItemCarrinho>>("Carrinho") ?? new List<ItemCarrinho>();

            var viewModel = new CarrinhoViewModel
            {
                Itens = carrinho,
                Cliente = new DadosCliente()
            };
            return View(viewModel);
        }


        [HttpPost]
        public IActionResult Carrinho(CarrinhoViewModel model)
        {

            if (ModelState.IsValid)
            {
                // Limpa o carrinho da sessão
                HttpContext.Session.Remove("Carrinho");

                // Passa os dados do cliente para a tela de confirmação
                TempData["ClienteNome"] = model.Cliente.Nome;
                Console.WriteLine($"TempData definido: {model.Cliente.Nome}");
                return RedirectToAction("Confirmacao");
            }

            if (model.Itens == null || !model.Itens.Any())
            {
                ModelState.AddModelError("", "Carrinho está vazio.");
                model.Itens = new List<ItemCarrinho>();
                return View(model);
            }

            // Carrega novamente os itens do carrinho para exibir na view
            model.Itens = HttpContext.Session.GetObjectFromJson<List<ItemCarrinho>>("Carrinho") ?? new();
            return View(model);
        }

        public IActionResult Confirmacao()
        {
            ViewBag.ClienteNome = TempData["ClienteNome"]?.ToString();
            return View();
        }

    }

}
