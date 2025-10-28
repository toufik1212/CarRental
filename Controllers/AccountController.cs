using Microsoft.AspNetCore.Mvc;
using CarRental.Models;
using Microsoft.EntityFrameworkCore;

namespace CarRental.Controllers
{
    public class AccountController : Controller
    {

        private readonly AppDbContext _context;

        public AccountController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string username,string password)
        {
            var user = await _context.UserPass
                            .FirstOrDefaultAsync(u => u.Username == username && u.Password == password);

            if(user == null)
            {
                ViewBag.Message = "Username atau Password Salah";
                return View();
            }

            HttpContext.Session.SetString("Username", user.Username);
            HttpContext.Session.SetString("Role", user.Role);
            HttpContext.Session.SetString("UserId", Convert.ToString(user.Id));

            return RedirectToAction("Index", "Home");

        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }



    }
}
