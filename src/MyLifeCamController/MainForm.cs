using MyLifeCamController.Core;
using MyLifeCamController.Core.Camera;
using MyLifeCamController.Core.Settings;

namespace MyLifeCamController;

internal sealed class MainForm : Form
{
    private readonly ComboBox _devices = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 320 };
    private readonly Button _refresh = new() { Text = Lang.T("Atualizar lista", "Refresh list"), AutoSize = true };
    private readonly Panel _previewHost = new() { Dock = DockStyle.Fill, BackColor = Color.Black };
    private readonly Label _previewStatus = new()
    {
        Dock = DockStyle.Fill,
        ForeColor = Color.Gainsboro,
        TextAlign = ContentAlignment.MiddleCenter,
        Font = new Font(SystemFonts.MessageBoxFont!.FontFamily, 11f),
    };
    private readonly CheckBox _previewToggle = new() { Text = Lang.T("Pré-visualização", "Preview"), AutoSize = true, Checked = true };
    private readonly FlowLayoutPanel _rowsPanel = new()
    {
        Dock = DockStyle.Fill,
        FlowDirection = FlowDirection.TopDown,
        WrapContents = false,
        AutoScroll = true,
        Padding = new Padding(8, 4, 8, 4),
    };
    private readonly Button _reset = new() { Text = Lang.T("Repor predefinições", "Reset to defaults"), AutoSize = true };
    private readonly Button _driverDialog = new() { Text = Lang.T("Definições do driver…", "Driver settings…"), AutoSize = true };
    private readonly CheckBox _startup = new() { Text = Lang.T("Aplicar as minhas definições ao iniciar o Windows", "Apply my settings when Windows starts"), AutoSize = true };
    private readonly ComboBox _language = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 110, Items = { "Português", "English" } };
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

        _language.SelectedIndex = Lang.IsPortuguese ? 0 : 1;
        _language.SelectedIndexChanged += (_, _) => ChangeLanguage();

        Shown += (_, _) => LoadDevices();
        FormClosing += (_, _) => CloseDevice();
    }

    private void BuildLayout()
    {
        var top = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, Padding = new Padding(8, 8, 8, 0) };
        top.Controls.Add(new Label { Text = Lang.T("Câmara:", "Camera:"), AutoSize = true, Padding = new Padding(0, 6, 0, 0) });
        top.Controls.Add(_devices);
        top.Controls.Add(_refresh);
        top.Controls.Add(_previewToggle);
        _previewToggle.Padding = new Padding(12, 4, 0, 0);
        top.Controls.Add(new Label { Text = Lang.T("Idioma:", "Language:"), AutoSize = true, Padding = new Padding(24, 6, 0, 0) });
        top.Controls.Add(_language);

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
            ShowStatus(Lang.T($"Erro a listar câmaras: {ex.Message}", $"Error listing cameras: {ex.Message}"));
            devices = [];
        }

        foreach (var d in devices)
        {
            _devices.Items.Add(d);
        }

        if (devices.Count == 0)
        {
            _previewStatus.Text = Lang.T("Nenhuma câmara encontrada.\nLiga a câmara e carrega em \"Atualizar lista\".", "No camera found.\nConnect the camera and click \"Refresh list\".");
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
            _previewStatus.Text = Lang.T($"Não foi possível abrir \"{device.Name}\".\n{ex.Message}", $"Could not open \"{device.Name}\".\n{ex.Message}");
            UpdateButtons();
            return;
        }

        _settings.LastDevice = device.SettingsKey;
        var saved = _settings.GetOrCreate(device);
        if (saved.Properties.Count > 0)
        {
            int n = SettingsApplier.Apply(SettingsApplier.For(_session), saved);
            ShowStatus(Lang.T($"{n} definição(ões) guardada(s) aplicada(s).", $"{n} saved setting(s) applied."));
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
                    Text = currentGroup == PropertyGroup.Camera ? Lang.T("Câmara", "Camera") : Lang.T("Imagem", "Image"),
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
            _rowsPanel.Controls.Add(new Label { Text = Lang.T("Esta câmara não expõe controlos.", "This camera exposes no controls."), AutoSize = true });
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
            ShowStatus(Lang.T($"{row.Property.Definition.DisplayName}: a câmara recusou o valor ({ex.Message}).", $"{row.Property.Definition.DisplayName}: the camera rejected the value ({ex.Message})."));
            return;
        }

        var saved = _settings.GetOrCreate(_session.Device);
        saved.Properties[row.Property.Key] = new PropertySetting { Value = value.Value, Auto = value.Auto };
        ScheduleSave();

        if (row.Property.Definition.Name == "Focus" && !value.Auto)
        {
            ShowStatus(Lang.T("Foco fixo. Fica guardado para a próxima vez.", "Focus locked. It's saved for next time."));
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
                ShowStatus(Lang.T("Perdi a ligação à câmara. Carrega em \"Atualizar lista\".", "Lost the connection to the camera. Click \"Refresh list\"."));
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
            ShowStatus(Lang.T($"Erro a repor: {ex.Message}", $"Error resetting: {ex.Message}"));
        }

        _settings.GetOrCreate(_session.Device).Properties.Clear();
        ScheduleSave();
        RefreshValues(force: true);
        ShowStatus(Lang.T("Predefinições repostas.", "Defaults restored."));
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
                ShowStatus(Lang.T("Esta câmara não tem janela de definições do driver.", "This camera has no driver settings window."));
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
            _previewStatus.Text = Lang.T("Pré-visualização desligada.", "Preview off.");
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
                ? Lang.T("A câmara está a ser usada por outra app (ex.: browser, Discord).\nOs controlos continuam a funcionar.", "The camera is being used by another app (e.g. a browser or Discord).\nThe controls still work.")
                : ex.Message + Lang.T("\nOs controlos continuam a funcionar.", "\nThe controls still work.");
            _previewStatus.Visible = true;
        }
        catch (Exception ex)
        {
            _previewStatus.Text = Lang.T($"Pré-visualização indisponível: {ex.Message}\nOs controlos continuam a funcionar.", $"Preview unavailable: {ex.Message}\nThe controls still work.");
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

    private void ChangeLanguage()
    {
        bool portuguese = _language.SelectedIndex == 0;
        if (portuguese == Lang.IsPortuguese)
        {
            return;
        }

        _settings.Language = portuguese ? "pt" : "en";
        SaveNow();

        // The whole window is built in the chosen language, so restart to rebuild it.
        Application.Restart();
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
            ShowStatus(Lang.T($"Não consegui guardar as definições: {ex.Message}", $"Couldn't save the settings: {ex.Message}"));
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
                ? Lang.T("As tuas definições vão ser aplicadas sempre que entrares no Windows.", "Your settings will be applied every time you sign in to Windows.")
                : Lang.T("Já não aplica ao iniciar o Windows.", "No longer applied when Windows starts."));
        }
        catch (Exception ex)
        {
            ShowStatus(Lang.T($"Erro ao alterar o arranque: {ex.Message}", $"Error changing the startup setting: {ex.Message}"));
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
