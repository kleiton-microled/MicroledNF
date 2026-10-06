using System.Windows;
using System.Windows.Media;
using Microled.Nfe.DesktopLauncher.Services;
using Microsoft.Win32;

namespace Microled.Nfe.DesktopLauncher;

public partial class MainWindow : Window
{
    private static readonly Brush OffBrush = new SolidColorBrush(Color.FromRgb(192, 192, 192));
    private static readonly Brush OnBrush = new SolidColorBrush(Color.FromRgb(46, 160, 67));
    private static readonly Brush BusyBrush = new SolidColorBrush(Color.FromRgb(230, 162, 60));

    private readonly StackOrchestrator _stack = new();
    private readonly CertificateClient _certificates = new();
    private OnPremOptions _options = OnPremOptionsStore.Load();
    private bool _busy;

    public MainWindow()
    {
        InitializeComponent();
        AccessPathBox.Text = _options.AccessDatabasePath;
        AppendLog("Pronto. Indique o arquivo Access e clique em Iniciar ambiente.");
        _ = RefreshLightsAsync();
    }

    private async void OnStartClick(object sender, RoutedEventArgs e)
    {
        if (_busy)
        {
            return;
        }

        try
        {
            SaveOptionsFromUi();
        }
        catch (Exception ex)
        {
            AppendLog("Não foi possível salvar a configuração: " + ex.Message);
        }

        if (string.IsNullOrWhiteSpace(_options.AccessDatabasePath) || !File.Exists(_options.AccessDatabasePath))
        {
            MessageBox.Show(
                this,
                "Selecione um arquivo Access (.mdb ou .accdb) existente.",
                "Caminho do Access",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        _busy = true;
        SetBusyLights();
        try
        {
            await _stack.StartAsync(_options, AppendLog, CancellationToken.None);
            await LoadCertificatesAsync();
        }
        catch (Exception ex)
        {
            AppendLog("Falha ao iniciar: " + ex.Message);
            MessageBox.Show(this, ex.Message, "Não foi possível iniciar", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            _busy = false;
            await RefreshLightsAsync();
        }
    }

    private void OnStopClick(object sender, RoutedEventArgs e)
    {
        try
        {
            SaveOptionsFromUi();
        }
        catch (Exception ex)
        {
            AppendLog("Não foi possível salvar a configuração: " + ex.Message);
        }

        _stack.Stop(AppendLog);
        AppendLog("Ambiente parado. O banco local permanece em execução.");
        _ = RefreshLightsAsync();
    }

    private void OnOpenClick(object sender, RoutedEventArgs e)
    {
        if (!_stack.ApiUp)
        {
            MessageBox.Show(
                this,
                "Inicie o ambiente antes de abrir o sistema.",
                "Sistema",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        StackOrchestrator.OpenFront();
    }

    private void OnBrowseAccessClick(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Access (*.mdb;*.accdb)|*.mdb;*.accdb|Todos os arquivos (*.*)|*.*",
            Title = "Selecione o arquivo NF.mdb"
        };
        if (dialog.ShowDialog(this) == true)
        {
            AccessPathBox.Text = dialog.FileName;
        }
    }

    private void OnSaveAccessClick(object sender, RoutedEventArgs e)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(AccessPathBox.Text) || !File.Exists(AccessPathBox.Text.Trim()))
            {
                MessageBox.Show(
                    this,
                    "Selecione um arquivo Access (.mdb ou .accdb) existente.",
                    "Caminho do Access",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            SaveOptionsFromUi();
            AppendLog("Caminho do Access salvo em " + LauncherPaths.AgentSettingsFile);
        }
        catch (Exception ex)
        {
            AppendLog("Não foi possível salvar o Access: " + ex.Message);
            MessageBox.Show(
                this,
                "Não foi possível gravar o caminho do Access." + Environment.NewLine + ex.Message,
                "Salvar caminho",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private async void OnRefreshCertificatesClick(object sender, RoutedEventArgs e)
    {
        await LoadCertificatesAsync();
    }

    private async void OnSelectCertificateClick(object sender, RoutedEventArgs e)
    {
        if (CertificateBox.SelectedItem is not CertificateItem item)
        {
            MessageBox.Show(this, "Escolha um certificado na lista.", "Certificado", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            await _certificates.SelectAsync(_options.AgentUrl, item, CancellationToken.None);
            AppendLog("Certificado selecionado: " + item);
        }
        catch (Exception ex)
        {
            AppendLog("Não foi possível gravar o certificado: " + ex.Message);
            MessageBox.Show(this, ex.Message, "Certificado", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OnClosing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        try
        {
            SaveOptionsFromUi();
        }
        catch (Exception ex)
        {
            AppendLog("Não foi possível salvar ao sair: " + ex.Message);
        }

        _stack.Stop(AppendLog);
        _stack.Dispose();
    }

    private void SaveOptionsFromUi()
    {
        _options.AccessDatabasePath = AccessPathBox.Text.Trim();
        OnPremOptionsStore.Save(_options);
        if (!string.IsNullOrWhiteSpace(_options.AccessDatabasePath))
        {
            AgentUserSettingsWriter.WriteAccessAndApi(_options.AccessDatabasePath, _options.ApiUrl);
        }
    }

    private async Task LoadCertificatesAsync()
    {
        try
        {
            var items = await _certificates.ListAsync(_options.AgentUrl, CancellationToken.None);
            CertificateBox.ItemsSource = items;
            if (items.Count > 0 && CertificateBox.SelectedIndex < 0)
            {
                CertificateBox.SelectedIndex = 0;
            }

            AppendLog(items.Count == 0
                ? "Nenhum certificado encontrado no Windows. Instale o A1/A3 no usuário atual."
                : $"Encontrados {items.Count} certificado(s).");
        }
        catch (Exception ex)
        {
            AppendLog("Lista de certificados indisponível (inicie o ambiente): " + ex.Message);
        }
    }

    private async Task RefreshLightsAsync()
    {
        try
        {
            await _stack.RefreshHealthAsync(_options, CancellationToken.None);
        }
        catch
        {
            // ignore probe errors
        }

        PostgresLight.Fill = _stack.PostgresUp ? OnBrush : OffBrush;
        ApiLight.Fill = _stack.ApiUp ? OnBrush : OffBrush;
        AgentLight.Fill = _stack.AgentUp ? OnBrush : OffBrush;
        FrontLight.Fill = _stack.FrontUp ? OnBrush : OffBrush;
    }

    private void SetBusyLights()
    {
        PostgresLight.Fill = BusyBrush;
        ApiLight.Fill = BusyBrush;
        AgentLight.Fill = BusyBrush;
        FrontLight.Fill = BusyBrush;
    }

    private void OnCopyLogClick(object sender, RoutedEventArgs e)
    {
        var text = LogBox.SelectedText.Length > 0 ? LogBox.SelectedText : LogBox.Text;
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        Clipboard.SetText(text);
        AppendLog("Conteúdo do console copiado.");
    }

    private void AppendLog(string message)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(() => AppendLog(message));
            return;
        }

        var line = DateTime.Now.ToString("HH:mm:ss") + "  " + message + Environment.NewLine;
        LogBox.AppendText(line);
        if (LogBox.Text.Length > 120_000)
        {
            LogBox.Text = LogBox.Text[^80_000..];
        }

        LogBox.CaretIndex = LogBox.Text.Length;
        LogBox.ScrollToEnd();
    }
}
