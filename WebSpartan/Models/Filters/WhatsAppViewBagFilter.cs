namespace WebSpartan.Models.Filters
{
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Filters;
    using WebSpartan.Models;

    public class WhatsAppViewBagFilter : IActionFilter
    {
        private readonly AppDbContext _context;

        public WhatsAppViewBagFilter(AppDbContext context)
        {
            _context = context;
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            var controller = context.Controller as Controller;
            if (controller != null)
            {
                var config = _context.Configuracoes.FirstOrDefault(c => c.Chave == "WhatsAppNum");
                controller.ViewBag.WhatsAppNum = config?.Valor  ?? "5500000000000";
            }
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            // Nada necessário aqui
        }
    }

}
