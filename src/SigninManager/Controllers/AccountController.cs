using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AspNet.Security.OAuth.GitHub;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using SigninManager.Models;
using SigninManager.Models.AccountViewModels;

namespace SigninManager.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        //private readonly UserManager<ApplicationUser> _userManager;
       // private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger _logger;

        public AccountController(
            //UserManager<ApplicationUser> userManager,
            //SignInManager<ApplicationUser> signInManager,
            ILoggerFactory loggerFactory)
        {
            //_userManager = userManager;
            //_signInManager = signInManager;
            _logger = loggerFactory.CreateLogger<AccountController>();
        }

        //
        // GET: /Account/Login
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }
        
        
        //
        // POST: /Account/LogOff
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LogOff()
        {
            await HttpContext.Authentication.SignOutAsync(GitHubAuthenticationDefaults.AuthenticationScheme);
           // await _signInManager.SignOutAsync();
            _logger.LogInformation(4, "User logged out.");
            return RedirectToAction(nameof(HomeController.Index), "Home");
        }

        //
        // POST: /Account/ExternalLogin
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public IActionResult ExternalLogin(string provider, string returnUrl = null)
        {
            // Request a redirect to the external login provider.
            var redirectUrl = Url.Action("ExternalLoginCallback", "Account", new { ReturnUrl = returnUrl });
          //  var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
              return Challenge(new AuthenticationProperties { RedirectUri = "/"}, GitHubAuthenticationDefaults.AuthenticationScheme);
        }

        //
        // GET: /Account/ExternalLoginCallback
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ExternalLoginCallback(string returnUrl = null, string remoteError = null)
        {
            if (remoteError != null)
            {
                ModelState.AddModelError(string.Empty, $"Error from external provider: {remoteError}");
                return View(nameof(Login));
            }
            return RedirectToAction("Index","Home");
            //var info = await _signInManager.GetExternalLoginInfoAsync();
            //if (info == null)
            //{
            //    return RedirectToAction(nameof(Login));
            //}

            //// Sign in the user with this external login provider if the user already has a login.
            //var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false);
            //if (result.Succeeded)
            //{
            //    _logger.LogInformation(5, "User logged in with {Name} provider.", info.LoginProvider);
            //    return RedirectToLocal(returnUrl);
            //}
            //else
            //{
            //    // If the user does not have an account, then ask the user to create an account.
            //    ViewData["ReturnUrl"] = returnUrl;
            //    ViewData["LoginProvider"] = info.LoginProvider;
            //    var email = info.Principal.FindFirstValue(ClaimTypes.Email);
            //    return View("ExternalLoginConfirmation", new ExternalLoginConfirmationViewModel { Email = email });
            //}
        }
        
       
       
        #region Helpers

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }
        
        private IActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction(nameof(HomeController.Index), "Home");
            }
        }

        #endregion
    }
}
