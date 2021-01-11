using Microsoft.Azure.Cosmos.Table;

namespace Moodful.TableEntities
{
    public abstract class MappableTableEntity<TDestination> : TableEntity
    {
        public MappableTableEntity()
        {
        }

#pragma warning disable IDE0060 // Remove unused parameter
        public MappableTableEntity(string partitionKey, string rowKey)
#pragma warning restore IDE0060 // Remove unused parameter
        {
        }

#pragma warning disable IDE0060 // Remove unused parameter
        public MappableTableEntity(string partitionKey, string rowKey, TDestination model)
#pragma warning restore IDE0060 // Remove unused parameter
        {
        }

        public abstract TDestination MapTo();
    }
}
