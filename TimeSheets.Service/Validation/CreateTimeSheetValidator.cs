using Cmas.Services.TimeSheets.Dtos;
using Cmas.Services.TimeSheets.Dtos.Requests;
using FluentValidation;

namespace Cmas.Services.TimeSheets.Validation
{
    public class CreateTimeSheetValidator : AbstractValidator<CreateTimeSheetRequest>
    {
        public CreateTimeSheetValidator()
        {
            RuleFor(request => request.CallOffOrderId).NotEmpty().WithMessage("You must specify a call-off-order's id");
        }
    }
}