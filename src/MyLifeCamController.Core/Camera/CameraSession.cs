using System.Runtime.InteropServices;
using MyLifeCamController.Core.DirectShow;

namespace MyLifeCamController.Core.Camera;

/// <summary>
/// An open camera. Reads and writes its properties through IAMCameraControl / IAMVideoProcAmp.
/// Works even while another app (Discord, browser...) is streaming from the camera.
/// </summary>
public sealed class CameraSession : IDisposable
{
    private object? _filter;
    private readonly IAMCameraControl? _cameraControl;
    private readonly IAMVideoProcAmp? _videoProcAmp;

    public CameraDeviceInfo Device { get; }

    /// <summary>Properties the camera reports as supported, in catalog order.</summary>
    public IReadOnlyList<CameraProperty> Properties { get; }

    public CameraSession(CameraDeviceInfo device)
    {
        Device = device;
        _filter = device.CreateFilter();
        _cameraControl = _filter as IAMCameraControl;
        _videoProcAmp = _filter as IAMVideoProcAmp;
        Properties = DiscoverProperties();
    }

    /// <summary>The underlying source filter, shared with the preview graph.</summary>
    internal object Filter => _filter ?? throw new ObjectDisposedException(nameof(CameraSession));

    private List<CameraProperty> DiscoverProperties()
    {
        var list = new List<CameraProperty>();
        foreach (var def in PropertyCatalog.All)
        {
            int hr;
            int min, max, step, dflt;
            DsControlFlags caps;
            if (def.Group == PropertyGroup.Camera)
            {
                if (_cameraControl == null) continue;
                hr = _cameraControl.GetRange(def.Id, out min, out max, out step, out dflt, out caps);
            }
            else
            {
                if (_videoProcAmp == null) continue;
                hr = _videoProcAmp.GetRange(def.Id, out min, out max, out step, out dflt, out caps);
            }

            // Some drivers report success with an empty range for unsupported properties.
            if (hr != 0 || max <= min)
            {
                continue;
            }

            bool auto = caps.HasFlag(DsControlFlags.Auto);
            bool manual = caps.HasFlag(DsControlFlags.Manual) || !auto;
            list.Add(new CameraProperty(def, min, max, Math.Max(step, 1), dflt, auto, manual));
        }

        return list;
    }

    public PropertyValue Get(CameraProperty property)
    {
        int value;
        DsControlFlags flags;
        int hr = property.Definition.Group == PropertyGroup.Camera
            ? _cameraControl!.Get(property.Definition.Id, out value, out flags)
            : _videoProcAmp!.Get(property.Definition.Id, out value, out flags);
        Marshal.ThrowExceptionForHR(hr);
        return new PropertyValue(value, flags.HasFlag(DsControlFlags.Auto));
    }

    public void Set(CameraProperty property, PropertyValue value)
    {
        bool auto = value.Auto && property.SupportsAuto;
        int snapped = PropertyMath.Snap(value.Value, property.Min, property.Max, property.Step);
        var flags = auto ? DsControlFlags.Auto : DsControlFlags.Manual;
        int hr = property.Definition.Group == PropertyGroup.Camera
            ? _cameraControl!.Set(property.Definition.Id, snapped, flags)
            : _videoProcAmp!.Set(property.Definition.Id, snapped, flags);
        Marshal.ThrowExceptionForHR(hr);
    }

    /// <summary>Puts every property back to the driver default (automatic where supported).</summary>
    public void ResetToDefaults()
    {
        foreach (var p in Properties)
        {
            Set(p, new PropertyValue(p.Default, p.SupportsAuto));
        }
    }

    /// <summary>Opens the driver's own settings dialog (the "hidden" Windows camera dialog).</summary>
    public bool ShowDriverDialog(IntPtr ownerHandle)
    {
        if (Filter is not ISpecifyPropertyPages pages || pages.GetPages(out CAUUID cauuid) != 0)
        {
            return false;
        }

        try
        {
            object unknown = Filter;
            int hr = NativeMethods.OleCreatePropertyFrame(
                ownerHandle, 0, 0, Device.Name, 1, ref unknown,
                cauuid.cElems, cauuid.pElems, 0, 0, IntPtr.Zero);
            return hr == 0;
        }
        finally
        {
            Marshal.FreeCoTaskMem(cauuid.pElems);
        }
    }

    public void Dispose()
    {
        if (_filter != null)
        {
            Marshal.FinalReleaseComObject(_filter);
            _filter = null;
        }
    }
}
