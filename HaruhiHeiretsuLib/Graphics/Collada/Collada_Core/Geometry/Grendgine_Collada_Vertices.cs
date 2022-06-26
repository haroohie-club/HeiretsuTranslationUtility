using System;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
namespace grendgine_collada
{
	[System.SerializableAttribute()]
	[System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true)]
	public partial class Grendgine_Collada_Vertices
	{
		[XmlAttribute("id")]
		public string ID;
		
		[XmlAttribute("name")]
		public string Name;	
		
		
		[XmlElement(ElementName = "input")]
		public Grendgine_Collada_Input_Unshared[] Input;			

	    [XmlElement(ElementName = "extra")]
		public Grendgine_Collada_Extra[] Extra;
	}
}

