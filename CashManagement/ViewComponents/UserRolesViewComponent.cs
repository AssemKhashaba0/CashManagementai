using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using CashManagement.Models;
using System.Threading.Tasks;

namespace CashManagement.ViewComponents
{
    public class UserRolesViewComponent : ViewComponent
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public UserRolesViewComponent(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var user = await _userManager.GetUserAsync(HttpContext.User);
            if (user == null)
                return Content("");

            var roles = await _userManager.GetRolesAsync(user);
            return View(roles);
        }
    }
}