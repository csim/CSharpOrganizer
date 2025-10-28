using System.IO.Pipes;
using System.Text;

namespace CSharply;

public sealed class DaemonService(string pipeName = "csharply") : IDisposable
{
    public void Dispose()
    {
        Stop();
        _cancellationTokenSource.Dispose();
    }

    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private bool _isRunning;
    private readonly string _pipeName = pipeName;

    public async Task StartAsync()
    {
        if (_isRunning)
        {
            throw new InvalidOperationException("Daemon is already running");
        }

        _isRunning = true;

        Console.WriteLine($"Starting CSharply Daemon on pipe: {_pipeName}");
        Console.WriteLine("Waiting for client connections...");

        try
        {
            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    await ProcessClientAsync(_cancellationTokenSource.Token);
                }
                catch (OperationCanceledException)
                {
                    break; // Exit the loop when cancellation is requested
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing client: {ex.Message}");
                    // Continue to next iteration to accept new connections
                }
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Daemon stopped.");
        }
        finally
        {
            _isRunning = false;
        }
    }

    public void Stop()
    {
        if (!_isRunning)
        {
            return;
        }

        Console.WriteLine("Stopping CSharply Daemon...");
        _cancellationTokenSource.Cancel();
    }

    private static async Task HandleClientRequestAsync(
        NamedPipeServerStream pipeServer,
        CancellationToken cancellationToken
    )
    {
        try
        {
            // Read the incoming message
            using MemoryStream buffer = new();
            byte[] readBuffer = new byte[4096];
            int bytesRead;

            do
            {
                bytesRead = await pipeServer.ReadAsync(readBuffer, cancellationToken);
                await buffer.WriteAsync(readBuffer.AsMemory(0, bytesRead), cancellationToken);
            } while (!pipeServer.IsMessageComplete);

            string inputCode = Encoding.UTF8.GetString(buffer.ToArray());

            if (string.IsNullOrWhiteSpace(inputCode))
            {
                await SendResponseAsync(pipeServer, "ERROR: No code provided", cancellationToken);
                return;
            }

            Console.WriteLine($"Processing {inputCode.Length} characters of code...");

            // Organize the code
            string organizedCode = OrganizeService.OrganizeCode(inputCode);

            // Send the response
            await SendResponseAsync(pipeServer, organizedCode, cancellationToken);

            Console.WriteLine("Code organized and sent back to client.");
        }
        catch (Exception ex)
        {
            await SendResponseAsync(pipeServer, $"ERROR: {ex.Message}", cancellationToken);
        }
    }

    private async Task ProcessClientAsync(CancellationToken cancellationToken)
    {
        using NamedPipeServerStream pipeServer = new(
            _pipeName,
            PipeDirection.InOut,
            1, // Max number of server instances
            PipeTransmissionMode.Message,
            PipeOptions.Asynchronous
        );

        try
        {
            // Wait for a client to connect
            await pipeServer.WaitForConnectionAsync(cancellationToken);
            Console.WriteLine("Client connected.");

            // Process the request
            await HandleClientRequestAsync(pipeServer, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Expected when cancellation is requested
        }
        catch (IOException ex)
        {
            Console.WriteLine($"IO Error: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unexpected error: {ex.Message}");
        }
        finally
        {
            if (pipeServer.IsConnected)
            {
                pipeServer.Disconnect();
                Console.WriteLine("Client disconnected.");
            }
        }
    }

    private static async Task SendResponseAsync(
        NamedPipeServerStream pipeServer,
        string response,
        CancellationToken cancellationToken
    )
    {
        try
        {
            byte[] responseBytes = Encoding.UTF8.GetBytes(response);
            await pipeServer.WriteAsync(responseBytes, cancellationToken);
            await pipeServer.FlushAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to send response: {ex.Message}");
        }
    }
}
