using Microsoft.AspNetCore.SignalR.Client;

public class SignalRService(IConfiguration configuration)
{
    public HubConnection? HubConnection;
    private readonly string apiBaseUrl = (configuration["ApiBaseUrl"] ?? "http://localhost:5135").TrimEnd('/');
    private readonly SemaphoreSlim connectionLock = new(1, 1);

    public async Task EnsureConnected()
    {
        await connectionLock.WaitAsync();
        try
        {
            HubConnection ??= new HubConnectionBuilder()
                .WithUrl($"{apiBaseUrl}/api/gameHub")
                .WithAutomaticReconnect()
                .Build();
            if (HubConnection.State == HubConnectionState.Disconnected)
                await HubConnection.StartAsync();
        }
        finally { connectionLock.Release(); }
    }

    public HubConnection? GetConnection() => HubConnection;

    public async Task StopConnectionAsync()
    {
        await connectionLock.WaitAsync();
        try
        {
            if (HubConnection is null) return;
            await HubConnection.DisposeAsync();
            HubConnection = null;
        }
        finally { connectionLock.Release(); }
    }
}
