param([string]$Src = (Join-Path $PSScriptRoot "../Sprites/CoinFace.png"),
      [string]$Dst = (Join-Path $PSScriptRoot "../Sprites/CoinFlip.png"), [int]$Cols = 4, [int]$Rows = 4, [int]$Cell = 144,
      [double]$PitchDeg = 0, [double]$ThickRatio = 0.2197, [int]$Notches = 0, [int]$SS = 3,
      [double]$DepthX = -0.12, [double]$DepthY = 0.32)
Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @"
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

public static class CoinRender
{
    static byte[] src; static int sw, sh;
    static double texCx, texCy, texR;

    // EN: Render a seamless upright turn with circular frontal faces and a continuous solid silhouette and a gold edge visible below the circular frontal face using oblique depth projection.
    // VI: Dựng vòng xoay thẳng liền mạch với mặt chính diện tròn và đường bao đặc liên tục và cạnh vàng lộ dưới mặt chính diện tròn bằng phép chiếu chiều sâu xiên.
    public static void Render(string srcPath, string dstPath, int cols, int rows, int cell,
                              double pitchDeg, double thickRatio, int notches, int ss, double depthX, double depthY)
    {
        Bitmap s = LoadArgb(srcPath, out src); sw = s.Width; sh = s.Height; s.Dispose();
        MeasureCoin();

        int W = cols * cell, H = rows * cell;
        byte[] dst = new byte[W * H * 4];
        double R = 1.0, T = thickRatio;
        double scale = texR;                          // pixels per unit radius
        double pitch = -pitchDeg * Math.PI / 180.0;   // negative: look slightly down onto the coin
        double[] L = Norm(-0.45, 0.65, 0.62);
        double[] Hh = Norm(L[0], L[1], L[2] + 1);
        double[] c4 = new double[4];
        int frames = cols * rows;

        for (int f = 0; f < frames; f++)
        {
            double yaw = 2 * Math.PI * f / frames;    // full turn: with a tilted view only 360 degrees loops seamlessly
            double cy = Math.Cos(yaw), sy = Math.Sin(yaw), cp = Math.Cos(pitch), sp = Math.Sin(pitch);
            // World = Rx(pitch) * Ry(yaw) * local. Rows of the matrix:
            double m00 = cy,      m01 = 0,   m02 = sy;
            double m10 = sp * sy, m11 = cp,  m12 = -sp * cy;
            double m20 = -cp * sy, m21 = sp, m22 = cp * cy;
            int ox = (f % cols) * cell, oy = (f / cols) * cell;

            for (int py = 0; py < cell; py++)
                for (int px = 0; px < cell; px++)
                {
                    double ab = 0, ag = 0, ar = 0, aa = 0;
                    for (int qy = 0; qy < ss; qy++)
                        for (int qx = 0; qx < ss; qx++)
                        {
                            double wx = (px + (qx + 0.5) / ss - cell / 2.0) / scale;
                            double wy = -(py + (qy + 0.5) / ss - cell / 2.0) / scale;
                            // Oblique depth keeps the front face circular while exposing the lower edge.
                            double oxw = wx - depthX * 5, oyw = wy - depthY * 5, ozw = 5;
                            double lox = m00 * oxw + m10 * oyw + m20 * ozw;
                            double loy = m01 * oxw + m11 * oyw + m21 * ozw;
                            double loz = m02 * oxw + m12 * oyw + m22 * ozw;
                            double ldx = m00 * depthX + m10 * depthY - m20;
                            double ldy = m01 * depthX + m11 * depthY - m21;
                            double ldz = m02 * depthX + m12 * depthY - m22;

                            double best = double.MaxValue; int hit = 0; double hx = 0, hy = 0, hz = 0;
                            if (Math.Abs(ldz) > 1e-9)
                                for (int side = -1; side <= 1; side += 2)
                                {
                                    double t = (side * T - loz) / ldz;
                                    double x = lox + ldx * t, y = loy + ldy * t;
                                    if (t > 0 && t < best && x * x + y * y <= R * R) { best = t; hit = side; hx = x; hy = y; hz = side * T; }
                                }
                            double qa = ldx * ldx + ldy * ldy;
                            if (qa > 1e-12)
                            {
                                double qb = 2 * (lox * ldx + loy * ldy), qc = lox * lox + loy * loy - R * R;
                                double disc = qb * qb - 4 * qa * qc;
                                if (disc >= 0)
                                {
                                    double t = (-qb - Math.Sqrt(disc)) / (2 * qa);
                                    double z = loz + ldz * t;
                                    if (t > 0 && t < best && Math.Abs(z) <= T) { best = t; hit = 2; hx = lox + ldx * t; hy = loy + ldy * t; hz = z; }
                                }
                            }
                            if (hit == 0) continue;

                            double nx, ny, nz, cb, cg, cr, ca;
                            if (hit == 2)
                            {
                                double ang = Math.Atan2(hy, hx);
                                double ridge = notches > 0 ? Math.Cos(ang * notches) : 0;
                                // Smooth bevel shading near both faces, without periodic bands.
                                double depth = hz / T;
                                double bevel = Math.Max(0, (Math.Abs(depth) - 0.65) / 0.35);
                                bevel = bevel * bevel * (3 - 2 * bevel);
                                nx = hx + 0.18 * ridge * -Math.Sin(ang);
                                ny = hy + 0.18 * ridge * Math.Cos(ang);
                                nz = Math.Sign(depth) * 0.85 * bevel;
                                double centreHighlight = Math.Exp(-Math.Pow((depth + 0.18) / 0.50, 2));
                                double edgeShade = 1 - 0.10 * bevel;
                                cb = (45 + 56 * centreHighlight + 8 * ridge) * edgeShade;
                                cg = (188 + 34 * centreHighlight + 6 * ridge) * edgeShade;
                                cr = 255; ca = 1;
                            }
                            else
                            {
                                nx = 0; ny = 0; nz = hit;
                                double u = hit > 0 ? hx : -hx;  // mirror the back face so it reads the same way
                                Sample(texCx + u * scale - 0.5, texCy - hy * scale - 0.5, c4);
                                // Geometry owns opacity; composite the face over solid gold to seal the cap/edge seam.
                                double uncovered = 1 - c4[3];
                                cb = c4[0] + 54 * uncovered;
                                cg = c4[1] + 194 * uncovered;
                                cr = c4[2] + 255 * uncovered;
                                ca = 1;
                            }
                            // Normal to world space.
                            double wnx = m00 * nx + m01 * ny + m02 * nz;
                            double wny = m10 * nx + m11 * ny + m12 * nz;
                            double wnz = m20 * nx + m21 * ny + m22 * nz;
                            double nl = Math.Sqrt(wnx * wnx + wny * wny + wnz * wnz); wnx /= nl; wny /= nl; wnz /= nl;
                            double diff = Math.Max(0, wnx * L[0] + wny * L[1] + wnz * L[2]);
                            double light = hit == 2 ? 0.88 + 0.22 * diff : 0.90 + 0.18 * diff;
                            double spec = Math.Pow(Math.Max(0, wnx * Hh[0] + wny * Hh[1] + wnz * Hh[2]), hit == 2 ? 18 : 60) * (hit == 2 ? 0.4 : 0.22);
                            // Warm highlight (channels are B, G, R).
                            double b = Math.Min(255, cb * light + 170 * spec);
                            double g = Math.Min(255, cg * light + 235 * spec);
                            double r = Math.Min(255, cr * light + 255 * spec);
                            ab += b * ca; ag += g * ca; ar += r * ca; aa += ca;
                        }
                    if (aa <= 0) continue;
                    int n = ss * ss;
                    int k = ((oy + py) * W + ox + px) * 4;
                    dst[k] = (byte)Math.Round(ab / aa); dst[k + 1] = (byte)Math.Round(ag / aa); dst[k + 2] = (byte)Math.Round(ar / aa);
                    dst[k + 3] = (byte)Math.Round(255 * aa / n);
                }
        }

        Bitmap outBmp = new Bitmap(W, H, PixelFormat.Format32bppArgb);
        BitmapData od = outBmp.LockBits(new Rectangle(0, 0, W, H), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
        Marshal.Copy(dst, 0, od.Scan0, dst.Length);
        outBmp.UnlockBits(od);
        outBmp.Save(dstPath, ImageFormat.Png);
        outBmp.Dispose();
    }

    // EN: Loads the source face into a consistent BGRA buffer for texture sampling.
    // VI: Nạp mặt xu gốc thành bộ đệm BGRA thống nhất để lấy mẫu texture.
    static Bitmap LoadArgb(string path, out byte[] data)
    {
        Bitmap b = new Bitmap(path);
        Bitmap c = new Bitmap(b.Width, b.Height, PixelFormat.Format32bppArgb);
        using (Graphics g = Graphics.FromImage(c)) g.DrawImage(b, 0, 0, b.Width, b.Height);
        b.Dispose();
        BitmapData bd = c.LockBits(new Rectangle(0, 0, c.Width, c.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        data = new byte[bd.Stride * c.Height];
        Marshal.Copy(bd.Scan0, data, 0, data.Length);
        c.UnlockBits(bd);
        return c;
    }

    // EN: Measures the source coin centre and radius from its visible alpha silhouette.
    // VI: Đo tâm và bán kính đồng xu gốc theo hình bao alpha nhìn thấy.
    static void MeasureCoin()
    {
        double sx = 0, sy = 0, n = 0; int minX = sw, maxX = 0;
        for (int y = 0; y < sh; y++)
            for (int x = 0; x < sw; x++)
                if (src[(y * sw + x) * 4 + 3] > 128) { sx += x; sy += y; n++; if (x < minX) minX = x; if (x > maxX) maxX = x; }
        texCx = sx / n + 0.5; texCy = sy / n + 0.5; texR = (maxX - minX + 1) / 2.0 - 0.5;
    }

    // EN: Normalizes a lighting vector for consistent diffuse and specular shading.
    // VI: Chuẩn hóa vector ánh sáng để tính màu khuếch tán và phản chiếu nhất quán.
    static double[] Norm(double x, double y, double z) { double l = Math.Sqrt(x * x + y * y + z * z); return new double[] { x / l, y / l, z / l }; }

    // EN: Samples premultiplied BGRA bilinearly to preserve clean transparent edges.
    // VI: Lấy mẫu BGRA nội suy song tuyến tính có nhân alpha để giữ mép trong suốt sạch.
    static void Sample(double x, double y, double[] o)
    {
        o[0] = o[1] = o[2] = o[3] = 0;
        int x0 = (int)Math.Floor(x), y0 = (int)Math.Floor(y);
        double fx = x - x0, fy = y - y0;
        for (int j = 0; j < 2; j++)
            for (int i = 0; i < 2; i++)
            {
                int px = x0 + i, py = y0 + j;
                if (px < 0 || py < 0 || px >= sw || py >= sh) continue;
                double w = (i == 0 ? 1 - fx : fx) * (j == 0 ? 1 - fy : fy);
                int k = (py * sw + px) * 4;
                double a = src[k + 3] / 255.0;
                o[0] += src[k] * a * w; o[1] += src[k + 1] * a * w; o[2] += src[k + 2] * a * w; o[3] += a * w;
            }
    }
}
"@
[CoinRender]::Render($Src, $Dst, $Cols, $Rows, $Cell, $PitchDeg, $ThickRatio, $Notches, $SS, $DepthX, $DepthY)
"saved $Dst"
