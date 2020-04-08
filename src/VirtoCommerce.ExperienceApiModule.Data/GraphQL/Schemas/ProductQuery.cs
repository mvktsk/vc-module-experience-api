using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Builders;
using GraphQL.DataLoader;
using GraphQL.Resolvers;
using GraphQL.Types;
using GraphQL.Types.Relay.DataObjects;
using MediatR;
using VirtoCommerce.CatalogModule.Core.Model;
using VirtoCommerce.CatalogModule.Core.Model.Search;
using VirtoCommerce.ExperienceApiModule.Core.Requests;
using VirtoCommerce.ExperienceApiModule.GraphQLEx;

namespace VirtoCommerce.ExperienceApiModule.Data.GraphQL.Schemas
{
    public class ProductQuery : ISchemaBuilder
    {
        private readonly IMediator _mediator;
        private readonly IDataLoaderContextAccessor _dataLoader;
        public ProductQuery(IMediator mediator, IDataLoaderContextAccessor dataLoader)
        {
            _mediator = mediator;
            _dataLoader = dataLoader;
        }
        public void Build(ISchema schema)
        {
            var productField = new FieldType
            {
                Name = "product",
                Arguments = new QueryArguments(new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "id", Description = "id of the product" }),
                Type = GraphTypeExtenstionHelper.GetActualType<ProductType>(),
                Resolver = new AsyncFieldResolver<object>(async context =>
                {
                    var loader = _dataLoader.Context.GetOrAddBatchLoader<string, CatalogProduct>("productsLoader", (ids) => LoadProductsAsync(_mediator, ids));
                    return await loader.LoadAsync(context.GetArgument<string>("id"));
                })
            };
            schema.Query.AddField(productField);



            var productsConnectionBuilder = ConnectionBuilderExt.Create<ProductType, object>()
                .Name("products")
                .Argument<StringGraphType>("query", "the search phrase")
                .Argument<StringGraphType>("catalog", "the catalog id")
                .Unidirectional()
                .PageSize(20);

            productsConnectionBuilder.ResolveAsync(async context =>
                {
                    return await ResolveConnectionAsync(_mediator, context);
                });


            schema.Query.AddField(productsConnectionBuilder.FieldType);

        }

        public static async Task<IDictionary<string, CatalogProduct>> LoadProductsAsync(IMediator mediator, IEnumerable<string> ids)
        {
            var response = await mediator.Send(new LoadProductRequest { Ids = ids.ToArray(), ResponseGroup = ItemResponseGroup.ItemInfo.ToString() });
            return response.Products.ToDictionary(x => x.Id);
        }


        private static async Task<object> ResolveConnectionAsync(IMediator mediator, IResolveConnectionContext<object> context)
        {
            var first = context.First;
            var skip = Convert.ToInt32(context.After ?? 0.ToString());
            var criteria = new ProductSearchCriteria
            {
                Skip = skip,
                Take = first ?? context.PageSize ?? 10,
                ResponseGroup = ItemResponseGroup.ItemInfo.ToString(),
                Keyword = context.GetArgument<string>("query"),
                CatalogId = context.GetArgument<string>("catalog"),
            };
            var response = await mediator.Send(new SearchProductRequest { Criteria = criteria });


            return new Connection<CatalogProduct>()
            {
                Edges = response.Result.Results
                    .Select((x, index) =>
                        new Edge<CatalogProduct>()
                        {
                            Cursor = (skip + index).ToString(),
                            Node = x,
                        })
                    .ToList(),
                PageInfo = new PageInfo()
                {
                    HasNextPage = response.Result.TotalCount > (skip + first),
                    HasPreviousPage = skip > 0,
                    StartCursor = skip.ToString(),
                    EndCursor = Math.Min(response.Result.TotalCount, (int)(skip + first)).ToString()
                },
                TotalCount = response.Result.TotalCount,
            };
        }
    }
}