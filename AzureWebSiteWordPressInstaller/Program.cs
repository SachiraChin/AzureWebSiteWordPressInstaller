using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AzureWebSiteWordPressInstaller
{
    class Program
    {
        static void Main(string[] args)
        {
            string wordpressVersion = null;
            var isForce = false;
            if (args != null && args.Length > 0)
            {
                for (int i = 0; i < args.Length; i++)
                {
                    var arg = args[i];

                    switch (arg.ToLower())
                    {
                        case "-help":
                        case "-h":
                            WriteHelp();
                            return;
                        case "-force":
                        case "-f":
                            isForce = true;
                            break;
                        case "-version":
                        case "-v":
                            wordpressVersion = args[i + 1];
                            i++;
                            break;
                        case "-ip":
                            ShowIp();
                            return;
                        default:
                            break;
                    }
                }
            }

            Console.WriteLine("\n\nWordpress installer for Azure WebSites\n");
            Console.WriteLine("This tool lets system administrator to download and install WordPress on Azure WebSite.\nUse -h paramter to see available options to run the tool.\n");
            var webRoot = Environment.GetEnvironmentVariable("WEBROOT_PATH");
#if DEBUG
            webRoot = @"C:\webroot";
#endif
            Console.WriteLine($"Web application root path: {webRoot}");

            if (Directory.GetFiles(webRoot, "*", SearchOption.AllDirectories).Length > 0)
            {
                if (isForce)
                {
                    Console.WriteLine("There are files exists under web root path. Do you want to delete all? (Y/n)");
                    if (Console.ReadLine() == "Y")
                    {
                        DeleteAllFiles(webRoot, true);
                    }
                    else
                    {
                        Console.WriteLine("Files exists under web root. Installation cancelled.");
#if DEBUG
                        Console.ReadLine();
#endif
                        return;
                    }
                }
                else
                {
                    Console.WriteLine("Files exists under web root. Please use -force option to delete all files and install. Current installation cancelled.");

#if DEBUG
                    Console.ReadLine();
#endif
                    return;
                }



            }
            if (wordpressVersion == null)
            {
                Console.WriteLine("Wordpress version not provided. Determining latest version of WordPress.");
                var version = GetLatestWordPressVersion();
                if (version == null)
                {
                    Console.WriteLine("Could not determine the latest version of WordPress. Please enter version from -v parameter.");
#if DEBUG
                    Console.ReadLine();
#endif
                    return;
                }

                Console.WriteLine($"Latest version: {version}\n");
                wordpressVersion = version;
            }
            Console.WriteLine($"Downloading version {wordpressVersion}");
            var zipPath = DownloadWordPress(wordpressVersion);

            ExtractWordPressPackage(webRoot, zipPath);
            File.Delete(zipPath);
            CopyWebConfig(webRoot);

            Console.WriteLine($"Extracting completed.\n\n\nStarting configuration of wordpress..\nPlease enter following fields.\n\n");

            var AUTH_KEY = "";
            var SECURE_AUTH_KEY = "";
            var LOGGED_IN_KEY = "";
            var NONCE_KEY = "";
            var AUTH_SALT = "";
            var SECURE_AUTH_SALT = "";
            var LOGGED_IN_SALT = "";
            var NONCE_SALT = "";
            var dbHost = "";
            var dbName = "";
            var dbUsername = "";
            var dbPassword = "";
            var dbTablePrefix = "";
            var getInput = true;
            while (getInput)
            {
                Console.WriteLine("Database host name: ");
                dbHost = Console.ReadLine();
                Console.WriteLine("Database name: ");
                dbName = Console.ReadLine();
                Console.WriteLine("Database username: ");
                dbUsername = Console.ReadLine();
                Console.WriteLine("Database password: ");
                dbPassword = Console.ReadLine();
                Console.WriteLine("Database table prefix (wp_)(Press space if you want it empty): ");
                dbTablePrefix = Console.ReadLine();
                if (dbTablePrefix == "") dbTablePrefix = "wp_";
                if (dbTablePrefix == " ") dbTablePrefix = "";

                Console.WriteLine("Do you want to auto generate Authentication Unique Keys and Salts? (Y/n) ");
                var autoGenKeys = Console.ReadLine().ToLower() == "y";
                if (autoGenKeys)
                {
                    AUTH_KEY = Guid.NewGuid().ToString("n");
                    SECURE_AUTH_KEY = Guid.NewGuid().ToString("n");
                    LOGGED_IN_KEY = Guid.NewGuid().ToString("n");
                    NONCE_KEY = Guid.NewGuid().ToString("n");
                    AUTH_SALT = Guid.NewGuid().ToString("n");
                    SECURE_AUTH_SALT = Guid.NewGuid().ToString("n");
                    LOGGED_IN_SALT = Guid.NewGuid().ToString("n");
                    NONCE_SALT = Guid.NewGuid().ToString("n");
                }
                else
                {
                    Console.WriteLine("Do you want to manually enter Authentication Unique Keys and Salts? (Y/n) ");
                    var manualKeys = Console.ReadLine().ToLower() == "Y";
                    if (manualKeys)
                    {
                        Console.WriteLine("AUTH_KEY: ");
                        AUTH_KEY = Console.ReadLine();
                        Console.WriteLine("SECURE_AUTH_KEY: ");
                        SECURE_AUTH_KEY = Console.ReadLine();
                        Console.WriteLine("LOGGED_IN_KEY: ");
                        LOGGED_IN_KEY = Console.ReadLine();
                        Console.WriteLine("NONCE_KEY: ");
                        NONCE_KEY = Console.ReadLine();
                        Console.WriteLine("AUTH_SALT: ");
                        AUTH_SALT = Console.ReadLine();
                        Console.WriteLine("SECURE_AUTH_SALT: ");
                        SECURE_AUTH_SALT = Console.ReadLine();
                        Console.WriteLine("LOGGED_IN_SALT: ");
                        LOGGED_IN_SALT = Console.ReadLine();
                        Console.WriteLine("NONCE_SALT: ");
                        NONCE_SALT = Console.ReadLine();
                    }
                }

                Console.WriteLine("\n\nConfiguration summary:");
                Console.WriteLine($"Database host name: {dbHost}");
                Console.WriteLine($"Database name: {dbName}");
                Console.WriteLine($"Database username: {dbUsername}");
                Console.WriteLine($"Database password: {dbPassword}");
                Console.WriteLine($"Database table prefix: {dbTablePrefix}\n");
                Console.WriteLine($"AUTH_KEY: {AUTH_KEY}");
                Console.WriteLine($"SECURE_AUTH_KEY: {SECURE_AUTH_KEY}");
                Console.WriteLine($"LOGGED_IN_KEY: {LOGGED_IN_KEY}");
                Console.WriteLine($"NONCE_KEY: {NONCE_KEY}");
                Console.WriteLine($"AUTH_SALT: {AUTH_SALT}");
                Console.WriteLine($"SECURE_AUTH_SALT: {SECURE_AUTH_SALT}");
                Console.WriteLine($"LOGGED_IN_SALT: {LOGGED_IN_SALT}");
                Console.WriteLine($"NONCE_SALT: {NONCE_SALT}");
                Console.WriteLine("\n\nDo you confirm this configuration? (Y/n)");
                getInput = Console.ReadLine() != "Y";
            }

            Console.WriteLine("\nUpdating configuration file.");
            using (Stream input = Assembly.GetExecutingAssembly().GetManifestResourceStream("AzureWebSiteWordPressInstaller.wp-config.php"))
            {
                input.Position = 0;
                using (StreamReader reader = new StreamReader(input, Encoding.UTF8))
                {
                    var config = reader.ReadToEnd();
                    config = config
                        .Replace("@DbName", dbName)
                        .Replace("@DbUser", dbUsername)
                        .Replace("@DbPassword", dbPassword)
                        .Replace("@DbHost", dbHost)
                        .Replace("@DbTablePrefix", dbTablePrefix)

                        .Replace("@KeyAuthSalt", AUTH_SALT)
                        .Replace("@KeySecureAuthSalt", SECURE_AUTH_SALT)
                        .Replace("@KeyLoggedInSalt", LOGGED_IN_SALT)
                        .Replace("@KeyNonceSalt", NONCE_SALT)

                        .Replace("@KeyAuth", AUTH_KEY)
                        .Replace("@KeySecureAuth", SECURE_AUTH_KEY)
                        .Replace("@KeyLoggedIn", LOGGED_IN_KEY)
                        .Replace("@KeyNonce", NONCE_KEY);

                    using (var writer = new StreamWriter(webRoot + "\\wp-config.php", false))
                    {
                        writer.Write(config);
                        writer.Flush();
                        writer.Close();
                    }
                }
            }
            File.Delete(webRoot + "\\wp-config-sample.php");
            Console.WriteLine("\nWordPress Site configuration completed.");

#if DEBUG
            Console.ReadLine();
#endif
        }

        private static void ShowIp()
        {
            Console.WriteLine("Determining machine public IP");
            string ip = "";
            try
            {
                ip = new WebClient().DownloadString("http://icanhazip.com");
            }
            catch (Exception)
            {
            }

            if (ip == "")
            {
                try
                {
                    ip = new System.Net.WebClient().DownloadString("http://bot.whatismyipaddress.com");
                }
                catch (Exception)
                {
                }

            }
            if (ip == "")
            {
                try
                {
                    ip = new System.Net.WebClient().DownloadString("http://ipinfo.io/ip");
                }
                catch (Exception)
                {
                }

            }
            if (ip == "")
            {
                try
                {
                    using (var client = new WebClient())
                    {
                        var txt = client.DownloadString("http://checkip.dyndns.org");
                        string[] a = txt.Split(':');
                        ip = a[1].Substring(1);

                    }
                }
                catch (Exception)
                {
                }

            }

            Console.WriteLine($"Machine public IP: {ip}");
        }

        private static void DeleteAllFiles(string path, bool shouldLog)
        {
            var di = new DirectoryInfo(path);

            foreach (FileInfo file in di.GetFiles())
            {
                if (shouldLog)
                    Console.WriteLine($"Deleting file: {file.FullName}");
                file.Delete();
            }
            foreach (DirectoryInfo dir in di.GetDirectories())
            {
                if (shouldLog)
                    Console.WriteLine($"Deleting folder: {dir.FullName}");
                dir.Delete(true);
            }
        }
        public static void CopyWebConfig(string webRoot)
        {
            using (Stream input = Assembly.GetExecutingAssembly().GetManifestResourceStream("AzureWebSiteWordPressInstaller.web.config"))
            using (Stream output = File.Create(webRoot + "\\web.config"))
            {
                CopyStream(input, output);
            }
        }
        public static void CopyStream(Stream input, Stream output)
        {
            byte[] buffer = new byte[8192];

            int bytesRead;
            while ((bytesRead = input.Read(buffer, 0, buffer.Length)) > 0)
            {
                output.Write(buffer, 0, bytesRead);
            }
        }
        private static void ExtractWordPressPackage(string webRoot, string packagePath)
        {
            Console.WriteLine($"Extracting WordPress package..");
            var tempPath = Path.GetTempPath() + Guid.NewGuid().ToString("N").Substring(0, 8);
            var wordpressPath = tempPath + "\\wordpress";
            ZipFile.ExtractToDirectory(packagePath, tempPath);

            Console.WriteLine($"Copying files to web root..");
            foreach (string dirPath in Directory.GetDirectories(wordpressPath, "*", SearchOption.AllDirectories))
                Directory.CreateDirectory(dirPath.Replace(wordpressPath, webRoot));
            
            foreach (string newPath in Directory.GetFiles(wordpressPath, "*.*", SearchOption.AllDirectories))
                File.Copy(newPath, newPath.Replace(wordpressPath, webRoot), true);

            Console.WriteLine($"Removing temp data..");
            DeleteAllFiles(tempPath, false);
            Directory.Delete(tempPath);
        }

        private static void WriteHelp()
        {
            Console.WriteLine("Available parameters for Wordpress installer for Azure WebSites.\n");
            Console.WriteLine("Parameter\tDescription");
            Console.WriteLine("---------\t-----------");
            Console.WriteLine("-h,-help\tShow available parameters for Wordpress installer for Azure WebSites.\n");
            Console.WriteLine("-v,-version\tVersion of WordPress which should be installed. If value is not provided, latest vesion of WordPress will be installed.\n");
            Console.WriteLine("-f,-force\tIf WordPress is already installed in application path, content will be deleted before installation. If you use this option, result will be irreversible.\n");
            Console.WriteLine("-i\tGet the public IP of the machine. Needed to configure users for MySQL for specific IP.\n");
        }

        private static string DownloadWordPress(string version)
        {
            using (var client = new WebClient())
            {
                var packagePath = $"https://wordpress.org/wordpress-{version}-IIS.zip";
                var stream = client.OpenRead(packagePath);
                var bytes_total = Convert.ToInt64(client.ResponseHeaders["Content-Length"]);
                Console.WriteLine($"File size: {bytes_total / 1024}KB");
                stream.Flush();
                stream.Close();

                var filename = Path.GetTempPath() + Guid.NewGuid().ToString("N").Substring(0, 8) + ".zip";
                client.DownloadFile(new Uri(packagePath), filename);

                Console.WriteLine();

                Console.WriteLine($"Package downloaded to {filename}");
                return filename;
            }
        }

        private static string GetLatestWordPressVersion()
        {
            try
            {
                using (var client = new WebClient())
                {
                    var str = client.DownloadString("https://api.wordpress.org/core/version-check/1.7/");
                    var version = JsonConvert.DeserializeObject<WordPressVersionCheckResponse>(str);
                    if (version == null || version.offers == null || version.offers.Count == 0)
                        return null;

                    return version.offers[0].version;
                }
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
