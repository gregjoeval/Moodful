using FluentValidation;

namespace Moodful.Models
{
    public class TagValidator : AbstractValidator<Tag>
    {
        public TagValidator()
        {
            this.CascadeMode = CascadeMode.Stop;

            RuleFor(o => o.Id).IsGuid();
            RuleFor(o => o.Title).Length(0, 256);
            RuleFor(o => o.Color).Matches("^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$"); // hex color
        }
    }
}
