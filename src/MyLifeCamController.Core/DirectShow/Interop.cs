using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace MyLifeCamController.Core.DirectShow;

// Minimal hand-written DirectShow COM interop. Only the methods we call are declared,
// but every interface keeps the vtable order of the native header up to the last one used.

internal static class DsGuids
{
    public static readonly Guid CLSID_SystemDeviceEnum = new("62BE5D10-60EB-11d0-BD3B-00A0C911CE86");
    public static readonly Guid CLSID_VideoInputDeviceCategory = new("860BB310-5D01-11d0-BD3B-00A0C911CE86");
    public static readonly Guid CLSID_FilterGraph = new("E436EBB3-524F-11CE-9F53-0020AF0BA770");
    public static readonly Guid CLSID_CaptureGraphBuilder2 = new("BF87B6E1-8C27-11d0-B3F0-00AA003761C5");
    public static readonly Guid PIN_CATEGORY_PREVIEW = new("fb6c4282-0353-11d1-905f-0000c0cc16ba");
    public static readonly Guid PIN_CATEGORY_CAPTURE = new("fb6c4281-0353-11d1-905f-0000c0cc16ba");
    public static readonly Guid MEDIATYPE_Video = new("73646976-0000-0010-8000-00AA00389B71");
    public static readonly Guid IID_IBaseFilter = new("56a86895-0ad4-11ce-b03a-0020af0ba770");
    public static readonly Guid IID_IPropertyBag = new("55272A00-42CB-11CE-8135-00AA004BB851");
}

/// <summary>Flags used by IAMCameraControl and IAMVideoProcAmp.</summary>
[Flags]
internal enum DsControlFlags
{
    None = 0,
    Auto = 0x1,
    Manual = 0x2,
}

[ComImport, Guid("29840822-5B84-11D0-BD3B-00A0C911CE86"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface ICreateDevEnum
{
    [PreserveSig]
    int CreateClassEnumerator([In] ref Guid clsidDeviceClass, out IEnumMoniker? enumMoniker, int flags);
}

[ComImport, Guid("55272A00-42CB-11CE-8135-00AA004BB851"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IPropertyBag
{
    [PreserveSig]
    int Read([In, MarshalAs(UnmanagedType.LPWStr)] string propName,
             [Out, MarshalAs(UnmanagedType.Struct)] out object? value,
             IntPtr errorLog);

    [PreserveSig]
    int Write([In, MarshalAs(UnmanagedType.LPWStr)] string propName,
              [In, MarshalAs(UnmanagedType.Struct)] ref object value);
}

/// <summary>Marker interface: we never call IBaseFilter methods, we only need the right pointer.</summary>
[ComImport, Guid("56a86895-0ad4-11ce-b03a-0020af0ba770"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IBaseFilter
{
}

[ComImport, Guid("C6E13370-30AC-11d0-A18C-00A0C9118956"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IAMCameraControl
{
    [PreserveSig] int GetRange(int property, out int min, out int max, out int step, out int defaultValue, out DsControlFlags caps);
    [PreserveSig] int Set(int property, int value, DsControlFlags flags);
    [PreserveSig] int Get(int property, out int value, out DsControlFlags flags);
}

[ComImport, Guid("C6E13360-30AC-11d0-A18C-00A0C9118956"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IAMVideoProcAmp
{
    [PreserveSig] int GetRange(int property, out int min, out int max, out int step, out int defaultValue, out DsControlFlags caps);
    [PreserveSig] int Set(int property, int value, DsControlFlags flags);
    [PreserveSig] int Get(int property, out int value, out DsControlFlags flags);
}

[StructLayout(LayoutKind.Sequential)]
internal struct CAUUID
{
    public int cElems;
    public IntPtr pElems;
}

[ComImport, Guid("B196B28B-BAB4-101A-B69C-00AA00341D07"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface ISpecifyPropertyPages
{
    [PreserveSig] int GetPages(out CAUUID pages);
}

[ComImport, Guid("56a868a9-0ad4-11ce-b03a-0020af0ba770"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IGraphBuilder
{
    // IFilterGraph
    [PreserveSig] int AddFilter([In] IBaseFilter filter, [In, MarshalAs(UnmanagedType.LPWStr)] string? name);
    [PreserveSig] int RemoveFilter([In] IBaseFilter filter);
}

[ComImport, Guid("93E5A4E0-2D50-11d2-ABFA-00A0C9C6E38D"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface ICaptureGraphBuilder2
{
    [PreserveSig] int SetFiltergraph([In] IGraphBuilder graph);
    [PreserveSig] int GetFiltergraph(out IntPtr graph);
    [PreserveSig] int SetOutputFileName(IntPtr type, IntPtr file, IntPtr ppf, IntPtr ppSink);
    [PreserveSig] int FindInterface(IntPtr category, IntPtr type, IntPtr filter, IntPtr riid, IntPtr ppint);
    [PreserveSig]
    int RenderStream([In] ref Guid category, [In] ref Guid type,
                     [In, MarshalAs(UnmanagedType.IUnknown)] object source,
                     [In] IBaseFilter? compressor, [In] IBaseFilter? renderer);
}

[ComImport, Guid("56a868b1-0ad4-11ce-b03a-0020af0ba770"), InterfaceType(ComInterfaceType.InterfaceIsDual)]
internal interface IMediaControl
{
    [PreserveSig] int Run();
    [PreserveSig] int Pause();
    [PreserveSig] int Stop();
}

[ComImport, Guid("56a868b5-0ad4-11ce-b03a-0020af0ba770"), InterfaceType(ComInterfaceType.InterfaceIsDual)]
internal interface IBasicVideo
{
    [PreserveSig] int get_AvgTimePerFrame(out double value);
    [PreserveSig] int get_BitRate(out int value);
    [PreserveSig] int get_BitErrorRate(out int value);
    [PreserveSig] int get_VideoWidth(out int value);
    [PreserveSig] int get_VideoHeight(out int value);
}

[ComImport, Guid("56a868b4-0ad4-11ce-b03a-0020af0ba770"), InterfaceType(ComInterfaceType.InterfaceIsDual)]
internal interface IVideoWindow
{
    [PreserveSig] int put_Caption([In, MarshalAs(UnmanagedType.BStr)] string caption);
    [PreserveSig] int get_Caption([Out, MarshalAs(UnmanagedType.BStr)] out string caption);
    [PreserveSig] int put_WindowStyle(int style);
    [PreserveSig] int get_WindowStyle(out int style);
    [PreserveSig] int put_WindowStyleEx(int style);
    [PreserveSig] int get_WindowStyleEx(out int style);
    [PreserveSig] int put_AutoShow(int autoShow);
    [PreserveSig] int get_AutoShow(out int autoShow);
    [PreserveSig] int put_WindowState(int state);
    [PreserveSig] int get_WindowState(out int state);
    [PreserveSig] int put_BackgroundPalette(int palette);
    [PreserveSig] int get_BackgroundPalette(out int palette);
    [PreserveSig] int put_Visible(int visible);
    [PreserveSig] int get_Visible(out int visible);
    [PreserveSig] int put_Left(int left);
    [PreserveSig] int get_Left(out int left);
    [PreserveSig] int put_Width(int width);
    [PreserveSig] int get_Width(out int width);
    [PreserveSig] int put_Top(int top);
    [PreserveSig] int get_Top(out int top);
    [PreserveSig] int put_Height(int height);
    [PreserveSig] int get_Height(out int height);
    [PreserveSig] int put_Owner(IntPtr owner);
    [PreserveSig] int get_Owner(out IntPtr owner);
    [PreserveSig] int put_MessageDrain(IntPtr drain);
    [PreserveSig] int get_MessageDrain(out IntPtr drain);
    [PreserveSig] int get_BorderColor(out int color);
    [PreserveSig] int put_BorderColor(int color);
    [PreserveSig] int get_FullScreenMode(out int mode);
    [PreserveSig] int put_FullScreenMode(int mode);
    [PreserveSig] int SetWindowForeground(int focus);
    [PreserveSig] int NotifyOwnerMessage(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam);
    [PreserveSig] int SetWindowPosition(int left, int top, int width, int height);
}

internal static class NativeMethods
{
    public const int WS_CHILD = 0x40000000;
    public const int WS_CLIPSIBLINGS = 0x04000000;
    public const int WS_CLIPCHILDREN = 0x02000000;

    [DllImport("ole32.dll")]
    public static extern int CreateBindCtx(int reserved, out IBindCtx ctx);

    [DllImport("ole32.dll", CharSet = CharSet.Unicode)]
    public static extern int MkParseDisplayName(IBindCtx pbc, string szUserName, out int pchEaten, out IMoniker ppmk);

    [DllImport("oleaut32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
    public static extern int OleCreatePropertyFrame(
        IntPtr hwndOwner, int x, int y,
        [MarshalAs(UnmanagedType.LPWStr)] string caption,
        int cObjects,
        [MarshalAs(UnmanagedType.Interface)] ref object ppUnk,
        int cPages, IntPtr pPageClsId, int lcid, int dwReserved, IntPtr pvReserved);
}

internal static class ComHelper
{
    public static void Release(object? com)
    {
        if (com != null && Marshal.IsComObject(com))
        {
            Marshal.ReleaseComObject(com);
        }
    }
}
