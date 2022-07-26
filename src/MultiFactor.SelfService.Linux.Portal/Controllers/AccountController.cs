﻿using Microsoft.AspNetCore.Mvc;
using MultiFactor.SelfService.Linux.Portal.Exceptions;
using MultiFactor.SelfService.Linux.Portal.Models;
using MultiFactor.SelfService.Linux.Portal.Stories.LoginStory;
using MultiFactor.SelfService.Linux.Portal.Stories.SignOutStory;

namespace MultiFactor.SelfService.Linux.Portal.Controllers
{
    public class AccountController : ControllerBase
    {
        public AccountController(SignOutStory signOutStory) : base(signOutStory)
        {
        }

        public IActionResult Login(string returnUrl)
        {
            var referer = Request.Headers["Referer"];
            return View(new LoginModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginModel model, string returnUrl, [FromServices] LoginStory loginStory)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var result = await loginStory.ExecuteAsync(model, new Dto.MultiFactorClaimsDto("", ""));
                return result;
            }
            catch (ModelStateErrorException ex)
            {
                // TODO: move this handling logic to the some custom FILTER and register GLOBALLY.
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(model);
            }
        }   
    }
}
