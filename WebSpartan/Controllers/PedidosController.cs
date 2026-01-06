using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using WebSpartan.Models;

namespace WebSpartan.Controllers
{
    [Authorize]
    public class PedidosController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public PedidosController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // === Tela de Checkout ===
        [HttpGet]
        public async Task<IActionResult> Finalizar()
        {
            var user = await _userManager.GetUserAsync(User);
            var carrinho = HttpContext.Session.GetObjectFromJson<List<ItemCarrinho>>("Carrinho") ?? new();

            if (!carrinho.Any())
                return RedirectToAction("Carrinho", "Home");

            var enderecos = await _context.Enderecos.Where(e => e.UserId == user.Id).ToListAsync();

            var subtotal = carrinho.Sum(c => c.Produto.Preco * c.Quantidade);

            var limitePercent = await ObterLimiteCashbackPercentAsync();
            var limiteValor = Math.Round(subtotal * limitePercent, 2);

            ViewBag.Enderecos = enderecos;
            ViewBag.Total = subtotal;
            ViewBag.SaldoCashback = user?.SaldoCashback ?? 0m;

            ViewBag.LimiteCashbackPercent = limitePercent; // 0.30
            ViewBag.LimiteCashbackValor = limiteValor;     // subtotal * %
            return View(carrinho);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Finalizar(int? enderecoId, bool combinarEntrega = false, bool usarCashback = false, string metodoPagamento = "pix")
        {
            var user = await _userManager.GetUserAsync(User);
            var carrinho = HttpContext.Session.GetObjectFromJson<List<ItemCarrinho>>("Carrinho") ?? new();

            if (!carrinho.Any())
                return RedirectToAction("Finalizar");

            // subtotal sempre é dos produtos
            decimal subtotal = carrinho.Sum(c => c.Produto.Preco * c.Quantidade);

            // cashback gerado (apenas produtos)
            decimal cashbackGerado = carrinho.Sum(c =>
            {
                decimal perc = c.Produto?.CashbackPercentual ?? 0m;
                return (c.Produto.Preco * c.Quantidade) * perc;
            });
            cashbackGerado = Math.Round(cashbackGerado, 2);

            // cashback gerado (apenas produtos)
            
            cashbackGerado = Math.Round(cashbackGerado, 2);

            // ✅ PEGA O LIMITE DO PAINEL (10%, 30%, etc)
            var limitePercent = await ObterLimiteCashbackPercentAsync();

            // ✅ AGORA calcula o valor máximo permitido
            decimal limiteCashback = Math.Round(subtotal * limitePercent, 2);

            decimal cashbackUsado = 0m;

            if (usarCashback && user.SaldoCashback > 0)
            {
                cashbackUsado = Math.Min(user.SaldoCashback, limiteCashback);
                cashbackUsado = Math.Min(cashbackUsado, subtotal);
                cashbackUsado = Math.Round(cashbackUsado, 2);
            }

            decimal frete = 0m;
            int? enderecoEntregaId = null;

            if (!combinarEntrega)
            {
                if (!enderecoId.HasValue)
                {
                    TempData["MsgErro"] = "Selecione um endereço ou escolha combinar a entrega com o vendedor.";
                    return RedirectToAction("Finalizar");
                }

                var endereco = await _context.Enderecos
                    .FirstOrDefaultAsync(e => e.Id == enderecoId.Value && e.UserId == user.Id);

                if (endereco == null)
                {
                    TempData["MsgErro"] = "Endereço inválido.";
                    return RedirectToAction("Finalizar");
                }

                var freteEstado = await _context.FretesEstado
                    .FirstOrDefaultAsync(f => f.Estado == endereco.Estado);

                frete = freteEstado?.Valor ?? 0m;
                enderecoEntregaId = enderecoId.Value;
            }

            var pedido = new Pedido
            {
                UserId = user.Id,
                EnderecoEntregaId = enderecoEntregaId, // null se combinarEntrega
                EntregaCombinada = combinarEntrega,
                ObservacaoEntrega = combinarEntrega ? "Entrega a combinar com o vendedor." : null,

                Subtotal = subtotal,
                Frete = frete,
                Desconto = cashbackUsado,

                Status = "Pendente",
                CashbackDebitado = false,
                CashbackGerado = cashbackGerado
            };

            _context.Pedidos.Add(pedido);
            await _context.SaveChangesAsync();

            foreach (var item in carrinho)
            {
                _context.ItensPedido.Add(new ItemPedido
                {
                    PedidoId = pedido.Id,
                    ProdutoId = item.ProdutoId,
                    Quantidade = item.Quantidade,
                    ValorUnitario = item.Produto.Preco
                });
            }

            await _context.SaveChangesAsync();
            HttpContext.Session.Remove("Carrinho");
            if (string.Equals(metodoPagamento, "cartao", StringComparison.OrdinalIgnoreCase))
                return RedirectToAction("Cartao", "Pedidos", new { id = pedido.Id });
            return RedirectToAction("Pix", "Pedidos", new { id = pedido.Id });
        }



        [HttpGet]
        public async Task<IActionResult> Pix(int id)
        {
            var user = await _userManager.GetUserAsync(User);

            var pedido = await _context.Pedidos
                .Include(p => p.Itens)
                    .ThenInclude(i => i.Produto)
                .FirstOrDefaultAsync(p => p.Id == id && p.UserId == user.Id);

            if (pedido == null) return NotFound();

            return View(pedido); // ou o modelo que sua Pix.cshtml espera
        }

        [HttpGet]
        public async Task<IActionResult> Cartao(int id)
        {
            var user = await _userManager.GetUserAsync(User);

            var pedido = await _context.Pedidos
                .Include(p => p.Itens).ThenInclude(i => i.Produto)
                .FirstOrDefaultAsync(p => p.Id == id && p.UserId == user.Id);

            if (pedido == null) return NotFound();
            return View(pedido);
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> PagamentoAprovado(int id)
        {
            var user = await _userManager.GetUserAsync(User);

            var pedido = await _context.Pedidos
                .Include(p => p.Itens).ThenInclude(i => i.Produto)
                .Include(p => p.EnderecoEntrega)
                .FirstOrDefaultAsync(p => p.Id == id && p.UserId == user.Id);

            if (pedido == null) return NotFound();

            // Se alguém tentar abrir sem estar pago ainda:
            if (pedido.Status != "Pago" && pedido.Status != "PAGO")
                return RedirectToAction("Pix", "Pedidos", new { id = pedido.Id });

            return View(pedido);
        }


        [HttpGet]
        public async Task<IActionResult> Retorno(string status, string preference_id, string payment_id)
        {
            // status: success | failure | pending
            // preference_id: ID da Preference (bate com MpPreferenceId)
            // payment_id: ID do pagamento no Mercado Pago

            var pedido = await _context.Pedidos
                .FirstOrDefaultAsync(p => p.MpPreferenceId == preference_id);

            if (pedido == null)
            {
                // opcional: logar erro
                return RedirectToAction("Historico");
            }

            pedido.MpPaymentId = payment_id;

            if (status == "success")
                pedido.Status = "Pago";
            else if (status == "pending")
                pedido.Status = "Pendente";
            else
                pedido.Status = "Falha";

            await _context.SaveChangesAsync();

            // mostrar uma view de confirmação bacana
            return View("Retorno", pedido);
        }


        // === Histórico de pedidos ===
        public async Task<IActionResult> Historico()
        {
            var user = await _userManager.GetUserAsync(User);
            var pedidos = await _context.Pedidos
                .Include(p => p.Itens)
                .ThenInclude(i => i.Produto)
                .Include(p => p.EnderecoEntrega)
                .Where(p => p.UserId == user.Id)
                .OrderByDescending(p => p.Data)
                .ToListAsync();

            return View(pedidos);
        }

        // === Detalhes ===
        public async Task<IActionResult> Detalhes(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var pedido = await _context.Pedidos
                .Include(p => p.Itens).ThenInclude(i => i.Produto)
                .Include(p => p.EnderecoEntrega)
                .FirstOrDefaultAsync(p => p.Id == id && p.UserId == user.Id);

            if (pedido == null) return NotFound();
            return View(pedido);
        }

        [HttpGet]
        public IActionResult RetornoSucesso()
        {
            TempData["MsgSucesso"] = "Pagamento aprovado com sucesso!";
            return RedirectToAction("Historico");
        }

        [HttpGet]
        public IActionResult RetornoFalha()
        {
            TempData["MsgErro"] = "Ocorreu um erro no pagamento. Tente novamente.";
            return RedirectToAction("Historico");
        }

        [HttpGet]
        public IActionResult RetornoPendente()
        {
            TempData["MsgAviso"] = "Seu pagamento está pendente. Assim que for aprovado, seu pedido será atualizado.";
            return RedirectToAction("Historico");
        }
        [HttpGet]
        public async Task<IActionResult> CalcularFrete(int enderecoId)
        {
            var user = await _userManager.GetUserAsync(User);

            // Endereço escolhido
            var endereco = await _context.Enderecos
                .FirstOrDefaultAsync(e => e.Id == enderecoId && e.UserId == user.Id);

            if (endereco == null)
            {
                return Json(new
                {
                    sucesso = false,
                    mensagem = "Endereço inválido."
                });
            }

            // Frete por estado (tabela FreteEstado)
            var freteEstado = await _context.FretesEstado
                .FirstOrDefaultAsync(f => f.Estado == endereco.Estado);

            decimal frete = freteEstado?.Valor ?? 0m;

            // Subtotal do carrinho
            var carrinho = HttpContext.Session
                .GetObjectFromJson<List<ItemCarrinho>>("Carrinho") ?? new();

            decimal subtotal = carrinho.Sum(c => c.Produto.Preco * c.Quantidade);
            decimal total = subtotal + frete;

            return Json(new
            {
                sucesso = true,
                frete,
                subtotal,
                total
            });
        }
        private decimal CalcularFretePorEstado(string uf)
        {
            if (string.IsNullOrWhiteSpace(uf))
                return 0m;

            uf = uf.ToUpper().Trim();

            // tenta achar frete exato por UF
            var freteEstado = _context.FretesEstado
                .FirstOrDefault(f => f.Estado == uf);

            if (freteEstado != null)
                return freteEstado.Valor;

            // opcional: frete "padrão" (tipo linha Estado = "OUTROS" na tabela)
            var freteDefault = _context.FretesEstado
                .FirstOrDefault(f => f.Estado == "OUTROS");

            if (freteDefault != null)
                return freteDefault.Valor;

            // fallback: sem frete configurado
            return 0m;
        }
        private async Task<decimal> ObterLimiteCashbackPercentAsync()
        {
            // ✅ ajuste a chave para a que você já usa no painel admin
            var cfg = await _context.Configuracoes
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Chave == "CashbackMaxUsoPercentual");

            // fallback: 30%
            var raw = cfg?.Valor?.Trim();
            if (string.IsNullOrWhiteSpace(raw))
                return 0.10m;

            raw = raw.Replace("%", "").Trim();

            // tenta parse pt-BR (10,5) e invariant (0.30)
            if (!decimal.TryParse(raw, NumberStyles.Any, new CultureInfo("pt-BR"), out var v) &&
                !decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out v))
                return 0.10m;

            // Se admin salvar 30, vira 0.30
            if (v > 1m) v = v / 100m;

            // trava em 0..1
            if (v < 0m) v = 0m;
            if (v > 1m) v = 1m;

            return v;
        }
    }
}
