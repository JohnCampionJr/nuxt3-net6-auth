﻿namespace Features.Account.Manage;

public class MfaDisable
{
    public class Command : IRequest<Result> { }

    public class Result : BaseResult { }

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

            var disableMfaResult = await _userManager.SetTwoFactorEnabledAsync(user, false);
            if (!disableMfaResult.Succeeded)
            {
                return new Result().Error(
                    $"Unexpected error occurred disabling 2FA for user with ID '{_userManager.GetUserId(_user)}'.");
            }

            return new Result().Success(
                "Mfa has been disabled. You can reenable 2fa when you setup an authenticator app");
        }
    }
}
