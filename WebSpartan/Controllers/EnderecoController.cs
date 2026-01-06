using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebSpartan.Models;

namespace WebSpartan.Controllers
{
    [Authorize]
    public class EnderecoController : Controller
    {
        private readonly AppDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public EnderecoController(AppDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            var enderecos = await _db.Enderecos.Where(e => e.UserId == user.Id).ToListAsync();
            return View(enderecos);
        }

        [HttpGet]
        public IActionResult Criar() => View();

        [HttpPost]
        public async Task<IActionResult> Criar(UserAddress model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.GetUserAsync(User);
            model.UserId = user.Id;

            if (model.Principal)
            {
                var outros = _db.Enderecos.Where(e => e.UserId == user.Id && e.Principal);
                foreach (var e in outros)
                    e.Principal = false;
            }

            _db.Enderecos.Add(model);
            await _db.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Editar(int id)
        {
            var endereco = await _db.Enderecos.FindAsync(id);
            if (endereco == null) return NotFound();
            return View(endereco);
        }

        [HttpPost]
        public async Task<IActionResult> Editar(UserAddress model)
        {
            if (!ModelState.IsValid) return View(model);

            var endereco = await _db.Enderecos.FindAsync(model.Id);
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
                var user = await _userManager.GetUserAsync(User);
                var outros = _db.Enderecos.Where(e => e.UserId == user.Id && e.Id != model.Id);
                foreach (var e in outros)
                    e.Principal = false;
            }

            await _db.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Excluir(int id)
        {
            var endereco = await _db.Enderecos.FindAsync(id);
            if (endereco == null) return NotFound();

            _db.Enderecos.Remove(endereco);
            await _db.SaveChangesAsync();
            return RedirectToAction("Index");
        }
    }
}
