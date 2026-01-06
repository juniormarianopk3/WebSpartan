using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebSpartan.Models;

namespace WebSpartan.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminPedidosController : Controller
    {
        private readonly AppDbContext _context;

        public AdminPedidosController(AppDbContext context)
        {
            _context = context;
        }

        // 🔹 Lista de pedidos
        public async Task<IActionResult> Index()
        {
            var pedidos = await _context.Set<Pedido>()
                .Include(p => p.User)
                .Include(p => p.Itens)
                .ThenInclude(i => i.Produto)
                .OrderByDescending(p => p.Data)
                .ToListAsync();

            return View(pedidos);
        }

        // 🔹 Detalhes do pedido
        public async Task<IActionResult> Detalhes(int id)
        {
            var pedido = await _context.Pedidos
    .Include(p => p.User)
    .Include(p => p.EnderecoEntrega)
    .Include(p => p.Itens)
        .ThenInclude(i => i.Produto)
    .FirstOrDefaultAsync(p => p.Id == id);

            if (pedido == null) return NotFound();
            return View(pedido);
        }

        // 🔹 Aprovar pedido
        [HttpPost]
        public async Task<IActionResult> Aprovar(int id)
        {
            var pedido = await _context.Set<Pedido>().FindAsync(id);
            if (pedido == null) return NotFound();

            pedido.Status = "Aprovado";
            await _context.SaveChangesAsync();

            TempData["Msg"] = $"Pedido #{pedido.Id} aprovado com sucesso!";
            return RedirectToAction("Index");
        }

        // 🔹 Rejeitar pedido
        [HttpPost]
        public async Task<IActionResult> Rejeitar(int id)
        {
            var pedido = await _context.Set<Pedido>().FindAsync(id);
            if (pedido == null) return NotFound();

            pedido.Status = "Rejeitado";
            await _context.SaveChangesAsync();

            TempData["Msg"] = $"Pedido #{pedido.Id} rejeitado.";
            return RedirectToAction("Index");
        }
    }
}
