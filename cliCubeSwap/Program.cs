// See https://aka.ms/new-console-template for more information
using System.Text;
using System.Diagnostics;

char a = '⠀';

Console.CursorVisible = false;
StringBuilder sb = new StringBuilder();
Stopwatch sw = new Stopwatch();
sw.Start();
int frashrate = 144;

for(int k = 0; k < 1000; k++)
{
    for (int j = 0; j < 16; j++)
    {
        for (int i = 0; i < 64; i++)
        {
            sb.Append((char)(a + System.Random.Shared.Next(k/4)));
        }
        sb.AppendLine();
    }
    Console.Write(sb.ToString());
    sb.Clear();
    Console.Write("\e[16A");
    while(k * 1000.0 / frashrate > sw.ElapsedMilliseconds)
    {
        Thread.Sleep(1);
    }
}

Console.Write("\e[16B");
