using System;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
namespace grendgine_collada
{
	[System.SerializableAttribute()]
	[System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true)]
	public partial class Grendgine_Collada_Controller
	{

		[XmlAttribute("id")]
		public string ID;
		
		[XmlAttribute("name")]
		public string Name;			

		
		[XmlElement(ElementName = "skin")]
		public Grendgine_Collada_Skin Skin;
		
		[XmlElement(ElementName = "morph")]
		public Grendgine_Collada_Morph Morph;
		
		
		[XmlElement(ElementName = "asset")]
		public Grendgine_Collada_Asset Asset;
		
	    [XmlElement(ElementName = "extra")]
		public Grendgine_Collada_Extra[] Extra;	
	}
}

