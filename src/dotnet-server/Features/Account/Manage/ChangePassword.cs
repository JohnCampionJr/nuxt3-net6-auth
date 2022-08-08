﻿namespace Features.Account.Manage;

public class ChangePassword
{
    public class Command : IRequest<Result>
    {
        public string OldPassword { get; set; }
        public string Password { get; set; }
        public string ConfirmPassword { get; set; }
    }

    public class CommandValidator : AbstractValidator<Command>
    {
        public CommandValidator()
        {
            RuleFor(p => p.OldPassword).NotEmpty().MinimumLength(8);
            RuleFor(p => p.Password).NotEmpty().MinimumLength(8);
            RuleFor(p => p.ConfirmPassword).Matches(v => v.Password);
        }
    }

    public class Result : BaseResult { }

    public class CommandHandler : IRequestHandler<Command, Result>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ClaimsPrincipal _user;

        public CommandHandler(UserManager<ApplicationUser> userManager, IUserAccessor user, SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _user = user.User;
            _signInManager = signInManager;
        }

        public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
        {
            var user = await _userManager.GetUserAsync(_user);
            var changePasswordResult = await _userManager.ChangePasswordAsync(user, request.OldPassword, request.Password);

            if (!changePasswordResult.Succeeded)
            {
                return new Result().Error(changePasswordResult.Errors.First().Description);
            }

            await _signInManager.RefreshSignInAsync(user);
            return new Result().Success("Your password has been changed.");
        }
    }
}
