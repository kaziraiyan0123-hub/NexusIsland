using System;
using System.Management;

namespace NexusIsland.Services;

public class BrightnessService
{
    private bool _isSupported = true;

    public bool IsSupported => _isSupported;

    public int GetBrightness()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("root\\wmi", "SELECT * FROM WmiMonitorBrightness");
            foreach (ManagementObject obj in searcher.Get())
            {
                var cur = obj["CurrentBrightness"];
                if (cur != null)
                {
                    _isSupported = true;
                    return Convert.ToInt32(cur);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"GetBrightness exception: {ex.Message}");
            _isSupported = false;
        }

        return 50; // default fallback
    }

    public bool SetBrightness(int percent)
    {
        percent = Math.Clamp(percent, 0, 100);
        try
        {
            using var searcher = new ManagementObjectSearcher("root\\wmi", "SELECT * FROM WmiMonitorBrightnessMethods");
            bool updated = false;
            foreach (ManagementObject obj in searcher.Get())
            {
                obj.InvokeMethod("WmiSetBrightness", new object[] { 1, (byte)percent });
                updated = true;
            }
            if (updated)
            {
                _isSupported = true;
                return true;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SetBrightness exception: {ex.Message}");
            _isSupported = false;
        }

        return false;
    }
}
