using System;
using System.Windows.Media;
using Color = System.Windows.Media.Color;
using Point = System.Windows.Point;
using NexusIsland.Core;

namespace NexusIsland.Themes;

public class IslandTheme
{
    public required string Name { get; init; }
    public required Color AccentColor { get; init; }
    public required Color SecondaryAccentColor { get; init; }
    public required Color AccentGlow { get; init; }
    public required Color BackgroundPrimary { get; init; }
    public required Color BackgroundSecondary { get; init; }
    public required Color BorderColor { get; init; }
    public required Color TextPrimary { get; init; }
    public required Color TextSecondary { get; init; }
    public required Color TextAccent { get; init; }

    public SolidColorBrush AccentBrush { get; init; } = null!;
    public SolidColorBrush GlowBrush { get; init; } = null!;
    public SolidColorBrush BackgroundBrush { get; init; } = null!;
    public SolidColorBrush SecondaryBrush { get; init; } = null!;
    public SolidColorBrush BorderBrush { get; init; } = null!;
    public SolidColorBrush TextPrimaryBrush { get; init; } = null!;
    public SolidColorBrush TextSecondaryBrush { get; init; } = null!;
    public SolidColorBrush TextAccentBrush { get; init; } = null!;

    // Ultra-luxury gradients
    public LinearGradientBrush BackgroundGradient { get; init; } = null!;
    public LinearGradientBrush AccentGradient { get; init; } = null!;
    public LinearGradientBrush CardGradient { get; init; } = null!;
    public LinearGradientBrush BorderGradient { get; init; } = null!;

    public static IslandTheme Create(
        string name,
        Color accent,
        Color secondaryAccent,
        Color glow,
        Color bgPrimary,
        Color bgSecondary,
        Color border,
        Color textPrimary,
        Color textSecondary,
        Color textAccent)
    {
        SolidColorBrush FreezeSolid(Color c)
        {
            var b = new SolidColorBrush(c);
            b.Freeze();
            return b;
        }

        byte rTop = (byte)Math.Clamp(bgPrimary.R + accent.R / 10 + 20, 0, 255);
        byte gTop = (byte)Math.Clamp(bgPrimary.G + accent.G / 10 + 24, 0, 255);
        byte bTop = (byte)Math.Clamp(bgPrimary.B + accent.B / 10 + 34, 0, 255);

        var bgGrad = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(0, 1)
        };
        // Ultra-Frosted Translucent Glass (Crystalline transparency with depth)
        bgGrad.GradientStops.Add(new GradientStop(Color.FromArgb(170, rTop, gTop, bTop), 0.0));
        bgGrad.GradientStops.Add(new GradientStop(Color.FromArgb(195, bgPrimary.R, bgPrimary.G, bgPrimary.B), 0.5));
        bgGrad.GradientStops.Add(new GradientStop(Color.FromArgb(220, bgSecondary.R, bgSecondary.G, bgSecondary.B), 1.0));
        bgGrad.Freeze();

        var accentGrad = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(1, 1)
        };
        accentGrad.GradientStops.Add(new GradientStop(accent, 0.0));
        accentGrad.GradientStops.Add(new GradientStop(secondaryAccent, 1.0));
        accentGrad.Freeze();

        var cardGrad = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(0, 1)
        };
        // Frosted Glass Pod Surfaces with subtle chromatic ambient tone
        byte rCard = (byte)Math.Clamp(accent.R / 5 + 34, 0, 255);
        byte gCard = (byte)Math.Clamp(accent.G / 5 + 36, 0, 255);
        byte bCard = (byte)Math.Clamp(accent.B / 5 + 46, 0, 255);
        cardGrad.GradientStops.Add(new GradientStop(Color.FromArgb(135, rCard, gCard, bCard), 0.0));
        cardGrad.GradientStops.Add(new GradientStop(Color.FromArgb(95, 18, 20, 28), 1.0));
        cardGrad.Freeze();

        var borderGrad = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(1, 1)
        };
        // 3D Luminous Chromatic Refraction Rim (Brilliant white top chamfer + radiant theme color edges)
        borderGrad.GradientStops.Add(new GradientStop(Color.FromArgb(235, 255, 255, 255), 0.0));
        borderGrad.GradientStops.Add(new GradientStop(Color.FromArgb(200, accent.R, accent.G, accent.B), 0.28));
        borderGrad.GradientStops.Add(new GradientStop(Color.FromArgb(175, secondaryAccent.R, secondaryAccent.G, secondaryAccent.B), 0.72));
        borderGrad.GradientStops.Add(new GradientStop(Color.FromArgb(100, 255, 255, 255), 1.0));
        borderGrad.Freeze();

        return new IslandTheme
        {
            Name = name,
            AccentColor = accent,
            SecondaryAccentColor = secondaryAccent,
            AccentGlow = glow,
            BackgroundPrimary = bgPrimary,
            BackgroundSecondary = bgSecondary,
            BorderColor = border,
            TextPrimary = textPrimary,
            TextSecondary = textSecondary,
            TextAccent = textAccent,
            AccentBrush = FreezeSolid(accent),
            GlowBrush = FreezeSolid(glow),
            BackgroundBrush = FreezeSolid(Color.FromArgb(185, bgPrimary.R, bgPrimary.G, bgPrimary.B)),
            SecondaryBrush = FreezeSolid(Color.FromArgb(195, bgSecondary.R, bgSecondary.G, bgSecondary.B)),
            BorderBrush = FreezeSolid(border),
            TextPrimaryBrush = FreezeSolid(textPrimary),
            TextSecondaryBrush = FreezeSolid(textSecondary),
            TextAccentBrush = FreezeSolid(textAccent),
            BackgroundGradient = bgGrad,
            AccentGradient = accentGrad,
            CardGradient = cardGrad,
            BorderGradient = borderGrad
        };
    }
}

public static class ThemePalette
{
    public static IslandTheme GetTheme(ThemePreset preset) => preset switch
    {
        ThemePreset.MidnightOLED => IslandTheme.Create(
            "Midnight OLED",
            Color.FromRgb(10, 132, 255),
            Color.FromRgb(48, 209, 88),
            Color.FromArgb(135, 10, 132, 255),
            Color.FromRgb(0, 0, 0),
            Color.FromRgb(0, 0, 0),
            Color.FromArgb(160, 10, 132, 255),
            Color.FromRgb(255, 255, 255),
            Color.FromRgb(150, 155, 170),
            Color.FromRgb(10, 132, 255)),

        ThemePreset.CyberpunkCyan => IslandTheme.Create(
            "Cyberpunk Cyan",
            Color.FromRgb(0, 240, 255),
            Color.FromRgb(217, 70, 239),
            Color.FromArgb(150, 0, 240, 255),
            Color.FromArgb(248, 8, 12, 20),
            Color.FromArgb(225, 12, 18, 28),
            Color.FromArgb(160, 0, 240, 255),
            Color.FromRgb(240, 249, 255),
            Color.FromRgb(165, 180, 205),
            Color.FromRgb(0, 240, 255)),

        ThemePreset.AppleMinimal => IslandTheme.Create(
            "Apple Minimal",
            Color.FromRgb(10, 132, 255),
            Color.FromRgb(255, 45, 85),
            Color.FromArgb(135, 10, 132, 255),
            Color.FromArgb(248, 8, 12, 20),
            Color.FromArgb(225, 12, 18, 28),
            Color.FromArgb(160, 10, 132, 255),
            Color.FromRgb(255, 255, 255),
            Color.FromRgb(160, 165, 185),
            Color.FromRgb(10, 132, 255)),

        ThemePreset.NeonBlue => IslandTheme.Create(
            "Neon Blue",
            Color.FromRgb(59, 130, 246),
            Color.FromRgb(99, 102, 241),
            Color.FromArgb(95, 59, 130, 246),
            Color.FromArgb(248, 10, 14, 28),
            Color.FromArgb(225, 15, 22, 38),
            Color.FromArgb(120, 59, 130, 246),
            Color.FromRgb(248, 250, 252),
            Color.FromRgb(148, 163, 184),
            Color.FromRgb(96, 165, 250)),

        ThemePreset.Crimson => IslandTheme.Create(
            "Crimson Red",
            Color.FromRgb(239, 68, 68),
            Color.FromRgb(249, 115, 22),
            Color.FromArgb(95, 239, 68, 68),
            Color.FromArgb(248, 20, 10, 12),
            Color.FromArgb(225, 30, 14, 18),
            Color.FromArgb(120, 239, 68, 68),
            Color.FromRgb(254, 242, 242),
            Color.FromRgb(203, 213, 225),
            Color.FromRgb(248, 113, 113)),

        ThemePreset.Aurora => IslandTheme.Create(
            "Aurora Purple",
            Color.FromRgb(168, 85, 247),
            Color.FromRgb(236, 72, 153),
            Color.FromArgb(95, 168, 85, 247),
            Color.FromArgb(248, 16, 10, 26),
            Color.FromArgb(225, 24, 16, 40),
            Color.FromArgb(120, 168, 85, 247),
            Color.FromRgb(250, 245, 255),
            Color.FromRgb(192, 132, 252),
            Color.FromRgb(192, 132, 252)),

        ThemePreset.MatrixEmerald => IslandTheme.Create(
            "Matrix Emerald",
            Color.FromRgb(16, 185, 129),
            Color.FromRgb(6, 182, 212),
            Color.FromArgb(95, 16, 185, 129),
            Color.FromArgb(248, 6, 18, 14),
            Color.FromArgb(225, 10, 26, 20),
            Color.FromArgb(120, 16, 185, 129),
            Color.FromRgb(236, 253, 245),
            Color.FromRgb(110, 231, 183),
            Color.FromRgb(52, 211, 153)),

        ThemePreset.AmberGold => IslandTheme.Create(
            "Amber Gold",
            Color.FromRgb(245, 158, 11),
            Color.FromRgb(239, 68, 68),
            Color.FromArgb(95, 245, 158, 11),
            Color.FromArgb(248, 20, 14, 8),
            Color.FromArgb(225, 30, 20, 12),
            Color.FromArgb(120, 245, 158, 11),
            Color.FromRgb(254, 252, 232),
            Color.FromRgb(252, 211, 77),
            Color.FromRgb(245, 158, 11)),

        ThemePreset.SynthwaveMagenta => IslandTheme.Create(
            "Synthwave Magenta",
            Color.FromRgb(236, 72, 153),
            Color.FromRgb(6, 182, 212),
            Color.FromArgb(95, 236, 72, 153),
            Color.FromArgb(248, 22, 8, 20),
            Color.FromArgb(225, 32, 12, 28),
            Color.FromArgb(120, 236, 72, 153),
            Color.FromRgb(253, 242, 248),
            Color.FromRgb(244, 114, 182),
            Color.FromRgb(236, 72, 153)),

        _ => GetTheme(ThemePreset.MidnightOLED)
    };

    public static IslandTheme CreateCustomTheme(string hex)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(hex)) hex = "#00F0FF";
            if (!hex.StartsWith("#")) hex = "#" + hex;
            var c = (Color)System.Windows.Media.ColorConverter.ConvertFromString(hex);
            var sec = Color.FromRgb((byte)((c.R + 60) % 256), (byte)((c.G + 80) % 256), (byte)((c.B + 120) % 256));

            return IslandTheme.Create(
                $"Custom ({hex})",
                c,
                sec,
                Color.FromArgb(95, c.R, c.G, c.B),
                Color.FromArgb(248, 8, 12, 18),
                Color.FromArgb(225, 14, 20, 28),
                Color.FromArgb(120, c.R, c.G, c.B),
                Color.FromRgb(248, 250, 252),
                Color.FromArgb(200, c.R, c.G, c.B),
                c);
        }
        catch
        {
            return GetTheme(ThemePreset.CyberpunkCyan);
        }
    }
}
