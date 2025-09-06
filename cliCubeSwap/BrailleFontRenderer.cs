using System.Text;
using System.Diagnostics;
using Microsoft.Extensions.Configuration;

namespace CliCubeSwap;

public class BrailleFontRenderer
{
    public int Width { get; init; } = 10*20;
    public int Height { get; init; } = (5+1)*16 - 12;

    public int Framerate { get; init; } = 60;

    private byte[] _drawBuffer;

    private string firstrow = "     ⡀    ";
    private string pattern = "⠀⢀⡠⠒⠉⠈⠑⠢⣀⠀" +
                             "⡎⠁⠀⠀⠀⠀⠀⠀⠀⠉" +
                             "⡇⠀⠀⠀⠀⠀⠀⠀⠀⠀" +
                             "⡇⠀⠀⠀⠀⠀⠀⠀⠀⠀"; 
    private string rightside = " " +
                               "⡆" +
                               "⡇" +
                               "⡇";
    private string bottomrow = "⠀⠀⠀⠀⠉⠊⠁⠀⠀⠀";

    private const char BrailleEmptySymbol = '⠀';

    public BrailleFontRenderer()
    {
        int bufferSizeIfBytes = Width * Height / 8;
        _drawBuffer = new byte[bufferSizeIfBytes];
        for (int row = 0; row < Height/16 + 1; row++)
        {
            int rowDelta = -(row % 2) * 5;
            for (int column = 0; column < Width/20 + 1; column++)
            {
                for (int i = 0; i < 10; i++)
                {
                    int i_index = i + column * 10 + rowDelta;
                    if (i_index >= 0 && i_index < Width / 2)
                    {
                        for (int j = 0; j < 4; j++)
                        {
                            int index = i_index + Width / 2 * (j + row * 4);
                            if(index < bufferSizeIfBytes)
                            {
                                _drawBuffer[index] = (byte)(pattern[i + 10 * j] - BrailleEmptySymbol);
                            }
                        }
                    }
                }
            }
        }
    }

    public async Task Loop(CancellationToken token)
    {
        await Task.Run( () => {
            for (int i = 0; i < Width / 2 / 10; i++)
            {
                Console.Write(firstrow);
            }

            Console.WriteLine();
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
                    char last_in_row = ' ';
                    if( (j/4) % 2 == 0 )
                    {
                        last_in_row = rightside[j%4];
                    }
                    
                    sb.Append(last_in_row);
                    sb.AppendLine();
                }

                for (int i = 0; i < Width/20; i++)
                {
                    sb.Append(bottomrow);
                }

                sb.AppendLine();
                Console.Write(sb.ToString());
                sb.Clear();
                while (frame * 1000.0 / Framerate > sw.ElapsedMilliseconds)
                {
                    Thread.Sleep(1);
                }

                Console.Write($"\e[{Height/4 + 1}A");
            }
            Console.Write($"\e[{Height/4 + 1}B");
        });
         
    }

    private void Draw()
    {
        
    }

}
