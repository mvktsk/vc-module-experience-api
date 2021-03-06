using System.Reflection;
using VirtoCommerce.ExperienceApiModule.Core;
using VirtoCommerce.ExperienceApiModule.Core.Binding;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.SearchModule.Core.Model;

namespace VirtoCommerce.ExperienceApiModule.DigitalCatalog.Binding
{
    public class ExpProductBinder : IIndexModelBinder
    {
        public BindingInfo BindingInfo { get; set; }

        public virtual object BindModel(SearchDocument doc)
        {
            var result = AbstractTypeFactory<ExpProduct>.TryCreateInstance();
            var productProperties = result.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var property in productProperties)
            {
                var binder = property.GetIndexModelBinder();

                if (binder != null)
                {
                    property.SetValue(result, binder.BindModel(doc));
                }
            }

            return result;
        }

    }
}
