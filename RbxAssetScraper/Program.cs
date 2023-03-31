using RbxAssetScraper.Models;
using System.Reflection;

namespace RbxAssetScraper
{
    internal class Program
    {
        static List<CommandInfo> Commands = new List<CommandInfo>()
        {
            new("-?, -h, --help", "Displays the help menu (this)."),
            new("", ""),
            new("-g, --game", "Uses the game scraper."),
            new("-l, --list", "Uses the list scraper."),
            new("-r, --range", "Uses the range scraper."),
            new("", "" ),
            new("-i, --input", "Input argument. This varies based on scraper:"),
            new("", "Game id for game scraper."),
            new("", "Path to text file for list scraper."),
            new("", "Id range in the format of (start)-(end) for range scraper."),
            new("-o, --output", "Output folder."),
            new("", "Default = output."),
            new("", ""),
            new("-e, --extension", "Extension for all scraped files to include."),
            new("", "Default = None."),
            new("-mh, --maxhttp", "Maximum amount of HTTP connections at any time."),
            new("", "Less connections are better for slower internet speeds."),
            new("", "Default = 1."),
            new("-mr, --maxretries", "Maximum amount of asset download retries."),
            new("", "Default = 0."),
            new("-ot, --outputtype", "Output type. Options are 'FilesOnly', 'IndexOnly', 'FilesAndIndex'."),
            new("", "Range scraper does not support indexes."),
            new("", "Default = FilesOnly."),
            new("-c, --compression", "Compression type. Options are 'None', 'GZip', 'BZip2'."),
            new("", "Default = None."),
            new("-rs, --roblosecurity", "ROBLOSECURITY cookie, for downloading copylocked assets on an account."),
        };

        static string GetValueFromArgs(ref string[] args, ref int index)
        {
            if (args.Length < index + 2)
            {
                Console.WriteLine($"Value for argument '{args[index]}' is missing");
                Environment.Exit(-1);
            }
            index++;
            string arg = args[index];
            return arg;
        }

        static void DisplayHelp()
        {
            int longest = Commands.Aggregate(0, (max, curr) => max > curr.Name.Length ? max : curr.Name.Length); // https://stackoverflow.com/a/7975983

            // we dont use the final number
            string? version = Assembly.GetExecutingAssembly().GetName().Version?.ToString()[..^2];
            Console.WriteLine($"RbxAssetScraper {version}");
            Console.WriteLine("COMMANDS:");
            foreach (var command in Commands)
            {
                string cmdName = command.Name;
                string cmdDesc = command.Description;
                Console.WriteLine($"{cmdName.PadRight(longest)} {cmdDesc}");
            }
            Console.WriteLine();
            Console.WriteLine("Example: \"RbxAssetScraper -g -i 1818 -e rbxl\"");
            Console.WriteLine("         \"RbxAssetScraper -l -i list-of-hats.txt -e rbxm -mh 5\"");
            Console.WriteLine("         \"RbxAssetScraper -r -i 100000-200000 -e rbxm\"");
        }

        static async Task Main(string[] args)
        {
            IScraper? scraper = null;
            string? input = null;

            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];

                switch (arg)
                {
                    case "-?":
                    case "-h":
                    case "--help":
                        DisplayHelp();
                        return;

                    case "-g":
                    case "--game":
                        scraper = new Scrapers.Game();
                        break;

                    case "-l":
                    case "--list":
                        scraper = new Scrapers.List();
                        break;

                    case "-r":
                    case "--range":
                        scraper = new Scrapers.Range();
                        break;

                    case "-i":
                    case "--input":
                        input = GetValueFromArgs(ref args, ref i);
                        break;

                    case "-o":
                    case "--output":
                        Config.OutputPath = GetValueFromArgs(ref args, ref i);
                        break;

                    case "-e":
                    case "--extension":
                        Config.OutputExtension = GetValueFromArgs(ref args, ref i);
                        break;

                    case "-mh":
                    case "--maxhttp":
                        Config.MaxHttpRequests = int.Parse(GetValueFromArgs(ref args, ref i));
                        break;

                    case "-mr":
                    case "--maxretries":
                        Config.MaxRetries = int.Parse(GetValueFromArgs(ref args, ref i));
                        break;

                    case "-ot":
                    case "--outputtype":
                        Config.OutputType = (OutputType)Enum.Parse(typeof(OutputType), GetValueFromArgs(ref args, ref i), true);
                        break;

                    case "-c":
                    case "--compression":
                        Config.CompressionType = (CompressionType)Enum.Parse(typeof(CompressionType), GetValueFromArgs(ref args, ref i), true);
                        break;

                    case "-rs":
                    case "--roblosecurity":
                        Config.RobloSecurity = GetValueFromArgs(ref args, ref i);
                        break;

                    default:
                        Console.WriteLine($"Unknown command: \"{arg}\"!");
                        DisplayHelp();
                        return;
                }
            }

            if (scraper != null)
            {
                if (input != null)
                {
                    Directory.CreateDirectory(Config.OutputPath);
                    await scraper.Start(input);
                }
                else
                {
                    Console.WriteLine("An input is required!");
                    DisplayHelp();
                }
            }
            else
                DisplayHelp();
        }
    }
}