using Moodful.Models;
using System;

namespace Moodful.TableEntities
{
    public class TagTableEntity : MappableTableEntity<Tag>, IIdentifiable, IModifiable
    {
        public TagTableEntity()
        {
        }

        public TagTableEntity(string partitionKey, string rowKey)
        {
            PartitionKey = partitionKey;
            RowKey = rowKey;
        }

        public TagTableEntity(string partitionKey, string rowKey, Tag model)
        {
            PartitionKey = partitionKey;
            RowKey = rowKey;
            ETag = "*"; // ETag = "*" is a forced unconditional update https://stackoverflow.com/a/37369338/7571132

            UserId = partitionKey;
            Id = model.Id;
            CreatedAt = model.CreatedAt;
            LastModified = model.LastModified;
            Title = model.Title;
            Color = model.Color;
        }

        public string UserId { get; set; }

        public Guid Id { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset? LastModified { get; set; }

        public string Title { get; set; }

        public string Avatar { get; set; }

        public string Color { get; set; }

        public override Tag MapTo()
        {
            if (this == null) return null;

            return new Tag
            {
                Id = Id,
                CreatedAt = CreatedAt,
                LastModified = LastModified,
                Title = Title,
                Avatar = Avatar,
                Color = Color
            };
        }
    }
}
