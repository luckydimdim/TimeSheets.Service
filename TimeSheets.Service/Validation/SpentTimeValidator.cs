using System.Linq;
using Cmas.Services.TimeSheets.Dtos.Requests;
using FluentValidation;

namespace Cmas.Services.TimeSheets.Validation
{
    public class SpentTimeValidator : AbstractValidator<double>
    {
        public SpentTimeValidator()
        {
            RuleFor(time => time).LessThan(2).WithMessage("Нееет");
        }
    }
}