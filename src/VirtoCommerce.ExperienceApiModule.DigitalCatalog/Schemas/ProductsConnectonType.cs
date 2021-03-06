using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GraphQL.Types;
using GraphQL.Types.Relay;
using GraphQL.Types.Relay.DataObjects;

namespace VirtoCommerce.ExperienceApiModule.DigitalCatalog.Schemas
{
    public class ProductsConnectonType<TNodeType> : ConnectionType<TNodeType, EdgeType<TNodeType>>
        where TNodeType : IGraphType
    {
        public ProductsConnectonType()
        {
            Field<ListGraphType<FilterFacetResultType>>("filter_facets",
               resolve: context =>
               {
                   return ((ProductsConnection<ExpProduct>)context.Source).Facets.OfType<FilterFacetResult>();
               });

            Field<ListGraphType<RangeFacetResultType>>("range_facets",
               resolve: context =>
               {
                   return ((ProductsConnection<ExpProduct>)context.Source).Facets.OfType<RangeFacetResult>();
               });

            Field<ListGraphType<TermFacetResultType>>("term_facets",
                resolve: context =>
                {
                    return ((ProductsConnection<ExpProduct>)context.Source).Facets.OfType<TermFacetResult>();
                });

        }

    }

    public class ProductsConnection<TNode> : Connection<TNode>
    {
        public IList<FacetResult> Facets { get; set; }
    }
}
