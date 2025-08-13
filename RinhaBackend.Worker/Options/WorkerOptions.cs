namespace RinhaBackend.Worker.Options;

public class WorkerOptions
{
    public int WorkerPort { get; set; } = 5000;
    public int MaxConcurrentTasks { get; set; } = 64;
}