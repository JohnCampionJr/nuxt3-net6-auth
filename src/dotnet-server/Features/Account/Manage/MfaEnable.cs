﻿using QRCoder;

namespace Features.Account.Manage;

public class MfaEnable
{
    public class Query : IRequest<QueryResult> { }

    [TsInterface(Name = "MfaEnableResult")]
    public class QueryResult : BaseResult
    {
        public string SharedKey { get; set; }
        public string AuthenticatorUri { get; set; }
        public string QrCodeBase64 { get; set; }
    }

    public class Command : IRequest<Result>
    {
        public string VerificationCode { get; set; }
    }

    public class CommandValidator : AbstractValidator<Command>
    {
        public CommandValidator()
        {
            RuleFor(p => p.VerificationCode).NotEmpty().Length(6, 8);
        }
    }

    public class Result : BaseResult { }

    public class QueryHandler : IRequestHandler<Query, QueryResult>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ClaimsPrincipal _user;
        private readonly UrlEncoder _urlEncoder;

        private const string AuthenticatorUriFormat = "otpauth://totp/{0}:{1}?secret={2}&issuer={0}&digits=6";

        public QueryHandler(UserManager<ApplicationUser> userManager, IUserAccessor user, UrlEncoder urlEncoder)
        {
            _userManager = userManager;
            _urlEncoder = urlEncoder;
            _user = user.User;
        }

        public async Task<QueryResult> Handle(Query request, CancellationToken cancellationToken)
        {
            var user = await _userManager.GetUserAsync(_user);

            var result = await LoadSharedKeyAndQrCodeUriAsync(user);

            result.QrCodeBase64 = CreateQRCode(result.AuthenticatorUri);

            return result;
        }

        private async Task<QueryResult> LoadSharedKeyAndQrCodeUriAsync(ApplicationUser user)
        {
            var result = new QueryResult().Success();

            // Load the authenticator key & QR code URI to display on the form
            var unformattedKey = await _userManager.GetAuthenticatorKeyAsync(user);
            if (string.IsNullOrEmpty(unformattedKey))
            {
                await _userManager.ResetAuthenticatorKeyAsync(user);
                unformattedKey = await _userManager.GetAuthenticatorKeyAsync(user);
            }

            result.SharedKey = FormatKey(unformattedKey);

            var email = await _userManager.GetEmailAsync(user);
            result.AuthenticatorUri = GenerateQrCodeUri(email, unformattedKey);

            return result;
        }

        private static string CreateQRCode(string text)
        {
            var qrGenerator = new QRCodeGenerator();
            var qrCodeData = qrGenerator.CreateQrCode(text, QRCodeGenerator.ECCLevel.L);
            var qrCode = new PngByteQRCode(qrCodeData);
            var qrCodeImage = qrCode.GetGraphic(4);
            return Convert.ToBase64String(qrCodeImage);
        }

        private static string FormatKey(string unformattedKey)
        {
            var result = new StringBuilder();
            var currentPosition = 0;
            while (currentPosition + 4 < unformattedKey.Length)
            {
                result.Append(unformattedKey.Substring(currentPosition, 4)).Append(' ');
                currentPosition += 4;
            }
            if (currentPosition < unformattedKey.Length)
            {
                result.Append(unformattedKey.Substring(currentPosition));
            }

            return result.ToString().ToLowerInvariant();
        }

        private string GenerateQrCodeUri(string email, string unformattedKey)
        {
            return string.Format(
                AuthenticatorUriFormat,
                _urlEncoder.Encode("Blazor5Auth.Server"),
                _urlEncoder.Encode(email),
                unformattedKey);
        }

    }

    public class CommandHandler : IRequestHandler<Command, Result>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ClaimsPrincipal _user;

        public CommandHandler(UserManager<ApplicationUser> userManager, IUserAccessor user)
        {
            _userManager = userManager;
            _user = user.User;
        }

        public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
        {
            var user = await _userManager.GetUserAsync(_user);

            // Strip spaces and hypens
            var verificationCode = request.VerificationCode.Replace(" ", string.Empty).Replace("-", string.Empty);

            var isMfaTokenValid = await _userManager.VerifyTwoFactorTokenAsync(
                user, _userManager.Options.Tokens.AuthenticatorTokenProvider, verificationCode);

            if (!isMfaTokenValid)
            {
                var result = new Result().Invalid();
                result.ValidationErrors.Add("VerificationCode", "Verification code is invalid.");
                return result;
            }

            await _userManager.SetTwoFactorEnabledAsync(user, true);
            return new Result().Success("Your authenticator app has been verified.");
        }
    }
}
