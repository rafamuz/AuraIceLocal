using System.Diagnostics;
using Velopack;

namespace AuraIceLocal;

internal sealed class MainForm : Form
{
    private readonly AppSettings _settings = AppSettings.Load();
    private readonly EmaFilter _temperatureFilter = new();
    private readonly ThermalProtectionController _thermalProtection = new();
    private readonly AuraIceHidTransport _hidTransport = new();
    private readonly HidDeviceDetector _deviceDetector = new();
    private readonly UsbWriteSession _usbWriteSession = new();
    private readonly object _usbWriteGate = new();
    private readonly System.Windows.Forms.Timer _safetyTimer = new() { Interval = 500 };
    private readonly bool _startedWithWindows;
    private readonly TrayTemperatureIcon _trayIcon;
    private readonly AppUpdateService _updateService = new();

    private HardwareMonitorService? _hardwareMonitor;
    private CancellationTokenSource? _monitorCts;
    private Task? _monitorTask;
    private HidDeviceCandidate? _selectedDevice;
    private bool _updatingDeviceControls;
    private bool _loadingAutomationControls;
    private bool _exitRequested;
    private bool _windowPlacementReady;
    private bool _updateBusy;
    private FormWindowState _lastVisibleWindowState = FormWindowState.Normal;
    private HelpForm? _helpForm;
    private readonly Bitmap _appIconBitmap = AppVisualAssets.CreateApplicationBitmap();

    private readonly MenuStrip _mainMenu = new();

    private readonly ComboBox _deviceCombo = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 520, DropDownWidth = 760 };
    private readonly Button _scanDevicesButton = new() { Text = "Procurar visores", AutoSize = true };
    private readonly Label _profileSourceLabel = new() { AutoSize = true };

    private readonly ComboBox _sensorCombo = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 240 };
    private readonly NumericUpDown _smoothing = new() { DecimalPlaces = 1, Minimum = 0, Maximum = 20, Increment = 0.5M, Width = 90 };
    private readonly Button _startStopButton = new() { Text = "Iniciar monitoramento", AutoSize = true };
    private readonly Button _singlePacketTestButton = new() { Text = "Enviar um pacote de teste", AutoSize = true, Enabled = false };
    private readonly Button _installPawnIoButton = new() { Text = "Instalar suporte de sensores", AutoSize = true, Visible = false };
    private readonly CheckBox _startWithWindows = new() { Text = "Iniciar com o Windows", AutoSize = true };
    private readonly CheckBox _autoStartMonitoring = new() { Text = "Monitorar e enviar ao abrir", AutoSize = true };
    private readonly Button _checkUpdatesButton = new() { Text = "Verificar atualizações", AutoSize = true };
    private readonly Label _updateStatusLabel = new() { AutoSize = true };
    private readonly ProgressBar _updateProgress = new() { Width = 180, Height = 22, Visible = false };

    private readonly Label _statusLabel = NewValueLabel("Parado");
    private readonly Label _rawTemperatureLabel = NewValueLabel("-- °C");
    private readonly Label _smoothedTemperatureLabel = NewValueLabel("-- °C");
    private readonly Label _displayTemperatureLabel = NewValueLabel("-- °C");
    private readonly Label _displaySensorLabel = NewValueLabel("Core Average");
    private readonly Label _protectionSensorLabel = NewValueLabel("Core Max / CPU Package");
    private readonly Label _thermalProtectionStateLabel = NewValueLabel("Normal");
    private readonly Label _usbLabel = NewValueLabel("Verificando...");
    private readonly Label _packetLabel = new() { AutoSize = true, MaximumSize = new Size(900, 0), Text = "--" };

    private readonly ListView _sensorList = new() { Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true, GridLines = true };
    private readonly ListView _deviceList = new() { Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true, GridLines = true };

    private bool IsRunning => _monitorCts is not null;

    public MainForm(bool startedWithWindows = false)
    {
        _startedWithWindows = startedWithWindows;
        _trayIcon = new TrayTemperatureIcon();
        _trayIcon.PanelRequested += ShowPanel;
        _trayIcon.ExitRequested += ExitApplication;
        Text = "RM Aura Ice Display 0.3.3 — Rise Mode Aura Ice";
        StartPosition = FormStartPosition.Manual;
        AutoScaleMode = AutoScaleMode.Dpi;
        AutoScroll = true;
        MinimumSize = new Size(940, 680);
        DoubleBuffered = true;
        UiTheme.ApplyForm(this);
        ApplySavedWindowPlacement();

        if (_startedWithWindows)
        {
            ShowInTaskbar = false;
            WindowState = FormWindowState.Minimized;
            Opacity = 0;
        }

        BuildLayout();
        LoadSettingsIntoControls();
        _installPawnIoButton.Visible = !PawnIoSupport.IsInstalled();
        ScanDevices(showErrors: false);

        _safetyTimer.Tick += (_, _) => RefreshSinglePacketTestButton();
        _safetyTimer.Start();

        Shown += (_, _) => OnInitialShown();
        ResizeEnd += (_, _) => CaptureWindowPlacement(saveImmediately: true);
        SizeChanged += (_, _) => OnWindowStateChanged();
        FormClosing += OnFormClosing;
        _windowPlacementReady = true;
    }

    private void BuildLayout()
    {
        ConfigureLists();
        ConfigureMenu();
        ConfigureControlStyles();

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            Padding = new Padding(20),
            BackColor = UiTheme.AppBackground,
            ColumnCount = 1,
            RowCount = 4,
            GrowStyle = TableLayoutPanelGrowStyle.FixedSize
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var header = new TableLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            Margin = new Padding(0, 0, 0, 16)
        };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 76));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        header.Controls.Add(new PictureBox
        {
            Image = _appIconBitmap,
            SizeMode = PictureBoxSizeMode.Zoom,
            Size = new Size(64, 64),
            Margin = new Padding(0, 0, 12, 0)
        }, 0, 0);
        var headerText = new TableLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Margin = Padding.Empty
        };
        headerText.Controls.Add(new Label
        {
            Text = "RM Aura Ice Display",
            AutoSize = true,
            Font = new Font("Segoe UI", 20, FontStyle.Bold),
            ForeColor = UiTheme.Text,
            Margin = new Padding(0, 4, 0, 0)
        }, 0, 0);
        headerText.Controls.Add(new Label
        {
            Text = "Monitoramento local e proteção térmica do seu Aura Ice",
            AutoSize = true,
            ForeColor = UiTheme.MutedText,
            Margin = Padding.Empty
        }, 0, 1);
        header.Controls.Add(headerText, 1, 0);
        header.Controls.Add(new Label
        {
            Text = "HID AA88:8666  •  relatório de 11 bytes",
            AutoSize = true,
            Anchor = AnchorStyles.Right,
            BackColor = Color.FromArgb(225, 238, 255),
            ForeColor = UiTheme.PrimaryDark,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            Padding = new Padding(12, 8, 12, 8),
            Margin = new Padding(12, 12, 0, 0)
        }, 2, 0);
        root.Controls.Add(header, 0, 0);

        var controlsCard = CreateCard(rowCount: 5);
        controlsCard.Controls.Add(CreateSectionHeading(
            "Configuração e automação",
            "Escolha o visor e o sensor, controle o monitoramento e as atualizações.",
            UiIconKind.Automation), 0, 0);

        var deviceControls = CreateControlRow();
        deviceControls.Controls.Add(NewControlLabel("Visor LCD"));
        deviceControls.Controls.Add(_deviceCombo);
        deviceControls.Controls.Add(_scanDevicesButton);
        deviceControls.Controls.Add(_profileSourceLabel);
        controlsCard.Controls.Add(deviceControls, 0, 1);

        var monitorControls = CreateControlRow();
        monitorControls.Controls.Add(NewControlLabel("Sensor da CPU"));
        monitorControls.Controls.Add(_sensorCombo);
        monitorControls.Controls.Add(NewControlLabel("Suavização"));
        monitorControls.Controls.Add(_smoothing);
        monitorControls.Controls.Add(new Label { Text = "segundos", AutoSize = true, ForeColor = UiTheme.MutedText, Margin = new Padding(0, 9, 14, 0) });
        monitorControls.Controls.Add(_startStopButton);
        monitorControls.Controls.Add(_singlePacketTestButton);
        monitorControls.Controls.Add(_installPawnIoButton);
        controlsCard.Controls.Add(monitorControls, 0, 2);

        var automationControls = CreateControlRow();
        automationControls.Controls.Add(NewControlLabel("Automação"));
        automationControls.Controls.Add(_startWithWindows);
        automationControls.Controls.Add(_autoStartMonitoring);
        automationControls.Controls.Add(new Label
        {
            Text = "Inicia na bandeja e conecta automaticamente quando as duas opções estão marcadas.",
            AutoSize = true,
            ForeColor = UiTheme.MutedText,
            Margin = new Padding(4, 9, 0, 0)
        });
        controlsCard.Controls.Add(automationControls, 0, 3);

        var updateControls = CreateControlRow(bottomMargin: 0);
        updateControls.Controls.Add(NewControlLabel("Atualizações"));
        updateControls.Controls.Add(_checkUpdatesButton);
        _updateStatusLabel.Text = $"Versão instalada: {_updateService.CurrentVersion}";
        _updateStatusLabel.ForeColor = UiTheme.MutedText;
        _updateStatusLabel.Margin = new Padding(8, 9, 10, 0);
        updateControls.Controls.Add(_updateStatusLabel);
        _updateProgress.Margin = new Padding(6, 7, 0, 0);
        updateControls.Controls.Add(_updateProgress);
        controlsCard.Controls.Add(updateControls, 0, 4);
        root.Controls.Add(controlsCard, 0, 1);

        var summaryCard = CreateCard(rowCount: 4);
        summaryCard.Controls.Add(CreateSectionHeading(
            "Estado em tempo real",
            "Leituras atuais, proteção térmica e último pacote preparado para o visor.",
            UiIconKind.Cpu), 0, 0);
        var metrics = new TableLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 4,
            Margin = new Padding(-6, 4, -6, 4)
        };
        metrics.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        metrics.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        for (int row = 0; row < 4; row++)
        {
            metrics.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        }
        metrics.Controls.Add(CreateMetricCard("Estado", _statusLabel), 0, 0);
        metrics.Controls.Add(CreateMetricCard("LCD USB", _usbLabel), 1, 0);
        metrics.Controls.Add(CreateMetricCard("Sensor de exibição", _displaySensorLabel), 0, 1);
        metrics.Controls.Add(CreateMetricCard("Temperatura bruta", _rawTemperatureLabel), 1, 1);
        metrics.Controls.Add(CreateMetricCard("Temperatura suavizada", _smoothedTemperatureLabel), 0, 2);
        metrics.Controls.Add(CreateMetricCard("Temperatura exibida", _displayTemperatureLabel), 1, 2);
        metrics.Controls.Add(CreateMetricCard("Sensor de proteção", _protectionSensorLabel), 0, 3);
        metrics.Controls.Add(CreateMetricCard("Proteção térmica", _thermalProtectionStateLabel), 1, 3);
        summaryCard.Controls.Add(metrics, 0, 1);

        var packetCard = new TableLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Color.FromArgb(240, 246, 255),
            Padding = new Padding(12),
            Margin = new Padding(0, 6, 0, 8)
        };
        packetCard.Controls.Add(UiTheme.NewCaption("ÚLTIMO PACOTE"), 0, 0);
        _packetLabel.ForeColor = UiTheme.Text;
        _packetLabel.Margin = Padding.Empty;
        packetCard.Controls.Add(_packetLabel, 0, 1);
        summaryCard.Controls.Add(packetCard, 0, 2);

        var warning = new Label
        {
            AutoSize = false,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            BackColor = Color.FromArgb(255, 248, 225),
            ForeColor = Color.FromArgb(92, 67, 0),
            Height = 56,
            Padding = new Padding(14, 6, 14, 6),
            Text = "Envio automático: ao iniciar o monitoramento, o app conecta ao visor confirmado e envia uma vez por segundo. Antes de cada escrita, revalida o perfil, o relatório HID e se o software oficial está fechado.",
            Margin = Padding.Empty
        };
        summaryCard.Controls.Add(warning, 0, 3);
        root.Controls.Add(summaryCard, 0, 2);

        var tabs = new TabControl
        {
            Dock = DockStyle.Fill,
            Padding = new Point(18, 7),
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            MinimumSize = new Size(0, 310)
        };
        var tabImages = new ImageList { ImageSize = new Size(20, 20), ColorDepth = ColorDepth.Depth32Bit };
        tabImages.Images.Add("devices", UiIconFactory.Get(UiIconKind.Device, UiTheme.Primary));
        tabImages.Images.Add("sensors", UiIconFactory.Get(UiIconKind.Cpu, UiTheme.Accent));
        tabs.ImageList = tabImages;
        var deviceTab = new TabPage("Dispositivos HID / diagnóstico") { BackColor = Color.White, Padding = new Padding(8), ImageKey = "devices" };
        var sensorTab = new TabPage("Sensores de temperatura da CPU") { BackColor = Color.White, Padding = new Padding(8), ImageKey = "sensors" };
        deviceTab.Controls.Add(_deviceList);
        sensorTab.Controls.Add(_sensorList);
        tabs.TabPages.Add(deviceTab);
        tabs.TabPages.Add(sensorTab);
        var tabsCard = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = UiTheme.CardBackground,
            BorderStyle = BorderStyle.FixedSingle,
            Padding = new Padding(10),
            Margin = new Padding(0, 0, 0, 4)
        };
        tabsCard.Controls.Add(tabs);
        root.Controls.Add(tabsCard, 0, 3);

        Controls.Add(root);
        Controls.Add(_mainMenu);
        MainMenuStrip = _mainMenu;

        _scanDevicesButton.Click += (_, _) => ScanDevices(showErrors: true);
        _deviceCombo.SelectedIndexChanged += (_, _) => OnSelectedDeviceChanged();
        _startStopButton.Click += (_, _) => ToggleMonitoring();
        _singlePacketTestButton.Click += (_, _) => SendSingleTestPacket();
        _installPawnIoButton.Click += async (_, _) => await InstallPawnIoAsync();
        _checkUpdatesButton.Click += async (_, _) => await CheckForUpdatesAsync();
        _sensorCombo.SelectedIndexChanged += (_, _) =>
        {
            if (_sensorCombo.SelectedItem is string selected)
            {
                lock (_settings)
                {
                    _settings.CpuSensorName = selected;
                }
                _temperatureFilter.Reset();
                _thermalProtection.Reset();
            }
        };
        _smoothing.ValueChanged += (_, _) =>
        {
            lock (_settings)
            {
                _settings.SmoothingSeconds = (double)_smoothing.Value;
            }
            _temperatureFilter.Reset();
            _thermalProtection.Reset();
        };
        _startWithWindows.CheckedChanged += (_, _) => OnStartWithWindowsChanged();
        _autoStartMonitoring.CheckedChanged += (_, _) =>
        {
            if (!_loadingAutomationControls)
            {
                _settings.AutoStartMonitoring = _autoStartMonitoring.Checked;
                _settings.Save();
            }
        };
    }

    private void ConfigureControlStyles()
    {
        UiTheme.StyleButton(_scanDevicesButton, UiIconKind.Search);
        UiTheme.StyleButton(_startStopButton, UiIconKind.Play, UiButtonKind.Primary);
        UiTheme.StyleButton(_singlePacketTestButton, UiIconKind.Send);
        UiTheme.StyleButton(_installPawnIoButton, UiIconKind.Update, UiButtonKind.Primary);
        UiTheme.StyleButton(_checkUpdatesButton, UiIconKind.Update);
        UiTheme.StyleInput(_deviceCombo);
        UiTheme.StyleInput(_sensorCombo);
        UiTheme.StyleInput(_smoothing);
        UiTheme.StyleCheckBox(_startWithWindows);
        UiTheme.StyleCheckBox(_autoStartMonitoring);
        _profileSourceLabel.ForeColor = UiTheme.MutedText;
        _profileSourceLabel.Margin = new Padding(6, 9, 0, 0);
    }

    private static TableLayoutPanel CreateCard(int rowCount)
    {
        var card = new TableLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Dock = DockStyle.Fill,
            BackColor = UiTheme.CardBackground,
            BorderStyle = BorderStyle.FixedSingle,
            Padding = new Padding(18),
            ColumnCount = 1,
            RowCount = rowCount,
            Margin = new Padding(0, 0, 0, 14),
            GrowStyle = TableLayoutPanelGrowStyle.FixedSize
        };
        card.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (int row = 0; row < rowCount; row++)
        {
            card.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        }
        return card;
    }

    private static TableLayoutPanel CreateSectionHeading(string title, string subtitle, UiIconKind icon)
    {
        var heading = new TableLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0, 0, 0, 12)
        };
        heading.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 36));
        heading.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        heading.Controls.Add(new PictureBox
        {
            Image = UiIconFactory.Get(icon, UiTheme.Primary),
            SizeMode = PictureBoxSizeMode.CenterImage,
            Size = new Size(28, 28),
            BackColor = Color.FromArgb(229, 239, 255),
            Margin = new Padding(0, 2, 8, 0)
        }, 0, 0);
        var text = new TableLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Margin = Padding.Empty
        };
        text.Controls.Add(new Label
        {
            Text = title,
            AutoSize = true,
            Font = new Font("Segoe UI", 12F, FontStyle.Bold),
            ForeColor = UiTheme.Text,
            Margin = Padding.Empty
        }, 0, 0);
        text.Controls.Add(new Label
        {
            Text = subtitle,
            AutoSize = true,
            ForeColor = UiTheme.MutedText,
            Margin = new Padding(0, 1, 0, 0)
        }, 0, 1);
        heading.Controls.Add(text, 1, 0);
        return heading;
    }

    private static FlowLayoutPanel CreateControlRow(int bottomMargin = 8) => new()
    {
        AutoSize = true,
        AutoSizeMode = AutoSizeMode.GrowAndShrink,
        Dock = DockStyle.Fill,
        WrapContents = true,
        Margin = new Padding(0, 0, 0, bottomMargin)
    };

    private static Label NewControlLabel(string text) => new()
    {
        Text = text,
        AutoSize = true,
        Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
        ForeColor = UiTheme.Text,
        Margin = new Padding(0, 9, 8, 0)
    };

    private static TableLayoutPanel CreateMetricCard(string caption, Control value)
    {
        var card = new TableLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            BackColor = UiTheme.SoftBackground,
            Padding = new Padding(12, 10, 12, 10),
            ColumnCount = 1,
            RowCount = 2,
            Margin = new Padding(6)
        };
        card.Controls.Add(UiTheme.NewCaption(caption.ToUpperInvariant()), 0, 0);
        value.Margin = Padding.Empty;
        value.ForeColor = UiTheme.Text;
        card.Controls.Add(value, 0, 1);
        return card;
    }

    private async Task CheckForUpdatesAsync()
    {
        if (_updateBusy)
        {
            return;
        }

        _updateBusy = true;
        _checkUpdatesButton.Enabled = false;
        _updateProgress.Visible = false;

        try
        {
            if (!_updateService.IsInstalled)
            {
                _updateStatusLabel.Text = "Atualização integrada disponível após instalar com o Setup do RM Aura Ice Display";
                MessageBox.Show(
                    "Esta cópia é portátil ou de desenvolvimento. A atualização pelo próprio aplicativo fica disponível depois que o RM Aura Ice Display é instalado pelo Setup oficial.",
                    "Atualizações",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            VelopackAsset? pendingUpdate = _updateService.PendingUpdate;
            if (pendingUpdate is not null)
            {
                string pendingVersion = pendingUpdate.Version.ToString();
                DialogResult applyPending = MessageBox.Show(
                    $"A versão {pendingVersion} já foi baixada.\n\n" +
                    "O monitoramento será interrompido, o USB será desconectado e o RM Aura Ice Display reiniciará automaticamente. Deseja aplicar agora?",
                    "Atualização pronta",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button1);
                if (applyPending == DialogResult.Yes)
                {
                    PrepareForUpdate();
                    _updateService.ApplyUpdatesAndRestart(pendingUpdate);
                }
                else
                {
                    _updateStatusLabel.Text = $"Versão {pendingVersion} baixada e aguardando instalação";
                }
                return;
            }

            _updateStatusLabel.Text = "Procurando nova versão...";
            UpdateInfo? update = await _updateService.CheckForUpdatesAsync();
            if (update is null)
            {
                _updateStatusLabel.Text = $"RM Aura Ice Display {_updateService.CurrentVersion} está atualizado";
                return;
            }

            string targetVersion = update.TargetFullRelease.Version.ToString();
            DialogResult download = MessageBox.Show(
                $"A versão {targetVersion} está disponível. Deseja baixar agora?\n\n" +
                "O monitoramento continuará funcionando durante o download.",
                "Atualização disponível",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Information,
                MessageBoxDefaultButton.Button1);
            if (download != DialogResult.Yes)
            {
                _updateStatusLabel.Text = $"Versão {targetVersion} disponível";
                return;
            }

            _updateProgress.Value = 0;
            _updateProgress.Visible = true;
            _updateStatusLabel.Text = $"Baixando versão {targetVersion}...";
            IProgress<int> progress = new Progress<int>(value =>
            {
                _updateProgress.Value = Math.Clamp(value, 0, 100);
                _updateStatusLabel.Text = $"Baixando versão {targetVersion}: {_updateProgress.Value}%";
            });
            await _updateService.DownloadUpdatesAsync(update, progress.Report);

            _updateStatusLabel.Text = $"Versão {targetVersion} pronta para instalar";
            DialogResult apply = MessageBox.Show(
                $"A versão {targetVersion} foi baixada e está pronta.\n\n" +
                "O monitoramento será interrompido, o USB será desconectado e o RM Aura Ice Display reiniciará automaticamente. Deseja atualizar agora?",
                "Aplicar atualização",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button1);
            if (apply != DialogResult.Yes)
            {
                _updateStatusLabel.Text = $"Versão {targetVersion} baixada e aguardando instalação";
                return;
            }

            PrepareForUpdate();
            _updateService.ApplyUpdatesAndRestart(update);
        }
        catch (Exception ex)
        {
            _updateStatusLabel.Text = "Não foi possível atualizar";
            MessageBox.Show(
                $"A atualização não pôde ser concluída: {ex.Message}",
                "Falha na atualização",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            _updateBusy = false;
            _checkUpdatesButton.Enabled = true;
            _updateProgress.Visible = false;
        }
    }

    private void PrepareForUpdate()
    {
        StopMonitoring();
        DisableUsbWritesAndDisconnect();
        CaptureWindowPlacement(saveImmediately: false);
        SaveCurrentSettings();
        _exitRequested = true;
    }

    private void ConfigureMenu()
    {
        UiTheme.StyleMenu(_mainMenu);
        var helpMenu = new ToolStripMenuItem("&Ajuda")
        {
            Image = UiIconFactory.Get(UiIconKind.Help, UiTheme.Primary)
        };
        var manualItem = new ToolStripMenuItem("&Manual do usuário")
        {
            ShortcutKeys = Keys.F1,
            ShowShortcutKeys = true,
            Image = UiIconFactory.Get(UiIconKind.Help, UiTheme.Primary)
        };
        manualItem.Click += (_, _) => ShowUserGuide();

        var aboutItem = new ToolStripMenuItem("&Sobre o RM Aura Ice Display")
        {
            Image = UiIconFactory.Get(UiIconKind.Info, UiTheme.Accent)
        };
        aboutItem.Click += (_, _) => MessageBox.Show(
            $"RM Aura Ice Display {Application.ProductVersion}\n\n" +
            "Monitor local para o visor Rise Mode Aura Ice.\n" +
            "Perfil confirmado: HID AA88:8666, relatório de saída com 11 bytes.",
            "Sobre o RM Aura Ice Display",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);

        helpMenu.DropDownItems.Add(manualItem);
        helpMenu.DropDownItems.Add(new ToolStripSeparator());
        helpMenu.DropDownItems.Add(aboutItem);
        _mainMenu.Items.Add(helpMenu);
    }

    private void ShowUserGuide()
    {
        if (_helpForm is null || _helpForm.IsDisposed)
        {
            _helpForm = new HelpForm();
            _helpForm.FormClosed += (_, _) => _helpForm = null;
            _helpForm.Show(this);
            return;
        }

        if (_helpForm.WindowState == FormWindowState.Minimized)
        {
            _helpForm.WindowState = FormWindowState.Normal;
        }
        _helpForm.Activate();
        _helpForm.BringToFront();
    }

    private void ConfigureLists()
    {
        UiTheme.StyleListView(_sensorList);
        UiTheme.StyleListView(_deviceList);
        _sensorList.Columns.Add("Sensor", 220);
        _sensorList.Columns.Add("Identificador", 500);
        _sensorList.Columns.Add("Valor atual", 130);

        _deviceList.Columns.Add("Confiança", 100);
        _deviceList.Columns.Add("Pontos", 70);
        _deviceList.Columns.Add("Perfil", 180);
        _deviceList.Columns.Add("VID:PID", 100);
        _deviceList.Columns.Add("Produto", 180);
        _deviceList.Columns.Add("Fabricante", 150);
        _deviceList.Columns.Add("Série", 150);
        _deviceList.Columns.Add("Saída", 80);
        _deviceList.Columns.Add("Entrada", 80);
        _deviceList.Columns.Add("Feature", 80);
        _deviceList.Columns.Add("Usage", 130);
        _deviceList.Columns.Add("Diagnóstico", 320);
        _deviceList.Columns.Add("Caminho atual (não é salvo)", 430);
    }

    private void LoadSettingsIntoControls()
    {
        _sensorCombo.Items.AddRange(["Core Average", "CPU Package", "Core Max"]);
        _sensorCombo.SelectedItem = _settings.CpuSensorName;
        if (_sensorCombo.SelectedIndex < 0)
        {
            _sensorCombo.SelectedIndex = 0;
        }

        _smoothing.Value = (decimal)Math.Clamp(_settings.SmoothingSeconds, 0, 20);

        _loadingAutomationControls = true;
        try
        {
            bool startupRegistered = WindowsStartupManager.IsEnabled();
            _settings.StartWithWindows = startupRegistered;
            _startWithWindows.Checked = _settings.StartWithWindows;
            _autoStartMonitoring.Checked = _settings.AutoStartMonitoring;
        }
        finally
        {
            _loadingAutomationControls = false;
        }
    }

    private void OnInitialShown()
    {
        if (_startedWithWindows)
        {
            HideToTray();
            Opacity = 1;
        }

        if (_autoStartMonitoring.Checked && !IsRunning)
        {
            BeginInvoke(() => StartMonitoring(showErrors: !_startedWithWindows));
        }
    }

    private void OnStartWithWindowsChanged()
    {
        if (_loadingAutomationControls)
        {
            return;
        }

        bool requested = _startWithWindows.Checked;
        try
        {
            WindowsStartupManager.SetEnabled(requested);
            _settings.StartWithWindows = requested;
            _settings.Save();
        }
        catch (Exception ex)
        {
            _loadingAutomationControls = true;
            try
            {
                _startWithWindows.Checked = !requested;
            }
            finally
            {
                _loadingAutomationControls = false;
            }

            ShowError(ex);
        }
    }

    private void ScanDevices(bool showErrors)
    {
        try
        {
            _scanDevicesButton.Enabled = false;
            _usbLabel.Text = "Procurando...";
            HidScanResult result = _deviceDetector.Scan();
            PopulateDeviceControls(result);
        }
        catch (Exception ex)
        {
            _usbLabel.Text = $"Erro na detecção: {ex.Message}";
            if (showErrors)
            {
                ShowError(ex);
            }
        }
        finally
        {
            _scanDevicesButton.Enabled = !IsRunning;
        }
    }

    private void PopulateDeviceControls(HidScanResult result)
    {
        _updatingDeviceControls = true;
        try
        {
            string? currentPath = GetSelectedDevice()?.RuntimePath;
            HidDeviceCandidate[] safe = result.SafeCandidates.ToArray();
            HidDeviceCandidate[] choices = result.Candidates
                .Where(candidate => candidate.Profile is not null || candidate.HasOutputReport)
                .ToArray();

            _deviceCombo.BeginUpdate();
            _deviceCombo.Items.Clear();
            foreach (HidDeviceCandidate candidate in choices)
            {
                _deviceCombo.Items.Add(candidate);
            }

            HidDeviceCandidate? currentSelection = choices.FirstOrDefault(candidate =>
                !string.IsNullOrWhiteSpace(currentPath) &&
                string.Equals(candidate.RuntimePath, currentPath, StringComparison.OrdinalIgnoreCase));

            HidDeviceCandidate[] persistedMatches = choices.Where(candidate =>
                !string.IsNullOrWhiteSpace(_settings.SelectedDeviceIdentity) &&
                string.Equals(candidate.PersistentIdentity, _settings.SelectedDeviceIdentity, StringComparison.Ordinal)).ToArray();

            HidDeviceCandidate? selection = currentSelection
                ?? (persistedMatches.Length == 1 ? persistedMatches[0] : null)
                ?? (safe.Length == 1 ? safe[0] : null);

            _deviceCombo.SelectedItem = selection;
            _deviceCombo.EndUpdate();
            lock (_usbWriteGate)
            {
                _selectedDevice = selection;
            }

            UpdateDeviceList(result.AllDevices);
            _profileSourceLabel.Text = $"Perfis: {result.ProfileSource}";
        }
        finally
        {
            _updatingDeviceControls = false;
        }

        RefreshSelectedDeviceStatus();
    }

    private void UpdateDeviceList(IReadOnlyList<HidDeviceCandidate> devices)
    {
        _deviceList.BeginUpdate();
        _deviceList.Items.Clear();

        foreach (HidDeviceCandidate device in devices)
        {
            var item = new ListViewItem(ConfidenceText(device.Confidence));
            item.SubItems.Add(device.Score.ToString());
            item.SubItems.Add(device.Profile?.Name ?? "—");
            item.SubItems.Add($"{device.VendorId:X4}:{device.ProductId:X4}");
            item.SubItems.Add(device.ProductName ?? "—");
            item.SubItems.Add(device.Manufacturer ?? "—");
            item.SubItems.Add(device.SerialNumber ?? "—");
            item.SubItems.Add(device.OutputReportLength > 0 ? $"{device.OutputReportLength} B" : "—");
            item.SubItems.Add(device.InputReportLength > 0 ? $"{device.InputReportLength} B" : "—");
            item.SubItems.Add(device.FeatureReportLength > 0 ? $"{device.FeatureReportLength} B" : "—");
            item.SubItems.Add($"{device.UsagePage ?? "—"} / {device.Usage ?? "—"}");
            item.SubItems.Add(device.MatchDetails);
            item.SubItems.Add(device.RuntimePath);
            _deviceList.Items.Add(item);
        }

        _deviceList.EndUpdate();
    }

    private void OnSelectedDeviceChanged()
    {
        if (_updatingDeviceControls)
        {
            return;
        }

        HidDeviceCandidate? selectedDevice = _deviceCombo.SelectedItem as HidDeviceCandidate;
        bool writesWereEnabled;
        lock (_usbWriteGate)
        {
            writesWereEnabled = _usbWriteSession.WritesEnabled;
            _usbWriteSession.Disable();
            _selectedDevice = selectedDevice;
            _settings.SelectedDeviceIdentity = selectedDevice?.PersistentIdentity;
            _hidTransport.Disconnect();
        }

        if (writesWereEnabled)
        {
            if (IsRunning)
            {
                StopMonitoring();
            }
            else
            {
                SetDeviceSelectionEnabled(true);
            }

            MessageBox.Show(
                "A seleção do visor mudou. O monitoramento e o envio foram interrompidos.",
                "Envio USB bloqueado",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }

        RefreshSelectedDeviceStatus();
        RefreshSinglePacketTestButton();
    }

    private void RefreshSelectedDeviceStatus()
    {
        HidDeviceCandidate? selectedDevice = GetSelectedDevice();
        HidDeviceCandidate[] safe = _deviceCombo.Items
            .Cast<HidDeviceCandidate>()
            .Where(candidate => candidate.IsSafeForAutomaticUse)
            .ToArray();

        if (selectedDevice is not null)
        {
            _usbLabel.Text = selectedDevice.IsSafeForAutomaticUse
                ? $"{ConfidenceText(selectedDevice.Confidence)} — {selectedDevice.VendorId:X4}:{selectedDevice.ProductId:X4}, saída {selectedDevice.OutputReportLength} bytes"
                : $"Não reconhecido com segurança — envio bloqueado ({selectedDevice.VendorId:X4}:{selectedDevice.ProductId:X4})";
            return;
        }

        _usbLabel.Text = safe.Length switch
        {
            0 => "Nenhum visor compatível reconhecido",
            1 => "Visor reconhecido; selecione-o",
            _ => $"{safe.Length} visores compatíveis; selecione um"
        };
    }

    private void ToggleMonitoring()
    {
        if (IsRunning)
        {
            StopMonitoring();
            return;
        }

        StartMonitoring();
    }

    private void EnableUsbWritesForSelectedDevice()
    {
        lock (_usbWriteGate)
        {
            HidDeviceCandidate candidate = _selectedDevice
                ?? throw new InvalidOperationException("Nenhum visor confirmado foi selecionado.");

            if (!candidate.IsSafeForAutomaticUse)
            {
                throw new InvalidOperationException(
                    "O visor selecionado não está confirmado para o protocolo AuraIceV1 de 11 bytes.");
            }

            if (OfficialSoftwareMayBeRunning())
            {
                throw new InvalidOperationException(
                    "O software oficial da Rise Mode está aberto. Feche-o antes de iniciar o monitoramento.");
            }

            _hidTransport.Connect(candidate);
            _usbWriteSession.Authorize();
        }

        SetDeviceSelectionEnabled(false);
    }

    private void StartMonitoring(bool showErrors = true)
    {
        try
        {
            if (!PawnIoSupport.IsInstalled())
            {
                _installPawnIoButton.Visible = true;
                throw new InvalidOperationException(
                    "O suporte de sensores PawnIO 2.2 ou superior não está instalado. Use o botão 'Instalar suporte de sensores'.");
            }

            EnableUsbWritesForSelectedDevice();
            var hardwareMonitor = new HardwareMonitorService();
            _hardwareMonitor = hardwareMonitor;
            _monitorCts = new CancellationTokenSource();
            _temperatureFilter.Reset();
            _thermalProtection.Reset();
            _startStopButton.Text = "Parar monitoramento";
            UiTheme.StyleButton(_startStopButton, UiIconKind.Stop, UiButtonKind.Danger);
            _statusLabel.Text = "Iniciando...";
            _monitorTask = Task.Run(() => MonitorLoopAsync(hardwareMonitor, _monitorCts.Token, showErrors));
            RefreshSinglePacketTestButton();
        }
        catch (Exception ex)
        {
            StopMonitoring();
            _statusLabel.Text = $"Monitoramento não iniciado: {ex.Message}";
            if (showErrors)
            {
                ShowError(ex);
            }
        }
    }

    private async Task MonitorLoopAsync(
        HardwareMonitorService hardwareMonitor,
        CancellationToken cancellationToken,
        bool showErrors)
    {
        DateTime lastSample = DateTime.UtcNow;
        DateTime lastLcdUpdate = DateTime.MinValue;
        int consecutiveUnavailableSamples = 0;
        bool lowLevelRecoveryAttempted = false;

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                (string sensorName, double smoothingSeconds, int pollIntervalMs, int lcdUpdateIntervalMs) =
                    GetMonitorSettingsSnapshot();
                DateTime now = DateTime.UtcNow;
                double elapsedSeconds = Math.Max((now - lastSample).TotalSeconds, 0.001);
                lastSample = now;

                HardwareSnapshot snapshot = hardwareMonitor.Read(sensorName);
                if (snapshot.CpuTemperatureReadState == CpuTemperatureReadState.ValuesUnavailable)
                {
                    consecutiveUnavailableSamples++;
                    if (!lowLevelRecoveryAttempted && consecutiveUnavailableSamples >= 8)
                    {
                        lowLevelRecoveryAttempted = true;
                        hardwareMonitor.Reinitialize();
                        await Task.Delay(pollIntervalMs, cancellationToken);
                        continue;
                    }
                }
                else
                {
                    consecutiveUnavailableSamples = 0;
                    lowLevelRecoveryAttempted = false;
                }

                double? raw = snapshot.CpuTemperatureRaw;
                double? smoothed = null;

                if (raw.HasValue)
                {
                    smoothed = _temperatureFilter.Update(raw.Value, elapsedSeconds, smoothingSeconds);
                }

                ThermalProtectionResult protection = _thermalProtection.Evaluate(snapshot, smoothed, now);
                double? displayed = protection.DisplayTemperature;

                AuraIcePacket? packet = null;
                byte[]? sentReport = null;

                bool shouldUpdateLcd = displayed.HasValue &&
                    (now - lastLcdUpdate).TotalMilliseconds >= lcdUpdateIntervalMs;

                if (shouldUpdateLcd)
                {
                    packet = AuraIcePacket.FromSnapshot(snapshot, displayed!.Value);
                    lastLcdUpdate = now;

                    sentReport = SendPacketIfAuthorized(packet);
                }

                BeginInvoke(() => UpdateUi(snapshot, smoothed, protection, packet, sentReport));
                await Task.Delay(pollIntervalMs, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Encerramento normal.
        }
        catch (Exception ex)
        {
            BeginInvoke(() =>
            {
                StopMonitoring();
                _statusLabel.Text = $"Monitoramento interrompido: {ex.Message}";
                if (showErrors)
                {
                    ShowError(ex);
                }
            });
        }
    }

    private void UpdateUi(
        HardwareSnapshot snapshot,
        double? smoothed,
        ThermalProtectionResult protection,
        AuraIcePacket? packet,
        byte[]? sentReport)
    {
        _installPawnIoButton.Visible = !PawnIoSupport.IsInstalled();
        _statusLabel.Text = SensorReadStatus.MonitoringText(snapshot);

        _rawTemperatureLabel.Text = FormatTemperature(snapshot.CpuTemperatureRaw);
        _smoothedTemperatureLabel.Text = FormatTemperature(smoothed);
        _displaySensorLabel.Text = snapshot.SelectedCpuSensor;
        _protectionSensorLabel.Text = protection.ProtectionTemperature.HasValue
            ? $"{protection.ProtectionSensor} ({protection.ProtectionTemperature.Value:F1} °C)"
            : protection.ProtectionSensor;
        _thermalProtectionStateLabel.Text = ProtectionStateText(protection.State);
        _displayTemperatureLabel.Text = protection.DisplayTemperature.HasValue
            ? $"{protection.DisplayTemperature.Value:F1} °C → {Math.Round(protection.DisplayTemperature.Value, MidpointRounding.AwayFromZero):F0} °C"
            : "-- °C";
        _trayIcon.UpdateTemperature(protection.DisplayTemperature);

        if (packet is not null)
        {
            string mode = sentReport is null ? "SIMULAÇÃO" : "ENVIADO";
            string hex = sentReport is not null
                ? string.Join(" ", sentReport.Select(value => value.ToString("X2")))
                : packet.ToHex();
            _packetLabel.Text = $"[{mode}] {packet.ToReadableString()}\n{hex}";
        }

        UpdateSensorList(snapshot.CpuTemperatureSensors);
        RefreshSensorCombo(snapshot.CpuTemperatureSensors);
    }

    private async Task InstallPawnIoAsync()
    {
        if (PawnIoSupport.IsInstalled())
        {
            _installPawnIoButton.Visible = false;
            _statusLabel.Text = $"PawnIO {PawnIoSupport.GetInstalledVersion()} já está instalado";
            return;
        }

        DialogResult confirmation = MessageBox.Show(
            "As temperaturas da CPU exigem o driver PawnIO 2.2.0, fornecido e assinado digitalmente por namazso.eu.\n\n" +
            "O RM Aura Ice Display baixará o instalador oficial do GitHub, verificará o SHA-256 antes de executá-lo e removerá o arquivo temporário ao terminar.\n\n" +
            "Deseja instalar o suporte de sensores agora?",
            "Instalar suporte de sensores",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Information,
            MessageBoxDefaultButton.Button2);
        if (confirmation != DialogResult.Yes)
        {
            return;
        }

        StopMonitoring();
        _installPawnIoButton.Enabled = false;
        _statusLabel.Text = "Baixando e verificando o instalador oficial do PawnIO...";

        try
        {
            PawnIoInstallationResult result = await new PawnIoInstaller().InstallAsync();
            if (!PawnIoSupport.IsInstalled())
            {
                throw new InvalidOperationException(
                    "O instalador terminou, mas o PawnIO 2.2 não foi encontrado no Windows.");
            }

            _installPawnIoButton.Visible = false;
            _statusLabel.Text = $"PawnIO {PawnIoSupport.GetInstalledVersion()} instalado";
            string restartMessage = result.RebootRequired
                ? "A instalação foi concluída e o Windows precisa ser reiniciado antes de monitorar."
                : "A instalação foi concluída. O monitoramento já pode ser iniciado.";
            MessageBox.Show(
                restartMessage,
                "Suporte de sensores instalado",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            _statusLabel.Text = "Não foi possível instalar o suporte de sensores";
            ShowError(ex);
        }
        finally
        {
            _installPawnIoButton.Enabled = true;
        }
    }

    private void UpdateSensorList(IReadOnlyList<SensorReading> sensors)
    {
        _sensorList.BeginUpdate();
        _sensorList.Items.Clear();
        foreach (SensorReading sensor in sensors)
        {
            var item = new ListViewItem(sensor.Name);
            item.SubItems.Add(sensor.Identifier);
            item.SubItems.Add(FormatTemperature(sensor.Value));
            _sensorList.Items.Add(item);
        }
        _sensorList.EndUpdate();
    }

    private void RefreshSensorCombo(IReadOnlyList<SensorReading> sensors)
    {
        string? current = _sensorCombo.SelectedItem as string;
        string[] names = sensors.Select(sensor => sensor.Name).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

        if (names.Length == 0 || names.SequenceEqual(_sensorCombo.Items.Cast<string>(), StringComparer.OrdinalIgnoreCase))
        {
            return;
        }

        _sensorCombo.BeginUpdate();
        _sensorCombo.Items.Clear();
        _sensorCombo.Items.AddRange(names);
        _sensorCombo.SelectedItem = names.FirstOrDefault(name => string.Equals(name, current, StringComparison.OrdinalIgnoreCase))
                                    ?? names.FirstOrDefault(name => string.Equals(name, _settings.CpuSensorName, StringComparison.OrdinalIgnoreCase))
                                    ?? names[0];
        _sensorCombo.EndUpdate();
    }

    private void StopMonitoring()
    {
        CancellationTokenSource? cts = _monitorCts;
        Task? monitorTask = _monitorTask;
        HardwareMonitorService? hardwareMonitor = _hardwareMonitor;
        _monitorCts = null;
        _monitorTask = null;
        _hardwareMonitor = null;
        cts?.Cancel();
        DisableUsbWritesAndDisconnect();
        SetDeviceSelectionEnabled(true);

        bool cleanupDeferred = false;
        if (monitorTask is not null && !monitorTask.IsCompleted)
        {
            try
            {
                if (!monitorTask.Wait(TimeSpan.FromSeconds(2)))
                {
                    cleanupDeferred = true;
                    _ = monitorTask.ContinueWith(
                        _ =>
                        {
                            hardwareMonitor?.Dispose();
                            cts?.Dispose();
                        },
                        CancellationToken.None,
                        TaskContinuationOptions.ExecuteSynchronously,
                        TaskScheduler.Default);
                }
            }
            catch (AggregateException)
            {
                // O laço já encaminha erros para a interface.
            }
        }

        if (!cleanupDeferred)
        {
            cts?.Dispose();
            hardwareMonitor?.Dispose();
        }

        _temperatureFilter.Reset();
        _thermalProtection.Reset();

        _startStopButton.Text = "Iniciar monitoramento";
        UiTheme.StyleButton(_startStopButton, UiIconKind.Play, UiButtonKind.Primary);
        _statusLabel.Text = "Parado";
        _trayIcon.UpdateTemperature(null);
        RefreshSinglePacketTestButton();
    }

    private void SendSingleTestPacket()
    {
        HidDeviceCandidate? candidate = GetSelectedDevice();
        if (!CanSendSingleTestPacket(candidate))
        {
            MessageBox.Show(
                "O teste foi bloqueado porque uma ou mais condições de segurança deixaram de ser atendidas. Nenhum pacote foi enviado.",
                "Teste bloqueado",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            FinishSinglePacketTest();
            return;
        }

        try
        {
            using var hardwareMonitor = new HardwareMonitorService();
            HardwareSnapshot snapshot = hardwareMonitor.Read(_settings.CpuSensorName);
            ThermalProtectionResult protection = new ThermalProtectionController().Evaluate(
                snapshot,
                snapshot.CpuTemperatureRaw,
                DateTime.UtcNow);
            double displayed = protection.DisplayTemperature ?? 0;
            AuraIcePacket packet = AuraIcePacket.FromSnapshot(snapshot, displayed);
            byte[] report = packet.BuildReport();
            AuraIceHidTransport.ValidateReportLength(report.Length, candidate!.OutputReportLength);

            string product = string.IsNullOrWhiteSpace(candidate.ProductName) ? "não informado" : candidate.ProductName;
            DialogResult confirmation = MessageBox.Show(
                $"Enviar exatamente um pacote ao visor?\n\n" +
                $"VID/PID: {candidate.VendorId:X4}:{candidate.ProductId:X4}\n" +
                $"Produto: {product}\n" +
                $"Tamanho: {report.Length} bytes\n" +
                $"Bytes: {packet.ToHex()}\n\n" +
                "Não será iniciado nenhum timer ou envio contínuo.",
                "Confirmar pacote único",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2);

            if (confirmation != DialogResult.Yes)
            {
                _statusLabel.Text = "Teste de pacote cancelado; nenhum dado enviado";
                return;
            }

            EnableUsbWritesForSelectedDevice();
            byte[] sent = SendPacketIfAuthorized(packet)
                ?? throw new InvalidOperationException("A autorização de escrita foi retirada antes do envio.");
            _packetLabel.Text = $"[ENVIADO UMA VEZ] {packet.ToReadableString()}\n{string.Join(" ", sent.Select(value => value.ToString("X2")))}";
            MessageBox.Show(
                "Um único pacote foi enviado com sucesso. O transporte será desconectado em seguida.",
                "Teste concluído",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"O pacote único não pôde ser enviado: {ex.Message}",
                "Falha no teste",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            FinishSinglePacketTest();
        }
    }

    private bool CanSendSingleTestPacket(HidDeviceCandidate? candidate)
    {
        lock (_usbWriteGate)
        {
            return candidate is
            {
                Confidence: DeviceConfidence.Confirmed,
                OutputReportLength: AuraIcePacket.ReportLength
            } &&
                !IsRunning &&
                !OfficialSoftwareMayBeRunning();
        }
    }

    private void RefreshSinglePacketTestButton()
    {
        if (IsDisposed || Disposing)
        {
            return;
        }

        _singlePacketTestButton.Enabled = CanSendSingleTestPacket(GetSelectedDevice());
    }

    private void FinishSinglePacketTest()
    {
        DisableUsbWritesAndDisconnect();
        SetDeviceSelectionEnabled(true);
        RefreshSinglePacketTestButton();
    }

    private static bool OfficialSoftwareMayBeRunning()
    {
        string[] processNames = ["SendTemp", "CPU Server", "Rise Mode Temp CPU Driver R2.2"];
        foreach (string name in processNames)
        {
            Process[] processes = Process.GetProcessesByName(name);
            try
            {
                if (processes.Length > 0)
                {
                    return true;
                }
            }
            finally
            {
                foreach (Process process in processes)
                {
                    process.Dispose();
                }
            }
        }

        return false;
    }

    private void HideToTray()
    {
        CaptureWindowPlacement(saveImmediately: true);
        ShowInTaskbar = false;
        Hide();
    }

    private void ShowPanel()
    {
        Opacity = 1;
        ShowInTaskbar = true;
        Show();
        WindowState = _lastVisibleWindowState;
        Activate();
        BringToFront();
    }

    private void ApplySavedWindowPlacement()
    {
        Rectangle[] workingAreas = Screen.AllScreens.Select(screen => screen.WorkingArea).ToArray();
        Rectangle primaryWorkingArea = Screen.PrimaryScreen?.WorkingArea
            ?? workingAreas.FirstOrDefault(new Rectangle(0, 0, 1280, 720));

        if (_settings.HasWindowPlacement)
        {
            var savedBounds = new Rectangle(
                _settings.WindowX!.Value,
                _settings.WindowY!.Value,
                _settings.WindowWidth!.Value,
                _settings.WindowHeight!.Value);
            Bounds = WindowPlacement.RestoreVisibleBounds(
                savedBounds,
                workingAreas,
                primaryWorkingArea,
                MinimumSize);
            _lastVisibleWindowState = _settings.WindowMaximized
                ? FormWindowState.Maximized
                : FormWindowState.Normal;
            WindowState = _lastVisibleWindowState;
            return;
        }

        Bounds = WindowPlacement.CreateInitialBounds(primaryWorkingArea, MinimumSize);
        _lastVisibleWindowState = FormWindowState.Normal;
        WindowState = FormWindowState.Normal;
    }

    private void OnWindowStateChanged()
    {
        if (!_windowPlacementReady || WindowState == FormWindowState.Minimized)
        {
            return;
        }

        if (_lastVisibleWindowState != WindowState)
        {
            _lastVisibleWindowState = WindowState;
            CaptureWindowPlacement(saveImmediately: true);
        }
    }

    private void CaptureWindowPlacement(bool saveImmediately)
    {
        if (!_windowPlacementReady || WindowState == FormWindowState.Minimized)
        {
            return;
        }

        Rectangle bounds = WindowState == FormWindowState.Normal ? Bounds : RestoreBounds;
        lock (_settings)
        {
            _settings.WindowX = bounds.X;
            _settings.WindowY = bounds.Y;
            _settings.WindowWidth = bounds.Width;
            _settings.WindowHeight = bounds.Height;
            _settings.WindowMaximized = WindowState == FormWindowState.Maximized;

            if (saveImmediately)
            {
                _settings.Save();
            }
        }
    }

    private void ExitApplication()
    {
        _exitRequested = true;
        Close();
    }

    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        if (TrayWindowBehavior.ShouldHideInsteadOfExit(_exitRequested, e.CloseReason))
        {
            e.Cancel = true;
            HideToTray();
            return;
        }

        CaptureWindowPlacement(saveImmediately: false);
        StopMonitoring();
        _safetyTimer.Stop();
        _safetyTimer.Dispose();
        SaveCurrentSettings();
        _hidTransport.Dispose();
        _trayIcon.Dispose();
    }

    private void SaveCurrentSettings()
    {
        lock (_settings)
        {
            _settings.SmoothingSeconds = (double)_smoothing.Value;
            _settings.StartWithWindows = _startWithWindows.Checked;
            _settings.AutoStartMonitoring = _autoStartMonitoring.Checked;
            _settings.SelectedDeviceIdentity = GetSelectedDevice()?.PersistentIdentity;
            if (_sensorCombo.SelectedItem is string selected)
            {
                _settings.CpuSensorName = selected;
            }
            _settings.Save();
        }
    }

    private HidDeviceCandidate? GetSelectedDevice()
    {
        lock (_usbWriteGate)
        {
            return _selectedDevice;
        }
    }

    private byte[]? SendPacketIfAuthorized(AuraIcePacket packet)
    {
        lock (_usbWriteGate)
        {
            if (!_usbWriteSession.WritesEnabled)
            {
                return null;
            }

            HidDeviceCandidate candidate = _selectedDevice
                ?? throw new InvalidOperationException("O visor selecionado não está mais disponível.");

            if (!candidate.IsSafeForAutomaticUse)
            {
                _usbWriteSession.Disable();
                throw new InvalidOperationException(
                    "O visor selecionado deixou de ser reconhecido com segurança. Nenhum dado foi enviado.");
            }

            if (OfficialSoftwareMayBeRunning())
            {
                _usbWriteSession.Disable();
                throw new InvalidOperationException(
                    "O software oficial da Rise Mode foi detectado. O envio USB foi interrompido antes do próximo pacote.");
            }

            return _hidTransport.Send(packet, candidate);
        }
    }

    private void DisableUsbWritesAndDisconnect()
    {
        lock (_usbWriteGate)
        {
            _usbWriteSession.Disable();
            _hidTransport.Disconnect();
        }
    }

    private void SetDeviceSelectionEnabled(bool enabled)
    {
        _deviceCombo.Enabled = enabled;
        _scanDevicesButton.Enabled = enabled;
    }

    private (string SensorName, double SmoothingSeconds, int PollIntervalMs, int LcdUpdateIntervalMs)
        GetMonitorSettingsSnapshot()
    {
        lock (_settings)
        {
            return (
                _settings.CpuSensorName,
                _settings.SmoothingSeconds,
                _settings.PollIntervalMs,
                _settings.LcdUpdateIntervalMs);
        }
    }

    private static string ConfidenceText(DeviceConfidence confidence) => confidence switch
    {
        DeviceConfidence.Confirmed => "Confirmado",
        DeviceConfidence.Recognized => "Reconhecido",
        DeviceConfidence.Possible => "Possível",
        _ => "Desconhecido"
    };

    private static string FormatTemperature(float? value) => value.HasValue ? $"{value.Value:F1} °C" : "-- °C";

    private static string FormatTemperature(double? value) => value.HasValue ? $"{value.Value:F1} °C" : "-- °C";

    private static string ProtectionStateText(ThermalProtectionState state) => state switch
    {
        ThermalProtectionState.Active => "ATIVA — valor imediato",
        ThermalProtectionState.CoolingDown => "ATIVA — aguardando 5 s abaixo de 75 °C",
        _ => "Normal — suavização ativa"
    };

    private static Label NewValueLabel(string text) => new()
    {
        Text = text,
        AutoSize = true,
        Font = new Font("Segoe UI", 10, FontStyle.Bold),
        Margin = new Padding(6)
    };

    private static void ShowError(Exception ex)
    {
        MessageBox.Show(ex.Message, "Erro no RM Aura Ice Display", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _appIconBitmap.Dispose();
        }
        base.Dispose(disposing);
    }
}
