using Microsoft.Maui.Controls.Shapes;

namespace Servus.Mobile.Features.Home;

// Lucide icons (same set servus.web uses via lucide-react), hand-converted to 24x24 path data:
// rect/circle nodes expanded into arcs and compressed arc flags/decimals ("a2 2 0 012 2",
// ".405-.022") spelled out, since the MAUI path parser is stricter than a browser's.
// Render with a stroke (width 2, round caps), never a fill — they are outline icons.
public static class HomeIcons
{
    public static Geometry CalendarDays { get; } = Parse(
        "M8 2v3 M16 2v3 M5 3h14a2 2 0 0 1 2 2v14a2 2 0 0 1 -2 2H5a2 2 0 0 1 -2 -2V5a2 2 0 0 1 2 -2z M3 9h18 " +
        "M8 13h0.01 M12 13h0.01 M16 13h0.01 M8 17h0.01 M12 17h0.01 M16 17h0.01");

    public static Geometry CalendarOff { get; } = Parse(
        "M16 2v3 M2 2l20 20 M21 9h-5.5 M3 9h6 M3.586 3.586A2 2 0 0 0 3 5v14a2 2 0 0 0 2 2h14a2 2 0 0 0 1.414 -0.586 " +
        "M8.656 3H19a2 2 0 0 1 2 2v10.344");

    public static Geometry Repeat { get; } = Parse(
        "M17 2l4 4 -4 4 M3 11v-1a4 4 0 0 1 4 -4h14 M7 22l-4 -4 4 -4 M21 13v1a4 4 0 0 1 -4 4H3");

    public static Geometry Users { get; } = Parse(
        "M16 21v-2a4 4 0 0 0 -4 -4H6a4 4 0 0 0 -4 4v2 M16 3.128a4 4 0 0 1 0 7.744 M22 21v-2a4 4 0 0 0 -3 -3.87 " +
        "M5 7a4 4 0 1 0 8 0a4 4 0 1 0 -8 0");

    public static Geometry User { get; } = Parse(
        "M19 21v-2a4 4 0 0 0 -4 -4H9a4 4 0 0 0 -4 4v2 M8 7a4 4 0 1 0 8 0a4 4 0 1 0 -8 0");

    public static Geometry KeyRound { get; } = Parse(
        "M2.586 17.414A2 2 0 0 0 2 18.828V21a1 1 0 0 0 1 1h3a1 1 0 0 0 1 -1v-1a1 1 0 0 1 1 -1h1a1 1 0 0 0 1 -1v-1" +
        "a1 1 0 0 1 1 -1h0.172a2 2 0 0 0 1.414 -0.586l0.814 -0.814a6.5 6.5 0 1 0 -4 -4z M16.5 7.5h0.01");

    public static Geometry Building { get; } = Parse(
        "M10 12h4 M10 8h4 M14 21v-3a2 2 0 0 0 -4 0v3 M6 10H4a2 2 0 0 0 -2 2v7a2 2 0 0 0 2 2h16a2 2 0 0 0 2 -2V9" +
        "a2 2 0 0 0 -2 -2h-2 M6 21V5a2 2 0 0 1 2 -2h8a2 2 0 0 1 2 2v16");

    // Arcs and lines only, no Bezier segments: the shield-check and moon icons (Lucide's are
    // cubic curves) rendered blank on Android while every arc/line icon drew fine, and the MAUI
    // parser itself read them correctly. CircleCheck stands in for ShieldCheck.
    public static Geometry CircleCheck { get; } = Parse(
        "M2 12a10 10 0 1 0 20 0a10 10 0 1 0 -20 0 M9 12l2 2 4 -4");

    public static Geometry TriangleAlert { get; } = Parse(
        "M21.73 18l-8 -14a2 2 0 0 0 -3.48 0l-8 14A2 2 0 0 0 4 21h16a2 2 0 0 0 1.73 -3 M12 9v4 M12 17h0.01");

    public static Geometry Clock { get; } = Parse(
        "M2 12a10 10 0 1 0 20 0a10 10 0 1 0 -20 0 M12 6v6l4 2");

    // Lucide's pre-2024 moon: the same crescent drawn with two arcs.
    public static Geometry Moon { get; } = Parse("M12 3A6 6 0 0 0 21 12A9 9 0 1 1 12 3Z");

    public static Geometry Search { get; } = Parse("M3 11a8 8 0 1 0 16 0a8 8 0 1 0 -16 0 M21 21l-4.3 -4.3");

    public static Geometry ChevronRight { get; } = Parse("M9 18l6 -6 -6 -6");

    public static Geometry Menu { get; } = Parse("M4 6h16 M4 12h16 M4 18h16");

    public static Geometry LayoutGrid { get; } = Parse(
        "M4 3h5a1 1 0 0 1 1 1v5a1 1 0 0 1 -1 1H4a1 1 0 0 1 -1 -1V4a1 1 0 0 1 1 -1z " +
        "M15 3h5a1 1 0 0 1 1 1v5a1 1 0 0 1 -1 1h-5a1 1 0 0 1 -1 -1V4a1 1 0 0 1 1 -1z " +
        "M15 14h5a1 1 0 0 1 1 1v5a1 1 0 0 1 -1 1h-5a1 1 0 0 1 -1 -1v-5a1 1 0 0 1 1 -1z " +
        "M4 14h5a1 1 0 0 1 1 1v5a1 1 0 0 1 -1 1H4a1 1 0 0 1 -1 -1v-5a1 1 0 0 1 1 -1z");

    public static Geometry LogOut { get; } = Parse(
        "M16 17l5 -5 -5 -5 M21 12H9 M9 21H5a2 2 0 0 1 -2 -2V5a2 2 0 0 1 2 -2h4");

    private static Geometry Parse(string data) =>
        (Geometry)new PathGeometryConverter().ConvertFromInvariantString(data)!;
}
