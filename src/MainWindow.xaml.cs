using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Web.WebView2.Core;

namespace EnterprisePOS.Desktop
{
    public partial class MainWindow : Window
    {
        private Process? _serverProcess;
        private const string PosUrl = "http://127.0.0.1:5288";

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
            Closing += MainWindow_Closing;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                statusText.Text = "Starting offline POS server...";
                await StartBackendServerAsync();

                statusText.Text = "Initializing UI engine...";
                var envOptions = new CoreWebView2EnvironmentOptions(
                    additionalBrowserArguments: "--kiosk-printing --disable-print-preview"
                );
                var env = await CoreWebView2Environment.CreateAsync(
                    userDataFolder: Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "EnterprisePOS_Desktop_WebView"),
                    options: envOptions);
                await posWebView.EnsureCoreWebView2Async(env);

                await printWebView.EnsureCoreWebView2Async(env);

                var printLock = new System.Threading.SemaphoreSlim(1, 1);
                posWebView.CoreWebView2.WebMessageReceived += async (s, args) =>
                {
                    try
                    {
                        var msg = args.TryGetWebMessageAsString();
                        if (string.IsNullOrEmpty(msg)) return;

                        if (msg.Contains("\"action\":\"print\""))
                        {
                            var match = System.Text.RegularExpressions.Regex.Match(msg, "\"url\":\"([^\"]+)\"");
                            if (match.Success)
                            {
                                var receiptRelativeUrl = match.Groups[1].Value;
                                var fullReceiptUrl = $"{PosUrl}{receiptRelativeUrl}";
                                
                                await printLock.WaitAsync();
                                try
                                {
                                    await PrintReceiptSilentlyAsync(fullReceiptUrl);
                                }
                                finally
                                {
                                    printLock.Release();
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Print error: " + ex);
                    }
                };

                statusText.Text = "Loading Enterprise POS...";
                posWebView.Source = new Uri(PosUrl);
                posWebView.NavigationCompleted += (s, args) =>
                {
                    splashOverlay.Visibility = Visibility.Collapsed;
                    posWebView.Visibility = Visibility.Visible;
                };
            }
            catch (Exception ex)
            {
                statusText.Text = "Startup error: " + ex.Message;
                MessageBox.Show($"Failed to launch POS:\n{ex.Message}", "Enterprise POS Offline", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task StartBackendServerAsync()
        {
            // First check if already responding on port
            if (await IsServerRespondingAsync())
            {
                return;
            }

            // Find backend directory
            string appDir = AppDomain.CurrentDomain.BaseDirectory;
            string backendSubDir = Path.Combine(appDir, "backend");
            string backendExe = Path.Combine(backendSubDir, "EnterprisePOS.exe");
            string workingDir = backendSubDir;

            if (!File.Exists(backendExe))
            {
                backendExe = Path.Combine(appDir, "EnterprisePOS.exe");
                workingDir = appDir;
            }

            ProcessStartInfo psi;
            if (File.Exists(backendExe))
            {
                psi = new ProcessStartInfo
                {
                    FileName = backendExe,
                    Arguments = $"--urls \"{PosUrl}\"",
                    WorkingDirectory = workingDir,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = false,
                    RedirectStandardError = false
                };
            }
            else
            {
                string backendDll = Path.Combine(appDir, "EnterprisePOS.dll");
                psi = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = $"\"{backendDll}\" --urls \"{PosUrl}\"",
                    WorkingDirectory = workingDir,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = false,
                    RedirectStandardError = false
                };
            }

            psi.EnvironmentVariables["ASPNETCORE_URLS"] = PosUrl;
            psi.EnvironmentVariables["ASPNETCORE_ENVIRONMENT"] = "Production";

            _serverProcess = Process.Start(psi);

            // Wait until the server responds
            var stopwatch = Stopwatch.StartNew();
            while (stopwatch.Elapsed < TimeSpan.FromSeconds(30))
            {
                if (await IsServerRespondingAsync())
                {
                    return;
                }
                await Task.Delay(500);
            }

            throw new TimeoutException("Timed out waiting for offline POS server to start.");
        }

        private static async Task<bool> IsServerRespondingAsync()
        {
            try
            {
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(1) };
                var response = await client.GetAsync($"{PosUrl}/ping");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        private async Task PrintReceiptSilentlyAsync(string fullReceiptUrl)
        {
            var tcs = new TaskCompletionSource<bool>();

            EventHandler<CoreWebView2NavigationCompletedEventArgs>? navHandler = null;
            navHandler = async (sender, e) =>
            {
                printWebView.NavigationCompleted -= navHandler;
                try
                {
                    await Task.Delay(200); // Allow DOM layout to complete

                    var printSettings = printWebView.CoreWebView2.Environment.CreatePrintSettings();
                    printSettings.ShouldPrintBackgrounds = true;
                    printSettings.ShouldPrintHeaderAndFooter = false;
                    printSettings.MarginTop = 0;
                    printSettings.MarginBottom = 0;
                    printSettings.MarginLeft = 0;
                    printSettings.MarginRight = 0;

                    // Automatically prints silently to whatever default printer the user has installed
                    var status = await printWebView.CoreWebView2.PrintAsync(printSettings);
                    tcs.TrySetResult(true);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Direct print error: " + ex);
                    tcs.TrySetResult(false);
                }
            };

            printWebView.NavigationCompleted += navHandler;
            printWebView.Source = new Uri(fullReceiptUrl);

            await Task.WhenAny(tcs.Task, Task.Delay(10000));
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                if (_serverProcess != null && !_serverProcess.HasExited)
                {
                    _serverProcess.Kill(entireProcessTree: true);
                }
            }
            catch { }
        }
    }
}