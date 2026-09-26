using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.Identity.Client.Platforms.Features.DesktopOs.Kerberos;
using ToDoApi.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.DotNet.Scaffolding.Shared.Messaging;
using System.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;



namespace ToDoApi.Controllers
{
    
[ApiController]
[Route("api/[controller]")]

public class AuthenticationController : ControllerBase
    {
        
public readonly UserManager<IdentityUser> _userManager;
public readonly IConfiguration _configuration;

public AuthenticationController (UserManager<IdentityUser> userManager, IConfiguration configuration)
        {
            
_userManager = userManager;
_configuration = configuration;

        }

[HttpPost("register")]
        public async Task<ActionResult> Register(Credentials credentials)
        {
        
            var user = new IdentityUser() {UserName = credentials.Username};
            // user.UserName = credentials.Username;
            var result = await _userManager.CreateAsync(user,credentials.Password); //Aquí mandamos el usuario a la base, y con credentials.password .Net nos ahorra el trabajo de hacer el PasswordHash, que es la incriptación.
            if(!result.Succeeded)
            {
                return BadRequest(result.Errors.Select(e => e.Description));
            } 

            return StatusCode(StatusCodes.Status201Created, new {message = "User created successfully"});

        }


[HttpPost("login")]
        public async Task<ActionResult> Login(Credentials credentials)
        {
        
            var user = await _userManager.FindByNameAsync(credentials.Username);
            
            if(user == null)
        return Unauthorized("User or password does not exist");

        var passwordValid = await _userManager.CheckPasswordAsync(user, credentials.Password);
        if (!passwordValid)
            return Unauthorized("User or password does not exist");

            var jwtSettings = _configuration.GetSection("Jwt");
            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]!));
            var signingCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.UserName!)
            };

            var expireInMinutes = double.Parse(jwtSettings["ExpireInMinutes"]!);

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expireInMinutes),
                signingCredentials: signingCredentials
            );

            
        return Ok(new {token = new JwtSecurityTokenHandler().WriteToken(token)});

        }








    }

}