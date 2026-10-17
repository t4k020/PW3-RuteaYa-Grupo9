using Microsoft.AspNetCore.Mvc;
using RuteaYa.Servicios;

namespace RuteaYa.Controllers
{
    public class HomeController : Controller
    {
        private readonly IViajeService _viajeService;
        private readonly IEnvioService _envioService;
        public HomeController(IViajeService viajeService, IEnvioService envioService)
        {
            _viajeService = viajeService;
            _envioService = envioService;
        }
        public IActionResult Index()
        {
            return View();
        }


    }
}
