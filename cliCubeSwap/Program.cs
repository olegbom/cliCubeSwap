// See https://aka.ms/new-console-template for more information
using System.Text;
using System.Diagnostics;
using Microsoft.Extensions.Configuration;

namespace CliCubeSwap;

class Program
{
    static CancellationTokenSource _cts = new CancellationTokenSource();

    static BrailleFontRenderer renderer;
    public static void Main(string[] args)
    {
        var builder = new ConfigurationBuilder();
        builder.SetBasePath(Directory.GetCurrentDirectory())
           .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

        IConfiguration config = builder.Build();

        Console.CancelKeyPress += OnCancelKeyPress;
        
        renderer = new BrailleFontRenderer(){ Framerate = int.Parse(config["Framerate"] ?? "60")};
        Console.WriteLine("Press Ctrl+C for exit");
        Console.WriteLine("          W↖ ↗E");
        Console.WriteLine("Control: A← ⬡ →D");
        Console.WriteLine("          Z↙ ↘X");
        Task.WhenAll( new List<Task> () {renderer.Loop(_cts.Token), ReadKeyLoop(_cts.Token)}).Wait();
    }

    static void OnCancelKeyPress(object sender, ConsoleCancelEventArgs e)
    {
        Console.WriteLine("Ctrl+C pressed. Signaling cancellation...");
        _cts.Cancel();
        if( e != null)
            e.Cancel = true; // Prevent immediate termination
    }

    static async Task ReadKeyLoop(CancellationToken token)
    {
        await Task.Run(() => {
            while (!token.IsCancellationRequested)
            {
                if (Console.KeyAvailable)
                {
                    ConsoleKeyInfo keyInfo = Console.ReadKey(true);
                    Point old = renderer.Player;
                    int newX = old.X, newY = old.Y;
                    switch (keyInfo.Key)
                    {
                        case ConsoleKey.W:
                            newX = old.X - 1 + (old.Y%2);
                            newY = old.Y - 1;
                            break;
                        case ConsoleKey.E:
                            newX = old.X + (old.Y%2);
                            newY = old.Y - 1;
                            break;
                        case ConsoleKey.A:
                            newX = old.X - 1;
                            break;
                        case ConsoleKey.D:
                            newX = old.X + 1;
                            break;
                        case ConsoleKey.Z:
                            newX = old.X - 1 + (old.Y%2);
                            newY = old.Y + 1;
                            break;
                        case ConsoleKey.X:
                            newX = old.X + (old.Y%2);
                            newY = old.Y + 1;
                            break;
                    }

                    if(renderer.IsCoordValid(newX, newY))
                    {
                        old.X = (byte)(newX);
                        old.Y = (byte)(newY);
                    }

                    renderer.Player = old;
                }
                else
                {
                    Thread.Sleep(1);
                }
            }
        });
    }
}


