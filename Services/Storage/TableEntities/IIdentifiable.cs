using System;

namespace Moodful.Services.Storage.TableEntities
{
    public interface IIdentifiable

    {
        public string UserId { get; set; }

        public Guid Id { get; set; }
    }
}
