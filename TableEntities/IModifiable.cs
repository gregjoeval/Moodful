using System;

namespace Moodful.TableEntities
{
    public interface IModifiable
    {
        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset? LastModified { get; set; }
    }
}
