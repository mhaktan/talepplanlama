using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Abp.AspNetCore.Mvc.Controllers;
using talepplanlama.Authentication;
using talepplanlama.Entities;
using talepplanlama.EntityFrameworkCore;

namespace talepplanlama.Web.Host.Controllers
{
    [Route("api/[controller]/[action]")]
    public class TokenAuthController : AbpController
    {
        private readonly IConfiguration _configuration;

        public TokenAuthController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpPost]
        public ActionResult Authenticate([FromBody] AuthenticateModel model)
        {
            try
            {
                if (string.IsNullOrEmpty(model.UserNameOrEmailAddress) || string.IsNullOrEmpty(model.Password))
                {
                    return new JsonResult(new { success = false, error = "Invalid credentials" }) { StatusCode = 400 };
                }

                var connStr = _configuration.GetConnectionString("Default");
                var optionsBuilder = new DbContextOptionsBuilder();
                optionsBuilder.UseNpgsql(connStr);

                using var db = new talepplanlamaDbContext(optionsBuilder.Options);

                // !u.IsDeleted SART: burada ham DbContext kullaniliyor, ABP'nin soft-delete
                // filtresi DEVREDE DEGIL. AppUser FullAuditedEntity oldugu icin silme
                // soft-delete; filtre olmadan iki sorun cikiyordu:
                //   1) GUVENLIK: silinmis kullanici hala giris yapabiliyor.
                //   2) Ayni kullanici adi yeniden olusturulunca FirstOrDefault ESKI
                //      (silinmis) satiri seciyor, token yanlis user id tasiyor ve
                //      rol/onay atamalari hic eslesmiyor.
                var user = db.AppUsers
                    .Include(u => u.UserRoles).ThenInclude(ur => ur.Role).ThenInclude(r => r.RolePermissions)
                    .FirstOrDefault(u =>
                        (u.UserName == model.UserNameOrEmailAddress ||
                         u.EmailAddress == model.UserNameOrEmailAddress) &&
                        u.IsActive && !u.IsDeleted);

                if (user == null)
                {
                    return new JsonResult(new { success = false, error = "Invalid username or password" }) { StatusCode = 401 };
                }

                // Dual-format password verify: legacy BCrypt hashes (admin seed) + PBKDF2 hashes (CreateAsync via PasswordHelper)
                bool pwOk;
                if (user.PasswordHash != null && user.PasswordHash.StartsWith("PBKDF2$"))
                    pwOk = talepplanlama.Users.PasswordHelper.VerifyPbkdf2(model.Password, user.PasswordHash);
                else
                    pwOk = BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash);
                if (!pwOk)
                {
                    return new JsonResult(new { success = false, error = "Invalid username or password" }) { StatusCode = 401 };
                }

                var jwtConfig = _configuration.GetSection("Authentication:JwtBearer");
                var securityKey = jwtConfig["SecurityKey"];
                var issuer = jwtConfig["Issuer"];
                var audience = jwtConfig["Audience"];
                var expireMinutes = int.Parse(jwtConfig["ExpireMinutes"] ?? "1440");

                // Aggregate role names + permission names from UserRoles → RolePermissions
                var roleNames = user.UserRoles.Select(ur => ur.Role).Where(r => r != null && r.IsActive).Select(r => r.Name).Distinct().ToList();
                var permissionNames = user.UserRoles
                    .Where(ur => ur.Role != null && ur.Role.IsActive)
                    .SelectMany(ur => ur.Role.RolePermissions ?? new List<RolePermission>())
                    .Select(rp => rp.PermissionName)
                    .Distinct()
                    .ToList();

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.UserName),
                    new Claim(ClaimTypes.Email, user.EmailAddress),
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                };
                foreach (var rn in roleNames) claims.Add(new Claim(ClaimTypes.Role, rn));
                // ABP and ASP.NET Core read this claim type by default
                foreach (var pn in permissionNames) claims.Add(new Claim("permission", pn));

                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(securityKey));
                var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
                var token = new JwtSecurityToken(
                    issuer: issuer,
                    audience: audience,
                    claims: claims,
                    expires: DateTime.UtcNow.AddMinutes(expireMinutes),
                    signingCredentials: credentials
                );

                return new JsonResult(new
                {
                    success = true,
                    result = new AuthenticateResultModel
                    {
                        AccessToken = new JwtSecurityTokenHandler().WriteToken(token),
                        ExpireInSeconds = expireMinutes * 60,
                        UserId = user.Id,
                    }
                });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, error = ex.Message, details = ex.InnerException?.Message }) { StatusCode = 500 };
            }
        }
    }
}
