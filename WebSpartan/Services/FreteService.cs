using WebSpartan.Models;

namespace WebSpartan.Services
{
    public interface IFreteService
    {
        decimal CalcularPorEstado(string uf);
    }

    public class FreteService : IFreteService
    {
        private readonly AppDbContext _context;

        public FreteService(AppDbContext context)
        {
            _context = context;
        }

        public decimal CalcularPorEstado(string uf)
        {
            if (string.IsNullOrWhiteSpace(uf))
                return 0m;

            uf = uf.Trim().ToUpper();

            var registro = _context.FretesEstado
                .FirstOrDefault(f => f.Estado == uf);

            if (registro != null)
                return registro.Valor;

            // Se não tiver configurado, você escolhe um default
            return 40m;
        }
    }
}
