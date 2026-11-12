using System;
using System.Diagnostics;
using Microsoft.Win32;
using NexusIsland.Core;

namespace NexusIsland.Services;

public class QuickControlsService
{
    public void OpenWifiFlyout()
    {
        try
        {
            Process.Start(new ProcessStartInfo("explorer.exe", "ms-availablenetworks:") { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"OpenWifiFlyout error: {ex.Message}");
        }
    }

    public void OpenBluetoothSettings()
    {
        try
        {
            Process.Start(new ProcessStartInfo("ms-settings:bluetooth") { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"OpenBluetoothSettings error: {ex.Message}");
        }
    }

    public void OpenAirplaneMode()
    {
        try
        {
            Process.Start(new ProcessStartInfo("ms-settings:network-airplanemode") { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"OpenAirplaneMode error: {ex.Message}");
        }
    }

    public void OpenBatterySaver()
    {
        try
        {
            Process.Start(new ProcessStartInfo("ms-settings:batterysaver") { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"OpenBatterySaver error: {ex.Message}");
        }
    }

    public void OpenNightLight()
    {
        try
        {
            Process.Start(new ProcessStartInfo("ms-settings:nightlight") { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"OpenNightLight error: {ex.Message}");
        }
    }

    public bool IsWindowsDarkMode()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            if (key?.GetValue("AppsUseLightTheme") is int val)
            {
                return val == 0;
            }
        }
        catch
        {
            // Registry fallback
        }
        return true;
    }

    public void ToggleWindowsTheme()
    {
        try
        {
            bool currentDark = IsWindowsDarkMode();
            int newThemeValue = currentDark ? 1 : 0; // 1 = Light, 0 = Dark

            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", true);
            if (key != null)
            {
                key.SetValue("AppsUseLightTheme", newThemeValue, RegistryValueKind.DWord);
                key.SetValue("SystemUsesLightTheme", newThemeValue, RegistryValueKind.DWord);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ToggleWindowsTheme error: {ex.Message}");
        }
    }

    public void TriggerScreenCapture()
    {
        try
        {
            Process.Start(new ProcessStartInfo("explorer.exe", "ms-screenclip:") { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"TriggerScreenCapture error: {ex.Message}");
        }
    }

    public void TriggerScreenRecording()
    {
        try
        {
            Process.Start(new ProcessStartInfo("explorer.exe", "ms-gamebar:") { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"TriggerScreenRecording error: {ex.Message}");
        }
    }

    public void OpenFocusAssistance()
    {
        try
        {
            Process.Start(new ProcessStartInfo("ms-settings:quiethours") { UseShellExecute = true });
        }
        catch
        {
            try
            {
                Process.Start(new ProcessStartInfo("ms-settings:notifications") { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"OpenFocusAssistance error: {ex.Message}");
            }
        }
    }

    public void LockScreen()
    {
        try
        {
            NativeMethods.LockWorkStation();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LockScreen error: {ex.Message}");
        }
    }

    public void SleepSystem()
    {
        try
        {
            NativeMethods.SetSuspendState(false, true, false);
        }
        catch
        {
            try
            {
                Process.Start(new ProcessStartInfo("rundll32.exe", "powrprof.dll,SetSuspendState 0,1,0") { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SleepSystem error: {ex.Message}");
            }
        }
    }

    public void RestartSystem()
    {
        try
        {
            Process.Start(new ProcessStartInfo("shutdown.exe", "/r /t 0") { CreateNoWindow = true, UseShellExecute = false });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"RestartSystem error: {ex.Message}");
        }
    }

    public void ShutdownSystem()
    {
        try
        {
            Process.Start(new ProcessStartInfo("shutdown.exe", "/s /t 0") { CreateNoWindow = true, UseShellExecute = false });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ShutdownSystem error: {ex.Message}");
        }
    }

    public void LaunchApp(string path, string args = "")
    {
        try
        {
            Process.Start(new ProcessStartInfo(path, args) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LaunchApp error for '{path}': {ex.Message}");
        }
    }
}
