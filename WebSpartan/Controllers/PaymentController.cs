using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MercadoPago.Client.Payment;
using MercadoPago.Resource.Payment;
using MercadoPago.Config;
using WebSpartan.Models;
using Microsoft.AspNetCore.Identity;
using MercadoPago.Client.Common;
using System.Buffers.Text;

namespace WebSpartan.Controllers
{
    [Authorize] // só usuário logado pode pagar
    public class PaymentController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;


        public PaymentController(IConfiguration configuration, AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
            // 🔑 Configura credenciais do Mercado Pago (pega do appsettings.json)
            MercadoPagoConfig.AccessToken = configuration["MercadoPago:AccessToken"]
                                            ?? configuration["MercadoPago:AccessToken"];
        }

        // ✅ Gera PIX para um pedido específico
        // URL: /Payment/CreatePix?pedidoId=123
        [HttpPost]
        public async Task<IActionResult> CreatePix(int pedidoId)
        {
            // Busca o pedido no banco
            var pedido = await _context.Pedidos
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.Id == pedidoId);

            if (pedido == null)
                return Json(new { error = true, message = "Pedido não encontrado." });

            // 🔹 Calcula o TOTAL com frete e desconto (desconto só em produtos, frete não entra no cashback)
            var total = Math.Round((pedido.Subtotal - pedido.Desconto) + pedido.Frete, 2);

            if (total <= 0)
                return Json(new { error = true, message = "Valor total do pedido inválido." });

            try
            {
                // ✅ Evita duplicidade se já existe um PIX pendente gerado (opcional)
                // Se quiser permitir "regenerar PIX", basta remover esse bloco.
                var jaExistePixPendente = await _context.PixOrders
                    .AnyAsync(x => x.PedidoId == pedido.Id && !x.Processado);

                if (jaExistePixPendente && !string.IsNullOrEmpty(pedido.MpPaymentId) && long.TryParse(pedido.MpPaymentId, out var pid))
                {
                    var cliente = new PaymentClient();
                    var paymentExisting = await cliente.GetAsync(pid);

                    return Json(new
                    {
                        error = false,
                        message = "PIX já foi gerado para este pedido.",
                        qrCodeBase64 = paymentExisting?.PointOfInteraction?.TransactionData?.QrCodeBase64,
                        qrCode = paymentExisting?.PointOfInteraction?.TransactionData?.QrCode,
                        status = paymentExisting?.Status,
                        paymentId = paymentExisting?.Id,
                        pedidoId = pedido.Id
                    });
                }


                var client = new PaymentClient();

                var request = new PaymentCreateRequest
                {
                    TransactionAmount = total,
                    Description = $"Pedido #{pedido.Id} - Spartan AES",
                    PaymentMethodId = "pix",
                    Payer = new PaymentPayerRequest
                    {
                        Email = pedido.User?.Email ?? "cliente@spartan.com",
                        FirstName = pedido.User?.NomeCompleto ?? "Cliente"
                    },
                    ExternalReference = pedido.Id.ToString()
                };

                var payment = await client.CreateAsync(request);

                // ✅ SALVA o paymentId no pedido (isso resolve sair da página e voltar depois)
                pedido.MpPaymentId = payment.Id?.ToString();

                // Registra internamente essa transação PIX
                var pixOrder = new PixOrder
                {
                    TransactionId = payment.Id.ToString(),
                    Processado = false,
                    Status = payment.Status,
                    PedidoId = pedido.Id
                };

                _context.PixOrders.Add(pixOrder);
                _context.Pedidos.Update(pedido);

                await _context.SaveChangesAsync();

                return Json(new
                {
                    error = false,
                    qrCodeBase64 = payment.PointOfInteraction?.TransactionData?.QrCodeBase64,
                    qrCode = payment.PointOfInteraction?.TransactionData?.QrCode,
                    status = payment.Status,
                    paymentId = payment.Id,
                    pedidoId = pedido.Id
                });
            }
            catch (Exception ex)
            {
                return Json(new { error = true, message = ex.Message });
            }
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCardPayment([FromBody] CardPaymentRequest dto)
        {
            var pedido = await _context.Pedidos
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.Id == dto.PedidoId);

            if (pedido == null)
                return Json(new { error = true, message = "Pedido não encontrado." });

            var total = (pedido.Subtotal - pedido.Desconto) + pedido.Frete;
            if (total <= 0)
                return Json(new { error = true, message = "Valor total inválido." });

            try
            {
                var client = new PaymentClient();

                var request = new PaymentCreateRequest
                {
                    TransactionAmount = total,
                    Token = dto.Token,
                    Installments = dto.Installments,
                    PaymentMethodId = dto.PaymentMethodId,
                    Description = $"Pedido #{pedido.Id} - Spartan AES",
                    ExternalReference = pedido.Id.ToString(),
                    Payer = new PaymentPayerRequest
                    {
                        Email = string.IsNullOrWhiteSpace(dto.PayerEmail) ? (pedido.User?.Email ?? "cliente@spartan.com") : dto.PayerEmail,
                        FirstName = pedido.User?.NomeCompleto ?? "Cliente",
                        Identification = new IdentificationRequest
                        {
                            Type = dto.IdentificationType,
                            Number = dto.IdentificationNumber
                        }
                    }
                };

                var payment = await client.CreateAsync(request);

                // ✅ garante registro interno (pra CheckPayment processar cashback também no cartão)
                var order = await _context.PixOrders
                    .FirstOrDefaultAsync(x => x.TransactionId == payment.Id.ToString());

                if (order == null)
                {
                    order = new PixOrder
                    {
                        TransactionId = payment.Id.ToString(),
                        Processado = false,
                        Status = payment.Status,
                        PedidoId = pedido.Id
                    };
                    _context.PixOrders.Add(order);
                }

                // ✅ Se já aprovou, já processa agora (não depende do CheckPayment)
                if (payment.Status == "approved" && !order.Processado)
                {
                    order.Processado = true;
                    order.Status = payment.Status;

                    pedido.Status = "Pago";

                    // 🔻 Debita cashback usado (1x)
                    if (!pedido.CashbackDebitado && pedido.Desconto > 0)
                    {
                        var saldoAntes = pedido.User.SaldoCashback;

                        pedido.User.SaldoCashback -= pedido.Desconto;
                        if (pedido.User.SaldoCashback < 0) pedido.User.SaldoCashback = 0;

                        var saldoDepois = pedido.User.SaldoCashback;

                        _context.CashbackMovimentos.Add(new CashbackMovimento
                        {
                            UserId = pedido.UserId,
                            Tipo = "DEBITO",
                            Valor = pedido.Desconto,
                            SaldoAntes = saldoAntes,
                            SaldoDepois = saldoDepois,
                            PedidoId = pedido.Id,
                            Descricao = $"Uso de cashback no Pedido #{pedido.Id}"
                        });

                        pedido.CashbackDebitado = true;
                    }

                    // ➕ Credita cashback gerado (evita duplicar crédito)
                    if (pedido.CashbackGerado > 0)
                    {
                        bool jaCreditou = await _context.CashbackMovimentos.AnyAsync(m =>
                            m.PedidoId == pedido.Id && m.Tipo == "CREDITO");

                        if (!jaCreditou)
                        {
                            var saldoAntes = pedido.User.SaldoCashback;

                            pedido.User.SaldoCashback += pedido.CashbackGerado;

                            var saldoDepois = pedido.User.SaldoCashback;

                            _context.CashbackMovimentos.Add(new CashbackMovimento
                            {
                                UserId = pedido.UserId,
                                Tipo = "CREDITO",
                                Valor = pedido.CashbackGerado,
                                SaldoAntes = saldoAntes,
                                SaldoDepois = saldoDepois,
                                PedidoId = pedido.Id,
                                Descricao = $"Cashback do Pedido #{pedido.Id}"
                            });
                        }
                    }
                }
                else
                {
                    order.Status = payment.Status;
                }

                // ✅ salva o paymentId no Pedido (pra recuperar depois, mesmo fora da tela)
                pedido.MpPaymentId = payment.Id.ToString();
                await _context.SaveChangesAsync();

                return Json(new
                {
                    error = false,
                    status = payment.Status,
                    paymentId = payment.Id,
                    pedidoId = pedido.Id,
                    redirectUrl = Url.Action("PagamentoAprovado", "Pedidos", new { id = pedido.Id })
                });
            }
            catch (Exception ex)
            {
                return Json(new { error = true, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> CheckPayment(long paymentId, int? pedidoIdSeguro)
        {
            try
            {
                var client = new PaymentClient();
                var payment = await client.GetAsync(paymentId);

                if (payment == null)
                    return Json(new { status = "not_found" });

                // tenta achar PixOrder
                var pixOrder = await _context.PixOrders
                    .FirstOrDefaultAsync(p => p.TransactionId == payment.Id.ToString());

                Pedido? pedido = null;

                if (pixOrder != null)
                {
                    pedido = await _context.Pedidos
                        .Include(p => p.User)
                        .FirstOrDefaultAsync(p => p.Id == pixOrder.PedidoId);
                }
                else
                {
                    // fallback: tenta achar pedido pelo paymentId salvo
                    pedido = await _context.Pedidos
                        .Include(p => p.User)
                        .FirstOrDefaultAsync(p => p.MpPaymentId == payment.Id.ToString());

                    // fallback 2: external reference (id do pedido)
                    if (pedido == null && !string.IsNullOrWhiteSpace(payment.ExternalReference) &&
                        int.TryParse(payment.ExternalReference, out var pedidoIdRef))
                    {
                        pedido = await _context.Pedidos
                            .Include(p => p.User)
                            .FirstOrDefaultAsync(p => p.Id == pedidoIdRef);
                    }

                    // se achou pedido, cria PixOrder (pra padronizar fluxo)
                    if (pedido != null)
                    {
                        pixOrder = new PixOrder
                        {
                            TransactionId = payment.Id.ToString(),
                            Processado = false,
                            Status = payment.Status,
                            PedidoId = pedido.Id
                        };
                        _context.PixOrders.Add(pixOrder);
                        await _context.SaveChangesAsync();
                    }
                }

                if (pedido == null || pixOrder == null)
                    return Json(new { status = payment.Status, message = "Pedido não encontrado para esse pagamento." });

                // ✅ TRAVA DE SEGURANÇA: se veio pedidoIdSeguro, só processa se for o mesmo
                if (pedidoIdSeguro.HasValue && pedido.Id != pedidoIdSeguro.Value)
                    return Json(new { status = "forbidden", message = "Pedido não corresponde ao pagamento." });

                // ✅ TRAVA DE DONO: se veio pedidoIdSeguro, ele já foi validado pelo user logado.
                // Mesmo assim, para segurança extra:
                if (pedidoIdSeguro.HasValue)
                {
                    var user = await _userManager.GetUserAsync(User);
                    if (pedido.UserId != user.Id)
                        return Json(new { status = "forbidden", message = "Você não tem permissão." });
                }

                // ✅ PROCESSA QUANDO APPROVED E NÃO PROCESSADO
                if (payment.Status == "approved" && !pixOrder.Processado)
                {
                    pixOrder.Processado = true;
                    pixOrder.Status = payment.Status;

                    pedido.Status = "Pago";

                    // 🔻 Debita cashback usado
                    if (!pedido.CashbackDebitado && pedido.Desconto > 0)
                    {
                        var saldoAntes = pedido.User.SaldoCashback;

                        pedido.User.SaldoCashback -= pedido.Desconto;
                        if (pedido.User.SaldoCashback < 0) pedido.User.SaldoCashback = 0;

                        var saldoDepois = pedido.User.SaldoCashback;

                        _context.CashbackMovimentos.Add(new CashbackMovimento
                        {
                            UserId = pedido.UserId,
                            Tipo = "DEBITO",
                            Valor = pedido.Desconto,
                            SaldoAntes = saldoAntes,
                            SaldoDepois = saldoDepois,
                            PedidoId = pedido.Id,
                            Descricao = $"Uso de cashback no Pedido #{pedido.Id}"
                        });

                        pedido.CashbackDebitado = true;
                    }

                    // ➕ Credita cashback gerado (evitar duplicar crédito)
                    if (pedido.CashbackGerado > 0)
                    {
                        bool jaCreditou = await _context.CashbackMovimentos.AnyAsync(m =>
                            m.PedidoId == pedido.Id && m.Tipo == "CREDITO");

                        if (!jaCreditou)
                        {
                            var saldoAntes = pedido.User.SaldoCashback;

                            pedido.User.SaldoCashback += pedido.CashbackGerado;

                            var saldoDepois = pedido.User.SaldoCashback;

                            _context.CashbackMovimentos.Add(new CashbackMovimento
                            {
                                UserId = pedido.UserId,
                                Tipo = "CREDITO",
                                Valor = pedido.CashbackGerado,
                                SaldoAntes = saldoAntes,
                                SaldoDepois = saldoDepois,
                                PedidoId = pedido.Id,
                                Descricao = $"Cashback do Pedido #{pedido.Id}"
                            });
                        }
                    }

                    if (_context.Entry(pixOrder).State == EntityState.Detached)
                        _context.PixOrders.Attach(pixOrder);

                    if (_context.Entry(pedido).State == EntityState.Detached)
                        _context.Pedidos.Attach(pedido);
                    await _context.SaveChangesAsync();

                    return Json(new
                    {
                        status = payment.Status,
                        pedidoStatus = pedido.Status,
                        redirectUrl = Url.Action("PagamentoAprovado", "Pedidos", new { id = pedido.Id })
                    });
                }
                var qrBase64 = payment.PointOfInteraction?.TransactionData?.QrCodeBase64;
                var qrCopiaCola = payment.PointOfInteraction?.TransactionData?.QrCode;
                return Json(new
                {
                    status = payment.Status,
                    processado = pixOrder.Processado,
                    qrCodeBase64 = qrBase64,
                    qrCode = qrCopiaCola
                });
            }
            catch (Exception ex)
            {
                return Json(new { status = "error", message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> CheckPaymentByPedido(int pedidoId)
        {
            var user = await _userManager.GetUserAsync(User);

            var pedido = await _context.Pedidos
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == pedidoId && p.UserId == user.Id);

            if (pedido == null)
                return Json(new { status = "not_found", message = "Pedido não encontrado." });

            if (string.IsNullOrWhiteSpace(pedido.MpPaymentId) || !long.TryParse(pedido.MpPaymentId, out var paymentId))
                return Json(new { status = "not_generated", message = "Pagamento ainda não foi gerado." });

            // chama o motor principal (CheckPayment)
            return await CheckPayment(paymentId, pedidoId);
        }
    }
}
