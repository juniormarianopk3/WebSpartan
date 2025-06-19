using Microsoft.AspNetCore.Mvc;
using WebSpartan.Settings;

namespace WebSpartan.Controllers
{
    public class ConfiguracoesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ConfiguracoesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Configuracoes/EditWhatsApp
        public async Task<IActionResult> EditWhatsApp()
        {
            var config = _context.Configuracoes.FirstOrDefault(c => c.Chave == "WhatsAppNum");
            if (config == null)
            {
                config = new Configuracao { Chave = "WhatsAppNum", Valor = "5511980748056" };
                _context.Configuracoes.Add(config);
                await _context.SaveChangesAsync();
            }
            return View(config);
        }

        // POST: Configuracoes/EditWhatsApp
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditWhatsApp([Bind("Id,Chave,Valor")] Configuracao configuracao)
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
