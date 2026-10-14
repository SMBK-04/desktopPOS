using Microsoft.AspNetCore.Mvc;

namespace EnterprisePOS.Controllers
{
    public class BillingController : Controller
    {
        public IActionResult PayYourBills()
        {
            return View();
        }
    }
}
