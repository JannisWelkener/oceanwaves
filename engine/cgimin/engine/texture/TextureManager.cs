using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using SkiaSharp;
using OpenTK.Graphics.OpenGL;

namespace cgimin.engine.texture;

public static class TextureManager {
    // Methode zum laden einer Textur
    public static int LoadTexture(string fullAssetPath) {
        // Textur wird generiert
        var returnTextureId = GL.GenTexture();

        // Textur wird "gebunden", folgende Befehle beziehen sich auf die gesetzte Textur (Statemachine)
        GL.BindTexture(TextureTarget.Texture2D, returnTextureId);
        
        #if Windows
            CreateTexture(fullAssetPath);
#else
            CreateTextureOsx(fullAssetPath);
#endif

        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMinFilter.Linear);
        GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);

        return returnTextureId;
    }

    private static void CreateTexture(string fullAssetPath) {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;
        var bmp = new Bitmap(fullAssetPath);
        var bmpData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly,
            System.Drawing.Imaging.PixelFormat.Format32bppArgb);

        // Textur-Parameter, Pixelformat etc.
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, bmpData.Width, bmpData.Height, 0,
            OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, bmpData.Scan0);

        bmp.UnlockBits(bmpData);
    }

    private static void CreateTextureOsx(string fullAssetPath) {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) return;
        var bitmap = CreateBitmapFromFile(fullAssetPath);
        var bitMapPointer = SaveBitmapInMemory(bitmap);

        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, bitmap.Width, bitmap.Height, 0,
            OpenTK.Graphics.OpenGL.PixelFormat.Rgba, PixelType.UnsignedByte, bitMapPointer);

        FreeBitmapMemory(ref bitMapPointer);
    }

    private static SKBitmap CreateBitmapFromFile(string fullAssetPath) {
        var stream = new SKFileStream(fullAssetPath);

        var codec = SKCodec.Create(stream);

        var bitmap = new SKBitmap(codec.Info);

        // Decode bitmap and write it into memory 
        var decodingResult = codec.GetPixels(bitmap.Info, bitmap.GetPixels());
        Console.WriteLine($"Texture bitmap decoding result: {decodingResult}");

        return bitmap;
    }

    private static IntPtr SaveBitmapInMemory(SKBitmap bitmap) {
        var width = bitmap.Width;
        var height = bitmap.Height;

        var info = new SKImageInfo(width, height, bitmap.ColorType, bitmap.AlphaType);

        var pBitMap = Marshal.AllocHGlobal(bitmap.Bytes.Length);
        var rowByte = width * 4;

        var surface = SKSurface.Create(info, pBitMap, rowByte);

        var canvas = surface.Canvas;

        canvas.DrawBitmap(bitmap, 0, 0);

        return pBitMap;
    }

    private static void FreeBitmapMemory(ref IntPtr bitmapPointer) {
        if (bitmapPointer == IntPtr.Zero) return;
        Marshal.FreeHGlobal(bitmapPointer);
        bitmapPointer = IntPtr.Zero;
    }

    public static int LoadCubemapTexture(string[] faces)
    {
        int skyboxTexture = GL.GenTexture();
        GL.BindTexture(TextureTarget.TextureCubeMap, skyboxTexture);

        for (int i = 0; i < faces.Length; i++)
        {
            using (var image = new Bitmap(faces[i]))
            {
                var data = image.LockBits(
                    new Rectangle(0, 0, image.Width, image.Height),
                    ImageLockMode.ReadOnly,
                    System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                GL.TexImage2D(
                    TextureTarget.TextureCubeMapPositiveX + i,
                    0,
                    PixelInternalFormat.Rgba,
                    image.Width,
                    image.Height,
                    0,
                    OpenTK.Graphics.OpenGL.PixelFormat.Bgra,
                    PixelType.UnsignedByte,
                    data.Scan0);

                image.UnlockBits(data);
            }
            GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapR, (int)TextureWrapMode.ClampToEdge);
        }
        return skyboxTexture;
    }

    //public static void UpdateCubemapTexture(int texture, string[] faces)
    //{
    //    Console.WriteLine("update cubemap");
    //    GL.BindTexture(TextureTarget.TextureCubeMap, texture);

    //    for (int i = 0; i < faces.Length; i++)
    //    {
    //        using (Bitmap image = new Bitmap(faces[i]))
    //        {
    //            var data = image.LockBits(
    //                new Rectangle(0, 0, image.Width, image.Height),
    //                ImageLockMode.ReadOnly,
    //                System.Drawing.Imaging.PixelFormat.Format32bppArgb);

    //            GL.TexSubImage2D(
    //                TextureTarget.TextureCubeMapPositiveX + i,
    //                0,
    //                0,
    //                0,
    //                image.Width,
    //                image.Height,
    //                OpenTK.Graphics.OpenGL.PixelFormat.Bgra,
    //                PixelType.UnsignedByte,
    //                data.Scan0);

    //            image.UnlockBits(data);
    //        }
    //    }
    //}
}