using Microsoft.AspNetCore.Mvc;

namespace WebSpartan.Controllers
{
    public class ContatoController : Controller
    {
        private readonly AppDbContext _context;

        // Injeção de dependência do contexto
        public ContatoController(AppDbContext context)
        {
            _context = context;
        }
        public IActionResult Index()
        {
            //var numeroWpp = _context.Configuracoes.FirstOrDefault(c => c.Chave == "WhatsAppNum")?.Valor
            //      ?? "5511980748056"; // valor padrão se não achar

            //ViewBag.WhatsAppNum = numeroWpp;
            return View();
        }
    }
}
