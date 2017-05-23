using Cmas.Services.TimeSheets.Dtos.Requests;
using FluentValidation;

namespace Cmas.Services.TimeSheets.Validation
{
    public class UpdateTimeSheetValidator : AbstractValidator<UpdateTimeSheetRequest>
    {
        public UpdateTimeSheetValidator()
        {
            RuleFor(request => request)
                .Must( r=> r.From < r.Till)
                .WithMessage("From must be less than Till");
        }
    }
}