using System;

namespace Moodful.Services.Storage.TableEntities
{
    public interface IModifiable
    {
        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset? LastModified { get; set; }
    }
}
