using System;

namespace Moodful.TableEntities
{
    public interface IIdentifiable
    {
        public string UserId { get; set; }

        public Guid Id { get; set; }
    }
}
