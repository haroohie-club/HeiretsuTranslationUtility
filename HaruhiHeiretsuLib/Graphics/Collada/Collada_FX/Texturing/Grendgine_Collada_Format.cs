using System;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
namespace grendgine_collada
{
	[System.SerializableAttribute()]
	[System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true)]
	[System.Xml.Serialization.XmlRootAttribute(ElementName="format", Namespace="http://www.collada.org/2005/11/COLLADASchema", IsNullable=true)]
	public partial class Grendgine_Collada_Format
	{
		[XmlElement(ElementName = "hint")]
		public Grendgine_Collada_Format_Hint Hint;	
		
		[XmlElement(ElementName = "exact")]
		public string Exact;	
	}
}

