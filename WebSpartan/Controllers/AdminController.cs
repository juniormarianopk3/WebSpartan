using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Security.Policy;
using WebSpartan.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using WebSpartan.Models;
using System.Globalization;

namespace WebSpartan.Controllers
{
    [Authorize(Roles = "Admin")]

    public class AdminController : Controller
    {
        private readonly AppDbContext _context;


        public AdminController(AppDbContext context)
        {
            _context = context;
        }
        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> EditWhatsApp()
        {
            var config = _context.Configuracoes.FirstOrDefault(c => c.Chave == "WhatsAppNum");
            if (config == null)
            {
                config = new Configuracoes { Chave = "WhatsAppNum", Valor = "5511980748056" };
                _context.Configuracoes.Add(config);
                await _context.SaveChangesAsync();
            }
            return View(config);
        }

        // POST: Configuracoes/EditWhatsApp
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditWhatsApp([Bind("Id,Chave,Valor")] Configuracoes configuracao)
        {
            if (ModelState.IsValid)
            {
                var configDb = _context.Configuracoes.FirstOrDefault(c => c.Id == configuracao.Id);
                if (configDb != null)
                {
                    configDb.Valor = configuracao.Valor;
                    await _context.SaveChangesAsync();
                    TempData["MensagemSucesso"] = "Número do WhatsApp atualizado com sucesso!";
                    return RedirectToAction(nameof(EditWhatsApp));
                }
                ModelState.AddModelError("", "Configuração não encontrada.");
            }
            return View(configuracao);
        }

        [HttpGet]
        public async Task<IActionResult> Usuarios(string sort = "alpha", string? q = null)
        {
            q = q?.Trim();

            var usersQuery = _context.Users.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(q))
            {
                usersQuery = usersQuery.Where(u =>
                    (u.NomeCompleto != null && u.NomeCompleto.Contains(q)) ||
                    (u.Email != null && u.Email.Contains(q)) ||
                    (u.PhoneNumber != null && u.PhoneNumber.Contains(q))
                );
            }

            var lista = await usersQuery
                .Select(u => new AdminUsuarioListItemViewModel
                {
                    UserId = u.Id,
                    Nome = u.NomeCompleto,
                    Email = u.Email,
                    Telefone = u.PhoneNumber,
                    SaldoCashback = u.SaldoCashback,

                    TotalPedidosPago = _context.Pedidos.Count(p => p.UserId == u.Id && p.Status == "Pago"),

                    // ✅ Não use .Value aqui. Max() pode voltar null.
                    UltimaCompra = _context.Pedidos
                        .Where(p => p.UserId == u.Id && p.Status == "Pago")
                        .Select(p => (DateTime?)p.Data)
                        .Max(),

                    TotalGasto = _context.Pedidos
    .Where(p => p.UserId == u.Id && p.Status == "Pago")
    .Sum(p => (decimal?)((p.Subtotal - p.Desconto) + p.Frete)) ?? 0m,
                })
                .ToListAsync();

            // ✅ calcula em memória (sem explodir quando UltimaCompra == null)
            var agora = DateTime.Now;
            foreach (var u in lista)
            {
                u.DiasSemComprar = u.UltimaCompra.HasValue
                    ? (agora - u.UltimaCompra.Value).Days
                    : null;
            }

            lista = sort switch
            {
                "recentes" => lista
                    .OrderByDescending(x => x.UltimaCompra.HasValue)  // quem tem compra primeiro
                    .ThenByDescending(x => x.UltimaCompra)
                    .ToList(),

                "semcomprar" => lista
                    .OrderBy(x => x.UltimaCompra.HasValue ? 0 : 1)   // null por último (se quiser null primeiro, inverta)
                    .ThenBy(x => x.UltimaCompra)                    // mais antigo = mais tempo sem comprar
                    .ToList(),

                _ => lista
                    .OrderBy(x => x.Nome)
                    .ToList(),
            };

            ViewBag.Sort = sort;
            ViewBag.Q = q;

            return View(lista);
        }

        [HttpGet]
        public async Task<IActionResult> EditCashbackLimite()
        {
            var chave = "CashbackMaxUsoPercentual";

            var config = _context.Configuracoes.FirstOrDefault(c => c.Chave == chave);
            if (config == null)
            {
                config = new Configuracoes { Chave = chave, Valor = "0.00" }; // default 30%
                _context.Configuracoes.Add(config);
                await _context.SaveChangesAsync();
            }

            // manda o valor pra view como "30" (porcentagem)
            ViewBag.PercentualAtual = (decimal.TryParse(config.Valor, NumberStyles.Any, CultureInfo.InvariantCulture, out var v)
                ? v * 100m
                : 10m);

            return View(config);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCashbackLimite(int id, decimal percentual)
        {
            // percentual vem em "30" = 30%
            if (percentual < 0 || percentual > 100)
            {
                TempData["MsgErro"] = "Percentual inválido. Use de 0 a 100.";
                return RedirectToAction(nameof(EditCashbackLimite));
            }

            var configDb = _context.Configuracoes.FirstOrDefault(c => c.Id == id);
            if (configDb == null)
            {
                TempData["MsgErro"] = "Configuração não encontrada.";
                return RedirectToAction(nameof(EditCashbackLimite));
            }

            var valorDecimal = percentual / 100m; // 30 => 0.30
            configDb.Valor = valorDecimal.ToString("0.00", CultureInfo.InvariantCulture);

            await _context.SaveChangesAsync();
            TempData["MensagemSucesso"] = "Limite de uso do cashback atualizado com sucesso!";
            return RedirectToAction(nameof(EditCashbackLimite));
        }
    }
}
