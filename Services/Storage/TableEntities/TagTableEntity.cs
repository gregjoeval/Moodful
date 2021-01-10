using Microsoft.Azure.Cosmos.Table;
using System;
using System.Collections.Generic;

namespace Moodful.Services.Storage.TableEntities
{
    public class TagTableEntity : TableEntity, IIdentifiable, IModifiable
    {
        public string UserId { get; set; }

        public Guid Id { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset? LastModified { get; set; }

        public string Title { get; set; }

        public string Color { get; set; }
    }
}
