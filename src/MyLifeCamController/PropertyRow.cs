using MyLifeCamController.Core.Camera;

namespace MyLifeCamController;

/// <summary>One line of the panel: name, slider, value and an "Auto" checkbox.</summary>
internal sealed class PropertyRow : UserControl
{
    private readonly TrackBar _slider;
    private readonly Label _value;
    private readonly CheckBox _auto;
    private bool _updating;
    private bool _dragging;

    public CameraProperty Property { get; }

    /// <summary>Raised only for changes made by the user, never by <see cref="ShowValue"/>.</summary>
    public event EventHandler<PropertyValue>? UserChanged;

    /// <summary>True while the user is holding the slider, so live refreshes don't fight them.</summary>
    public bool IsInteracting => _dragging;

    public bool IsAuto => _auto.Checked;

    public PropertyRow(CameraProperty property)
    {
        Property = property;
        Height = 40;
        Margin = new Padding(0);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 1,
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70));

        var name = new Label
        {
            Text = property.Definition.DisplayName,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true,
        };

        _slider = new TrackBar
        {
            Dock = DockStyle.Fill,
            Minimum = property.Min,
            Maximum = property.Max,
            SmallChange = property.Step,
            LargeChange = Math.Max(property.Step, (property.Max - property.Min) / 10),
            TickStyle = TickStyle.None,
            AutoSize = false,
            Height = 30,
        };
        _slider.ValueChanged += (_, _) => OnSliderChanged();
        _slider.MouseDown += (_, _) => _dragging = true;
        _slider.MouseUp += (_, _) => _dragging = false;

        _value = new Label
        {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleRight,
        };

        _auto = new CheckBox
        {
            Text = "Auto",
            Dock = DockStyle.Fill,
            Enabled = property.SupportsAuto,
            Visible = property.SupportsAuto,
            AutoSize = false,
            Margin = new Padding(10, 0, 0, 0),
        };
        _auto.CheckedChanged += (_, _) => OnAutoChanged();

        layout.Controls.Add(name, 0, 0);
        layout.Controls.Add(_slider, 1, 0);
        layout.Controls.Add(_value, 2, 0);
        layout.Controls.Add(_auto, 3, 0);
        Controls.Add(layout);
    }

    /// <summary>Displays a value read from the camera without raising <see cref="UserChanged"/>.</summary>
    public void ShowValue(PropertyValue value)
    {
        _updating = true;
        try
        {
            _slider.Value = Math.Clamp(value.Value, _slider.Minimum, _slider.Maximum);
            _auto.Checked = value.Auto && Property.SupportsAuto;
            UpdateVisuals();
        }
        finally
        {
            _updating = false;
        }
    }

    private void OnSliderChanged()
    {
        int snapped = PropertyMath.Snap(_slider.Value, Property.Min, Property.Max, Property.Step);
        if (snapped != _slider.Value)
        {
            _slider.Value = snapped; // re-enters with the snapped value
            return;
        }

        UpdateVisuals();
        if (!_updating)
        {
            UserChanged?.Invoke(this, new PropertyValue(_slider.Value, false));
        }
    }

    private void OnAutoChanged()
    {
        UpdateVisuals();
        if (!_updating)
        {
            UserChanged?.Invoke(this, new PropertyValue(_slider.Value, _auto.Checked));
        }
    }

    private void UpdateVisuals()
    {
        _value.Text = PropertyMath.Format(Property.Definition, _slider.Value);
        // In auto mode the slider only shows what the camera is doing.
        _slider.Enabled = !_auto.Checked;
        _value.ForeColor = _auto.Checked ? SystemColors.GrayText : SystemColors.ControlText;
    }
}
