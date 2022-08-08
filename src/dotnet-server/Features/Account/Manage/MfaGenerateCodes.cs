﻿namespace Features.Account.Manage;

public class MfaGenerateCodes
{
    public class Command : IRequest<Result> { }

    public class Result : BaseResult
    {
        public string[] RecoveryCodes { get; set; }
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
            var isTwoFactorEnabled = await _userManager.GetTwoFactorEnabledAsync(user);
            var userId = await _userManager.GetUserIdAsync(user);
            if (!isTwoFactorEnabled)
            {
                return new Result().Error(
                    $"Cannot generate recovery codes for user with ID '{userId}' as they do not have 2FA enabled.");
            }

            var result = new Result().Success("You have generated new recovery codes.");

            result.RecoveryCodes = (await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10)).ToArray();

            return result;
        }
    }
}
