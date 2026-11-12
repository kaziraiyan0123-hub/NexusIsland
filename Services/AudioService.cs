using System;
using System.Runtime.InteropServices;

namespace NexusIsland.Services;

public class AudioService
{
    private IAudioEndpointVolume? _endpointVolume;
    private IAudioMeterInformation? _meterInfo;

    public event Action<float, bool>? VolumeChanged;

    public AudioService()
    {
        Initialize();
    }

    private void Initialize()
    {
        try
        {
            var enumerator = (IMMDeviceEnumerator)new MMDeviceEnumerator();
            enumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia, out var device);
            if (device != null)
            {
                var iidEndpoint = typeof(IAudioEndpointVolume).GUID;
                device.Activate(ref iidEndpoint, CLSCTX.CLSCTX_INPROC_SERVER, IntPtr.Zero, out var epObj);
                _endpointVolume = epObj as IAudioEndpointVolume;

                var iidMeter = typeof(IAudioMeterInformation).GUID;
                device.Activate(ref iidMeter, CLSCTX.CLSCTX_INPROC_SERVER, IntPtr.Zero, out var meterObj);
                _meterInfo = meterObj as IAudioMeterInformation;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"AudioService initialization failed: {ex.Message}");
        }
    }

    public float GetMasterVolume()
    {
        try
        {
            if (_endpointVolume != null)
            {
                _endpointVolume.GetMasterVolumeLevelScalar(out float level);
                return level;
            }
        }
        catch
        {
            Initialize();
        }
        return 0.5f;
    }

    public bool IsMuted()
    {
        try
        {
            if (_endpointVolume != null)
            {
                _endpointVolume.GetMute(out bool isMuted);
                return isMuted;
            }
        }
        catch
        {
            Initialize();
        }
        return false;
    }

    public void SetMasterVolume(float level)
    {
        try
        {
            level = Math.Clamp(level, 0.0f, 1.0f);
            if (_endpointVolume != null)
            {
                _endpointVolume.SetMasterVolumeLevelScalar(level, Guid.Empty);
                VolumeChanged?.Invoke(level, IsMuted());
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SetMasterVolume failed: {ex.Message}");
            Initialize();
        }
    }

    public void ToggleMute()
    {
        try
        {
            if (_endpointVolume != null)
            {
                bool current = IsMuted();
                _endpointVolume.SetMute(!current, Guid.Empty);
                VolumeChanged?.Invoke(GetMasterVolume(), !current);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ToggleMute failed: {ex.Message}");
            Initialize();
        }
    }

    public float GetPeakSample()
    {
        try
        {
            if (_meterInfo != null)
            {
                _meterInfo.GetPeakValue(out float peak);
                return peak;
            }
        }
        catch
        {
            // Transient COM error when audio driver switches
        }
        return 0.0f;
    }

    #region COM Interfaces

    [ComImport]
    [Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
    private class MMDeviceEnumerator
    {
    }

    private enum EDataFlow
    {
        eRender,
        eCapture,
        eAll
    }

    private enum ERole
    {
        eConsole,
        eMultimedia,
        eCommunications
    }

    [Flags]
    private enum CLSCTX : uint
    {
        CLSCTX_INPROC_SERVER = 0x1
    }

    [Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDeviceEnumerator
    {
        int NotImpl1();
        [PreserveSig]
        int GetDefaultAudioEndpoint(EDataFlow dataFlow, ERole role, out IMMDevice ppDevice);
    }

    [Guid("D666063F-1587-4E43-81F1-B948E807363F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDevice
    {
        [PreserveSig]
        int Activate(ref Guid iid, CLSCTX dwClsCtx, IntPtr pActivationParams, [MarshalAs(UnmanagedType.IUnknown)] out object ppInterface);
    }

    [Guid("5CDF2C82-841E-4546-9722-0CF74078229A"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IAudioEndpointVolume
    {
        int NotImpl1();
        int NotImpl2();
        int NotImpl3();
        int NotImpl4();
        [PreserveSig]
        int SetMasterVolumeLevelScalar(float fLevel, [MarshalAs(UnmanagedType.LPStruct)] Guid pguidEventContext);
        int NotImpl5();
        [PreserveSig]
        int GetMasterVolumeLevelScalar(out float pfLevel);
        int NotImpl6();
        int NotImpl7();
        int NotImpl8();
        int NotImpl9();
        [PreserveSig]
        int SetMute([MarshalAs(UnmanagedType.Bool)] bool bMute, [MarshalAs(UnmanagedType.LPStruct)] Guid pguidEventContext);
        [PreserveSig]
        int GetMute([MarshalAs(UnmanagedType.Bool)] out bool pbMute);
    }

    [Guid("C02216F6-8C67-4B5B-9D00-D008E73E0064"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IAudioMeterInformation
    {
        [PreserveSig]
        int GetPeakValue(out float pfPeak);
    }

    #endregion
}
