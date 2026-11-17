using System.Runtime.InteropServices;
using MyLifeCamController.Core.DirectShow;

namespace MyLifeCamController.Core.Camera;

/// <summary>
/// Live video preview rendered by DirectShow into a host window (e.g. a WinForms Panel).
/// Fails with <see cref="PreviewUnavailableException"/> if the camera is busy in another app.
/// </summary>
public sealed class PreviewGraph : IDisposable
{
    private object? _graph;
    private object? _builder;
    private IVideoWindow? _window;
    private readonly IntPtr _host;

    /// <summary>Native size of the video, or (0,0) if unknown.</summary>
    public (int Width, int Height) VideoSize { get; private set; }

    public PreviewGraph(CameraSession session, IntPtr hostHandle)
    {
        _host = hostHandle;
        try
        {
            Build(session);
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    private void Build(CameraSession session)
    {
        _graph = Activator.CreateInstance(Type.GetTypeFromCLSID(DsGuids.CLSID_FilterGraph, true)!)!;
        _builder = Activator.CreateInstance(Type.GetTypeFromCLSID(DsGuids.CLSID_CaptureGraphBuilder2, true)!)!;

        var graph = (IGraphBuilder)_graph;
        var builder = (ICaptureGraphBuilder2)_builder;
        Check(builder.SetFiltergraph(graph), "SetFiltergraph");

        var filter = (IBaseFilter)session.Filter;
        Check(graph.AddFilter(filter, "Camera"), "AddFilter");

        var video = DsGuids.MEDIATYPE_Video;
        var preview = DsGuids.PIN_CATEGORY_PREVIEW;
        int hr = builder.RenderStream(ref preview, ref video, filter, null, null);
        if (hr < 0)
        {
            // Cameras without a preview pin: render the capture pin instead.
            var capture = DsGuids.PIN_CATEGORY_CAPTURE;
            Check(builder.RenderStream(ref capture, ref video, filter, null, null), "RenderStream");
        }

        if (_graph is IBasicVideo basic
            && basic.get_VideoWidth(out int w) == 0
            && basic.get_VideoHeight(out int h) == 0)
        {
            VideoSize = (w, h);
        }

        _window = (IVideoWindow)_graph;
        Check(_window.put_Owner(_host), "put_Owner");
        Check(_window.put_WindowStyle(NativeMethods.WS_CHILD | NativeMethods.WS_CLIPSIBLINGS | NativeMethods.WS_CLIPCHILDREN), "put_WindowStyle");
        _window.put_MessageDrain(_host);

        Check(((IMediaControl)_graph).Run(), "Run");
        _window.put_Visible(-1); // OATRUE
    }

    /// <summary>Positions the video inside the host, letterboxed to keep the aspect ratio.</summary>
    public void Resize(int hostWidth, int hostHeight)
    {
        if (_window == null || hostWidth <= 0 || hostHeight <= 0)
        {
            return;
        }

        var (vw, vh) = VideoSize;
        if (vw <= 0 || vh <= 0)
        {
            _window.SetWindowPosition(0, 0, hostWidth, hostHeight);
            return;
        }

        double scale = Math.Min(hostWidth / (double)vw, hostHeight / (double)vh);
        int w = (int)(vw * scale);
        int h = (int)(vh * scale);
        _window.SetWindowPosition((hostWidth - w) / 2, (hostHeight - h) / 2, w, h);
    }

    private static void Check(int hr, string step)
    {
        if (hr < 0)
        {
            throw new PreviewUnavailableException(step, hr);
        }
    }

    public void Dispose()
    {
        if (_graph is IMediaControl control)
        {
            control.Stop();
        }

        if (_window != null)
        {
            _window.put_Visible(0);
            _window.put_Owner(IntPtr.Zero);
            _window = null;
        }

        if (_builder != null)
        {
            Marshal.FinalReleaseComObject(_builder);
            _builder = null;
        }

        if (_graph != null)
        {
            Marshal.FinalReleaseComObject(_graph);
            _graph = null;
        }
    }
}

public sealed class PreviewUnavailableException(string step, int hresult)
    : Exception(Lang.T($"Pré-visualização indisponível ({step} falhou, HRESULT 0x{hresult:X8}).", $"Preview unavailable ({step} failed, HRESULT 0x{hresult:X8})."))
{
    public int HResultCode { get; } = hresult;

    /// <summary>True when the failure most likely means another app is using the camera.</summary>
    public bool CameraBusy => HResultCode is unchecked((int)0x800705AA) or unchecked((int)0x80070020) or unchecked((int)0x8007001F);
}
