/**
MIT License

Copyright (c) 2018 Florian Krönert

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
using System.Runtime.Serialization.Json;
using System.IO;
using System.Text;

namespace Xrm.Oss.FluentQuery
{
    public static class IOrganizationServiceFluentQuery
    {
        /**
         * <summary>
         * Creates a new fluent query for your early bound entity of type T, while automatically using the entity name of type T. Results will also be of type T.
         * If developing late bound, use the non-generic Query function.
         * </summary>
         * <returns>Fluent Query object. Use Retrieve or RetrieveAll for getting the results.</returns>
         */
        public static IFluentQuery<T> Query<T>(this IOrganizationService service) where T : Entity, new()
        {
            return new FluentQuery<T>(new T().LogicalName, service);
        }

        /**
         * <summary>
         * Creates a new fluent query for your early bound entity of type T, while automatically using the entity name of type T. Results will also be of type T.
         * If developing late bound, use the non-generic Query function.
         * </summary>
         * <param name="entityName">The logical name of the entity you want to query.</param>
         * <returns>Fluent Query object. Use Retrieve or RetrieveAll for getting the results.</returns>
         */
        [Obsolete("Entity name is no longer needed for early bound queries, use the parameterless overload. For late bound queries, use the non generic overload.")]
        public static IFluentQuery<T> Query<T>(this IOrganizationService service, string entityName) where T : Entity, new()
        {
            return new FluentQuery<T>(entityName, service);
        }

        /**
         * <summary>
         * Creates a new fluent query in late binding style. Results will be of type Entity.
         * If developing early bound, use the generic Query function.
         * </summary>
         * <param name="entityName">The logical name of the entity you want to query.</param>
         * <returns>Fluent Query object. Use Retrieve or RetrieveAll for getting the results.</returns>
         */
        public static IFluentQuery<Entity> Query(this IOrganizationService service, string entityName)
        {
            return new FluentQuery<Entity>(entityName, service);
        }
    }

    public interface IFluentQuery<T> where T : Entity
    {
        /**
         * <summary>
         * Adds the given columns to your query. Multiple calls will just add to the existing columns.
         * </summary>
         * <param name="columns">Params array of your columns.</param>
         */
        IFluentQuery<T> IncludeColumns(params string[] columns);

        /**
         * <summary>
         * Adds all columns to the query. This is disadvised, specify the columns you need if possible.
         * </summary>
         */
        IFluentQuery<T> IncludeAllColumns();

        /**
         * <summary>
         * Returns the Query Expression that represents the current fluent query.
         * </summary>
         */
        QueryExpression Expression { get; }

        /**
         * <summary>
         * Retrieves the first page for your query.
         * </summary>
         * <returns>Records retrieved from your query.</returns>
         */
        List<T> Retrieve();

        /**
         * <summary>
         * Retrieves all pages for your query.
         * </summary>
         * <returns>Records retrieved from your query.</returns>
         */
        List<T> RetrieveAll();

        /**
         * <summary>
         * Use this for setting further options in your query.
         * </summary>
         */
        IFluentQuerySetting<T> With { get; }

        /**
         * <summary>
         * Adds a link to a connected entity.
         * </summary> 
         * <param name="definition">Action for setting the link properties. Use a lambda for readability.</param>
         */
        IFluentQuery<T> Link(Action<IFluentLinkEntity> definition);

        /**
         * <summary>
         * Adds filter conditions to your query.
         * </summary> 
         * <remarks>Multiple calls to this method currently override the existing filter.</remarks>
         * <param name="definition">Action for setting the filter properties. Use a lambda for readability.</param>
         */
        IFluentQuery<T> Where(Action<IFluentFilterExpression> definition);

        /**
         * <summary>
         * Instructs to use the supplied cache.
         * Cache keys will automatically be generated by the used query expression.
         * Retrieving and setting of the cache items is done automatically when executing retrieve.
         * </summary>
         * <param name="cache">The memory cache to use for getting and setting results</param>
         * <param name="absoluteExpiration">Expiration date for cached results</param>
         */
        IFluentQuery<T> UseCache(MemoryCache cache, DateTimeOffset absoluteExpiration);

        /**
         * <summary>
         * Adds another condition to the top level filter expression.
         * </summary>
         * <param name="definition">The condition expression to add.</param>
         */
        void AddCondition(Action<IFluentConditionExpression> definition);

        /**
         * <summary>
         * Adds a child filter to your top level filter.
         * </summary> 
         * <param name="definition">Action for setting the filter properties. Use a lambda for readability.</param>
         */
        void AddFilter(Action<IFluentFilterExpression> definition);

        /**
         * <summary>
         * Adds an order expression to your query.
         * </summary> 
         * <param name="definition">Action for setting the order properties. Use a lambda for readability.</param>
         */
        IFluentQuery<T> Order(Action<IFluentOrderExpression> definition);
    }

    public interface IFluentQuerySetting<T> where T : Entity
    {
        /**
         * <summary>
         * Set this for defining how many records you want to retrieve. The first top X records will be retrieved.
         * </summary>
         * <param name="topCount">Top X count of records to retrieve</param>
         */
        IFluentQuery<T> RecordCount(int? topCount);

        /**
         * <summary>
         * Defines whether the record should be locked for retrieval. Not locking is a recommended best practice, but might lead to dirty reads.
         * </summary>
         * <remarks>Default of true is recommended as best practice. Dirty reads might occur if data is written to this record simultaneously. Turn off if you know what you're doing.</remarks>
         * <param name="useLock">True for locking the database record for retrieval, false otherwise.</param>
         */
        IFluentQuery<T> DatabaseLock(bool useLock = true);

        /**
         * <summary>
         * Specifies whether duplicate records in your query will be filtered out or not.
         * </summary>
         * <param name="unique">True for only returning unique records, false otherwise</param>
         */
        IFluentQuery<T> UniqueRecords(bool unique = true);

        /**
         * <summary>
         * Adds paging info to your query, such as page size, page number or paging cookie.
         * </summary>
         * <remarks>Use retrieve all for automatic retrieval of all records using paging.</remarks>
         * <param name="definition">Action for setting the paging info properties. Use a lambda for readability.</param>
         */
        IFluentQuery<T> PagingInfo(Action<IFluentPagingInfo> definition);

        /**
         * <summary>
         * Determines whether to use a paging cookie when retrieving all records with paging. Speeds up retrieval, but may loose to result loss if you're adding link entities.
         * More on this topic: https://truenorthit.co.uk/2014/07/19/dynamics-crm-paging-cookies-some-gotchas/
         * </summary>
         * <remarks>Paging Cookies are not used by default</remarks>
         * <param name="useCookie">True for using cookie, false otherwise</param>
         */
        IFluentQuery<T> PagingCookie(bool useCookie = true);

        /**
         * <summary>
         * Specifies whether the total record count of your query results should be retrieved.
         * </summary>
         * <param name="returnTotalRecordCount">True for returning total record count, false otherwise.</param>
         */
        IFluentQuery<T> TotalRecordCount(bool returnTotalRecordCount = true);
    }

    public class FluentQuery<T> : IFluentQuery<T>, IFluentQuerySetting<T> where T : Entity
    {
        private QueryExpression _query;
        private bool _usePagingCookie;

        private IOrganizationService _service;
        private MemoryCache _cache;
        private DateTimeOffset _absoluteExpiration;

        public FluentQuery(string entityName, IOrganizationService service)
        {
            _query = new QueryExpression
            {
                EntityName = entityName,
                NoLock = true
            };

            _service = service;
            _usePagingCookie = false;
        }

        public IFluentQuery<T> IncludeColumns(params string[] columns)
        {
            _query.ColumnSet.AllColumns = false;
            _query.ColumnSet.AddColumns(columns);

            return this;
        }

        public IFluentQuery<T> IncludeAllColumns()
        {
            _query.ColumnSet.AllColumns = true;

            return this;
        }

        public IFluentQuerySetting<T> With
        {
            get
            {
                return this;
            }
        }

        public IFluentQuery<T> RecordCount(int? topCount)
        {
            _query.TopCount = topCount;

            return this;
        }

        public IFluentQuery<T> DatabaseLock(bool useLock = true)
        {
            _query.NoLock = !useLock;

            return this;
        }

        public IFluentQuery<T> PagingCookie(bool useCookie = true)
        {
            _usePagingCookie = useCookie;

            return this;
        }
        
        private string GenerateQueryCacheKey(QueryExpression query)
        {
            using (var memoryStream = new MemoryStream())
            {
                var serializer = new DataContractJsonSerializer(typeof(QueryExpression));
                serializer.WriteObject(memoryStream, query);

                var serialized = Encoding.UTF8.GetString(memoryStream.ToArray());
                return serialized;
            }
        }

        public List<T> ReturnFromCache()
        {
            if (_cache != null)
            {
                var key = GenerateQueryCacheKey(_query);

                if (_cache.Contains(key))
                {
                    return _cache.Get(key) as List<T>;
                }
            }

            return null;
        }

        public void SetCacheResult(List<T> result)
        {
            if (_cache != null)
            {
                var key = GenerateQueryCacheKey(_query);

                _cache.Set(key, result, _absoluteExpiration);
            }
        }

        public List<T> Retrieve()
        {
            var cacheResult = ReturnFromCache();

            if (cacheResult != null)
            {
                return cacheResult;
            }

            var result = _service.RetrieveMultiple(_query).Entities.Select(e => e.ToEntity<T>())
                .ToList();

            SetCacheResult(result);

            return result;
        }

        public List<T> RetrieveAll()
        {
            var cacheResult = ReturnFromCache();

            if (cacheResult != null)
            {
                return cacheResult;
            }

            var records = new List<T>();

            var previousPageNumber = _query.PageInfo.PageNumber;
            var previousPagingCookie = _query.PageInfo.PagingCookie;

            var moreRecords = false;
            var pageNumber = previousPageNumber;
            string pagingCookie = previousPagingCookie;

            do
            {
                _query.PageInfo.PageNumber = pageNumber;

                if (_usePagingCookie)
                {
                    _query.PageInfo.PagingCookie = pagingCookie;
                }

                var response = _service.RetrieveMultiple(_query);
                var result = response.Entities.Select(e => e.ToEntity<T>())
                    .ToList();

                records.AddRange(result);

                moreRecords = response.MoreRecords;
                pagingCookie = response.PagingCookie;

                pageNumber++;
            }
            while (moreRecords);

            _query.PageInfo.PageNumber = previousPageNumber;
            _query.PageInfo.PagingCookie = previousPagingCookie;

            SetCacheResult(records);

            return records;
        }

        public IFluentQuery<T> UniqueRecords(bool unique = true)
        {
            _query.Distinct = true;

            return this;
        }

        public IFluentQuery<T> Link(Action<IFluentLinkEntity> definition)
        {
            var link = new FluentLinkEntity();

            definition(link);

            _query.LinkEntities.Add(link.GetLinkEntity());

            return this;
        }

        public IFluentQuery<T> Where(Action<IFluentFilterExpression> definition)
        {
            var filter = new FluentFilterExpression();

            definition(filter);

            _query.Criteria = filter.GetFilter();

            return this;
        }

        public void AddCondition(Action<IFluentConditionExpression> definition)
        {
            var condition = new FluentConditionExpression();

            definition(condition);

            _query.Criteria.AddCondition(condition.GetCondition());
        }

        public void AddFilter(Action<IFluentFilterExpression> definition)
        {
            var filter = new FluentFilterExpression();

            definition(filter);

            _query.Criteria.AddFilter(filter.GetFilter());
        }

        public IFluentQuery<T> Order(Action<IFluentOrderExpression> definition)
        {
            var order = new FluentOrderExpression();

            definition(order);
            
            _query.Orders.Add(order.GetOrder());

            return this;
        }

        public IFluentQuery<T> PagingInfo(Action<IFluentPagingInfo> definition)
        {
            var PagingInfo = new FluentPagingInfo();

            definition(PagingInfo);

            _query.PageInfo = PagingInfo.GetPagingInfo();

            return this;
        }

        public IFluentQuery<T> TotalRecordCount(bool returnTotalRecordCount = true)
        {
            _query.PageInfo.ReturnTotalRecordCount = true;

            return this;
        }

        public IFluentQuery<T> UseCache(MemoryCache cache, DateTimeOffset absoluteExpiration)
        {
            _cache = cache;
            _absoluteExpiration = absoluteExpiration;

            return this;
        }

        public QueryExpression Expression
        {
            get
            {
                return _query;
            }
        }
    }

    public interface IFluentLinkEntity
    {
        /**
         * <summary>
         * Logical name of entity that the link is created from.
         * </summary>
         * <param name="entityName">Entity Logical Name</param>
         */
        IFluentLinkEntity FromEntity(string entityName);

        /**
         * <summary>
         * Logical name of attribute that the link is created from.
         * </summary>
         * <param name="attributeName">Attribute Logical Name</param>
         */
        IFluentLinkEntity FromAttribute(string attributeName);

        /**
         * <summary>
         * Logical name of entity that the link is created to.
         * </summary>
         * <param name="entityName">Entity Logical Name</param>
         */
        IFluentLinkEntity ToEntity(string entityName);

        /**
         * <summary>
         * Logical name of attribute that the link is created to.
         * </summary>
         * <param name="attributeName">Attribute Logical Name</param>
         */
        IFluentLinkEntity ToAttribute(string attributeName);

        /**
         * <summary>
         * Adds the given columns to the link entity. Multiple calls will just add to the existing columns.
         * </summary>
         * <param name="columns">Params array of your columns.</param>
         */
        IFluentLinkEntity IncludeColumns(params string[] columns);

        /**
        * <summary>
        * Use this for setting further options of your link.
        * </summary>
        */
        IFluentLinkEntitySetting With { get; }

        /**
         * <summary>
         * Adds a nested link to this link.
         * </summary>
         * <param name="definition">Action for setting the link properties. Use a lambda for readability.</param>
         */
        IFluentLinkEntity Link(Action<IFluentLinkEntity> definition);

        /**
         * <summary>
         * Adds filter conditions to your link.
         * </summary> 
         * <remarks>Multiple calls to this method currently override the existing filter.</remarks>
         * <param name="definition">Action for setting the filter properties. Use a lambda for readability.</param>
         */
        IFluentLinkEntity Where(Action<IFluentFilterExpression> definition);
    }

    public interface IFluentLinkEntitySetting
    {
        /**
         * <summary>
         * Sets an alias for the results of this link entity.
         * </summary>
         * <param name="name">Alias to set in results.</param>
         */
        IFluentLinkEntity Alias(string name);

        /**
         * <summary>Join type of this link.</summary>
         * <param name="joinOperator">Join type to use.</param>
         */
        IFluentLinkEntity LinkType(JoinOperator joinOperator);
    }

    public class FluentLinkEntity : IFluentLinkEntity, IFluentLinkEntitySetting
    {
        private LinkEntity _linkEntity;

        public FluentLinkEntity()
        {
            _linkEntity = new LinkEntity
            {
                Columns = new ColumnSet()
            };
        }

        public IFluentLinkEntitySetting With
        {
            get
            {
                return this;
            }
        }

        public IFluentLinkEntity Alias(string name)
        {
            _linkEntity.EntityAlias = name;

            return this;
        }

        public IFluentLinkEntity FromAttribute(string attributeName)
        {
            _linkEntity.LinkFromAttributeName = attributeName;

            return this;
        }

        public IFluentLinkEntity FromEntity(string entityName)
        {
            _linkEntity.LinkFromEntityName = entityName;

            return this;
        }

        public IFluentLinkEntity IncludeColumns(params string[] columns)
        {
            _linkEntity.Columns.AddColumns(columns);

            return this;
        }

        public IFluentLinkEntity Where(Action<IFluentFilterExpression> definition)
        {
            var filter = new FluentFilterExpression();

            definition(filter);

            _linkEntity.LinkCriteria = filter.GetFilter();

            return this;
        }

        public IFluentLinkEntity Link(Action<IFluentLinkEntity> definition)
        {
            var link = new FluentLinkEntity();

            definition(link);

            _linkEntity.LinkEntities.Add(link.GetLinkEntity());

            return this;
        }

        public IFluentLinkEntity LinkType(JoinOperator joinOperator)
        {
            _linkEntity.JoinOperator = joinOperator;

            return this;
        }

        public IFluentLinkEntity ToAttribute(string attributeName)
        {
            _linkEntity.LinkToAttributeName = attributeName;

            return this;
        }

        public IFluentLinkEntity ToEntity(string entityName)
        {
            _linkEntity.LinkToEntityName = entityName;

            return this;
        }

        internal LinkEntity GetLinkEntity()
        {
            return _linkEntity;
        }
    }

    public interface IFluentFilterExpression
    {
        /**
        * <summary>
        * Use this for setting further options of your filter.
        * </summary>
        */
        IFluentFilterExpressionSetting With { get; }

        /**
        * <summary>
        * Use this for adding a condition on an attribute to your filter.
        * </summary>
        * <remarks>Multiple calls to Attribute will add to the existing ones.</remarks>
        * <param name="definition">Action for setting the attribute properties. Use a lambda for readability.</param>
        */
        IFluentFilterExpression Attribute(Action<IFluentConditionExpression> definition);

        /**
         * <summary>
         * Adds nested filter conditions to your filter.
         * </summary> 
         * <remarks>Multiple calls to this method add to the existing filter conditions.</remarks>
         * <param name="definition">Action for setting the filter properties. Use a lambda for readability.</param>
         */
        IFluentFilterExpression Where(Action<IFluentFilterExpression> definition);
    }

    public interface IFluentFilterExpressionSetting
    {
        /**
         * <summary>
         * Sets the logical operator for chaining multiple conditions in this filter.
         * </summary>
         */
        IFluentFilterExpression Operator(LogicalOperator filterOperator);
    }

    public class FluentFilterExpression : IFluentFilterExpression, IFluentFilterExpressionSetting
    {
        private FilterExpression _filter;

        public FluentFilterExpression()
        {
            _filter = new FilterExpression();
        }

        public IFluentFilterExpressionSetting With
        {
            get
            {
                return this;
            }
        }

        public IFluentFilterExpression Operator(LogicalOperator filterOperator)
        {
            _filter.FilterOperator = filterOperator;

            return this;
        }

        public IFluentFilterExpression Where(Action<IFluentFilterExpression> definition)
        {
            var filter = new FluentFilterExpression();

            definition(filter);

            _filter.Filters.Add(filter.GetFilter());

            return this;
        }

        public IFluentFilterExpression Attribute(Action<IFluentConditionExpression> definition)
        {
            var condition = new FluentConditionExpression();

            definition(condition);

            _filter.Conditions.Add(condition.GetCondition());

            return this;
        }

        internal FilterExpression GetFilter()
        {
            return _filter;
        }
    }

    public interface IFluentConditionExpression
    {
        /**
         * <summary>
         * Set the entity name that your condition attribute targets, if it is not the main entity.
         * </summary>
         * <param name="entityName">Entity Logical Name</param>
         */
        IFluentConditionExpression Of(string entityName);

        /**
         * <summary>
         * Set the logical name of the attribute that your condition targets.
         * </summary>
         * <param name="attributeName">Attribute logical name</param>
         */
        IFluentConditionExpression Named(string attributeName);

        /**
         * <summary>
         * Set the condition operator for your condition.
         * </summary>
         * <param name="conditionOperator">Condition Operator Enum</param>
         */
        IFluentConditionExpression Is(ConditionOperator conditionOperator);

        /**
         * <summary>
         * Sets the value for the condition.
         * </summary>
         * <param name="value">Single object, use object array overload if necessary.</param>
         */
        IFluentConditionExpression Value<T>(T value);

        /**
         * <summary>
         * Sets the values for the condition.
         * </summary>
         * <param name="value">Object enumeration, use object overload if necessary.</param>
         */
        IFluentConditionExpression Values<T>(IEnumerable<T> values);

        /**
         * <summary>
         * Alias for Value, provides better readability on Equal conditions.
         * Sets the value for the condition.
         * </summary>
         * <param name="value">Single object, use object array overload if necessary.</param>
         */
        IFluentConditionExpression To<T>(T value);

        /**
        * <summary>
        * Alias for Value, provides better readability on Equal conditions.
        * Sets the values for the condition.
        * </summary>
        * <param name="value">Object enumeration, use object overload if necessary.</param>
        */
        IFluentConditionExpression ToMany<T>(IEnumerable<T> values);
    }

    public class FluentConditionExpression : IFluentConditionExpression
    {
        private ConditionExpression _condition;

        public FluentConditionExpression()
        {
            _condition = new ConditionExpression();
        }

        public IFluentConditionExpression Is(ConditionOperator conditionOperator)
        {
            _condition.Operator = conditionOperator;

            return this;
        }

        public IFluentConditionExpression Named(string attributeName)
        {
            _condition.AttributeName = attributeName;

            return this;
        }

        public IFluentConditionExpression Of(string entityName)
        {
            _condition.EntityName = entityName;

            return this;
        }

        public IFluentConditionExpression To<T>(T value)
        {
            return Value(value);
        }

        public IFluentConditionExpression ToMany<T>(IEnumerable<T> values)
        {
            return Values(values);
        }

        public IFluentConditionExpression Value<T>(T value)
        {
            _condition.Values.Add(value);

            return this;
        }

        public IFluentConditionExpression Values<T>(IEnumerable<T> values)
        {
            foreach (var value in values)
            {
                _condition.Values.Add(value);
            }

            return this;
        }

        internal ConditionExpression GetCondition ()
        {
            return _condition;
        }
    }

    public interface IFluentOrderExpression
    {
        /**
         * <summary>
         * Set the attribute name to order by.
         * </summary>
         * <param name="attributeName">Attribute logical name</param>
         */
        IFluentOrderExpression By(string attributeName);

        /**
         * <summary>
         * Sets the sort order to be ascending.
         * </summary>
         */
        IFluentOrderExpression Ascending();

        /**
         * <summary>
         * Sets the sort order to be descending.
         * </summary>
         */
        IFluentOrderExpression Descending();
    }

    public class FluentOrderExpression : IFluentOrderExpression
    {
        private OrderExpression _order;

        public FluentOrderExpression ()
        {
            _order = new OrderExpression();
        }
        
        public IFluentOrderExpression By(string attributeName)
        {
            _order.AttributeName = attributeName;

            return this;
        }

        public IFluentOrderExpression Ascending()
        {
            _order.OrderType = OrderType.Ascending;

            return this;
        }

        public IFluentOrderExpression Descending()
        {
            _order.OrderType = OrderType.Descending;

            return this;
        }

        internal OrderExpression GetOrder()
        {
            return _order;
        }
    }

    public interface IFluentPagingInfo
    {
        /**
         * <summary>
         * Set the page number to retrieve. Is set to 1 by default.
         * </summary>
         * <param name="number">Number of the page, starts at 1.</param>
         */
        IFluentPagingInfo PageNumber(int number);

        /**
         * <summary>
         * Set the paging cookie for retrieving records from pages after the first.
         * </summary>
         * <remarks>Use retrieve all for automatic retrieval of all records using paging.</remarks>
         * <param name="pagingCookie">Paging cookie retrieved during last query response.</param>
         */
        IFluentPagingInfo PagingCookie(string pagingCookie);

        /**
         * <summary>
         * Set the size of each page.
         * </summary>
         * <param name="number">Number of records to return per page.</param>
         */
        IFluentPagingInfo PageSize(int number);

        /**
         * <summary>
         * Specifies whether the total record count of your query results should be retrieved.
         * </summary>
         * <param name="returnTotal">True for returning total record count, false otherwise.</param>
         */
        IFluentPagingInfo ReturnTotalRecordCount(bool returnTotal = true);
    }

    public class FluentPagingInfo : IFluentPagingInfo
    {
        private PagingInfo _pagingInfo;

        public FluentPagingInfo()
        {
            _pagingInfo = new PagingInfo
            {
                PageNumber = 1
            };
        }

        public IFluentPagingInfo PageNumber(int number)
        {
            _pagingInfo.PageNumber = number;

            return this;
        }

        public IFluentPagingInfo PageSize(int number)
        {
            _pagingInfo.Count = number;

            return this;
        }

        public IFluentPagingInfo PagingCookie(string pagingCookie)
        {
            _pagingInfo.PagingCookie = pagingCookie;

            return this;
        }

        public IFluentPagingInfo ReturnTotalRecordCount(bool returnTotal = true)
        {
            _pagingInfo.ReturnTotalRecordCount = returnTotal;

            return this;
        }

        public PagingInfo GetPagingInfo()
        {
            return _pagingInfo;
        }
    }
}
