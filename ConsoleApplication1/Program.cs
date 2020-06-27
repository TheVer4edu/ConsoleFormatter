using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;

namespace ConsoleApplication1
{
    internal class Program
    {
        public static void Main()
        {
            
            ConsoleFormatter cf = new ConsoleFormatter();
            foreach (string line in new FileReader("input.txt"))
                DrawEngine.SmartDraw(cf, new Command(line));
            Console.ReadKey(); //prevents console close
        }
    }

    static class DrawEngine
    {
        private static ConsoleColor Colors(string color)
        {
            Dictionary<string, ConsoleColor> colors = new Dictionary<string, ConsoleColor>();
            colors["Red"] = ConsoleColor.Red;
            colors["Green"] = ConsoleColor.Green;
            colors["Blue"] = ConsoleColor.Blue;
            return colors[color];
        }
        
        private static Position Positions(string position)
        {
            Dictionary<string, Position> positions = new Dictionary<string, Position>();
            positions["Top"] = Position.Top;
            positions["Left"] = Position.Left;
            positions["Right"] = Position.Right;
            positions["Bottom"] = Position.Bottom;
            return positions[position];
        }
        
        public static void SmartDraw(ConsoleFormatter cf, Command cp)
        {
            Timer startTimer = new Timer(cp.StartTime);
            Timer endTimer = new Timer(cp.EndTime);
            startTimer.Elapsed += async (sender, args) =>
            {
                if(cp.HasParams)
                    await cf.DrawText(Positions(cp.Position), Colors(cp.Color), cp.Text);
                else
                    await cf.DrawText(Position.No, ConsoleColor.White, cp.Text);
                startTimer.Stop();
            };
            endTimer.Elapsed += (sender, args) =>
            {
                cf.EraseText(cp.Text);
                endTimer.Stop();
            };
            startTimer.Start();
            endTimer.Start();

        }
    }
    
    class FileReader : IEnumerable
    {
        private string fileName;
        
        public FileReader(string fileName)
        {
            this.fileName = fileName;
        }
            
        public IEnumerator GetEnumerator()
        {
            string line;
            using (StreamReader sr = new StreamReader(fileName))
            {
                while ((line = sr.ReadLine()) != null)
                {
                    yield return line;
                }
            }
        }
    }

    class ConsoleFormatter
    {
        private Dictionary<string, Position> drawn = new Dictionary<string, Position>();
        public int FrameLeft { get; } = Console.WindowWidth / 4;
        public int FrameTop { get; } = Console.WindowHeight / 4;
        public int FrameRight { get; } = 3 * Console.WindowWidth / 4 - 1;
        public int FrameBottom { get; } = 3 * Console.WindowHeight / 4 - 1;
        public ConsoleFormatter()
        {
            Console.CursorVisible = false;
            Console.Clear();
            for (int i = 0; i < Console.WindowHeight/4 - 1; i++)
                Console.WriteLine();
            Console.WriteLine(new String(' ', Console.WindowWidth/4 - 2) + '╔' + new String('═', Console.WindowWidth/2) + '╗');
            for (int i = 0; i < Console.WindowHeight/2; i++)
            {
                Console.WriteLine(new String(' ', Console.WindowWidth/4 - 2) + '║' + new String(' ', Console.WindowWidth/2) + '║');
            }
            Console.WriteLine(new String(' ', Console.WindowWidth/4 - 2) + '╚' + new String('═', Console.WindowWidth/2) + '╝');
            Console.CursorLeft = 0;
            Console.CursorTop = 0;
        }

        private void SetCursor(Position pos, string text)
        {
            switch (pos)
            {
                case Position.Left:
                    Console.CursorLeft = FrameLeft;
                    Console.CursorTop = Console.WindowHeight/2 - 1;
                    break;
                case Position.Right:
                    Console.CursorLeft = FrameRight - text.Length;
                    Console.CursorTop = Console.WindowHeight/2 - 1;
                    break;
                case Position.Bottom:
                    Console.CursorLeft = Console.WindowWidth/2 - text.Length/2;
                    Console.CursorTop = FrameBottom;
                    break;
                case Position.Top:
                    Console.CursorLeft = Console.WindowWidth/2 - text.Length/2;
                    Console.CursorTop = FrameTop;
                    break;
                case Position.No:
                    Console.CursorLeft = Console.WindowWidth/2 - text.Length/2;
                    Console.CursorTop = Console.WindowHeight/2 - 1;
                    break;
            }
        }
        
        public async Task<bool> DrawText(Position pos, ConsoleColor color, string text)
        {
            return await Task.Run(() =>
            {
                Console.ForegroundColor = color;
                SetCursor(pos, text);
                Console.Write(text);
                drawn[text] = pos;
                Console.CursorLeft = 0;
                Console.CursorTop = 0;
                return true;
            });
        }

        public async Task<bool> EraseText(string text)
        {
            return await Task.Run(() =>
            {
                if (drawn.ContainsKey(text))
                {
                    SetCursor(drawn[text], text);
                    Console.Write(new String(' ', text.Length));
                    Console.CursorLeft = 0;
                    Console.CursorTop = 0;
                    return true;
                }
                return false;
            });
        }
    }

    enum Position
    {
        No, Top, Bottom, Left, Right
    }
    
    class Command
    {
        public int StartTime { get; private set; }
        public int EndTime { get; private set; }
        public string Position { get; private set; } = null;
        public string Color { get; private set; } = null;
        public string Text { get; private set; }
        public bool HasParams { get; private set; }
        private (string, string) GetTimeLimits(string cmd)
        {
            string regex = @"\d{2}:\d{2}";
            var a = Regex.Matches(cmd, regex);
            return (a[0].Value, a[1].Value);
        }
        
        private string GetTextParams(string cmd)
        {
            string regex = @"\[[a-zA-Z]+\, [a-zA-Z]+\]";
            var a = Regex.Matches(cmd, regex);
            return a.Count > 0 ? a[0].Value : null;
        }

        private (int, int) GetMinutesAndSeconds(string param)
        {
            string regex = @"\d{2}";
            var a = Regex.Matches(param, regex);
            return (int.Parse(a[0].Value), int.Parse(a[1].Value));
        }

        private string GetText(string cmd)
        {
            string regex = @"[a-zA-Z]+";
            var a = Regex.Matches(cmd, regex);
            var b = a.OfType<Match>().Select(x => x.Value).ToList();
            return string.Join(" ", cmd.Contains("]") ? b.Skip(2) : b);
        }
        
        private (string,string) GetTextParamsSeparated(string param)
        {
            string regex = @"[a-zA-Z]+";
            var a = Regex.Matches(param, regex);
            return (a[0].Value, a[1].Value);
        }

        public int GetTimeSpanMs((int, int) param)
        {
            return (param.Item1*60 + param.Item2) * 1000;
        }

        public Command(string cmd)
        {
            this.StartTime = GetTimeSpanMs(GetMinutesAndSeconds(GetTimeLimits(cmd).Item1));
            this.EndTime = GetTimeSpanMs(GetMinutesAndSeconds(GetTimeLimits(cmd).Item2));
            this.HasParams = GetTextParams(cmd) != null;
            this.Text = GetText(cmd);
            if (this.HasParams)
            {
                string param = GetTextParams(cmd);
                this.Position = GetTextParamsSeparated(param).Item1;
                this.Color = GetTextParamsSeparated(param).Item2;
            }
        }
        
    }
}