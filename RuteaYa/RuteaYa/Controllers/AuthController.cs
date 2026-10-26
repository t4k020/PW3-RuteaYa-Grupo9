using Microsoft.AspNetCore.Mvc;

namespace RuteaYa.Controllers
{
    public class AuthController : Controller
    {
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }
    }
}
