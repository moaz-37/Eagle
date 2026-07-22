using Eagle.DAL.Entities;
using Eagle.PL.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Eagle.PL.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<User> _signInManager;
        private readonly UserManager<User> _userManager;

        public AccountController(SignInManager<User> signInManager, UserManager<User> userManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Home");
            return View(new LoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid) return View(model);

            var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);
            if (result.Succeeded)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user != null)
                {
                    await _signInManager.SignInWithClaimsAsync(user, model.RememberMe, new[]
                    {
                new System.Security.Claims.Claim("FullName", user.FullName)
            });
                }

                return !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
                    ? Redirect(returnUrl)
                    : RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError(string.Empty, "البريد الإلكتروني أو كلمة المرور غير صحيحة");
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login");
        }

        [Authorize(Roles = "Manager")]
        [HttpGet]
        public IActionResult CreateUser() => View(new CreateUserViewModel());

        [Authorize(Roles = "Manager")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(CreateUserViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = new User { UserName = model.Email, Email = model.Email, FullName = model.FullName };
            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                foreach (var e in result.Errors) ModelState.AddModelError(string.Empty, e.Description);
                return View(model);
            }

            await _userManager.AddToRoleAsync(user, model.Role);
            TempData["Success"] = $"تم إنشاء الحساب لـ {model.FullName}";
            return RedirectToAction("CreateUser");
        }


        [Authorize(Roles = "Manager")]
        [HttpGet]
        public async Task<IActionResult> Users()
        {
            var users = _userManager.Users.ToList();
            var list = new List<UserListItemViewModel>();

            foreach (var u in users)
            {
                var roles = await _userManager.GetRolesAsync(u);
                list.Add(new UserListItemViewModel
                {
                    Id = u.Id,
                    FullName = u.FullName,
                    Email = u.Email ?? string.Empty,
                    Role = roles.FirstOrDefault() ?? "-"
                });
            }

            return View(list);
        }

        [Authorize(Roles = "Manager")]
        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user is null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);

            return View(new EditUserViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                Role = roles.FirstOrDefault() ?? "Cashier"
            });
        }

        [Authorize(Roles = "Manager")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditUserViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByIdAsync(model.Id.ToString());
            if (user is null) return NotFound();

            user.FullName = model.FullName;
            user.Email = model.Email;
            user.UserName = model.Email;
            user.NormalizedEmail = model.Email.ToUpperInvariant();
            user.NormalizedUserName = model.Email.ToUpperInvariant();

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                foreach (var e in updateResult.Errors) ModelState.AddModelError(string.Empty, e.Description);
                return View(model);
            }

            // Swap role if changed
            var currentRoles = await _userManager.GetRolesAsync(user);
            if (!currentRoles.Contains(model.Role))
            {
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
                await _userManager.AddToRoleAsync(user, model.Role);
            }

            // Optional password reset
            if (!string.IsNullOrWhiteSpace(model.NewPassword))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var resetResult = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);
                if (!resetResult.Succeeded)
                {
                    foreach (var e in resetResult.Errors) ModelState.AddModelError(string.Empty, e.Description);
                    return View(model);
                }
            }

            TempData["Success"] = "تم تحديث بيانات المستخدم";
            return RedirectToAction("Users");
        }

        [Authorize(Roles = "Manager")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var currentUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            if (id == currentUserId)
            {
                TempData["Error"] = "لا يمكنك حذف حسابك الخاص";
                return RedirectToAction("Users");
            }

            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user is null) return NotFound();

            await _userManager.DeleteAsync(user);
            TempData["Success"] = "تم حذف المستخدم";
            return RedirectToAction("Users");
        }

        public IActionResult AccessDenied() => View();
    }
}