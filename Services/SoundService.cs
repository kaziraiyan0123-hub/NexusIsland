using System;
using System.IO;
using System.Media;

namespace NexusIsland.Services;

public class SoundService
{
    public bool Enabled { get; set; } = false; // Disabled by default for silent, peaceful Apple-level feel
    private static byte[]? _softClickWav;

    static SoundService()
    {
        try
        {
            _softClickWav = GenerateSoftPopWav(1600, 10);
        }
        catch { }
    }

    public void PlayClick()
    {
        if (!Enabled || _softClickWav == null) return;
        try
        {
            using var ms = new MemoryStream(_softClickWav);
            using var player = new SoundPlayer(ms);
            player.Play();
        }
        catch { }
    }

    public void PlayNotification()
    {
        if (!Enabled) return;
        // Subtle soft cue
        PlayClick();
    }

    public void PlayComplete()
    {
        if (!Enabled) return;
        PlayClick();
    }

    private static byte[] GenerateSoftPopWav(double frequency, int durationMs)
    {
        int sampleRate = 44100;
        int samples = (sampleRate * durationMs) / 1000;
        short[] buffer = new short[samples];

        for (int i = 0; i < samples; i++)
        {
            double t = (double)i / sampleRate;
            double envelope = Math.Exp(-12.0 * ((double)i / samples));
            double sample = Math.Sin(2.0 * Math.PI * frequency * t) * envelope;
            buffer[i] = (short)(sample * 6000.0); // Gentle soft volume
        }

        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // RIFF header
        writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
        writer.Write(36 + samples * 2);
        writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
        writer.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
        writer.Write(16); // subchunk1 size
        writer.Write((short)1); // PCM
        writer.Write((short)1); // mono
        writer.Write(sampleRate);
        writer.Write(sampleRate * 2);
        writer.Write((short)2);
        writer.Write((short)16);
        writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
        writer.Write(samples * 2);

        foreach (var s in buffer)
        {
            writer.Write(s);
        }

        return ms.ToArray();
    }
}
