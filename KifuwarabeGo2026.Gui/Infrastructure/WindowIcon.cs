namespace KifuwarabeGo2026.Gui.Infrastructure;

using KifuwarabeGo2026.Gui.Infrastructure.Logging;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

internal static class WindowIcon
{
    public static void TryApply(IntPtr windowHandle)
    {
        try
        {
            using var iconStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("GuiIcon.ico");
            if (iconStream is null)
                throw new InvalidOperationException("The embedded GUI icon was not found.");

            using var bitmap = ReadLargestPngImage(iconStream);
            var bounds = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            var data = bitmap.LockBits(bounds, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            try
            {
                var surface = SdlCreateRgbSurfaceFrom(
                    data.Scan0,
                    bitmap.Width,
                    bitmap.Height,
                    32,
                    data.Stride,
                    0x00ff0000,
                    0x0000ff00,
                    0x000000ff,
                    0xff000000);
                if (surface == IntPtr.Zero)
                    throw new InvalidOperationException("SDL could not create the window icon surface.");

                try
                {
                    SdlSetWindowIcon(windowHandle, surface);
                }
                finally
                {
                    SdlFreeSurface(surface);
                }
            }
            finally
            {
                bitmap.UnlockBits(data);
            }
        }
        catch (Exception ex) when (
            ex is IOException or
            InvalidOperationException or
            ArgumentException or
            ExternalException or
            DllNotFoundException or
            EntryPointNotFoundException)
        {
            ApplicationErrorLog.Write("WINDOW ICON", "Could not apply the application icon to the MonoGame window.", ex);
        }
    }

    private static Bitmap ReadLargestPngImage(Stream iconStream)
    {
        using var reader = new BinaryReader(iconStream, System.Text.Encoding.UTF8, leaveOpen: true);
        if (reader.ReadUInt16() != 0 || reader.ReadUInt16() != 1)
            throw new InvalidOperationException("The embedded GUI icon is not an ICO file.");

        var count = reader.ReadUInt16();
        if (count == 0)
            throw new InvalidOperationException("The embedded GUI icon has no images.");

        uint largestSize = 0;
        uint largestOffset = 0;
        var largestArea = -1;
        for (var index = 0; index < count; index++)
        {
            var widthByte = reader.ReadByte();
            var heightByte = reader.ReadByte();
            reader.ReadBytes(6);
            var size = reader.ReadUInt32();
            var offset = reader.ReadUInt32();
            var width = widthByte == 0 ? 256 : widthByte;
            var height = heightByte == 0 ? 256 : heightByte;
            if (width * height <= largestArea)
                continue;

            largestArea = width * height;
            largestSize = size;
            largestOffset = offset;
        }

        iconStream.Position = largestOffset;
        var imageBytes = reader.ReadBytes(checked((int)largestSize));
        using var imageStream = new MemoryStream(imageBytes, writable: false);
        using var image = Image.FromStream(imageStream);
        return new Bitmap(image);
    }

    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl, EntryPoint = "SDL_CreateRGBSurfaceFrom")]
    private static extern IntPtr SdlCreateRgbSurfaceFrom(
        IntPtr pixels,
        int width,
        int height,
        int depth,
        int pitch,
        uint redMask,
        uint greenMask,
        uint blueMask,
        uint alphaMask);

    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl, EntryPoint = "SDL_SetWindowIcon")]
    private static extern void SdlSetWindowIcon(IntPtr window, IntPtr icon);

    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl, EntryPoint = "SDL_FreeSurface")]
    private static extern void SdlFreeSurface(IntPtr surface);
}
