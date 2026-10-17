using Microsoft.AspNetCore.Mvc;
using RuteaYa.Servicios;

namespace RuteaYa.Controllers
{
    public class EnvioController : Controller
    {

        
        private readonly IEnvioService _envioService;
        public EnvioController( IEnvioService envioService)
        {
            
            _envioService = envioService;
        }
        public IActionResult Index()
        {
            return View();
        }
    }
}
