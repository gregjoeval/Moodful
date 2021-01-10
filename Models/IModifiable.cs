using System;

namespace Moodful.Models
{
    public interface IModifiable
    {
        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset? LastModified { get; set; }
    }
}
