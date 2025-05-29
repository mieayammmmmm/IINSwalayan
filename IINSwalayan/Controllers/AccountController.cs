using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using IINSwalayan.Models;

namespace IINSwalayan.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ILogger<AccountController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password, string returnUrl = null)
        {
            try
            {
                if (!string.IsNullOrEmpty(email) && !string.IsNullOrEmpty(password))
                {
                    // Log attempt
                    _logger.LogInformation($"Login attempt for email: {email}");

                    var result = await _signInManager.PasswordSignInAsync(email, password, false, false);

                    if (result.Succeeded)
                    {
                        var user = await _userManager.FindByEmailAsync(email);
                        if (user != null)
                        {
                            var roles = await _userManager.GetRolesAsync(user);

                            _logger.LogInformation($"User {email} logged in successfully with roles: {string.Join(", ", roles)}");

                            // Check if user is Admin
                            if (roles.Contains("Admin"))
                            {
                                _logger.LogInformation($"Redirecting admin user {email} to admin dashboard");
                                return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
                            }

                            // Regular user - redirect based on returnUrl or default
                            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                            {
                                return Redirect(returnUrl);
                            }

                            return RedirectToAction("Index", "Home");
                        }
                    }
                    else
                    {
                        _logger.LogWarning($"Failed login attempt for email: {email}");

                        if (result.IsLockedOut)
                        {
                            ViewBag.Error = "Akun Anda terkunci. Silakan coba lagi nanti.";
                        }
                        else if (result.IsNotAllowed)
                        {
                            ViewBag.Error = "Login tidak diizinkan. Silakan konfirmasi email Anda.";
                        }
                        else
                        {
                            ViewBag.Error = "Email atau password salah";
                        }
                    }
                }
                else
                {
                    ViewBag.Error = "Email dan password harus diisi";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error during login for email: {email}");
                ViewBag.Error = "Terjadi kesalahan saat login. Silakan coba lagi.";
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            try
            {
                var userEmail = User.Identity?.Name;
                await _signInManager.SignOutAsync();
                _logger.LogInformation($"User {userEmail} logged out");

                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(string email, string password, string confirmPassword, string fullName)
        {
            try
            {
                if (password != confirmPassword)
                {
                    ViewBag.Error = "Password dan konfirmasi password tidak sama";
                    return View();
                }

                var user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    // Add other properties if needed
                };

                var result = await _userManager.CreateAsync(user, password);

                if (result.Succeeded)
                {
                    // Assign default role (User)
                    await _userManager.AddToRoleAsync(user, "User");

                    _logger.LogInformation($"New user registered: {email}");

                    // Auto login after registration
                    await _signInManager.SignInAsync(user, false);
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    foreach (var error in result.Errors)
                    {
                        ViewBag.Error += error.Description + " ";
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error during registration for email: {email}");
                ViewBag.Error = "Terjadi kesalahan saat registrasi. Silakan coba lagi.";
            }

            return View();
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}