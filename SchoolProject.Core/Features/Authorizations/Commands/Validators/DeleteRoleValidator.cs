﻿using FluentValidation;
using Microsoft.Extensions.Localization;
using SchoolProject.Core.Features.Authorization.Commands.Models;
using SchoolProject.Core.Resources;

namespace SchoolProject.Core.Features.Authorization.Commands.Validators
{
    internal class DeleteRoleValidator : AbstractValidator<DeleteRoleCommand>
    {
        #region Fields
        private readonly IStringLocalizer<SharedResources> _localizer;
        #endregion

        #region Constructors
        public DeleteRoleValidator(IStringLocalizer<SharedResources> localizer)
        {

            _localizer = localizer;
            ApplyValidationsRules();
            ApplyCustomValidationsRules();
        }
        #endregion

        #region Actions
        public void ApplyValidationsRules()
        {
            RuleFor(x => x.Id)
                 .NotEmpty().WithMessage(_localizer[SharedResourcesKeys.NotEmpty])
                 .NotNull().WithMessage(_localizer[SharedResourcesKeys.Required]);
        }

        public void ApplyCustomValidationsRules()
        {

        }
        #endregion
    }
}
