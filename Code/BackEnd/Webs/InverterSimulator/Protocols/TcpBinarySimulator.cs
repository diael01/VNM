using System.Net;
using System.Net.Sockets;

namespace InverterSimulator.Protocols.Tcp;

public class TcpBinarySimulator
{
    private readonly TcpBinarySimulatorConfig _config;

    public TcpBinarySimulator(TcpBinarySimulatorConfig config)
    {
        _config = config;
    }

    public async Task StartAsync(CancellationToken ct = default)
    {
        var listener = new TcpListener(IPAddress.Any, _config.Port);
        listener.Start();

        while (!ct.IsCancellationRequested)
        {
            var client = await listener.AcceptTcpClientAsync(ct);
            _ = HandleClientAsync(client, ct);
        }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken ct)
    {
        using var stream = client.GetStream();

        var buffer = new byte[_config.BufferSize];
        await ReadExactlyAsync(stream, buffer, ct);

        // Fake binary response (Modbus-style placeholder)
        var response = new byte[] { 0x01, 0x03, 0x02, 0x08, 0x98 };
        await stream.WriteAsync(response, ct);
    }

    private static async Task ReadExactlyAsync(
        NetworkStream stream,
        Memory<byte> buffer,
        CancellationToken ct)
    {
        var totalRead = 0;

        while (totalRead < buffer.Length)
        {
            var read = await stream.ReadAsync(buffer[totalRead..], ct);
            if (read == 0)
                throw new IOException("Client disconnected");

            totalRead += read;
        }
    }
}

