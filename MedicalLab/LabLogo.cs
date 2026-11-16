using System.IO;
using System.Windows.Media.Imaging;

namespace MedicalLab;

public static class LabLogo
{
    public static BitmapSource? Decode(byte[]? bytes)
    {
        if (bytes is null || bytes.Length == 0) return null;
        if (bytes.Length > 2_000_000) throw new ArgumentException("حجم الشعار يجب ألا يتجاوز 2 MB.");
        try
        {
            using var stream = new MemoryStream(bytes);
            var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
            var image = decoder.Frames[0];
            if (image.PixelWidth > 4000 || image.PixelHeight > 4000)
                throw new ArgumentException("أبعاد الشعار أكبر من المسموح (4000 × 4000).");
            image.Freeze();
            return image;
        }
        catch (ArgumentException) { throw; }
        catch (Exception ex) { throw new ArgumentException("شعار المختبر غير صالح؛ اختر صورة PNG أو JPEG صحيحة من الإعدادات.", ex); }
    }
}
