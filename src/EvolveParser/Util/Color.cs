// Decompiled with JetBrains decompiler
// Type: UnityEngine.Color
// Assembly: UnityEngine.CoreModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 540F8033-861F-4606-8EDD-63D729470AEE
// Assembly location: C:\Program Files\Unity\Hub\Editor\2023.1.10f1\Editor\Data\PlaybackEngines\WindowsStandaloneSupport\Variations\mono\Managed\UnityEngine.CoreModule.dll

using System;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace EvolveUI {

    public struct Color : IEquatable<Color>, IFormattable {

        public float r;
        public float g;
        public float b;
        public float a;

        public Color(float r, float g, float b, float a) {
            this.r = r;
            this.g = g;
            this.b = b;
            this.a = a;
        }

        public Color(float r, float g, float b) {
            this.r = r;
            this.g = g;
            this.b = b;
            this.a = 1f;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string ToString() => this.ToString((string)null, (IFormatProvider)null);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ToString(string format) => this.ToString(format, (IFormatProvider)null);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ToString(string format, IFormatProvider formatProvider) {
            if (string.IsNullOrEmpty(format))
                format = "F3";
            if (formatProvider == null)
                formatProvider = (IFormatProvider)CultureInfo.InvariantCulture.NumberFormat;
            return string.Format("RGBA({0}, {1}, {2}, {3})", (object)this.r.ToString(format, formatProvider), (object)this.g.ToString(format, formatProvider), (object)this.b.ToString(format, formatProvider), (object)this.a.ToString(format, formatProvider));
        }

        public override int GetHashCode() => this.r.GetHashCode() ^ this.g.GetHashCode() << 2 ^ this.b.GetHashCode() >> 2 ^ this.a.GetHashCode() >> 1;

        public override bool Equals(object other) => other is Color other1 && this.Equals(other1);

        public bool Equals(Color other) => this.r.Equals(other.r) && this.g.Equals(other.g) && this.b.Equals(other.b) && this.a.Equals(other.a);

        public static Color operator +(Color a, Color b) => new Color(a.r + b.r, a.g + b.g, a.b + b.b, a.a + b.a);

        public static Color operator -(Color a, Color b) => new Color(a.r - b.r, a.g - b.g, a.b - b.b, a.a - b.a);

        public static Color operator *(Color a, Color b) => new Color(a.r * b.r, a.g * b.g, a.b * b.b, a.a * b.a);

        public static Color operator *(Color a, float b) => new Color(a.r * b, a.g * b, a.b * b, a.a * b);

        public static Color operator *(float b, Color a) => new Color(a.r * b, a.g * b, a.b * b, a.a * b);

        public static Color operator /(Color a, float b) => new Color(a.r / b, a.g / b, a.b / b, a.a / b);

        public static bool operator ==(Color lhs, Color rhs) => lhs.r == rhs.r && lhs.g == rhs.g && lhs.b == rhs.b && lhs.a == rhs.a;

        public static bool operator !=(Color lhs, Color rhs) => !(lhs == rhs);

        public static Color Lerp(Color a, Color b, float t) {
            t = Math.Clamp(t, 0f, 1f);
            return new Color(a.r + (b.r - a.r) * t, a.g + (b.g - a.g) * t, a.b + (b.b - a.b) * t, a.a + (b.a - a.a) * t);
        }

        public static Color LerpUnclamped(Color a, Color b, float t) => new Color(a.r + (b.r - a.r) * t, a.g + (b.g - a.g) * t, a.b + (b.b - a.b) * t, a.a + (b.a - a.a) * t);

        internal Color RGBMultiplied(float multiplier) => new Color(this.r * multiplier, this.g * multiplier, this.b * multiplier, this.a);

        internal Color AlphaMultiplied(float multiplier) => new Color(this.r, this.g, this.b, this.a * multiplier);

        internal Color RGBMultiplied(Color multiplier) => new Color(this.r * multiplier.r, this.g * multiplier.g, this.b * multiplier.b, this.a);

        public static Color red {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new Color(1f, 0.0f, 0.0f, 1f);
        }

        public static Color green {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new Color(0.0f, 1f, 0.0f, 1f);
        }

        public static Color blue {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new Color(0.0f, 0.0f, 1f, 1f);
        }

        public static Color white {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new Color(1f, 1f, 1f, 1f);
        }

        public static Color black {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new Color(0.0f, 0.0f, 0.0f, 1f);
        }

        public static Color yellow {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new Color(1f, 0.92156863f, 0.015686275f, 1f);
        }

        public static Color cyan {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new Color(0.0f, 1f, 1f, 1f);
        }

        public static Color magenta {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new Color(1f, 0.0f, 1f, 1f);
        }

        public static Color gray {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new Color(0.5f, 0.5f, 0.5f, 1f);
        }

        public static Color grey {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new Color(0.5f, 0.5f, 0.5f, 1f);
        }

        public static Color clear {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new Color(0.0f, 0.0f, 0.0f, 0.0f);
        }

        public float grayscale {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (float)(0.29899999499320984 * (double)this.r + 0.5870000123977661 * (double)this.g + 57.0 / 500.0 * (double)this.b);
        }

        public float this[int index] {
            get {
                switch (index) {
                    case 0:
                        return this.r;

                    case 1:
                        return this.g;

                    case 2:
                        return this.b;

                    case 3:
                        return this.a;

                    default:
                        throw new IndexOutOfRangeException("Invalid Color index(" + index.ToString() + ")!");
                }
            }
            set {
                switch (index) {
                    case 0:
                        this.r = value;
                        break;

                    case 1:
                        this.g = value;
                        break;

                    case 2:
                        this.b = value;
                        break;

                    case 3:
                        this.a = value;
                        break;

                    default:
                        throw new IndexOutOfRangeException("Invalid Color index(" + index.ToString() + ")!");
                }
            }
        }

        public static void RGBToHSV(Color rgbColor, out float H, out float S, out float V) {
            if ((double)rgbColor.b > (double)rgbColor.g && (double)rgbColor.b > (double)rgbColor.r)
                Color.RGBToHSVHelper(4f, rgbColor.b, rgbColor.r, rgbColor.g, out H, out S, out V);
            else if ((double)rgbColor.g > (double)rgbColor.r)
                Color.RGBToHSVHelper(2f, rgbColor.g, rgbColor.b, rgbColor.r, out H, out S, out V);
            else
                Color.RGBToHSVHelper(0.0f, rgbColor.r, rgbColor.g, rgbColor.b, out H, out S, out V);
        }

        private static void RGBToHSVHelper(
            float offset,
            float dominantcolor,
            float colorone,
            float colortwo,
            out float H,
            out float S,
            out float V) {
            V = dominantcolor;
            if ((double)V != 0.0) {
                float num1 = (double)colorone <= (double)colortwo ? colorone : colortwo;
                float num2 = V - num1;
                if ((double)num2 != 0.0) {
                    S = num2 / V;
                    H = offset + (colorone - colortwo) / num2;
                }
                else {
                    S = 0.0f;
                    H = offset + (colorone - colortwo);
                }

                H /= 6f;
                if ((double)H >= 0.0)
                    return;
                ++H;
            }
            else {
                S = 0.0f;
                H = 0.0f;
            }
        }

        public static Color HSVToRGB(float H, float S, float V) => Color.HSVToRGB(H, S, V, true);

        public static Color HSVToRGB(float H, float S, float V, bool hdr) {
            Color white = Color.white;
            if ((double)S == 0.0) {
                white.r = V;
                white.g = V;
                white.b = V;
            }
            else if ((double)V == 0.0) {
                white.r = 0.0f;
                white.g = 0.0f;
                white.b = 0.0f;
            }
            else {
                white.r = 0.0f;
                white.g = 0.0f;
                white.b = 0.0f;
                float num1 = S;
                float num2 = V;
                float f = H * 6f;
                int num3 = (int)Math.Floor(f);
                float num4 = f - (float)num3;
                float num5 = num2 * (1f - num1);
                float num6 = num2 * (float)(1.0 - (double)num1 * (double)num4);
                float num7 = num2 * (float)(1.0 - (double)num1 * (1.0 - (double)num4));
                switch (num3) {
                    case -1:
                        white.r = num2;
                        white.g = num5;
                        white.b = num6;
                        break;

                    case 0:
                        white.r = num2;
                        white.g = num7;
                        white.b = num5;
                        break;

                    case 1:
                        white.r = num6;
                        white.g = num2;
                        white.b = num5;
                        break;

                    case 2:
                        white.r = num5;
                        white.g = num2;
                        white.b = num7;
                        break;

                    case 3:
                        white.r = num5;
                        white.g = num6;
                        white.b = num2;
                        break;

                    case 4:
                        white.r = num7;
                        white.g = num5;
                        white.b = num2;
                        break;

                    case 5:
                        white.r = num2;
                        white.g = num5;
                        white.b = num6;
                        break;

                    case 6:
                        white.r = num2;
                        white.g = num7;
                        white.b = num5;
                        break;
                }

                if (!hdr) {
                    white.r = Math.Clamp(white.r, 0.0f, 1f);
                    white.g = Math.Clamp(white.g, 0.0f, 1f);
                    white.b = Math.Clamp(white.b, 0.0f, 1f);
                }
            }

            return white;
        }

    }

}