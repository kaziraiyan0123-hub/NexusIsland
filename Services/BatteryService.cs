using System;
using System.Windows.Forms;

namespace NexusIsland.Services;

public class BatteryInfo
{
    public int Percent { get; set; }
    public bool IsCharging { get; set; }
    public bool HasBattery { get; set; }
    public string StatusText { get; set; } = "Calculating...";
    public string EstimatedTimeRemaining { get; set; } = string.Empty;
    public bool IsLow { get; set; }
}

public class BatteryService
{
    public BatteryInfo GetBatteryInfo()
    {
        var info = new BatteryInfo();
        try
        {
            var power = SystemInformation.PowerStatus;
            info.HasBattery = power.BatteryChargeStatus != BatteryChargeStatus.NoSystemBattery;

            if (!info.HasBattery)
            {
                info.Percent = 100;
                info.IsCharging = true;
                info.StatusText = "Desktop (AC Power)";
                info.EstimatedTimeRemaining = "Unlimited";
                return info;
            }

            info.Percent = (int)Math.Round(power.BatteryLifePercent * 100);
            info.Percent = Math.Clamp(info.Percent, 0, 100);

            info.IsCharging = power.PowerLineStatus == PowerLineStatus.Online ||
                              power.BatteryChargeStatus.HasFlag(BatteryChargeStatus.Charging);

            info.IsLow = info.Percent <= 20 && !info.IsCharging;

            if (info.IsCharging)
            {
                info.StatusText = info.Percent >= 100 ? "Fully Charged" : "Charging";
            }
            else
            {
                info.StatusText = $"{info.Percent}% on Battery";
            }

            int seconds = power.BatteryLifeRemaining;
            if (seconds > 0 && !info.IsCharging)
            {
                var span = TimeSpan.FromSeconds(seconds);
                info.EstimatedTimeRemaining = $"{span.Hours}h {span.Minutes}m remaining";
            }
            else if (info.IsCharging)
            {
                info.EstimatedTimeRemaining = "Connected to power";
            }
            else
            {
                info.EstimatedTimeRemaining = "Estimating...";
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"BatteryService error: {ex.Message}");
            info.Percent = 100;
            info.StatusText = "AC Connected";
        }

        return info;
    }
}
