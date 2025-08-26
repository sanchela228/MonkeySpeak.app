namespace App.Configurations;

public interface IContextData
{
    Guid ApplicationId { get; set; }
    string MachineId { get; set; }
}