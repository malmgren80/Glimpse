using System;
using System.Configuration;
using System.Linq;

namespace Glimpse.Core.Configuration
{
    [ConfigurationCollection(typeof(SetupElement), CollectionType = ConfigurationElementCollectionType.AddRemoveClearMap)]
    public class SetupElementCollection : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new SetupElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((SetupElement)element).Type;
        }

        public string GetSetupFor<T>()
        {
            return this.GetSetupFor(typeof(T));
        }

        public string GetSetupFor(Type type)
        {
            return this.Cast<SetupElement>()
                .Where(setup => setup.Type == type)
                .Select(setup => setup.Arguments).FirstOrDefault();
        }
    }
}
