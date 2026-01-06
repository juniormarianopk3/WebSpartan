using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebSpartan.Models;
using WebSpartan.Models.ViewModels;

namespace WebSpartan.Controllers
{
    [Authorize(Policy = "SomenteAdmin")]
    public class AdminProdutoController : Controller
    {
        private readonly AppDbContext _context;

        public AdminProdutoController(AppDbContext context)
        {
            _context = context;
        }

        // GET: AdminProdutos
        public async Task<IActionResult> Index()
        {
            return View(await _context.Produtos.ToListAsync());
        }

        [HttpGet]
        public IActionResult Criar()
        {
            var vm = new ProdutoViewModel
            {
                CashbackPercentual = 5 // default = 5%
            };

            vm.CashbackOpcoes = Enumerable.Range(1, 6)
                .Select(i => new SelectListItem
                {
                    Text = $"{i * 5}%",
                    Value = (i * 5).ToString(),
                    Selected = (i * 5) == vm.CashbackPercentual
                })
                .ToList();

            return View(vm);
        }



        [HttpPost]
        public async Task<IActionResult> Criar(ProdutoViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.CashbackOpcoes = Enumerable.Range(1, 6)
                    .Select(i => new SelectListItem
                    {
                        Text = $"{i * 5}%",
                        Value = (i * 5).ToString(),
                        Selected = (i * 5) == model.CashbackPercentual
                    })
                    .ToList();

                return View(model);
            }
            string caminhoImagem = "";

            if (model.ImagemUrl != null && model.ImagemUrl.Length > 0)
            {
                var pastaImagens = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/imagens");

                if (!Directory.Exists(pastaImagens))
                    Directory.CreateDirectory(pastaImagens);

                var nomeArquivo = Guid.NewGuid().ToString() + Path.GetExtension(model.ImagemUrl.FileName);
                var caminhoCompleto = Path.Combine(pastaImagens, nomeArquivo);

                using (var stream = new FileStream(caminhoCompleto, FileMode.Create))
                {
                    await model.ImagemUrl.CopyToAsync(stream);
                }

                caminhoImagem = "/imagens/" + nomeArquivo;
            }
            var cashbackDecimal = model.CashbackPercentual / 100m;
            var produto = new Produto
            {
                Nome = model.Nome,
                Descricao = model.Descricao,
                DescricaoCompleta = model.DescricaoCompleta,
                Preco = model.Preco,
                ImagemUrl = caminhoImagem,
                CashbackPercentual = cashbackDecimal
            };

            _context.Produtos.Add(produto);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Editar(int id)
        {
            var produto = await _context.Produtos.FindAsync(id);
            if (produto == null) return NotFound();

            var vm = new ProdutoViewModel
            {
                Id = produto.Id,
                Nome = produto.Nome,
                Descricao = produto.Descricao,
                DescricaoCompleta = produto.DescricaoCompleta,
                Preco = produto.Preco,
                ImagemAtual = produto.ImagemUrl,

                // 🔹 Converte 0.10 → 10, 0.05 → 5 etc.
                CashbackPercentual = (int)(produto.CashbackPercentual * 100m)
            };

            vm.CashbackOpcoes = Enumerable.Range(1, 6)
                .Select(i => new SelectListItem
                {
                    Text = $"{i * 5}%",
                    Value = (i * 5).ToString(),
                    Selected = (i * 5) == vm.CashbackPercentual
                })
                .ToList();

            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> Editar(ProdutoViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.CashbackOpcoes = Enumerable.Range(1, 6)
                    .Select(i => new SelectListItem
                    {
                        Text = $"{i * 5}%",
                        Value = (i * 5).ToString(),
                        Selected = (i * 5) == model.CashbackPercentual
                    })
                    .ToList();

                return View(model);
            }

            var produto = await _context.Produtos.FindAsync(model.Id);
            if (produto == null) return NotFound();

            produto.Nome = model.Nome;
            produto.Descricao = model.Descricao;
            produto.DescricaoCompleta = model.DescricaoCompleta;

            produto.Preco = model.Preco;
            produto.CashbackPercentual = model.CashbackPercentual/ 100m;

            if (model.ImagemUrl != null && model.ImagemUrl.Length > 0)
            {
                // Apaga a imagem antiga se quiser (opcional)
                if (!string.IsNullOrEmpty(produto.ImagemUrl))
                {
                    var caminhoAntigo = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", produto.ImagemUrl.TrimStart('/'));
                    if (System.IO.File.Exists(caminhoAntigo))
                        System.IO.File.Delete(caminhoAntigo);
                }

                var pastaImagens = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/imagens");
                if (!Directory.Exists(pastaImagens))
                    Directory.CreateDirectory(pastaImagens);

                var nomeArquivo = Guid.NewGuid().ToString() + Path.GetExtension(model.ImagemUrl.FileName);
                var caminhoCompleto = Path.Combine(pastaImagens, nomeArquivo);

                using (var stream = new FileStream(caminhoCompleto, FileMode.Create))
                {
                    await model.ImagemUrl.CopyToAsync(stream);
                }

                produto.ImagemUrl = "/imagens/" + nomeArquivo;
            }

            _context.Produtos.Update(produto);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }


        // GET: AdminProdutos/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var produto = await _context.Produtos
                .FirstOrDefaultAsync(m => m.Id == id);
            if (produto == null)
            {
                return NotFound();
            }

            return View(produto);
        }

        // POST: AdminProdutos/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var produto = await _context.Produtos.FindAsync(id);
            if (produto != null)
            {
                _context.Produtos.Remove(produto);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ProdutoExists(int id)
        {
            return _context.Produtos.Any(e => e.Id == id);
        }
    }
}
