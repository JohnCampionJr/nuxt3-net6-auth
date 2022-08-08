﻿namespace Features.Account.Manage;

public class MfaForgetBrowser
{
    public class Command : IRequest<Result> { }

    public class Result : BaseResult { }

    public class CommandHandler : IRequestHandler<Command, Result>
    {
        private readonly SignInManager<ApplicationUser> _signInManager;

        public CommandHandler(SignInManager<ApplicationUser> signInManager)
        {
            _signInManager = signInManager;
        }

        public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
        {
            await _signInManager.ForgetTwoFactorClientAsync();

            return new Result().Success(
                "The current browser has been forgotten. When you login again from this browser you will be prompted for your Mfa code.");
        }
    }
}
