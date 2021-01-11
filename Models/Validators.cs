using FluentValidation;
using System;
using System.Collections.Generic;

namespace Moodful.Models
{
    internal static class Validators
    {
        public static IRuleBuilderOptions<T, IList<TElement>> ListMustContainFewerThan<T, TElement>(this IRuleBuilder<T, IList<TElement>> ruleBuilder, int num)
        {
            return ruleBuilder.Must(list => list.Count < num).WithMessage("The list contains too many items");
        }

        public static IRuleBuilderOptions<T, TElement> IsGuid<T, TElement>(this IRuleBuilder<T, TElement> ruleBuilder)
        {
            return ruleBuilder
                .NotNull()
                .NotEmpty()
                .Must((id) => Guid.TryParse(id.ToString(), out var guid));
        }
    }
}
