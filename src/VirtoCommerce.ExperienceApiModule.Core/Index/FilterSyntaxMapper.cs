using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using VirtoCommerce.SearchModule.Core.Model;

namespace VirtoCommerce.ExperienceApiModule.DigitalCatalog.Index
{
    public static class FilterSyntaxMapper
    {
        private abstract class FilterToIndexMapper
        {
            protected FilterToIndexMapper()
            {
            }
            public virtual bool CanMap(IFilter filter)
            {
                return false;
            }
            public virtual IFilter Map(IFilter filter)
            {
                return filter;
            }

            protected string GetFilterName(IFilter filter)
            {
                string result = null;
                if (filter is TermFilter termFilter)
                {
                    result = termFilter.FieldName;
                }
                else if (filter is RangeFilter rangeFilter)
                {
                    result = rangeFilter.FieldName;
                }
                return result;
            }
            protected IFilter SetFilterName(IFilter filter, string filterName)
            {
                if (filter is TermFilter termFilter)
                {
                    termFilter.FieldName = filterName;
                }
                else if (filter is RangeFilter rangeFilter)
                {
                    rangeFilter.FieldName = filterName;
                }
                return filter;
            }

            protected IFilter SetFilterValue(IFilter filter, string filterValue)
            {
                if (filter is TermFilter termFilter)
                {
                    termFilter.Values = new[] { filterValue };
                }
                else if (filter is RangeFilter rangeFilter)
                {
                    throw new NotSupportedException();
                }
                return filter;
            }
        }

        private class RegexpNameMapper : FilterToIndexMapper
        {
            public RegexpNameMapper(Regex filterPattern, string indexPattern)
            {
                FilterPattern = filterPattern;
                IndexPattern = indexPattern;
            }
            protected Regex FilterPattern { get; private set; }
            protected string IndexPattern { get; private set; }

            public override bool CanMap(IFilter filter)
            {
                var filterName = GetFilterName(filter);
                var result = filterName != null;
                if (result)
                {
                    result = FilterPattern.Match(filterName).Success;
                }
                return result;
            }
            public override IFilter Map(IFilter filter)
            {
                var newFilterName = FilterPattern.Replace(GetFilterName(filter), IndexPattern);
                return SetFilterName(filter, newFilterName);
            }
        }

        private class RegexpNameAndValueMapper : RegexpNameMapper
        {
            public RegexpNameAndValueMapper(Regex filterPattern, string indexPattern, string valuePattern)
                :base(filterPattern, indexPattern)
            {
                ValuePattern = valuePattern;
            }
            private string ValuePattern { get; set; }
          
            public override IFilter Map(IFilter filter)
            {
                base.Map(filter);
                return SetFilterValue(filter, ValuePattern);
            }
        }

        private static IList<FilterToIndexMapper> _allMappers = new List<FilterToIndexMapper>()
        {
            new RegexpNameMapper(new Regex(@"price.([A-Za-z]{3})", RegexOptions.Compiled | RegexOptions.IgnoreCase),"price_$1"),
            new RegexpNameMapper(new Regex(@"catalog.id", RegexOptions.Compiled | RegexOptions.IgnoreCase), "catalog"),
            new RegexpNameMapper(new Regex(@"category.path", RegexOptions.Compiled | RegexOptions.IgnoreCase), "__outline"),
            new RegexpNameMapper(new Regex(@"sku", RegexOptions.Compiled | RegexOptions.IgnoreCase), "code"),
            new RegexpNameMapper(new Regex(@"properties.([A-Za-z0-9_\s+])", RegexOptions.Compiled | RegexOptions.IgnoreCase), "$1")
        };

        public static IFilter MapFilterSyntax(IFilter filter)
        {
            foreach (var mapper in _allMappers)
            {
                if (mapper.CanMap(filter))
                {
                    return mapper.Map(filter);
                }
            }
            return filter;
        }

    }
}
