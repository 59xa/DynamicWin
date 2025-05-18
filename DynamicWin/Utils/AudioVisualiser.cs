using DynamicWin.Main;
using DynamicWin.UI;
using NAudio.Wave;
using SkiaSharp;
using System.Numerics;

/*
*   Overview:
*    - Implement a new audio visualiser that aims to look closely similar to iOS audio visualisers.
*    
*   Author:                 Megan Park
*   GitHub:                 https://github.com/59xa
*   Implementation Date:    18 May 2024
*   Last Modified:          18 May 2024 08:53 KST (UTC+9)
*   
*/

namespace DynamicWin.Utils
{
    public class AudioVisualiser : UIObject
    {
        private const int V = 5;
        private readonly int fftLength = 2048;
        private readonly int barCount = 6;

        private float[] fftMagnitudes;
        private float[] barHeight;
        private float[] barMax;
        private readonly float[] barBias = new float[] { 1f, 0.6f, 0.9f, 1f, 1f, 2f };

        private WasapiLoopbackCapture capture;
        private readonly object fftLock = new object();

        public float barUpSmoothing = 20;
        public float barDownSmoothing = 10f;
        private float maxDecay = 0.90f;

        public Col Primary;
        public Col Secondary;

        private float averageAmplitude = 0f;
        public float AverageAmplitude { get => averageAmplitude; }

        private bool enableColourTransition = true;
        public bool EnableColourTransition { get => enableColourTransition; set => enableColourTransition = value; }

        private bool enableDotWhenLow = true;
        public bool EnableDotWhenLow { get => enableDotWhenLow; set => enableDotWhenLow = value; }
        public float BlurAmount { get; set; } = 0f;

        public float BarSpacing { get; set; } = 1f;

        public AudioVisualiser(UIObject? parent, Vec2 position, Vec2 size, UIAlignment alignment = UIAlignment.TopRight, Col Primary = null, Col Secondary = null) : base(parent, position, size, alignment)
        {
            roundRadius = V;

            this.Primary = Primary ?? Theme.Primary;
            this.Secondary = Secondary ?? Theme.Secondary.Override(a: 0.5f);

            fftMagnitudes = new float[fftLength / 2];
            barHeight = new float[barCount];
            barMax = new float[barCount];
            for (int i = 0; i < barCount; i++) barMax[i] = 0f;

            if (DynamicWinMain.defaultDevice != null)
            {
                capture = new WasapiLoopbackCapture(DynamicWinMain.defaultDevice);
                capture.DataAvailable += OnDataAvailable;
                capture.StartRecording();
            }
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            try
            {
                if (capture != null)
                {
                    capture.DataAvailable -= OnDataAvailable;
                    capture.StopRecording();
                    capture.Dispose();
                }
            }
            catch (ThreadInterruptedException) { }
        }

        public override void Update(float deltaTime)
        {
            base.Update(deltaTime);

            lock (fftLock)
            {
                float[] targetHeights = new float[barCount];
                float minFreq = 20f;
                float maxFreq = 5000f;
                float sampleRate = capture?.WaveFormat.SampleRate ?? 44100f;

                for (int i = 0; i < barCount; i++)
                {
                    float lowFreq = (float)(minFreq * Math.Pow(maxFreq / minFreq, (i + 0f) / barCount));
                    float highFreq = (float)(minFreq * Math.Pow(maxFreq / minFreq, (i + 1f) / barCount));

                    int lowIndex = (int)(lowFreq / sampleRate * fftLength);
                    int highIndex = Math.Min((int)(highFreq / sampleRate * fftLength), fftMagnitudes.Length - 1);

                    float avg = 0f;
                    int count = 0;
                    for (int j = lowIndex; j <= highIndex; j++)
                    {
                        avg += fftMagnitudes[j];
                        count++;
                    }

                    avg = (count > 0) ? avg / count : 0f;
                    if (avg < 0.001f) avg = 0f;

                    float compensation = 1.5f - i * 0.1f;
                    targetHeights[i] = avg * compensation;
                }

                float minThreshold = 0.005f;

                for (int i = 0; i < barCount; i++)
                {
                    if (targetHeights[i] < minThreshold)
                    {
                        targetHeights[i] = 0f;
                        continue;
                    }

                    if (targetHeights[i] > barMax[i] * 0.7f)
                    {
                        barMax[i] = targetHeights[i];
                    }
                    else
                    {
                        barMax[i] *= maxDecay;
                        barMax[i] = Math.Max(barMax[i], minThreshold);
                    }

                    float normalized = targetHeights[i] / barMax[i];
                    targetHeights[i] = Math.Clamp(normalized, 0f, 1f);
                    targetHeights[i] *= barBias[i];
                    targetHeights[i] = Math.Clamp(targetHeights[i], 0f, 1f);
                }

                averageAmplitude = targetHeights.Average();

                for (int i = 0; i < barCount; i++)
                {
                    float smoothing = targetHeights[i] > barHeight[i]
                        ? barUpSmoothing * (1f + i * 0.1f)
                        : barDownSmoothing * (1f + i * 0.1f);

                    float t = 1 - (float)Math.Exp(-smoothing * deltaTime);
                    barHeight[i] = Lerp(barHeight[i], targetHeights[i], t);
                }
            }
        }

        private void OnDataAvailable(object? sender, WaveInEventArgs e)
        {
            var buffer = new float[e.BytesRecorded / 4];
            Buffer.BlockCopy(e.Buffer, 0, buffer, 0, e.BytesRecorded);

            var samples = new Complex[fftLength];
            for (int i = 0; i < fftLength && i < buffer.Length; i++)
            {
                float window = 0.5f * (1 - MathF.Cos(2 * MathF.PI * i / (fftLength - 1)));
                samples[i] = new Complex(buffer[i] * window, 0);
            }

            FFT(samples);

            lock (fftLock)
            {
                for (int i = 0; i < fftMagnitudes.Length; i++)
                {
                    fftMagnitudes[i] = (float)samples[i].Magnitude;
                }
            }
        }

        private void FFT(Complex[] buffer)
        {
            int n = buffer.Length;
            int m = (int)Math.Log2(n);

            for (int i = 0; i < n; i++)
            {
                int j = BitReverse(i, m);
                if (j > i)
                {
                    (buffer[i], buffer[j]) = (buffer[j], buffer[i]);
                }
            }

            for (int s = 1; s <= m; s++)
            {
                int mval = 1 << s;
                int mval2 = mval >> 1;
                Complex wm = Complex.FromPolarCoordinates(1, -2 * Math.PI / mval);

                for (int k = 0; k < n; k += mval)
                {
                    Complex w = Complex.One;
                    for (int j = 0; j < mval2; j++)
                    {
                        Complex t = w * buffer[k + j + mval2];
                        Complex u = buffer[k + j];
                        buffer[k + j] = u + t;
                        buffer[k + j + mval2] = u - t;
                        w *= wm;
                    }
                }
            }
        }

        private int BitReverse(int n, int bits)
        {
            int reversed = 0;
            for (int i = 0; i < bits; i++)
            {
                reversed = (reversed << 1) | (n & 1);
                n >>= 1;
            }
            return reversed;
        }

        public override void Draw(SKCanvas canvas)
        {
            if (capture == null) return;

            float width = Size.X;
            float height = Size.Y;
            float centerY = Position.Y + height / 2;

            float spacing = BarSpacing;
            float totalSpacing = spacing * (barCount - 1);
            float barWidth = (width - totalSpacing) / barCount;
            float visualBoost = 1.5f;
            float dotHeight = barWidth;

            for (int i = 0; i < barCount; i++)
            {
                float rawHeight = barHeight[i] * visualBoost;
                bool isDot = enableDotWhenLow && rawHeight < 0.05f;
                float bH = isDot ? dotHeight : rawHeight * height * 0.8f;

                float x = Position.X + i * (barWidth + spacing);
                float barTopY = centerY - bH / 2;

                var rect = SKRect.Create(x, barTopY, barWidth, bH);
                var roundRect = new SKRoundRect(rect, barWidth / 2, barWidth / 2);

                float lerpAmount = isDot ? 0.2f : barHeight[i];
                Col pCol = EnableColourTransition
                    ? Col.Lerp(Secondary, Primary, lerpAmount)
                    : Primary;

                SKColor baseColor = GetColor(pCol).Value();

                SKColor startColor = baseColor.WithAlpha((byte)(baseColor.Alpha * lerpAmount));
                SKColor endColor = new SKColor(
                    (byte)(baseColor.Red * 0.7),
                    (byte)(baseColor.Green * 0.7),
                    (byte)(baseColor.Blue * 0.7),
                    (byte)(baseColor.Alpha * lerpAmount)
                );

                using var paint = new SKPaint
                {
                    IsAntialias = true,
                    Shader = SKShader.CreateLinearGradient(
                        new SKPoint(rect.Left, rect.Bottom),
                        new SKPoint(rect.Left, rect.Top),
                        new[] { startColor, endColor },
                        new float[] { 0, 1 },
                        SKShaderTileMode.Clamp
                    ),
                };

                if (Settings.AllowBlur)
                {
                    paint.MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, BlurAmount);
                }

                canvas.DrawRoundRect(roundRect, paint);
            }
        }

        public Col GetActionCol()
        {
            return Col.Lerp(Secondary, Primary, averageAmplitude * 2);
        }

        public Col GetInverseActionCol()
        {
            return Col.Lerp(Primary, Secondary, averageAmplitude * 2);
        }

        private float Lerp(float a, float b, float t)
        {
            return a + (b - a) * t;
        }
    }
}
