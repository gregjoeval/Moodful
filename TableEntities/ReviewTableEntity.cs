using Microsoft.Azure.Cosmos.Table;
using Moodful.Models;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Moodful.TableEntities
{
    /// <remarks>
    /// Reference: ReadEntity/WriteEntity https://stackoverflow.com/a/50781352/7571132
    /// </remarks>
    public class ReviewTableEntity : MappableTableEntity<Review>, IIdentifiable, IModifiable
    {
        public ReviewTableEntity()
        {
        }

        public ReviewTableEntity(string partitionKey, string rowKey)
        {
            PartitionKey = partitionKey;
            RowKey = rowKey;
        }

        public ReviewTableEntity(string partitionKey, string rowKey, Review model)
        {
            PartitionKey = partitionKey;
            RowKey = rowKey;
            ETag = "*"; // ETag = "*" is a forced unconditional update https://stackoverflow.com/a/37369338/7571132

            UserId = partitionKey;
            Id = model.Id;
            CreatedAt = model.CreatedAt;
            LastModified = model.LastModified;
            Secret = model.Secret;
            Rating = model.Rating;
            Description = model.Description;
            TagIds = model.TagIds;
        }

        public string UserId { get; set; }

        public Guid Id { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset? LastModified { get; set; }

        public bool Secret { get; set; }

        public int Rating { get; set; }

        public string Description { get; set; }

        [IgnoreProperty]
        public IEnumerable<Guid> TagIds { get; set; }

        public override IDictionary<string, EntityProperty> WriteEntity(OperationContext operationContext)
        {
            var json = base.WriteEntity(operationContext);
            json[nameof(TagIds)] = new EntityProperty(JsonSerializer.Serialize(TagIds));
            return json;
        }

        public override void ReadEntity(IDictionary<string, EntityProperty> properties, OperationContext operationContext)
        {
            base.ReadEntity(properties, operationContext);
            if (properties.TryGetValue(nameof(TagIds), out EntityProperty value))
            {
                TagIds = JsonSerializer.Deserialize<IEnumerable<Guid>>(value.StringValue);
            }
        }

        public override Review MapTo()
        {
            if (this == null) return null;

            return new Review
            {
                Id = Id,
                CreatedAt = CreatedAt,
                LastModified = LastModified,
                Secret = Secret,
                Rating = Rating,
                Description = Description,
                TagIds = TagIds
            };
        }
    }
}
