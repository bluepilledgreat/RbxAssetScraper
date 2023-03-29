using System.Reflection;

namespace RbxAssetScraper
{
    internal class Program
    {
        // LBA = leave blank area
        // LAB = leave as blank
        static Dictionary<string, string> Commands = new Dictionary<string, string>()
        {
            { "-?, -h, --help", "Displays the help menu (this)." },
            { "LAB1", "" },
            { "-g, --game", "Uses the game scraper." },
            { "-l, --list", "Uses the list scraper." },
            { "-r, --range", "Uses the range scraper." },
            { "LAB2", "" },
            { "-i, --input", "Input argument. This varies based on scraper:" },
            { "LAB3", "Game id for game scraper." },
            { "LAB4", "Path to text file for list scraper." },
            { "LAB5", "Id range in the format of (start)-(end) for range scraper." },
            { "-o, --output", "Output folder." },
            { "LAB6", "Default = output." },
            { "LAB7", "" },
            { "-e, --extension", "Extension for all scraped files to include." },
            { "LAB8", "Default = None." },
            { "-mh, --maxhttp", "Maximum amount of HTTP connections at any time." },
            { "LAB9", "Less connections are better for slower internet speeds." },
            { "LAB10", "Default = 1." },
            { "-mr, --maxretries", "Maximum amount of asset download retries." },
            { "LAB11", "Default = 0." },
            { "-ot, --outputtype", "Output type. Options are 'FilesOnly', 'IndexOnly', 'FilesAndIndex'." },
            { "LAB12", "Range scraper does not support indexes." },
            { "LAB13", "Default = FilesOnly." },
            { "-c, --compression", "Compression type. Options are 'None', 'GZip', 'BZip2'." },
            { "LAB14", "Default = None." },
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
            int longest = Commands.Aggregate(0, (max, curr) => max > curr.Key.Length ? max : curr.Key.Length); // https://stackoverflow.com/a/7975983

            Console.WriteLine($"RbxAssetScraper {Assembly.GetExecutingAssembly().GetName().Version}");
            Console.WriteLine("COMMANDS:");
            foreach (var command in Commands)
            {
                string cmdName = command.Key;
                string cmdDesc = command.Value;
                if (cmdName.StartsWith("LAB")) // LEAVE AS BLANK
                    cmdName = "";
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