using MyLifeCamController.Core.Camera;
using MyLifeCamController.Core.Settings;

namespace MyLifeCamController;

internal sealed class MainForm : Form
{
    private readonly ComboBox _devices = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 320 };
    private readonly Button _refresh = new() { Text = "Atualizar lista", AutoSize = true };
    private readonly Panel _previewHost = new() { Dock = DockStyle.Fill, BackColor = Color.Black };
    private readonly Label _previewStatus = new()
    {
        Dock = DockStyle.Fill,
        ForeColor = Color.Gainsboro,
        TextAlign = ContentAlignment.MiddleCenter,
        Font = new Font(SystemFonts.MessageBoxFont!.FontFamily, 11f),
    };
    private readonly CheckBox _previewToggle = new() { Text = "Pré-visualização", AutoSize = true, Checked = true };
    private readonly FlowLayoutPanel _rowsPanel = new()
    {
        Dock = DockStyle.Fill,
        FlowDirection = FlowDirection.TopDown,
        WrapContents = false,
        AutoScroll = true,
        Padding = new Padding(8, 4, 8, 4),
    };
    private readonly Button _reset = new() { Text = "Repor predefinições", AutoSize = true };
    private readonly Button _driverDialog = new() { Text = "Definições do driver…", AutoSize = true };
    private readonly CheckBox _startup = new() { Text = "Aplicar as minhas definições ao iniciar o Windows", AutoSize = true };
    private readonly Label _status = new() { AutoSize = true, ForeColor = SystemColors.GrayText, Padding = new Padding(0, 6, 0, 0) };

    private readonly System.Windows.Forms.Timer _refreshTimer = new() { Interval = 700 };
    private readonly System.Windows.Forms.Timer _saveTimer = new() { Interval = 800 };
    private readonly List<PropertyRow> _rows = [];

    private AppSettings _settings = SettingsStore.Load(SettingsStore.DefaultPath);
    private CameraSession? _session;
    private PreviewGraph? _preview;

    public MainForm()
    {
        Text = "My LifeCam Controller";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(1180, 640);
        MinimumSize = new Size(900, 480);
        Font = SystemFonts.MessageBoxFont!;

        BuildLayout();

        _refresh.Click += (_, _) => LoadDevices();
        _devices.SelectedIndexChanged += (_, _) => OpenSelectedDevice();
        _previewToggle.CheckedChanged += (_, _) => RestartPreview();
        _previewHost.Resize += (_, _) => _preview?.Resize(_previewHost.ClientSize.Width, _previewHost.ClientSize.Height);
        _rowsPanel.Resize += (_, _) => LayoutRows();
        _reset.Click += (_, _) => ResetToDefaults();
        _driverDialog.Click += (_, _) => ShowDriverDialog();
        _refreshTimer.Tick += (_, _) => RefreshValues();
        _saveTimer.Tick += (_, _) => SaveNow();

        _startup.Checked = SafeIsStartupEnabled();
        _startup.CheckedChanged += (_, _) => ToggleStartup();

        Shown += (_, _) => LoadDevices();
        FormClosing += (_, _) => CloseDevice();
    }

    private void BuildLayout()
    {
        var top = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, Padding = new Padding(8, 8, 8, 0) };
        top.Controls.Add(new Label { Text = "Câmara:", AutoSize = true, Padding = new Padding(0, 6, 0, 0) });
        top.Controls.Add(_devices);
        top.Controls.Add(_refresh);
        top.Controls.Add(_previewToggle);
        _previewToggle.Padding = new Padding(12, 4, 0, 0);

        _previewHost.Controls.Add(_previewStatus);
        var previewFrame = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8) };
        previewFrame.Controls.Add(_previewHost);

        var bottom = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, Padding = new Padding(8, 0, 8, 8) };
        bottom.Controls.Add(_reset);
        bottom.Controls.Add(_driverDialog);
        bottom.Controls.Add(_startup);
        bottom.Controls.Add(_status);
        _startup.Padding = new Padding(12, 4, 0, 0);
        _status.Padding = new Padding(12, 6, 0, 0);

        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 3 };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        root.Controls.Add(top, 0, 0);
        root.SetColumnSpan(top, 2);
        root.Controls.Add(previewFrame, 0, 1);
        root.Controls.Add(_rowsPanel, 1, 1);
        root.Controls.Add(bottom, 0, 2);
        root.SetColumnSpan(bottom, 2);
        Controls.Add(root);
    }

    // ---------- Devices ----------

    private void LoadDevices()
    {
        CloseDevice();
        _devices.Items.Clear();

        IReadOnlyList<CameraDeviceInfo> devices;
        try
        {
            devices = CameraDeviceInfo.ListConnected();
        }
        catch (Exception ex)
        {
            ShowStatus($"Erro a listar câmaras: {ex.Message}");
            devices = [];
        }

        foreach (var d in devices)
        {
            _devices.Items.Add(d);
        }

        if (devices.Count == 0)
        {
            _previewStatus.Text = "Nenhuma câmara encontrada.\nLiga a câmara e carrega em \"Atualizar lista\".";
            UpdateButtons();
            return;
        }

        // Last used camera, otherwise the first physical one (virtual cams have no device path).
        var preferred = devices.FirstOrDefault(d => string.Equals(d.SettingsKey, _settings.LastDevice, StringComparison.OrdinalIgnoreCase))
                        ?? devices.FirstOrDefault(d => !string.IsNullOrEmpty(d.DevicePath))
                        ?? devices[0];
        _devices.SelectedItem = preferred; // triggers OpenSelectedDevice
    }

    private void OpenSelectedDevice()
    {
        CloseDevice();
        if (_devices.SelectedItem is not CameraDeviceInfo device)
        {
            return;
        }

        try
        {
            _session = new CameraSession(device);
        }
        catch (Exception ex)
        {
            _previewStatus.Text = $"Não foi possível abrir \"{device.Name}\".\n{ex.Message}";
            UpdateButtons();
            return;
        }

        _settings.LastDevice = device.SettingsKey;
        var saved = _settings.GetOrCreate(device);
        if (saved.Properties.Count > 0)
        {
            int n = SettingsApplier.Apply(SettingsApplier.For(_session), saved);
            ShowStatus($"{n} definição(ões) guardada(s) aplicada(s).");
        }

        BuildRows();
        RefreshValues(force: true);
        RestartPreview();
        _refreshTimer.Start();
        UpdateButtons();
        ScheduleSave();
    }

    private void CloseDevice()
    {
        _refreshTimer.Stop();
        StopPreview();
        _session?.Dispose();
        _session = null;
        foreach (var control in _rowsPanel.Controls.Cast<Control>().ToList())
        {
            control.Dispose(); // also removes it from the panel
        }

        _rows.Clear();
        if (_saveTimer.Enabled)
        {
            SaveNow();
        }
    }

    // ---------- Property rows ----------

    private void BuildRows()
    {
        _rowsPanel.SuspendLayout();
        PropertyGroup? currentGroup = null;
        foreach (var property in _session!.Properties)
        {
            if (property.Definition.Group != currentGroup)
            {
                currentGroup = property.Definition.Group;
                _rowsPanel.Controls.Add(new Label
                {
                    Text = currentGroup == PropertyGroup.Camera ? "Câmara" : "Imagem",
                    Font = new Font(Font, FontStyle.Bold),
                    AutoSize = true,
                    Margin = new Padding(0, _rowsPanel.Controls.Count == 0 ? 4 : 14, 0, 2),
                });
            }

            var row = new PropertyRow(property);
            row.UserChanged += OnRowChanged;
            _rows.Add(row);
            _rowsPanel.Controls.Add(row);
        }

        if (_session.Properties.Count == 0)
        {
            _rowsPanel.Controls.Add(new Label { Text = "Esta câmara não expõe controlos.", AutoSize = true });
        }

        LayoutRows();
        _rowsPanel.ResumeLayout();
    }

    private void LayoutRows()
    {
        int width = _rowsPanel.ClientSize.Width - _rowsPanel.Padding.Horizontal - SystemInformation.VerticalScrollBarWidth;
        foreach (var row in _rows)
        {
            row.Width = Math.Max(300, width);
        }
    }

    private void OnRowChanged(object? sender, PropertyValue value)
    {
        if (_session == null || sender is not PropertyRow row)
        {
            return;
        }

        try
        {
            _session.Set(row.Property, value);
        }
        catch (Exception ex)
        {
            ShowStatus($"{row.Property.Definition.DisplayName}: a câmara recusou o valor ({ex.Message}).");
            return;
        }

        var saved = _settings.GetOrCreate(_session.Device);
        saved.Properties[row.Property.Key] = new PropertySetting { Value = value.Value, Auto = value.Auto };
        ScheduleSave();

        if (row.Property.Definition.Name == "Focus" && !value.Auto)
        {
            ShowStatus("Foco fixo. Fica guardado para a próxima vez.");
        }
    }

    /// <summary>Reads current values from the camera (shows what Auto is doing, or changes by other apps).</summary>
    private void RefreshValues(bool force = false)
    {
        if (_session == null)
        {
            return;
        }

        foreach (var row in _rows)
        {
            if (!force && (row.IsInteracting || row.ContainsFocus && !row.IsAuto))
            {
                continue;
            }

            try
            {
                row.ShowValue(_session.Get(row.Property));
            }
            catch (Exception)
            {
                // Camera unplugged: stop polling and let the user refresh the list.
                _refreshTimer.Stop();
                ShowStatus("Perdi a ligação à câmara. Carrega em \"Atualizar lista\".");
                return;
            }
        }
    }

    private void ResetToDefaults()
    {
        if (_session == null)
        {
            return;
        }

        try
        {
            _session.ResetToDefaults();
        }
        catch (Exception ex)
        {
            ShowStatus($"Erro a repor: {ex.Message}");
        }

        _settings.GetOrCreate(_session.Device).Properties.Clear();
        ScheduleSave();
        RefreshValues(force: true);
        ShowStatus("Predefinições repostas.");
    }

    private void ShowDriverDialog()
    {
        if (_session == null)
        {
            return;
        }

        _refreshTimer.Stop();
        try
        {
            if (!_session.ShowDriverDialog(Handle))
            {
                ShowStatus("Esta câmara não tem janela de definições do driver.");
            }
        }
        finally
        {
            RefreshValues(force: true);
            _refreshTimer.Start();
        }
    }

    // ---------- Preview ----------

    private void RestartPreview()
    {
        StopPreview();
        if (_session == null)
        {
            return;
        }

        if (!_previewToggle.Checked)
        {
            _previewStatus.Text = "Pré-visualização desligada.";
            _previewStatus.Visible = true;
            return;
        }

        try
        {
            _preview = new PreviewGraph(_session, _previewHost.Handle);
            _preview.Resize(_previewHost.ClientSize.Width, _previewHost.ClientSize.Height);
            _previewStatus.Visible = false;
        }
        catch (PreviewUnavailableException ex)
        {
            _previewStatus.Text = ex.CameraBusy
                ? "A câmara está a ser usada por outra app (ex.: browser, Discord).\nOs controlos continuam a funcionar."
                : ex.Message + "\nOs controlos continuam a funcionar.";
            _previewStatus.Visible = true;
        }
        catch (Exception ex)
        {
            _previewStatus.Text = $"Pré-visualização indisponível: {ex.Message}\nOs controlos continuam a funcionar.";
            _previewStatus.Visible = true;
        }
    }

    private void StopPreview()
    {
        _preview?.Dispose();
        _preview = null;
        _previewStatus.Visible = true;
    }

    // ---------- Settings & startup ----------

    private void ScheduleSave()
    {
        _saveTimer.Stop();
        _saveTimer.Start();
    }

    private void SaveNow()
    {
        _saveTimer.Stop();
        try
        {
            SettingsStore.Save(_settings, SettingsStore.DefaultPath);
        }
        catch (Exception ex)
        {
            ShowStatus($"Não consegui guardar as definições: {ex.Message}");
        }
    }

    private static bool SafeIsStartupEnabled()
    {
        try
        {
            return StartupRegistration.IsEnabled();
        }
        catch (Exception)
        {
            return false;
        }
    }

    private void ToggleStartup()
    {
        try
        {
            StartupRegistration.SetEnabled(_startup.Checked);
            ShowStatus(_startup.Checked
                ? "As tuas definições vão ser aplicadas sempre que entrares no Windows."
                : "Já não aplica ao iniciar o Windows.");
        }
        catch (Exception ex)
        {
            ShowStatus($"Erro ao alterar o arranque: {ex.Message}");
        }
    }

    private void UpdateButtons()
    {
        bool open = _session != null;
        _reset.Enabled = open;
        _driverDialog.Enabled = open;
    }

    private void ShowStatus(string text) => _status.Text = text;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _refreshTimer.Dispose();
            _saveTimer.Dispose();
            _session?.Dispose();
        }

        base.Dispose(disposing);
    }
}
