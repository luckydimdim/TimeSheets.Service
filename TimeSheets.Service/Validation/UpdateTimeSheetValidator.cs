using System.Linq;
using Cmas.Services.TimeSheets.Dtos.Requests;
using FluentValidation;

namespace Cmas.Services.TimeSheets.Validation
{
    public class UpdateTimeSheetValidator : AbstractValidator<UpdateTimeSheetRequest>
    {
        public UpdateTimeSheetValidator()
        {
            RuleFor(request => request.Month)
                .Must(month => (month <= 12 && month > 0))
                .WithMessage("Month must be between 1 and 12");

            RuleFor(request => request.Year).Must(year => (year > 2000)).WithMessage("Year must be greater than 2000");
        }
    }
}