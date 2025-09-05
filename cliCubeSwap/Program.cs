// See https://aka.ms/new-console-template for more information
using System.Text;
using System.Diagnostics;
using Microsoft.Extensions.Configuration;

namespace CliCubeSwap;

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


