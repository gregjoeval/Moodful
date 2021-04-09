using Microsoft.Azure.Cosmos.Table;
using System.Collections.Generic;
using System.Linq;

namespace Moodful.Extensions
{
    public static class CloudTableExtensions
    {
        public static TEntity CreateEntity<TEntity>(this CloudTable cloudTable, TEntity entity)
            where TEntity : ITableEntity
        {
            var operation = TableOperation.Insert(entity);
            cloudTable.Execute(operation);
            return entity;
        }

        public static IEnumerable<TEntity> RetrieveEntities<TEntity>(this CloudTable cloudTable, string userId)
            where TEntity : ITableEntity, new() // Copied from type signature of CloudTable.ExecuteQuery()
        {
            var query = new TableQuery<TEntity>()
                .Where(TableQuery.GenerateFilterCondition(nameof(TableEntity.PartitionKey), QueryComparisons.Equal, userId))
                .OrderByDesc(nameof(TableEntity.Timestamp));
            var entities = cloudTable.ExecuteQuery(query, null);
            return entities;
        }

        public static TEntity RetrieveEntity<TEntity>(this CloudTable cloudTable, string userId, string id)
            where TEntity : ITableEntity, new() // Copied from type signature of CloudTable.ExecuteQuery()
        {
            var query = new TableQuery<TEntity>().Where(
                TableQuery.CombineFilters(
                    TableQuery.GenerateFilterCondition(nameof(TableEntity.PartitionKey), QueryComparisons.Equal, userId),
                    TableOperators.And,
                    TableQuery.GenerateFilterCondition(nameof(TableEntity.RowKey), QueryComparisons.Equal, id)
                )
            );
            var entity = cloudTable.ExecuteQuery(query, null).FirstOrDefault();
            return entity;
        }

        public static TEntity UpdateEntity<TEntity>(this CloudTable cloudTable, TEntity entity)
            where TEntity : ITableEntity
        {
            var operation = TableOperation.Replace(entity);
            cloudTable.Execute(operation);
            return entity;
        }

        public static TEntity DeleteEntity<TEntity>(this CloudTable cloudTable, string userId, string id)
            where TEntity : ITableEntity, new()
        {
            var entity = cloudTable.RetrieveEntity<TEntity>(userId, id);
            if (entity == null)
            {
                return default;
            }

            var operation = TableOperation.Delete(entity);
            cloudTable.Execute(operation);
            return entity;
        }
    }
}
