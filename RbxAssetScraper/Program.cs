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
            new("-a, --asset", "Uses the asset scraper."),
            new("", "Input for asset scrapers should be the game id."),
            new("", ""),
            new("-l, --list", "Uses the list scraper."),
            new("-lv, --listversions", "Uses the list scraper, with version scraping. [EXPERIMENTAL]"),
            new("", "Input for list scrapers should be the path to the list."),
            new("", ""),
            new("-r, --range", "Uses the range scraper."),
            new("", "Input for range scrapers the start and end ids in the format of (start)-(end)."),
            new("", ""),
            new("-o, --output", "Output folder."),
            new("", "Default = output."),
            new("", ""),
            new("-e, --extension", "Extension for all scraped files to include."),
            new("", "Default = None."),
            new("", ""),
            new("-mh, --maxhttp", "Maximum amount of HTTP connections at any time."),
            new("", "Less connections are better for slower internet speeds."),
            new("", "Default = 1."),
            new("", ""),
            new("-mr, --maxretries", "Maximum amount of asset download retries."),
            new("", "Default = 0."),
            new("", ""),
            new("-ot, --outputtype", "Output type. Options are 'FilesOnly', 'IndexOnly', 'FilesAndIndex'."),
            new("", "Range scraper does not support indexes."),
            new("", "Default = FilesOnly."),
            new("", ""),
            new("-c, --compression", "Compression type. Options are 'None', 'GZip', 'BZip2'."),
            new("", "Default = None."),
            new("", ""),
            new("-rs, --roblosecurity", ".ROBLOSECURITY cookie, for downloading copylocked assets on an account."),
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
            Console.WriteLine("Example: \"RbxAssetScraper -a 1818 -e rbxl\"");
            Console.WriteLine("         \"RbxAssetScraper -l list-of-hats.txt -e rbxm -mh 5\"");
            Console.WriteLine("         \"RbxAssetScraper -lv list-of-historic-places.txt -e rbxl -mr 2 -mh 5 -c BZip2\"");
            Console.WriteLine("         \"RbxAssetScraper -r 100000-200000 -e rbxm\"");
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

                    case "-a":
                    case "--asset":
                        input = GetValueFromArgs(ref args, ref i);
                        scraper = new Scrapers.Asset();
                        break;

                    case "-l":
                    case "--list":
                        input = GetValueFromArgs(ref args, ref i);
                        scraper = new Scrapers.List(false);
                        break;

                    case "-lv":
                    case "--listversions":
                        input = GetValueFromArgs(ref args, ref i);
                        scraper = new Scrapers.List(true);
                        break;

                    case "-r":
                    case "--range":
                        input = GetValueFromArgs(ref args, ref i);
                        scraper = new Scrapers.Range();
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
                    if (string.IsNullOrEmpty(Config.OutputPath))
                        Config.OutputPath = scraper.BuildDefaultOutputPath(input);

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