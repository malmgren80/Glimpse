using System;
using System.IO;
using System.Xml.Serialization;

namespace Glimpse.Core.Extensibility
{
    public class CustomConfigurationProvider
    {
        private string CustomConfiguration { get; set; }

        public CustomConfigurationProvider(string customConfiguration)
        {
            if (string.IsNullOrEmpty(customConfiguration))
            {
                throw new ArgumentException("is null or empty", "customConfiguration");
            }

            CustomConfiguration = customConfiguration;
        }

        public TCustomConfigurationType GetMyCustomConfigurationAs<TCustomConfigurationType>()
            where TCustomConfigurationType : class
        {
            if (typeof(TCustomConfigurationType) == typeof(string))
            {
                return CustomConfiguration as TCustomConfigurationType;
            }

            var xmlSerializer = new XmlSerializer(typeof(TCustomConfigurationType));
            using (var stringReader = new StringReader(CustomConfiguration))
            {
                return (TCustomConfigurationType)xmlSerializer.Deserialize(stringReader);
            }
        }
    }
}
