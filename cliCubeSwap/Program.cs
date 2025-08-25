// See https://aka.ms/new-console-template for more information
using System.Text;
using System.Diagnostics;
using Microsoft.Extensions.Configuration;

class Program
{
    static CancellationTokenSource _cts = new CancellationTokenSource();

    public static async Task Main(string[] args)
    {
        var builder = new ConfigurationBuilder();
        builder.SetBasePath(Directory.GetCurrentDirectory())
           .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

        IConfiguration config = builder.Build();

        Console.CancelKeyPress += OnCancelKeyPress;

        BrailleFontRenderer renderer = new BrailleFontRenderer(){ Framerate = int.Parse(config["Framerate"])};
        Console.WriteLine("Press Ctrl+C for exit");
        renderer.Loop(_cts.Token).Wait();
    }

    static void OnCancelKeyPress(object sender, ConsoleCancelEventArgs e)
    {
        Console.WriteLine("Ctrl+C pressed. Signaling cancellation...");
        _cts.Cancel();
        e.Cancel = true; // Prevent immediate termination
    }
}

public class BrailleFontRenderer
{
    public int Width { get; init; } = 128;
    public int Height { get; init; } = 64;

    public int Framerate { get; init; } = 60;

    private byte[] _drawBuffer;
    private byte[] _backBuffer;

    public BrailleFontRenderer()
    {
        int bufferSizeIfBytes = Width * Height / 8;
        _drawBuffer = new byte[bufferSizeIfBytes];
        System.Random.Shared.NextBytes(_drawBuffer);
        _backBuffer = new byte[bufferSizeIfBytes];
    }
    
    public async Task Loop(CancellationToken token)
    {
        await Task.Run( () => {
            char a = '⠀';
            Console.CursorVisible = false;
            StringBuilder sb = new StringBuilder();
            Stopwatch sw = new Stopwatch();
            sw.Start();

            long frame = 0;

            while (!token.IsCancellationRequested)
            {
                frame++;
                for (int j = 0; j < Height/4; j++)
                {
                    for (int i = 0; i < Width/2; i++)
                    {
                        sb.Append((char)(a + _drawBuffer[i + Width/2*j]));
                    }
                    sb.AppendLine();
                }
                Console.Write(sb.ToString());
                sb.Clear();
                Console.Write("\e[16A");
                while (frame * 1000.0 / Framerate > sw.ElapsedMilliseconds)
                {
                    Thread.Sleep(1);
                }
                (_drawBuffer, _backBuffer) = (_backBuffer, _drawBuffer);
            }
            Console.Write("\e[16B");
        });
         
    }

}
