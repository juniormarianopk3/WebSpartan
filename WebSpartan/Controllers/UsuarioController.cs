using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using WebSpartan.Models;
using WebSpartan.Models.ViewModels;

namespace WebSpartan.Controllers
{
    public class UsuarioController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly AppDbContext _context;

        public UsuarioController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, AppDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
        }

        // GET: /Usuario/Registrar
        [HttpGet]
        public IActionResult Registrar() => View();

        // POST: /Usuario/Registrar
        [HttpPost]
        public async Task<IActionResult> Registrar(RegistrarViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                NomeCompleto = model.NomeCompleto,
                PhoneNumber = model.Telefone,
                CPF = model.CPF

            };


            var result = await _userManager.CreateAsync(user, model.Senha);

            if (result.Succeeded)
            {
                await _signInManager.SignInAsync(user, isPersistent: false);
                return RedirectToAction("Painel", "Usuario");
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            return View(model);
        }

        // GET: /Usuario/Login
        [HttpGet]
        public IActionResult Login() => View();

        // POST: /Usuario/Login
        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // 🔹 Busca o usuário pelo e-mail
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                ModelState.AddModelError("", "Usuário não encontrado.");
                return View(model);
            }

            // 🔹 Faz o login
            var result = await _signInManager.PasswordSignInAsync(user, model.Senha, model.LembrarMe, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                // 🔹 Busca as roles do usuário
                var roles = await _userManager.GetRolesAsync(user);

                // 🔹 Redireciona com base no perfil
                if (roles.Contains("Admin"))
                    return RedirectToAction("Index", "Admin");
                else
                    return RedirectToAction("Painel", "Usuario");
            }

            // 🔹 Se falhou
            ModelState.AddModelError("", "E-mail ou senha inválidos.");
            return View(model);
        }


        // /Usuario/Logout
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        [Authorize]
        public async Task<IActionResult> Painel()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Usuario");

            var enderecos = await _context.Enderecos
            .Where(e => e.UserId == user.Id)
                .ToListAsync();

            var pedidos = await _context.Set<Pedido>()
                .Where(p => p.UserId == user.Id)
                .OrderByDescending(p => p.Data)
                .Take(5)
                .ToListAsync();

            ViewBag.Enderecos = enderecos;
            ViewBag.Pedidos = pedidos;

            return View("Painel", user);
        }
        public async Task<IActionResult> Enderecos()
        {
            var user = await _userManager.GetUserAsync(User);
            var enderecos = await _context.Enderecos
                .Where(e => e.UserId == user.Id)
                .ToListAsync();

            return View(enderecos);
        }

        // === CRIAR NOVO ===
        [HttpGet]
        public IActionResult NovoEndereco()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> NovoEndereco(UserAddress model)
        {
            ModelState.Remove("UserId"); // 🔹 Ignora validação automática
            ModelState.Remove("User"); // 🔹 Ignora validação automática


            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.GetUserAsync(User);
            model.UserId = user.Id;

            if (model.Principal)
            {
                // remove flag principal de outros endereços
                var outros = _context.Enderecos.Where(e => e.UserId == user.Id);
                foreach (var e in outros)
                    e.Principal = false;
            }

            _context.Enderecos.Add(model);
            await _context.SaveChangesAsync();

            TempData["Msg"] = "Endereço cadastrado com sucesso!";
            return RedirectToAction("Enderecos");
        }

        // === EDITAR ===
        [HttpGet]
        public async Task<IActionResult> EditarEndereco(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var endereco = await _context.Enderecos.FirstOrDefaultAsync(e => e.Id == id && e.UserId == user.Id);
            if (endereco == null) return NotFound();

            return View(endereco);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarEndereco(UserAddress model)
        {
            var user = await _userManager.GetUserAsync(User);
            var endereco = await _context.Enderecos.FirstOrDefaultAsync(e => e.Id == model.Id && e.UserId == user.Id);
            if (endereco == null) return NotFound();

            endereco.Cep = model.Cep;
            endereco.Rua = model.Rua;
            endereco.Numero = model.Numero;
            endereco.Bairro = model.Bairro;
            endereco.Cidade = model.Cidade;
            endereco.Estado = model.Estado;
            endereco.Complemento = model.Complemento;
            endereco.Principal = model.Principal;

            if (model.Principal)
            {
                var outros = _context.Enderecos.Where(e => e.UserId == user.Id && e.Id != model.Id);
                foreach (var e in outros)
                    e.Principal = false;
            }
            await _context.SaveChangesAsync();
            TempData["Msg"] = "Endereço atualizado!";
            return RedirectToAction("Enderecos");
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Cashback()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login");

            var movimentos = await _context.CashbackMovimentos
                .Where(m => m.UserId == user.Id)
                .OrderByDescending(m => m.Data)
                .Take(100)
                .ToListAsync();

            var vm = new CashbackExtratoViewModel
            {
                SaldoAtual = user.SaldoCashback,
                Movimentos = movimentos
            };

            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> ExcluirEndereco(int id)
        {
            var user = await _userManager.GetUserAsync(User);

            var endereco = await _context.Enderecos
                .FirstOrDefaultAsync(e => e.Id == id && e.UserId == user.Id);

            if (endereco == null)
                return NotFound();

            // 👉 Verifica se existe pedido usando esse endereço
            bool enderecoEmUso = await _context.Pedidos
                .AnyAsync(p => p.EnderecoEntregaId == id);

            if (enderecoEmUso)
            {
                TempData["MsgErro"] = "Você não pode excluir este endereço, pois ele já foi utilizado em um pedido.";
                return RedirectToAction("Enderecos");
            }

            _context.Enderecos.Remove(endereco);
            await _context.SaveChangesAsync();

            TempData["Msg"] = "Endereço removido com sucesso!";
            return RedirectToAction("Enderecos");
        }


    }
}
