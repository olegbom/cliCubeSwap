// See https://aka.ms/new-console-template for more information
using System.Text;
using System.Diagnostics;
using Microsoft.Extensions.Configuration;

class Program
{
    static CancellationTokenSource _cts = new CancellationTokenSource();

    public static void Main(string[] args)
    {
        var builder = new ConfigurationBuilder();
        builder.SetBasePath(Directory.GetCurrentDirectory())
           .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

        IConfiguration config = builder.Build();

        Console.CancelKeyPress += OnCancelKeyPress;

        BrailleFontRenderer renderer = new BrailleFontRenderer(){ Framerate = int.Parse(config["Framerate"] ?? "60")};
        Console.WriteLine("Press Ctrl+C for exit");
        renderer.Loop(_cts.Token).Wait();
    }

    static void OnCancelKeyPress(object sender, ConsoleCancelEventArgs e)
    {
        Console.WriteLine("Ctrl+C pressed. Signaling cancellation...");
        _cts.Cancel();
        if( e != null)
            e.Cancel = true; // Prevent immediate termination
    }
}

public class BrailleFontRenderer
{
    public int Width { get; init; } = 128;
    public int Height { get; init; } = 64;

    public int Framerate { get; init; } = 60;

    private byte[] _drawBuffer;

    private string pattern = "⠀⢀⡠⠒⠉⠈⠑⠢⣀⠀" +
                             "⡎⠁⠀⠀⠀⠀⠀⠀⠀⠉" + 
                             "⡇⠀⠀⠀⠀⠀⠀⠀⠀⠀" + 
                             "⡇⠀⠀⠀⠀⠀⠀⠀⠀⠀"; 

    private const char BrailleEmptySymbol = '⠀';

    public BrailleFontRenderer()
    {
        int bufferSizeIfBytes = Width * Height / 8;
        _drawBuffer = new byte[bufferSizeIfBytes];
        for (int row = 0; row < 4; row++)
        {
            int rowDelta = -(row % 2) * 5;
            for (int column = 0; column < 7; column++)
            {
                for (int i = 0; i < 10; i++)
                {
                    int i_index = i + column * 10 + rowDelta;
                    if (i_index >= 0 && i_index < Width / 2)
                    {
                        for (int j = 0; j < 4; j++)
                        {
                            int index = i_index + Width / 2 * (j + row * 4);
                            _drawBuffer[index] = (byte)(pattern[i + 10 * j] - BrailleEmptySymbol);
                        }
                    }
                }
            }
        }
    }

    public async Task Loop(CancellationToken token)
    {
        await Task.Run( () => {
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
                        sb.Append((char)(BrailleEmptySymbol + _drawBuffer[i + Width/2*j]));
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
            }
            Console.Write("\e[16B");
        });
         
    }

    private void Draw()
    {
        
    }

}
