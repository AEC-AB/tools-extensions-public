

using System.Xml.Serialization;

namespace TeklaIFCExport
{
	[XmlRoot(ElementName="config")]
    public class IFCExportConfig
    {
        [XmlElement(ElementName="BaseQuantities")]
		public int BaseQuantities { get; set; }
		[XmlElement(ElementName="GridExport")]
		public int GridExport { get; set; }
		[XmlElement(ElementName="Assemblies")]
		public int Assemblies { get; set; }
		[XmlElement(ElementName="Bolts")]
		public int Bolts { get; set; }
		[XmlElement(ElementName="ReinforcingBars")]
		public int ReinforcingBars { get; set; }
		[XmlElement(ElementName="PourObjects")]
		public int PourObjects { get; set; }
		[XmlElement(ElementName="Welds")]
		public int Welds { get; set; }
		[XmlElement(ElementName="LayersNameAsPart")]
		public int LayersNameAsPart { get; set; }
		[XmlElement(ElementName="SurfaceTreatments")]
		public int SurfaceTreatments { get; set; }
		[XmlElement(ElementName="ExcludeSnglPrtAsmb")]
		public int ExcludeSnglPrtAsmb { get; set; }
		[XmlElement(ElementName="LocsFromOrganizer")]
		public int LocsFromOrganizer { get; set; }
		[XmlElement(ElementName="PLprofileToPlate")]
		public int PLprofileToPlate { get; set; }
		[XmlElement(ElementName="ViewColors")]
		public int ViewColors { get; set; }
		[XmlElement(ElementName="SectionedSpine")]
		public int SectionedSpine { get; set; }
		[XmlElement(ElementName="PSetContent")]
		public int PSetContent { get; set; }
		[XmlElement(ElementName="CreateAll")]
		public int CreateAll { get; set; }
		[XmlElement(ElementName="AdditionalPSets")]
		public string? AdditionalPSets { get; set; }
		[XmlElement(ElementName="OutputFile")]
		public string? OutputFile { get; set; }
		[XmlElement(ElementName="Format")]
		public int Format { get; set; }
		[XmlElement(ElementName="ExportType")]
		public int ExportType { get; set; }
		[XmlElement(ElementName="BasePointName")]
		public string? BasePointName { get; set; }
		[XmlElement(ElementName="LocationBy")]
		public int LocationBy { get; set; }
    }
}