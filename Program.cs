using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Versioning;
using System.Text;
using System.Text.RegularExpressions;

namespace test_csc
{
    class Program
    {
        static int Main(string[] args)
        {
            Encoding encoding;
            if (Console.IsOutputRedirected || args?.Length > 0 && args.Contains("/utf8output"))
            {
                encoding = new UTF8Encoding(false, true);
            }
            else
            {
                encoding = new UnicodeEncoding(!BitConverter.IsLittleEndian, false, true);
            }
            Console.InputEncoding = encoding;
            Console.OutputEncoding = encoding;

            var dotnetHostPathEnv = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH");
            var dotnetHostPath = string.IsNullOrEmpty(dotnetHostPathEnv) ? "dotnet" : dotnetHostPathEnv;

            var versionInfo = Process.Start(CalcDotnetStartInfo(dotnetHostPath, "--info"));
            var sdkInfoBuilder = new StringBuilder();
            versionInfo.OutputDataReceived += (pSender, pEventArgs) => sdkInfoBuilder.AppendLine(pEventArgs.Data);
            versionInfo.BeginOutputReadLine();
            versionInfo.WaitForExit();

            var sdkInfo = sdkInfoBuilder.ToString();
            var sdkVersionRx = new Regex(@"\d+.\d+.\d+");
            Version sdkVersion = null;

            Match match;
            if ((match = sdkVersionRx.Match(sdkInfo)).Success)
            {
                sdkVersion = new Version(match.Value);
            }

            if (sdkVersion == null)
            {
                Console.Error.WriteLine("Cannot determine SDK version!");
                return -1;
            }

            var proc = Process.Start(CalcDotnetStartInfo(dotnetHostPath, string.Join(" ", args)));
            proc.OutputDataReceived += OutputDataReceivedHandler;
            proc.ErrorDataReceived += ErrorDataReceivedHandler;
            proc.BeginErrorReadLine();
            proc.BeginOutputReadLine();

            proc.WaitForExit();

            return proc.ExitCode;
        }

        private static ProcessStartInfo CalcDotnetStartInfo(string dotnetHostPath, string args)
        {
            return new ProcessStartInfo(dotnetHostPath) {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardInput = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                Arguments = args
            };
        }

        private static void OutputDataReceivedHandler(object sender, DataReceivedEventArgs e)
        {
            if (e?.Data == null)
            {
                return;
            }
            Console.WriteLine(e.Data);
        }

        private static void ErrorDataReceivedHandler(object sender, DataReceivedEventArgs e)
        {
            if (e?.Data == null)
            {
                return;
            }
            Console.Error.WriteLine(e.Data);
        }

        public static void PrintAssemblyInfo()
        {
            var paths = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string;
            var exeAssembly = Assembly.GetExecutingAssembly();
            var targetFramework = exeAssembly.GetCustomAttributes(true).OfType<TargetFrameworkAttribute>().First();
            var exeLocation = exeAssembly.Location;

            foreach (var assemblies in paths.Split(Path.PathSeparator).SkipWhile(pAssembly => pAssembly != exeLocation))
            {
                Console.WriteLine($"Assemblies: {assemblies}");
            }
        }
    }
}