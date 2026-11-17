using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using MyLifeCamController.Core.DirectShow;

namespace MyLifeCamController.Core.Camera;

/// <summary>A video capture device as seen by DirectShow.</summary>
/// <param name="Name">Friendly name, e.g. "Microsoft LifeCam Studio".</param>
/// <param name="DevicePath">Windows device path; stable across reboots for the same USB port.</param>
/// <param name="MonikerName">DirectShow moniker display name, used to open the device.</param>
public sealed record CameraDeviceInfo(string Name, string DevicePath, string MonikerName)
{
    /// <summary>Key used to store settings for this device.</summary>
    public string SettingsKey => string.IsNullOrEmpty(DevicePath) ? MonikerName : DevicePath;

    public override string ToString() => Name;

    /// <summary>Lists the video capture devices currently connected.</summary>
    public static IReadOnlyList<CameraDeviceInfo> ListConnected()
    {
        var result = new List<CameraDeviceInfo>();
        var type = Type.GetTypeFromCLSID(DsGuids.CLSID_SystemDeviceEnum, throwOnError: true)!;
        var devEnum = (ICreateDevEnum)Activator.CreateInstance(type)!;
        IEnumMoniker? enumMoniker = null;
        try
        {
            var category = DsGuids.CLSID_VideoInputDeviceCategory;
            int hr = devEnum.CreateClassEnumerator(ref category, out enumMoniker, 0);
            if (hr != 0 || enumMoniker == null)
            {
                return result; // S_FALSE: no devices in this category.
            }

            var monikers = new IMoniker[1];
            while (enumMoniker.Next(1, monikers, IntPtr.Zero) == 0)
            {
                var moniker = monikers[0];
                try
                {
                    result.Add(Describe(moniker));
                }
                finally
                {
                    ComHelper.Release(moniker);
                }
            }
        }
        finally
        {
            ComHelper.Release(enumMoniker);
            ComHelper.Release(devEnum);
        }

        return result;
    }

    private static CameraDeviceInfo Describe(IMoniker moniker)
    {
        NativeMethods.CreateBindCtx(0, out var ctx);
        try
        {
            moniker.GetDisplayName(ctx, null, out string displayName);

            var bagId = DsGuids.IID_IPropertyBag;
            moniker.BindToStorage(ctx, null, ref bagId, out object bagObject);
            var bag = (IPropertyBag)bagObject;
            try
            {
                string name = ReadString(bag, "FriendlyName") ?? "Câmara desconhecida";
                string path = ReadString(bag, "DevicePath") ?? string.Empty;
                return new CameraDeviceInfo(name, path, displayName);
            }
            finally
            {
                ComHelper.Release(bag);
            }
        }
        finally
        {
            ComHelper.Release(ctx);
        }
    }

    private static string? ReadString(IPropertyBag bag, string name)
    {
        return bag.Read(name, out object? value, IntPtr.Zero) == 0 ? value as string : null;
    }

    /// <summary>Opens the device and returns its DirectShow source filter (caller releases it).</summary>
    internal object CreateFilter()
    {
        Marshal.ThrowExceptionForHR(NativeMethods.CreateBindCtx(0, out var ctx));
        IMoniker? moniker = null;
        try
        {
            Marshal.ThrowExceptionForHR(NativeMethods.MkParseDisplayName(ctx, MonikerName, out _, out moniker));
            var filterId = DsGuids.IID_IBaseFilter;
            moniker.BindToObject(ctx, null, ref filterId, out object filter);
            return filter;
        }
        finally
        {
            ComHelper.Release(moniker);
            ComHelper.Release(ctx);
        }
    }
}
