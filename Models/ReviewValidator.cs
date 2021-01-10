using FluentValidation;

namespace Moodful.Models
{
    public class ReviewValidator : AbstractValidator<Review>
    {
        public ReviewValidator()
        {
            this.CascadeMode = CascadeMode.Stop;

            RuleFor(o => o.Id).IsGuid();
            RuleForEach(o => o.TagIds).IsGuid();
            RuleFor(o => o.Rating)
                .GreaterThanOrEqualTo(int.MinValue)
                .LessThanOrEqualTo(int.MaxValue);
        }
    }
}
