using System.Diagnostics;

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

    private HardwareMonitorService? _hardwareMonitor;
    private CancellationTokenSource? _monitorCts;
    private Task? _monitorTask;
    private HidDeviceCandidate? _selectedDevice;
    private bool _updatingDeviceControls;
    private bool _loadingAutomationControls;
    private bool _exitRequested;
    private bool _windowPlacementReady;
    private FormWindowState _lastVisibleWindowState = FormWindowState.Normal;
    private HelpForm? _helpForm;

    private readonly MenuStrip _mainMenu = new();

    private readonly ComboBox _deviceCombo = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 520, DropDownWidth = 760 };
    private readonly Button _scanDevicesButton = new() { Text = "Procurar visores", AutoSize = true };
    private readonly Label _profileSourceLabel = new() { AutoSize = true };

    private readonly ComboBox _sensorCombo = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 240 };
    private readonly NumericUpDown _smoothing = new() { DecimalPlaces = 1, Minimum = 0, Maximum = 20, Increment = 0.5M, Width = 90 };
    private readonly Button _startStopButton = new() { Text = "Iniciar monitoramento", AutoSize = true };
    private readonly Button _singlePacketTestButton = new() { Text = "Enviar um pacote de teste", AutoSize = true, Enabled = false };
    private readonly CheckBox _startWithWindows = new() { Text = "Iniciar com o Windows", AutoSize = true };
    private readonly CheckBox _autoStartMonitoring = new() { Text = "Monitorar e enviar ao abrir", AutoSize = true };

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
        Text = "AuraIceLocal 0.3 — Rise Mode Aura Ice";
        StartPosition = FormStartPosition.Manual;
        AutoScaleMode = AutoScaleMode.Dpi;
        AutoScroll = true;
        MinimumSize = new Size(900, 650);
        ApplySavedWindowPlacement();
        Font = new Font("Segoe UI", 10);

        if (_startedWithWindows)
        {
            ShowInTaskbar = false;
            WindowState = FormWindowState.Minimized;
            Opacity = 0;
        }

        BuildLayout();
        LoadSettingsIntoControls();
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

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            Padding = new Padding(14),
            ColumnCount = 1,
            RowCount = 6,
            GrowStyle = TableLayoutPanelGrowStyle.FixedSize
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var title = new Label
        {
            Text = "Controle local do LCD — Rise Mode Aura Ice",
            AutoSize = true,
            Font = new Font("Segoe UI", 14, FontStyle.Bold),
            ForeColor = Color.FromArgb(31, 55, 78),
            Margin = new Padding(0, 0, 0, 14)
        };
        root.Controls.Add(title, 0, 0);

        var deviceControls = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Dock = DockStyle.Fill,
            WrapContents = true,
            Margin = new Padding(0, 0, 0, 8)
        };
        deviceControls.Controls.Add(new Label { Text = "Visor LCD:", AutoSize = true, Margin = new Padding(0, 7, 6, 0) });
        deviceControls.Controls.Add(_deviceCombo);
        deviceControls.Controls.Add(_scanDevicesButton);
        deviceControls.Controls.Add(_profileSourceLabel);
        root.Controls.Add(deviceControls, 0, 1);

        var monitorControls = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Dock = DockStyle.Fill,
            WrapContents = true,
            Margin = new Padding(0, 0, 0, 12)
        };
        monitorControls.Controls.Add(new Label { Text = "Sensor da CPU:", AutoSize = true, Margin = new Padding(0, 7, 6, 0) });
        monitorControls.Controls.Add(_sensorCombo);
        monitorControls.Controls.Add(new Label { Text = "Suavização:", AutoSize = true, Margin = new Padding(18, 7, 6, 0) });
        monitorControls.Controls.Add(_smoothing);
        monitorControls.Controls.Add(new Label { Text = "segundos", AutoSize = true, Margin = new Padding(4, 7, 15, 0) });
        monitorControls.Controls.Add(_startStopButton);
        monitorControls.Controls.Add(_singlePacketTestButton);
        root.Controls.Add(monitorControls, 0, 2);

        var automationControls = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Dock = DockStyle.Fill,
            WrapContents = true,
            Margin = new Padding(0, 0, 0, 12)
        };
        automationControls.Controls.Add(new Label
        {
            Text = "Automação:",
            AutoSize = true,
            Font = new Font(Font, FontStyle.Bold),
            Margin = new Padding(0, 4, 10, 0)
        });
        automationControls.Controls.Add(_startWithWindows);
        automationControls.Controls.Add(_autoStartMonitoring);
        automationControls.Controls.Add(new Label
        {
            Text = "(quando marcado, monitora e envia ao visor confirmado automaticamente)",
            AutoSize = true,
            ForeColor = SystemColors.GrayText,
            Margin = new Padding(10, 4, 0, 0)
        });
        root.Controls.Add(automationControls, 0, 3);

        var summary = new TableLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 6,
            CellBorderStyle = TableLayoutPanelCellBorderStyle.Single,
            Margin = new Padding(0, 0, 0, 12)
        };
        summary.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        summary.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        summary.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        summary.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        summary.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        summary.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        summary.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        summary.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        summary.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        summary.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));

        AddSummaryRow(summary, 0, "Estado:", _statusLabel, "LCD USB:", _usbLabel);
        AddSummaryRow(summary, 1, "Sensor de exibição:", _displaySensorLabel, "Temperatura bruta:", _rawTemperatureLabel);
        AddSummaryRow(summary, 2, "Temperatura suavizada:", _smoothedTemperatureLabel, "Temperatura exibida:", _displayTemperatureLabel);
        AddSummaryRow(summary, 3, "Sensor de proteção:", _protectionSensorLabel, "Proteção térmica:", _thermalProtectionStateLabel);
        summary.Controls.Add(new Label { Text = "Pacote:", AutoSize = true, Margin = new Padding(6) }, 0, 4);
        summary.Controls.Add(_packetLabel, 1, 4);
        summary.SetColumnSpan(_packetLabel, 3);

        var warning = new Label
        {
            AutoSize = false,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            BackColor = Color.FromArgb(255, 248, 225),
            ForeColor = Color.FromArgb(92, 67, 0),
            Padding = new Padding(10, 4, 10, 4),
            Text = "Envio automático: ao iniciar o monitoramento, o app conecta ao visor confirmado e envia uma vez por segundo. Antes de cada escrita, revalida o perfil, o relatório HID e se o software oficial está fechado.",
            Margin = new Padding(0, 8, 0, 0)
        };
        summary.Controls.Add(warning, 0, 5);
        summary.SetColumnSpan(warning, 4);
        root.Controls.Add(summary, 0, 4);

        var tabs = new TabControl { Dock = DockStyle.Fill, Padding = new Point(16, 6) };
        tabs.MinimumSize = new Size(0, 330);
        var deviceTab = new TabPage("Dispositivos HID / diagnóstico");
        var sensorTab = new TabPage("Sensores de temperatura da CPU");
        deviceTab.Controls.Add(_deviceList);
        sensorTab.Controls.Add(_sensorList);
        tabs.TabPages.Add(deviceTab);
        tabs.TabPages.Add(sensorTab);
        root.Controls.Add(tabs, 0, 5);

        Controls.Add(root);
        Controls.Add(_mainMenu);
        MainMenuStrip = _mainMenu;

        _scanDevicesButton.Click += (_, _) => ScanDevices(showErrors: true);
        _deviceCombo.SelectedIndexChanged += (_, _) => OnSelectedDeviceChanged();
        _startStopButton.Click += (_, _) => ToggleMonitoring();
        _singlePacketTestButton.Click += (_, _) => SendSingleTestPacket();
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

    private void ConfigureMenu()
    {
        var helpMenu = new ToolStripMenuItem("&Ajuda");
        var manualItem = new ToolStripMenuItem("&Manual do usuário")
        {
            ShortcutKeys = Keys.F1,
            ShowShortcutKeys = true
        };
        manualItem.Click += (_, _) => ShowUserGuide();

        var aboutItem = new ToolStripMenuItem("&Sobre o AuraIceLocal");
        aboutItem.Click += (_, _) => MessageBox.Show(
            $"AuraIceLocal {Application.ProductVersion}\n\n" +
            "Monitor local para o visor Rise Mode Aura Ice.\n" +
            "Perfil confirmado: HID AA88:8666, relatório de saída com 11 bytes.",
            "Sobre o AuraIceLocal",
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
            EnableUsbWritesForSelectedDevice();
            var hardwareMonitor = new HardwareMonitorService();
            _hardwareMonitor = hardwareMonitor;
            _monitorCts = new CancellationTokenSource();
            _temperatureFilter.Reset();
            _thermalProtection.Reset();
            _startStopButton.Text = "Parar monitoramento";
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
        _statusLabel.Text = snapshot.CpuTemperatureRaw.HasValue
            ? $"Monitorando — {snapshot.SelectedCpuSensor}"
            : "Sensor de temperatura não encontrado";

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
        _hidTransport.Dispose();
        _trayIcon.Dispose();
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

    private static void AddSummaryRow(TableLayoutPanel panel, int row, string leftTitle, Control leftValue, string rightTitle, Control rightValue)
    {
        panel.Controls.Add(new Label { Text = leftTitle, AutoSize = true, Margin = new Padding(6) }, 0, row);
        panel.Controls.Add(leftValue, 1, row);
        panel.Controls.Add(new Label { Text = rightTitle, AutoSize = true, Margin = new Padding(6) }, 2, row);
        panel.Controls.Add(rightValue, 3, row);
    }

    private static void ShowError(Exception ex)
    {
        MessageBox.Show(ex.Message, "Erro no AuraIceLocal", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}
