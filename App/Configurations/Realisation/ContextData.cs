namespace App.Configurations.Realisation;

public class ContextData : IContextData
{
    public Guid ApplicationId { get; set; }
    public string MachineId { get; set; }
}