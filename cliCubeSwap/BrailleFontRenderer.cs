using System.Text;
using System.Diagnostics;
using Microsoft.Extensions.Configuration;

namespace CliCubeSwap;

public enum Color: byte
{
    Default = 0,
    Gray,
    Red,
    Green,
    Blue,
    White,
}

public class BrailleFontRenderer
{
    public int Width { get; init; } = 6*20;
    public int Height { get; init; } = (5+1)*16 - 12;

    public int Rows => Height / 16;
    public int Columns => Width / 20;

    public int Framerate { get; init; } = 60;
    public Point Player = new Point(){ X = 2, Y = 1};
    
    private char[] _drawBuffer;
    private Color[] _colorBuffer;

    private string firstrow = "     ⡀    ";
    private string pattern  = "⠀⢀⡠⠒⠉⠈⠑⠢⣀⠀" +
                              "⡎⠁⠀⠀⠀⠀⠀⠀⠀⠉" +
                              "⡇⠀⠀⠀⠀⠀⠀⠀⠀⠀" +
                              "⡇⠀⠀⠀⠀⠀⠀⠀⠀⠀"; 
    private string rightside = " " +
                               "⡆" +
                               "⡇" +
                               "⡇";
    private string bottomrow = "⠀⠀⠀⠀⠉⠊⠁⠀⠀⠀";
    private string inside = "⢄⠀⠀⠀⠀⢀⠄" +
                            "⠀⠉⠒⡔⠊⠁⠀" +
                            "⠀⠀⠀⡇⠀⠀⠀";

    private string empty_cell  = "⠀⠀⠀⡇⠀⠀⠀" +
                                 "⠀⠀⣀⢇⡀⠀⠀" +
                                 "⠔⠉⠀⠀⠈⠑⠤"; 

    private const char BrailleEmptySymbol = '⠀';

    public BrailleFontRenderer()
    {
        int bufferSizeIfBytes = Width * Height / 8;
        _drawBuffer = new char[bufferSizeIfBytes];
        _colorBuffer = new Color[bufferSizeIfBytes];
        Draw();
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
                Draw();
                Color lastColor = Color.Default;
                for (int j = 0; j < Height/4; j++)
                {
                    for (int i = 0; i < Width/2; i++)
                    {
                        int index = i + Width/2*j;
                        Color currentColor = _colorBuffer[index];
                        if(lastColor != currentColor)
                        {
                            lastColor = currentColor;
                            sb.Append(
                            currentColor switch
                            {
                                Color.Gray => "\e[38;5;239m",
                                Color.Red => "\e[31m",
                                Color.Green => "\e[32m",
                                Color.Blue => "\e[34m",
                                Color.White => "\e[97m",
                                _ => "\e[39m",
                            });
                        }

                        sb.Append(_drawBuffer[index]);

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
        int bufferSizeIfBytes = _drawBuffer.Length;
        Array.Clear(_drawBuffer);
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
                                _drawBuffer[index] = pattern[i + 10 * j];
                            }
                        }
                    }
                }
            }
        }

        for(int i = 0; i < Columns; i++)
        {
            for(int j = 0; j < Rows; j++)
            {
                if( (j%2) == 0 || i < Columns - 1)
                {
                    DrawEmptyCell(new Point(){X = (byte)i, Y = (byte)j});
                }
            }
        }

        DrawInside(Player);
    }

    private void DrawInside(Point p)
    {
        int bufferSizeIfBytes = _drawBuffer.Length;
        int delta =  ToArrIdx( p.X * 10 + 2 + (p.Y % 2) * 5, 1 + p.Y * 4);
        for (int i = 0; i < 7; i++)
        {
            for(int j = 0; j < 3; j++)
            {
                int index = delta + ToArrIdx(i, j);
                if( index < bufferSizeIfBytes )
                {
                    _drawBuffer[index] = inside[i + j * 7];
                    _colorBuffer[index] = Color.White;
                } 
            }
        }
    }

    private void DrawEmptyCell(Point p)
    {
        int bufferSizeIfBytes = _drawBuffer.Length;
        int delta =  ToArrIdx( p.X * 10 + 2 + (p.Y % 2) * 5, 1 + p.Y * 4);
        for (int i = 0; i < 7; i++)
        {
            for(int j = 0; j < 3; j++)
            {
                int index = delta + ToArrIdx(i, j);
                if( index < bufferSizeIfBytes )
                {
                    _drawBuffer[index] = empty_cell[i + j * 7];
                    _colorBuffer[index] = Color.Gray;
                } 
            }
        }
    }

    private int ToArrIdx(int x, int y)
    {
        return x + y * Width / 2;
    }

    public bool IsCoordValid(int x, int y)
    {
        return x >= 0 && y >= 0 && y < Rows &&
            (((y%2) == 1 && x < Columns - 1) || ((y%2) == 0 && x < Columns));
    }
}
