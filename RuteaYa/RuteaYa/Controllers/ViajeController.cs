using Microsoft.AspNetCore.Mvc;
using RuteaYa.Servicios;

namespace RuteaYa.Controllers
{
    public class ViajeController : Controller
    {
        public readonly IViajeService _viajeService; 

        public ViajeController(IViajeService viajeService)
        {
            _viajeService = viajeService;
        }
        public IActionResult Index()
        {
            return View();
        }
    }
}
