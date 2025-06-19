using System;
using System.Collections.Generic;
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
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Criar(ProdutoViewModel model)
        {
            if (!ModelState.IsValid)
            {
                foreach (var erro in ModelState)
                {
                    Console.WriteLine($"{erro.Key}: {string.Join(", ", erro.Value.Errors.Select(e => e.ErrorMessage))}");
                }

                return View(model); // Retorna à View mostrando os erros
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

            var produto = new Produto
            {
                Nome = model.Nome,
                Descricao = model.Descricao,
                Preco = model.Preco,
                ImagemUrl = caminhoImagem
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

            var model = new ProdutoViewModel
            {
                Id = produto.Id,
                Nome = produto.Nome,
                Descricao = produto.Descricao,
                Preco = produto.Preco,
                ImagemAtual = produto.ImagemUrl
            };

            return View(model);
        }
        [HttpPost]
        public async Task<IActionResult> Editar(ProdutoViewModel model)
        {
            if (!ModelState.IsValid)
            {
                foreach (var erro in ModelState)
                {
                    Console.WriteLine($"{erro.Key}: {string.Join(", ", erro.Value.Errors.Select(e => e.ErrorMessage))}");
                }
                return View(model);
            }

            var produto = await _context.Produtos.FindAsync(model.Id);
            if (produto == null) return NotFound();

            produto.Nome = model.Nome;
            produto.Descricao = model.Descricao;
            produto.Preco = model.Preco;
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
