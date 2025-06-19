using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Security.Policy;
using WebSpartan.Settings;
using WebSpartan.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using WebSpartan.Models;

namespace WebSpartan.Controllers
{
    [Authorize]
    public class ContaController : Controller
    {
        private readonly AdminCredentialsSettings _adminSettings;
        private readonly AppDbContext _context;

        
        public ContaController(IOptions<AdminCredentialsSettings> adminSettings, AppDbContext context)
        {
            _adminSettings = adminSettings.Value;
            _context = context;
        }
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string returnUrl = null)
        {
            // Se já estiver autenticado como Admin, redireciona para onde veio
            if (User.Identity.IsAuthenticated && User.IsInRole("Admin"))
            {
                return RedirectToAction("Index", "Conta");
            }

            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // 1. Verifica credenciais (aqui, comparando com valores do appsettings)
            if (model.Username == _adminSettings.Username && model.Password == _adminSettings.Password)
            {
                // 2. Cria lista de claims, incluindo Role = "Admin"
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, model.Username),
                    new Claim(ClaimTypes.Role, "Admin")
                };

                var claimsIdentity = new ClaimsIdentity(
                    claims, CookieAuthenticationDefaults.AuthenticationScheme);

                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = model.LembrarMe, // mantem cookie entre sessões
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(1)
                };

                // 3. Sign in
                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                // 4. Redireciona para returnUrl ou para a área administrativa
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }
                else
                {
                    return RedirectToAction("Index", "Conta");
                }
            }

            // Se chegou aqui, credenciais inválidas
            ModelState.AddModelError(string.Empty, "Usuário ou senha inválidos.");
            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Logout()
        {
            // Desloga o cookie
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Conta");
        }

        [AllowAnonymous]
        public IActionResult AcessoNegado()
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
    }
}
