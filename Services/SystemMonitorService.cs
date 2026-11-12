using System;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices.ComTypes;
using System.Threading;
using System.Threading.Tasks;
using NexusIsland.Core;

namespace NexusIsland.Services;

public class SystemMetrics
{
    public double CpuPercent { get; set; }
    public double RamPercent { get; set; }
    public double RamUsedGb { get; set; }
    public double RamTotalGb { get; set; }
    public double NetworkDownloadKbps { get; set; }
    public double NetworkUploadKbps { get; set; }
    public double DiskPercent { get; set; }
    public string DiskFreeGb { get; set; } = string.Empty;
}

public class SystemMonitorService
{
    private ulong _prevIdleTime;
    private ulong _prevKernelTime;
    private ulong _prevUserTime;

    private long _prevBytesReceived;
    private long _prevBytesSent;
    private DateTime _prevNetworkTime = DateTime.UtcNow;

    public SystemMetrics CurrentMetrics { get; private set; } = new();
    public event Action<SystemMetrics>? MetricsUpdated;

    private CancellationTokenSource? _cts;

    public void Start()
    {
        InitializeTimes();
        InitializeNetwork();
        _cts = new CancellationTokenSource();
        _ = Task.Run(async () =>
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                try
                {
                    UpdateMetrics();
                    await Task.Delay(1000, _cts.Token);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"SystemMonitor error: {ex.Message}");
                }
            }
        });
    }

    public void Stop()
    {
        _cts?.Cancel();
    }

    private void InitializeTimes()
    {
        if (NativeMethods.GetSystemTimes(out var idle, out var kernel, out var user))
        {
            _prevIdleTime = FileTimeToUInt64(idle);
            _prevKernelTime = FileTimeToUInt64(kernel);
            _prevUserTime = FileTimeToUInt64(user);
        }
    }

    private void InitializeNetwork()
    {
        try
        {
            _prevBytesReceived = NetworkInterface.GetAllNetworkInterfaces()
                .Where(n => n.OperationalStatus == OperationalStatus.Up)
                .Sum(n => n.GetIPv4Statistics().BytesReceived);

            _prevBytesSent = NetworkInterface.GetAllNetworkInterfaces()
                .Where(n => n.OperationalStatus == OperationalStatus.Up)
                .Sum(n => n.GetIPv4Statistics().BytesSent);

            _prevNetworkTime = DateTime.UtcNow;
        }
        catch
        {
            // Transient network query failure
        }
    }

    private void UpdateMetrics()
    {
        var metrics = new SystemMetrics();

        // 1. CPU
        if (NativeMethods.GetSystemTimes(out var idle, out var kernel, out var user))
        {
            ulong curIdle = FileTimeToUInt64(idle);
            ulong curKernel = FileTimeToUInt64(kernel);
            ulong curUser = FileTimeToUInt64(user);

            ulong deltaIdle = curIdle - _prevIdleTime;
            ulong deltaKernel = curKernel - _prevKernelTime;
            ulong deltaUser = curUser - _prevUserTime;

            ulong totalSys = deltaKernel + deltaUser;
            if (totalSys > 0)
            {
                ulong totalWorking = totalSys - deltaIdle;
                metrics.CpuPercent = Math.Clamp((double)totalWorking * 100.0 / totalSys, 0.0, 100.0);
            }

            _prevIdleTime = curIdle;
            _prevKernelTime = curKernel;
            _prevUserTime = curUser;
        }

        // 2. RAM
        var memStatus = new NativeMethods.MEMORYSTATUSEX();
        if (NativeMethods.GlobalMemoryStatusEx(memStatus))
        {
            metrics.RamPercent = memStatus.dwMemoryLoad;
            metrics.RamTotalGb = Math.Round((double)memStatus.ullTotalPhys / (1024 * 1024 * 1024), 1);
            ulong usedBytes = memStatus.ullTotalPhys - memStatus.ullAvailPhys;
            metrics.RamUsedGb = Math.Round((double)usedBytes / (1024 * 1024 * 1024), 1);
        }

        // 3. Network Speeds
        try
        {
            var now = DateTime.UtcNow;
            double elapsedSec = (now - _prevNetworkTime).TotalSeconds;
            if (elapsedSec > 0.2)
            {
                long curRecv = NetworkInterface.GetAllNetworkInterfaces()
                    .Where(n => n.OperationalStatus == OperationalStatus.Up)
                    .Sum(n => n.GetIPv4Statistics().BytesReceived);

                long curSent = NetworkInterface.GetAllNetworkInterfaces()
                    .Where(n => n.OperationalStatus == OperationalStatus.Up)
                    .Sum(n => n.GetIPv4Statistics().BytesSent);

                long deltaRecv = Math.Max(0, curRecv - _prevBytesReceived);
                long deltaSent = Math.Max(0, curSent - _prevBytesSent);

                metrics.NetworkDownloadKbps = Math.Round((deltaRecv / 1024.0) / elapsedSec, 1);
                metrics.NetworkUploadKbps = Math.Round((deltaSent / 1024.0) / elapsedSec, 1);

                _prevBytesReceived = curRecv;
                _prevBytesSent = curSent;
                _prevNetworkTime = now;
            }
        }
        catch
        {
            // Network fallback
        }

        // 4. Disk Usage
        try
        {
            var systemDrive = DriveInfo.GetDrives().FirstOrDefault(d => d.IsReady && d.Name.StartsWith("C"));
            if (systemDrive != null)
            {
                double total = systemDrive.TotalSize;
                double free = systemDrive.AvailableFreeSpace;
                double used = total - free;
                metrics.DiskPercent = Math.Round((used / total) * 100.0, 1);
                metrics.DiskFreeGb = $"{Math.Round(free / (1024 * 1024 * 1024), 0)} GB Free";
            }
        }
        catch
        {
            metrics.DiskPercent = 50.0;
            metrics.DiskFreeGb = "OK";
        }

        CurrentMetrics = metrics;
        MetricsUpdated?.Invoke(metrics);
    }

    private static ulong FileTimeToUInt64(FILETIME ft)
    {
        return ((ulong)(uint)ft.dwHighDateTime << 32) | (uint)ft.dwLowDateTime;
    }
}
