using AutoMapper;
using Microsoft.Azure.Cosmos.Table;
using System.Collections.Generic;
using System.Linq;

namespace Moodful.Services.Storage
{
    public class StorageService<TTableEntity, TEntity> where TTableEntity : ITableEntity, new() // Copied from type signature of CloudTable.ExecuteQuery()
    {
        private readonly IMapper Mapper;

        public StorageService(IMapper mapper)
        {
            Mapper = mapper;
        }

        private TTableEntity MapTo(TEntity entity, string userId)
        {
            var tableEntity = Mapper.Map<TTableEntity>(entity);
            tableEntity.PartitionKey = userId;

            return tableEntity;
        }

        public TEntity Create(CloudTable cloudTable, string userId, TEntity entity)
        {
            var tableEntity = MapTo(entity, userId);

            
            var operation = TableOperation.Insert(tableEntity);
            cloudTable.Execute(operation);
            return entity;
        }

        public IEnumerable<TEntity> Retrieve(CloudTable cloudTable, string userId)
        {
            var query = new TableQuery<TTableEntity>()
                .Where(TableQuery.GenerateFilterCondition(nameof(TableEntity.PartitionKey), QueryComparisons.Equal, userId))
                .OrderByDesc(nameof(TableEntity.Timestamp));
            var tableEntities = cloudTable.ExecuteQuery(query, null);
            var entities = Mapper.Map<IEnumerable<TEntity>>(tableEntities);
            return entities;
        }

        public TEntity RetrieveById(CloudTable cloudTable, string userId, string id)
        {
            var query = new TableQuery<TTableEntity>().Where(
                TableQuery.CombineFilters(
                    TableQuery.GenerateFilterCondition(nameof(TableEntity.PartitionKey), QueryComparisons.Equal, userId),
                    TableOperators.And,
                    TableQuery.GenerateFilterCondition(nameof(TableEntity.RowKey), QueryComparisons.Equal, id)
                )
            );
            var tableEntity = cloudTable.ExecuteQuery(query, null).FirstOrDefault();
            var entity = Mapper.Map<TEntity>(tableEntity);
            return entity;
        }

        public TEntity Update(CloudTable cloudTable, string userId, TEntity entity)
        {
            var tableEntity = MapTo(entity, userId);
            tableEntity.ETag = "*"; // forced unconditional update https://stackoverflow.com/a/37369338/7571132

            var operation = TableOperation.Replace(tableEntity);
            cloudTable.Execute(operation);
            return entity;
        }

        public TEntity Delete(CloudTable cloudTable, string userId, string id)
        {
            var entity = RetrieveById(cloudTable, userId, id);
            var tableEntity = MapTo(entity, userId);
            var operation = TableOperation.Delete(tableEntity);
            cloudTable.Execute(operation);
            return entity;
        }
    }
}
