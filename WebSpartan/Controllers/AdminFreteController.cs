using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebSpartan.Models;

namespace WebSpartan.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminFreteController : Controller
    {
        private readonly AppDbContext _context;

        public AdminFreteController(AppDbContext context)
        {
            _context = context;
        }

        // Lista todos os estados configurados
        public async Task<IActionResult> Index()
        {
            var fretes = await _context.FretesEstado
                .OrderBy(f => f.Estado)
                .ToListAsync();

            return View(fretes);
        }

        // GET: criar
        [HttpGet]
        public IActionResult Criar()
        {
            return View();
        }

        // POST: criar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Criar(FreteEstado model)
        {
            if (!ModelState.IsValid)
                return View(model);

            model.Estado = model.Estado.Trim().ToUpper();

            // evita duplicar UF
            if (_context.FretesEstado.Any(f => f.Estado == model.Estado))
            {
                ModelState.AddModelError("", "Já existe frete configurado para esse estado.");
                return View(model);
            }

            _context.FretesEstado.Add(model);
            await _context.SaveChangesAsync();

            TempData["Msg"] = "Frete cadastrado com sucesso!";
            return RedirectToAction(nameof(Index));
        }

        // GET: editar
        [HttpGet]
        public async Task<IActionResult> Editar(int id)
        {
            var frete = await _context.FretesEstado.FindAsync(id);
            if (frete == null) return NotFound();

            return View(frete);
        }

        // POST: editar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(FreteEstado model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var frete = await _context.FretesEstado.FindAsync(model.Id);
            if (frete == null) return NotFound();

            frete.Estado = model.Estado.Trim().ToUpper();
            frete.Valor = model.Valor;

            await _context.SaveChangesAsync();

            TempData["Msg"] = "Frete atualizado com sucesso!";
            return RedirectToAction(nameof(Index));
        }

        // (Opcional) deletar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Excluir(int id)
        {
            var frete = await _context.FretesEstado.FindAsync(id);
            if (frete == null) return NotFound();

            _context.FretesEstado.Remove(frete);
            await _context.SaveChangesAsync();

            TempData["Msg"] = "Frete removido.";
            return RedirectToAction(nameof(Index));
        }
    }
}
