using System;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
namespace grendgine_collada
{
	[System.SerializableAttribute()]
	[System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true)]
	public partial class Grendgine_Collada_Geometry_Common_Fields
	{
		[XmlAttribute("count")]
		public int Count;
		
		[XmlAttribute("name")]
		public string Name;
		
		[XmlAttribute("material")]
		public string Material;

	    [XmlElement(ElementName = "p")]
		public Grendgine_Collada_Int_Array_String P;		

		[XmlElement(ElementName = "input")]
		public Grendgine_Collada_Input_Shared[] Input;		
		
	    [XmlElement(ElementName = "extra")]
		public Grendgine_Collada_Extra[] Extra;		
		
	}
}

