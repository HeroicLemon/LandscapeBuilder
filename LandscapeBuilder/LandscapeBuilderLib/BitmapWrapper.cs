using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace LandscapeBuilderLib
{
    // Wrapper class for Bitmap. 
    // The default implementation of Bitmap locks and unlocks the bitmap every time a pixel is accessed, which increases processing time significantly.
    // This wrapper allows direct access to the byte[] for the bitmap, which bypasses the locking issue.
    // This is not a complete wrap of Bitmap, and is not recommended for use outside of LandscapeBuilder.
    public class BitmapWrapper
    {
        private Bitmap _bitmap;
        public Bitmap Bitmap { get { return _bitmap; } }

        private BitmapData _bitmapData;

        private int _stride;
        public int Stride {  get { return _stride; } }

        private int _height;
        public int Height { get { return _height; } }

        private int _width;
        public int Width { get { return _width; } }

        private PixelFormat _pixelFormat;
        public PixelFormat PixelFormat {  get { return _pixelFormat; } }

        private ImageLockMode _lockMode;

        private byte[] _byteData;
        public byte[] ByteData {  get { return _byteData; } }

        public BitmapWrapper(string path, ImageLockMode lockMode = ImageLockMode.ReadOnly)
        {
            _bitmap = new Bitmap(path);
            initBitmap(_bitmap, lockMode);
        }

        public BitmapWrapper(int width, int height, PixelFormat pixelFormat, ImageLockMode lockMode = ImageLockMode.ReadOnly)
        {
            _bitmap = new Bitmap(width, height, pixelFormat);
            initBitmap(_bitmap, lockMode);
        }

        public BitmapWrapper(Bitmap bitmap, ImageLockMode lockmode = ImageLockMode.ReadOnly)
        {
            _bitmap = bitmap;
            initBitmap(bitmap, lockmode);
        }

        private void initBitmap(Bitmap bitmap, ImageLockMode lockMode)
        {
            _width = bitmap.Width;
            _height = bitmap.Height;
            _pixelFormat = bitmap.PixelFormat;
            _lockMode = lockMode;
            createByteArray();
        }

        private void createByteArray()
        {
            _bitmapData = _bitmap.LockBits(new Rectangle(0, 0, _width, _height), _lockMode, _pixelFormat);
            _stride = _bitmapData.Stride;
            int numBytes = _stride * _height;
            _byteData = new byte[numBytes];
            IntPtr ptr = _bitmapData.Scan0;

            Marshal.Copy(ptr, _byteData, 0, numBytes);

            _bitmap.UnlockBits(_bitmapData);
        }

        public Color GetPixel(int i, int j)
        {
            // Allow tiling
            i = i % _width;
            j = j % _height;

            int bytesPerPixel = Bitmap.GetPixelFormatSize(_pixelFormat) / 8;

            int pixelIndex = (j * _stride) + (i * bytesPerPixel);

            byte a, r, g, b;
            switch(_pixelFormat)
            {
                case PixelFormat.Format24bppRgb:
                case PixelFormat.Format32bppRgb:
                    {
                        b = _byteData[pixelIndex];
                        g = _byteData[pixelIndex + 1];
                        r = _byteData[pixelIndex + 2];
                        a = 0xff;
                    }
                    break;
                case PixelFormat.Format32bppArgb:
                    {
                        b = _byteData[pixelIndex];
                        g = _byteData[pixelIndex + 1];
                        r = _byteData[pixelIndex + 2];
                        a = _byteData[pixelIndex + 3];
                    }
                    break;
                default:
                    throw new NotSupportedException();
            }

            return Color.FromArgb(a, r, g, b);
        }

        public void SetPixel(int i, int j, Color color)
        {
            int bytesPerPixel = Bitmap.GetPixelFormatSize(_pixelFormat) / 8;
            int pixelIndex = (j * _stride) + (i * bytesPerPixel);

            switch(_pixelFormat)
            {
                case PixelFormat.Format32bppArgb:
                    {
                        _byteData[pixelIndex] = color.B;
                        _byteData[pixelIndex + 1] = color.G;
                        _byteData[pixelIndex + 2] = color.R;
                        _byteData[pixelIndex + 3] = color.A;
                    }
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        // i and j indicate the coordinates of the tile this data should be copied to
        // Starts from the bottom right.
        public void CopyToTile(int x, int y, BitmapWrapper bitmapToCopy)
        {
            x = _width - (x + 1) * bitmapToCopy.Width;
            y = _height - (y + 1) * bitmapToCopy.Height;

            for(int i = 0; i < bitmapToCopy.Width; i++)
            {
                for(int j = 0; j < bitmapToCopy.Height; j++)
                {
                    Color color = bitmapToCopy.GetPixel(i, j);
                    SetPixel(x + i, y + j, color);
                }
            }
        }

        public void Save(string path, int newSize = -1)
        {
            _bitmap = new Bitmap(_width, _height, _pixelFormat);
            _bitmapData = _bitmap.LockBits(new Rectangle(0, 0, _width, _height), ImageLockMode.WriteOnly, _pixelFormat);
            Marshal.Copy(_byteData, 0, _bitmapData.Scan0, _byteData.Length);
            _bitmap.UnlockBits(_bitmapData);

            if (newSize != -1)
            {
                _bitmap = new Bitmap(_bitmap, new Size(newSize, newSize));
            }

            _bitmap.Save(path, ImageFormat.Bmp);
        }

        public void Dispose()
        {
            _byteData = null;
            _bitmap.Dispose();
            _bitmap = null;
            _bitmapData = null;
        }
    }
}
