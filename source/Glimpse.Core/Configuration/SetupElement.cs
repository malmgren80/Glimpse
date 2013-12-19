using System;
using System.Configuration;
using System.Xml;

namespace Glimpse.Core.Configuration
{
    public class SetupElement : ConfigurationElement
    {
        public Type Type { get; private set; }
        public string Arguments { get; private set; }

        protected override void DeserializeElement(XmlReader reader, bool serializeCollectionKey)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(reader.ReadOuterXml());

            XmlAttribute typeAttribute = doc.DocumentElement.Attributes["type"];
            if (typeAttribute == null)
            {
                throw new Exception("Type is required");
            }

            this.Type = (Type)new TypeConverter().ConvertFrom(null, null, typeAttribute.Value);
            this.Arguments = doc.DocumentElement.InnerXml;
        }
    }
}
