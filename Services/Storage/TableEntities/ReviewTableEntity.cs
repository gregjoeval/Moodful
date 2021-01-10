using Microsoft.Azure.Cosmos.Table;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Moodful.Services.Storage.TableEntities
{
    /// <summary>
    /// Reference: ReadEntity/WriteEntity https://stackoverflow.com/a/50781352/7571132
    /// </summary>
    public class ReviewTableEntity : TableEntity, IIdentifiable, IModifiable
    {
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
        
    }
}
