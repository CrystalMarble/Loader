using System.IO;
using System.IO.Compression;
using System.Text.Json;
using System.Diagnostics;
using Sharprompt;

class Config
{
    public string? UltraPath { get; set; }
    public string? ClassicPath { get; set; }
    public string? DoorstopPath { get; set; }
    public bool Ultra { get; set; }
    public bool Classic { get; set; }
}

internal class Builder
{


    public static string GetDoorstopPath()
    {
        var ShouldDownloadDoorstop = Prompt.Confirm("Should the program automatically download Doorstop for you?", defaultValue: true);
        if (ShouldDownloadDoorstop) {
            Console.WriteLine("Downloading UnityDoorstop...");
            var client = new HttpClient();
            var responseTask = client.GetAsync("https://github.com/NeighTools/UnityDoorstop/releases/download/v4.3.0/doorstop_win_verbose_4.3.0.zip");
            responseTask.Wait();
            Directory.CreateDirectory("_Doorstop");
            var contentTask = responseTask.Result.Content.ReadAsByteArrayAsync();
            contentTask.Wait();
            File.WriteAllBytes("_Doorstop.zip", contentTask.Result);
            ZipFile.OpenRead("_Doorstop.zip").ExtractToDirectory("_Doorstop", true );
            Console.WriteLine("Successfully downloaded UnityDoorstop!");
            return Path.Combine(Directory.GetCurrentDirectory(), "_Doorstop");
        }
        var DoorstopPath = Prompt.Input<string>("Enter your UnityDoorstop build directory");
        if (DoorstopPath == null || !Directory.Exists(DoorstopPath) || !File.Exists(Path.Combine(DoorstopPath, "winhttp.dll")) || !File.Exists(Path.Combine(DoorstopPath, ".doorstop-version")))
        {
            Console.WriteLine("Doorstop build path cannot be found or is invalid"); Environment.Exit(1);
        }
        return DoorstopPath;
    }

    public static Config PromptForConfig()
    {
        Console.WriteLine("Config not found. creating it now...");
        var Games = Prompt.MultiSelect<string>("What games do you want to use CrystalMarble on?", new[] { "Marble it Up! Ultra", "Marble it Up! Classic" });
        if (Games.Count() == 0) { Console.WriteLine("You need to select at least one game!"); Environment.Exit(1); }
        var config = new Config();
        if (Games.Contains("Marble it Up! Ultra")) config.Ultra = true;
        if (Games.Contains("Marble it Up! Classic")) config.Classic = true;
        if (config.Ultra)
        {
            var UltraPath = Prompt.Input<string>("Enter your Marble it Up! Ultra installation directory", "C:\\Program Files (x86)\\Steam\\steamapps\\common\\Marble It Up!");
            if (UltraPath == "" && !Directory.Exists(UltraPath)) { Console.WriteLine("Marble it Up! Ultra installation path cannot be null"); Environment.Exit(1); }
            config.UltraPath = UltraPath;
        }
        if (config.Classic)
        {
            var ClassicPath = Prompt.Input<string>("Enter your Marble it Up! Classic installation directory", "C:\\Program Files (x86)\\Steam\\steamapps\\common\\Marble It Up! Classic");
            if (ClassicPath == "" && !Directory.Exists(ClassicPath)) { Console.WriteLine("Marble it Up! Classic installation path cannot be null"); Environment.Exit(1); }
            config.ClassicPath = ClassicPath;
        }
        config.DoorstopPath = GetDoorstopPath();
        return config;
    }

    public static Config ParseConfig()
    {
        if (File.Exists("config.json"))
        {
            Console.WriteLine("Using existing config...");
            return JsonSerializer.Deserialize<Config>(File.ReadAllText("config.json"));
        }
        var config = PromptForConfig();
        File.WriteAllText("config.json", JsonSerializer.Serialize(config));
        return config;
    }

    public static Config config { get; set; }

    public static string Build()
    {
        Directory.SetCurrentDirectory("../../../../Loader");
        Console.WriteLine("Building CrystalMarble Loader...");
        ProcessStartInfo psi = new ProcessStartInfo();
        psi.RedirectStandardError = true;
        psi.RedirectStandardOutput = true;
        psi.FileName = "dotnet.exe";
        psi.Arguments = "build";
        var proc = Process.Start(psi);
        proc.WaitForExit();
        if (proc.ExitCode != 0 )
        {
            Console.WriteLine($"Build failed with exit code {proc.ExitCode}");
            Console.WriteLine(proc.StandardOutput.ReadToEnd());
            Console.WriteLine(proc.StandardError.ReadToEnd());
            Environment.Exit(1);
        }
        var LoaderDllPath = Path.Combine(Directory.GetCurrentDirectory(), "bin", "Debug", "Loader.dll");
        Console.WriteLine($"Loader is available at {LoaderDllPath}");
        return LoaderDllPath;
    }

    private static void Main(string[] args)
    {
        Console.WriteLine("""
     _____                _        ____  ___           _     _      
    /  __ \              | |      | |  \/  |          | |   | |     
    | /  \/_ __ _   _ ___| |_ __ _| | .  . | __ _ _ __| |__ | | ___ 
    | |   | '__| | | / __| __/ _` | | |\/| |/ _` | '__| '_ \| |/ _ \
    | \__/\ |  | |_| \__ \ || (_| | | |  | | (_| | |  | |_) | |  __/
     \____/_|   \__, |___/\__\__,_|_\_|  |_/\__,_|_|  |_.__/|_|\___|
                 __/ |                                              
                |___/                                               
    """);
        config = ParseConfig();
        var LoaderPath = Build();
        var WinHttpDllPath = Path.Combine(config.DoorstopPath ?? "", "x64", "winhttp.dll");
        var DoorstopVersionPath = Path.Combine(config.DoorstopPath ?? "", "x64", ".doorstop_version");
        var DoorstopConfigContent = """
            [General]
            enabled=true
            target_assembly=CrystalMarble.dll

            [UnityMono]
            debug_enabled=true
            debug_address=127.0.0.1:10000
            debug_suspend=false
            
            """;
        if (config.Ultra)
        {
            Console.WriteLine("Installing CrystalMarble to Marble it Up! Ultra");
            File.WriteAllText(Path.Combine(config.UltraPath, "doorstop_config.ini"), DoorstopConfigContent);
            Console.WriteLine("Successfully written doorstop_config.ini");
            File.Copy(WinHttpDllPath, Path.Combine(config.UltraPath, "winhttp.dll"), true);
            Console.WriteLine("Successfully copied Doorstop winhttp.dll");
            File.Copy(DoorstopVersionPath, Path.Combine(config.UltraPath, ".doorstop_version"), true);
            Console.WriteLine("Successfully copied CrystalMarble.dll");
            File.Copy(LoaderPath, Path.Combine(config.UltraPath, "CrystalMarble.dll"), true);
        }
    }
}