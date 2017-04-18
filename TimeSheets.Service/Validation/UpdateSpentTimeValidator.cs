using System.Linq;
using Cmas.Services.TimeSheets.Dtos.Requests;
using FluentValidation;

namespace Cmas.Services.TimeSheets.Validation
{
    public class UpdateSpentTimeValidator : AbstractValidator<UpdateSpentTimesRequest>
    {
        public UpdateSpentTimeValidator()
        {
            RuleFor(request => request.SpentTime).Must(l => l.Count() <= 31).WithMessage("Days can not be more than 31");
            //RuleFor(request => request.SpentTime).SetCollectionValidator(new SpentTimeValidator());
        }
    }
}