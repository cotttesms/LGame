using System;
using Microsoft.Xna.Framework;
using Loon.Core;
using Loon.Core.Timer;
using Loon.Core.Graphics.OpenGL;
using Loon.Utils;
using Loon.Core.Graphics;
using Loon.Core.Geom;
using Loon.Java;

namespace Loon.Action.Sprite.Effect
{
    public class PShadowEffect : LObject, ISprite
    {

        private LTimer timer = new LTimer(100);

        private int width, height, scaleWidth, scaleHeight, pixelCount;

        static Color[] deasilTrans, widdershinTrans;

        private Color[] nowDrawPixels, finalDrawPixels;

        private Color[] backgroundPixels, finalBackgroundPixels;

        private bool visible = true, flag = true, isDirty, isClose;

        private LPixmap image;

        private static int max_pixel = 256;

        private static int min_pixel = 0;

        private int pixelSkip = 8;

        public static PShadowEffect NewScreenEffect(String fileName)
        {
            return new PShadowEffect(fileName, ScreenUtils.ToScreenCaptureTexturePixmap(), 0, 0, LSystem.screenRect.width,
                    LSystem.screenRect.height);
        }

        public PShadowEffect(String fileName)
            : this(new LPixmap(fileName))
        {

        }

        public PShadowEffect(LPixmap img)
            : this(img, 0, 0)
        {

        }

        public PShadowEffect(String fileName, String backFile)
            : this(new LPixmap(fileName), new LPixmap(backFile), 0, 0,
                LSystem.screenRect.width, LSystem.screenRect.height)
        {

        }

        public PShadowEffect(LPixmap img, int x, int y)
            : this(img, null, x, y, img.Width, img.Height)
        {

        }

        public PShadowEffect(String fileName, int x, int y, int w, int h)
            : this(new LPixmap(fileName), null, x, y, w,
                h)
        {

        }

        public PShadowEffect(LPixmap img, LPixmap back, int x, int y)
            : this(img, back, x, y, img.Width, img.Height)
        {

        }

        public PShadowEffect(String fileName, String bacFile, int x, int y, int w,
                int h)
            : this(new LPixmap(fileName), new LPixmap(bacFile), x, y, w, h)
        {

        }

        public PShadowEffect(String fileName, LPixmap back, int x, int y, int w,
                int h)
            : this(new LPixmap(fileName), back, x, y, w,
                h)
        {

        }

        private PixelThread pixelThread;

        public PShadowEffect(LPixmap img, LPixmap back, int x, int y, int w, int h)
        {
            if (deasilTrans == null || widdershinTrans == null)
            {
                deasilTrans = new Color[max_pixel];
                for (int i = 0; i < max_pixel; i++)
                {
                    deasilTrans[i] = new Color(i, i, i);
                }
                int count = 0;
                widdershinTrans = new Color[max_pixel];
                for (int i = 0; i < max_pixel; i++)
                {
                    widdershinTrans[count++] = deasilTrans[i];
                }
            }
            this.SetLocation(x, y);
            this.width = w;
            this.height = h;
            this.visible = true;
            LPixmap temp = null;
            if (back == null)
            {
                this.scaleWidth = width / 2;
                this.scaleHeight = height / 2;
                temp = GraphicsUtils.GetResize(img, scaleWidth, scaleHeight);
                this.image = new LPixmap(scaleWidth, scaleHeight, true);
                this.finalDrawPixels = temp.GetData();
                this.nowDrawPixels = (Color[])CollectionUtils.CopyOf(finalDrawPixels);
                if (temp != null)
                {
                    temp.Dispose();
                    temp = null;
                }
            }
            else
            {
                this.scaleWidth = width / 2;
                this.scaleHeight = height / 2;
                temp = GraphicsUtils.GetResize(img, scaleWidth, scaleHeight);
                this.image = new LPixmap(scaleWidth, scaleHeight, true);
                if (back.GetWidth() == scaleWidth
                        && back.GetHeight() == scaleHeight)
                {
                    this.finalBackgroundPixels = back.GetData();
                    this.backgroundPixels = (Color[])CollectionUtils
                            .CopyOf(finalBackgroundPixels);
                }
                else
                {
                    LPixmap tmp = GraphicsUtils.GetResize(back, scaleWidth,
                            scaleHeight);
                    this.finalBackgroundPixels = tmp.GetData();
                    if (tmp != null)
                    {
                        tmp.Dispose();
                        tmp = null;
                    }
                    this.backgroundPixels = (Color[])CollectionUtils
                            .CopyOf(finalBackgroundPixels);
                }
                this.finalDrawPixels = temp.GetData();
                this.nowDrawPixels = (Color[])CollectionUtils.CopyOf(finalDrawPixels);
            }
            this.SetBlackToWhite(flag);
            if (temp != null)
            {
                temp.Dispose();
                temp = null;
            }
            if (img != null)
            {
                img.Dispose();
                img = null;
            }
            if (back != null)
            {
                back.Dispose();
                back = null;
            }
        }

        public void Reset()
        {
            if (isClose)
            {
                return;
            }
            if (flag)
            {
                pixelCount = min_pixel;
            }
            else
            {
                pixelCount = max_pixel;
            }
            this.visible = true;
            this.nowDrawPixels = (Color[])CollectionUtils.CopyOf(finalDrawPixels);
            this.backgroundPixels = (Color[])CollectionUtils.CopyOf(finalBackgroundPixels);
            this.StartUsePixelThread();
        }

        private static void _Update(int pixelStart, int pixelEnd,
                 Color[] src, Color[] dst, Color[] colors)
        {
            int length = src.Length;
            if (pixelStart < pixelEnd)
            {
                int start = pixelStart + 1;
                int end = pixelEnd + 1;
                if (end > max_pixel)
                {
                    return;
                }
                for (int i = 0; i < length; i++)
                {
                    if (dst[i].PackedValue != LSystem.TRANSPARENT)
                    {
                        for (int pixIndex = start; pixIndex < end; pixIndex++)
                        {
                            if (colors[pixIndex] == src[i])
                            {
                                dst[i].PackedValue = LSystem.TRANSPARENT;
                            }
                            else if (src[i].PackedValue == Color.Black.PackedValue)
                            {
                                dst[i].PackedValue = LSystem.TRANSPARENT;
                            }
                        }
                    }
                }
            }
            else
            {
                int start = pixelEnd - 1;
                int end = pixelStart;
                if (start < 0)
                {
                    return;
                }
                for (int i = 0; i < length; i++)
                {
                    if (dst[i].PackedValue != LSystem.TRANSPARENT)
                    {
                        for (int pixIndex = start; pixIndex < end; pixIndex++)
                        {
                            if (colors[pixIndex] == src[i])
                            {
                                dst[i].PackedValue = LSystem.TRANSPARENT;
                            }
                            else if (src[i].PackedValue == Color.White.PackedValue)
                            {
                                dst[i].PackedValue = LSystem.TRANSPARENT;
                            }
                        }
                    }
                }
            }
        }

        public void CreateUI(GLEx g)
        {
            if (isClose)
            {
                return;
            }
            if (!visible)
            {
                return;
            }
             lock (image)
            {
         
                if (alpha > 0 && alpha < 1)
                {
                    g.SetAlpha(alpha);
                }
                if (!IsComplete() && isDirty)
                {
                    g.DrawTexture(image.Texture, x, y, width, height);
                    isDirty = false;
                }
                else if (!IsComplete())
                {
                    g.DrawTexture(image.Texture, x, y, width, height);
                }
                if (alpha > 0 && alpha < 1)
                {
                    g.SetAlpha(1f);
                }
            }
        }

        private long elapsed;

        public override void Update(long elapsedTime)
        {
            this.elapsed = elapsedTime;
        }

        private class PixelThread : Thread
        {

            PShadowEffect ps;

            public PixelThread(PShadowEffect effect)
            {
                this.ps = effect;
            }

            public override void Run()
            {
                for (; !ps.isClose && !ps.IsComplete(); )
                {
                    if (ps.visible && ps.timer.Action(ps.elapsed))
                    {
                        if (ps.backgroundPixels == null)
                        {
                            if (ps.flag)
                            {
                                _Update(ps.pixelCount, ps.pixelCount += ps.pixelSkip,
                                        ps.finalDrawPixels, ps.nowDrawPixels,
                                        widdershinTrans);
                            }
                            else
                            {
                                _Update(ps.pixelCount, ps.pixelCount -= ps.pixelSkip,
                                        ps.finalDrawPixels, ps.nowDrawPixels, deasilTrans);
                            }
                    
                            ps.image.SetData(ps.nowDrawPixels);
                        }
                        else
                        {
                            if (ps.flag)
                            {
                                _Update(ps.pixelCount, ps.pixelCount += ps.pixelSkip,
                                        ps.finalDrawPixels, ps.backgroundPixels,
                                        widdershinTrans);
                            }
                            else
                            {
                                _Update(ps.pixelCount, ps.pixelCount -= ps.pixelSkip,
                                        ps.finalDrawPixels, ps.backgroundPixels,
                                        deasilTrans);
                            }
                            ps.image.SetData(ps.backgroundPixels);
                        }
                        ps.isDirty = true;
                    }
                }
            }
        }

        public bool IsVisible()
        {
            return visible;
        }

        public void SetVisible(bool visible)
        {
            this.visible = visible;
        }

        void StartUsePixelThread()
        {
            if (pixelThread == null)
            {
                pixelThread = new PixelThread(this);
                pixelThread.Start();
            }
        }

        void EndUsePixelThread()
        {
            if (pixelThread != null)
            {
                try
                {
                    pixelThread.Interrupt();
                    pixelThread = null;
                }
                catch (Exception)
                {
                    pixelThread = null;
                }
            }
        }

        public bool IsComplete()
        {
            bool stop = flag ? (pixelCount > max_pixel)
                   : (pixelCount < min_pixel);
            if (!stop)
            {
                StartUsePixelThread();
            }
            else
            {
                EndUsePixelThread();
            }
            return stop;
        }

        public void SetDelay(long delay)
        {
            timer.SetDelay(delay);
        }

        public long GetDelay()
        {
            return timer.GetDelay();
        }

        public override int GetHeight()
        {
            return height;
        }

        public override int GetWidth()
        {
            return width;
        }

        public bool IsBlackToWhite()
        {
            return flag;
        }

        public void SetBlackToWhite(bool flag)
        {
            this.flag = flag;
            if (flag)
            {
                pixelCount = min_pixel;
            }
            else
            {
                pixelCount = max_pixel;
            }
        }

        public RectBox GetCollisionBox()
        {
            return GetRect(x, y, width, height);
        }

        public LTexture GetBitmap()
        {
            return image.Texture;
        }

        public bool IsClose()
        {
            return isClose;
        }

        public int GetPixelCount()
        {
            return pixelCount;
        }

        public int GetPixelSkip()
        {
            return pixelSkip;
        }

        public void SetPixelSkip(int pixelSkip)
        {
            this.pixelSkip = pixelSkip;
        }

        public void Dispose()
        {
            this.isClose = true;
            this.EndUsePixelThread();
            this.finalDrawPixels = null;
            this.nowDrawPixels = null;
            if (image != null)
            {
                image.Dispose();
                image = null;
            }
        }

    }
}
