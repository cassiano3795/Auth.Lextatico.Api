using Auth.Lextatico.Domain.Dtos.Message;
using Auth.Lextatico.Domain.Interfaces.Services;
using Auth.Lextatico.Domain.Models;
using Microsoft.AspNetCore.Identity;

namespace Auth.Lextatico.Domain.Services
{
    public class AuthService : IAuthService
    {
        private readonly IMessage _message;
        private readonly ITokenService _tokenService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        public AuthService(ITokenService tokenService,
                UserManager<ApplicationUser> userManger,
                SignInManager<ApplicationUser> signInManager,
                IMessage message)
        {
            _tokenService = tokenService;
            _userManager = userManger;
            _signInManager = signInManager;
            _message = message;
        }

        public async Task<bool> LogInAsync(string email, string password)
        {
            var result = await _signInManager.PasswordSignInAsync(email, password, false, true);

            if (!result.Succeeded)
            {
                var user = await _userManager.FindByEmailAsync(email);

                if (result.IsLockedOut)
                {
                    var lockoutEnd = user.LockoutEnd?.UtcDateTime
                        ?? DateTime.UtcNow + _signInManager.Options.Lockout.DefaultLockoutTimeSpan;

                    var endDate = TimeZoneInfo.ConvertTimeFromUtc(lockoutEnd, TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time"));

                    _message.AddError(string.Empty, $"Usuário bloqueado até: {endDate:HH:mm}. Aguarde e tente novamente.");
                }
                else if (result.IsNotAllowed)
                    _message.AddError(string.Empty, "Usuário não está liberado para fazer login.");
                else
                {
                    if (user != null)
                    {
                        var remainingAttempts =
                            _signInManager.Options.Lockout.MaxFailedAccessAttempts - user.AccessFailedCount;

                        _message.AddWarning($"Tentativas restantes antes do bloqueio: {remainingAttempts}");
                    }

                    _message.AddError(string.Empty, "Usuário ou senha incorreto.");
                }
            }

            return result.Succeeded;
        }

        public (string token, string refreshToken) GenerateFullJwt(string email)
        {
            return _tokenService
                    .WithUserManager(_userManager)
                    .WithEmail(email)
                    .WithJwtClaims()
                    .WithUserClaims()
                    .WithUserRoles()
                    .BuildToken();
        }
    }
}
