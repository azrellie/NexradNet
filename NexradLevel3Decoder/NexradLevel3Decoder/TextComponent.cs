namespace Azrellie.Meteorology.NexradNet.Level3;

public record TextComponent : Component
{
	public int NumberOfComponentParameters { get; set; }
	public int ComponentParameterList { get; set; }
	public string MessageType { get; set; }
	public string AttributeData { get; set; }
	public string Text { get; set; }
}