using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuthServer.Models;
using Infrastructure.Api;
using Infrastructure.Result.Implementation;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AuthServer.Controllers
{
    [Route("account")]
    public class AccountController: BaseController
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public AccountController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

            [HttpGet("do-something")]
            public async Task<IActionResult> DoSomething(string email, string password)
            {
                var user = new ApplicationUser()
                {
                    Email = email,
                    UserName = email
                };

                var result = await _userManager.CreateAsync(user, password);

                return Result(new Success<bool>(result.Succeeded));
            }
    }
}
