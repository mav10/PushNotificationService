using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Validation;
using IdentityModel;
using MccSoft.PushNotification.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace MccSoft.PushNotification.App.Services.Authentication
{
    public class MobileCodeGrantValidator : IExtensionGrantValidator
    {
        private const int _randomCodeNumbers = 5;
        private const int _numberOfPatientCodeLength = 3;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<MobileCodeGrantValidator> _logger;
        private const int _codeLenght = _randomCodeNumbers + _numberOfPatientCodeLength;

        public MobileCodeGrantValidator(UserManager<User> userManager,
            ILogger<MobileCodeGrantValidator> logger)
        {
            _userManager = userManager;
            _logger = logger;
        }

        public async Task ValidateAsync(ExtensionGrantValidationContext context)
        {
            string? loginId = context.Request.Raw.Get(OidcConstants.TokenRequest.UserName);
            string? code = context.Request.Raw.Get(OidcConstants.TokenRequest.Code);

            if (string.IsNullOrEmpty(loginId) || string.IsNullOrEmpty(code))
            {
                LogInvalidLogin("Empty UserName or Code.", loginId);
                context.Result = new GrantValidationResult(TokenRequestErrors.InvalidRequest);
                return;
            }

            if (!new Regex("[0-9]").IsMatch(loginId))
            {
                LogInvalidLogin(
                    $"{nameof(loginId)} has invalid format: '{loginId}'",
                    loginId);
                context.Result = new GrantValidationResult(TokenRequestErrors.InvalidRequest)
                {
                    Error = $"{nameof(loginId)} has invalid format: '{loginId}'",
                };
                return;
            }

            if (!new Regex("[0-9]").IsMatch(code)  || code.Length != _codeLenght)
            {
                LogInvalidLogin(
                    $"{nameof(code)} has invalid format: "
                    + $"'{code}'.",
                    loginId);
                context.Result = new GrantValidationResult(TokenRequestErrors.InvalidRequest)
                {
                    Error =
                        $"{nameof(code)} has invalid format: "
                        + $"'{code}'",
                };
                return;
            }

            var (username, accessCode) = ParsePatientLoginFromCredentials(loginId, code);
            User user = await _userManager.FindByNameAsync(username);
            if (user == null)
            {
                LogInvalidLogin("There is no such registered user", loginId);
                context.Result = new GrantValidationResult(TokenRequestErrors.UnauthorizedClient, "login_invalidCredentials");
                return;
            }

            var isLocked = await _userManager.IsLockedOutAsync(user);
            if (isLocked)
            {
                var time = await _userManager.GetLockoutEndDateAsync(user);
                LogInvalidLogin($"User is blocked until {time?.ToString()}.", loginId);
                context.Result = new GrantValidationResult(TokenRequestErrors.UnauthorizedClient,
                    "login_user_locked",
                    new Dictionary<string, object> { { "remainingLockUntil", time } });
                return;
            }

            bool isAuthorized = await _userManager.CheckPasswordAsync(user, accessCode);
            if (isAuthorized)
            {
                await _userManager.SetLockoutEnabledAsync(user, false);
                await _userManager.ResetAccessFailedCountAsync(user);
                await _userManager.SetLockoutEndDateAsync(user, null);
                context.Result = new GrantValidationResult(user.Id, GrantType);
                return;
            }
            else
            {
                LogInvalidLogin("Invalid credentials", loginId);
                var failedAttemptCount = await _userManager.GetAccessFailedCountAsync(user);

                if (failedAttemptCount >= 4)
                {
                    await _userManager.SetLockoutEnabledAsync(user, true);
                    await _userManager.SetLockoutEndDateAsync(user,
                        DateTimeOffset.Now.Add(TimeSpan.FromMinutes(failedAttemptCount * 3)));
                }
                await _userManager.AccessFailedAsync(user);
                context.Result =
                    new GrantValidationResult(TokenRequestErrors.UnauthorizedClient, "login_invalidCredentials");
                return;
            }
        }

        private void LogInvalidLogin(string message, string? loginId)
        {
            _logger.LogWarning($"{message} LoginId: '{loginId}'.");
        }

        private (string, string) ParsePatientLoginFromCredentials(string loginId, string code)
        {
            var uniqueId = code.Substring(0, _numberOfPatientCodeLength);
            var actualCode = code.Substring(_numberOfPatientCodeLength, _randomCodeNumbers);

            if (uniqueId.Length + actualCode.Length != _codeLenght)
            {
                LogInvalidLogin("Invalid login and code length", loginId);
                return (loginId, code);
            }
            return ($"{loginId}{uniqueId}", actualCode.ToString());
        }

        public string GrantType => "mobile_app_code";
    }
}