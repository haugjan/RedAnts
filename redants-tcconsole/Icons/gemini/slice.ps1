param(
    [string]$Src,
    [string]$OutDir,
    [string]$SdDir
)

Add-Type -AssemblyName System.Drawing

$code = @'
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

public class Slicer
{
    static int W, H, Stride;
    static byte[] P;
    static int BgR, BgG, BgB;

    static int Dist(int i)
    {
        int b = P[i], g = P[i + 1], r = P[i + 2];
        int dr = r - BgR, dg = g - BgG, db = b - BgB;
        return (int)Math.Sqrt(dr * dr + dg * dg + db * db);
    }

    public static string Run(string src, string outDir, string sdDir, string[] cols, int outMax, int sdSize)
    {
        StringBuilder log = new StringBuilder();
        Bitmap srcBmp = new Bitmap(src);
        W = srcBmp.Width; H = srcBmp.Height;
        Bitmap img = new Bitmap(W, H, PixelFormat.Format32bppArgb);
        using (Graphics g = Graphics.FromImage(img)) { g.DrawImage(srcBmp, 0, 0, W, H); }
        srcBmp.Dispose();

        BitmapData bd = img.LockBits(new Rectangle(0, 0, W, H), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
        Stride = bd.Stride;
        P = new byte[Stride * H];
        Marshal.Copy(bd.Scan0, P, 0, P.Length);

        BgB = P[0]; BgG = P[1]; BgR = P[2];

        // 1) Flood-Fill vom Rand
        bool[] bg = new bool[W * H];
        Stack<int> st = new Stack<int>();
        for (int x = 0; x < W; x++) { Push(st, bg, x, 0, 30); Push(st, bg, x, H - 1, 95); }
        for (int y = 0; y < H; y++) { Push(st, bg, 0, y, 95); Push(st, bg, W - 1, y, 95); }
        while (st.Count > 0)
        {
            int p = st.Pop(); int x = p % W, y = p / W;
            if (x > 0) Push(st, bg, x - 1, y, 95);
            if (x < W - 1) Push(st, bg, x + 1, y, 95);
            if (y > 0) Push(st, bg, x, y - 1, 95);
            if (y < H - 1) Push(st, bg, x, y + 1, 30);
        }

        // 2) Komponenten ohne dunkle Kontur = Schlagschatten -> weg
        bool[] seen = new bool[W * H];
        int dropped = 0;
        for (int p0 = 0; p0 < W * H; p0++)
        {
            if (bg[p0] || seen[p0]) continue;
            List<int> comp = new List<int>();
            Stack<int> s2 = new Stack<int>();
            s2.Push(p0); seen[p0] = true;
            int maxD = 0;
            while (s2.Count > 0)
            {
                int p = s2.Pop(); comp.Add(p);
                int d = Dist(Idx(p % W, p / W));
                if (d > maxD) maxD = d;
                int x = p % W, y = p / W;
                for (int dy = -1; dy <= 1; dy++)
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        int nx = x + dx, ny = y + dy;
                        if (nx < 0 || ny < 0 || nx >= W || ny >= H) continue;
                        int np = ny * W + nx;
                        if (bg[np] || seen[np]) continue;
                        seen[np] = true; s2.Push(np);
                    }
            }
            if (maxD < 140 || comp.Count < 60)
            {
                foreach (int p in comp) bg[p] = true;
                dropped++;
            }
        }
        log.AppendLine("verworfene Flaechen (Schatten/Artefakte): " + dropped);

        // 3) Alpha setzen, Randpixel weich und entmischen
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                int p = y * W + x, i = Idx(x, y);
                if (bg[p]) { P[i + 3] = 0; P[i] = 0; P[i + 1] = 0; P[i + 2] = 0; continue; }
                bool edge = false;
                for (int dy = -1; dy <= 1 && !edge; dy++)
                    for (int dx = -1; dx <= 1 && !edge; dx++)
                    {
                        int nx = x + dx, ny = y + dy;
                        if (nx < 0 || ny < 0 || nx >= W || ny >= H) continue;
                        if (bg[ny * W + nx]) edge = true;
                    }
                if (!edge) { P[i + 3] = 255; continue; }
                int d = Dist(i);
                double a = d / 200.0;
                if (a > 1) a = 1; if (a < 0.12) a = 0.12;
                P[i + 2] = Unmix(P[i + 2], BgR, a);
                P[i + 1] = Unmix(P[i + 1], BgG, a);
                P[i] = Unmix(P[i], BgB, a);
                P[i + 3] = (byte)Math.Round(a * 255);
            }

        Marshal.Copy(P, 0, bd.Scan0, P.Length);
        img.UnlockBits(bd);

        // 4) Spalten global, Zeilen je Spalte (Icons duerfen ueber zwei Zeilen gehen)
        int[] colCnt = new int[W];
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
                if (P[Idx(x, y) + 3] > 24) colCnt[x]++;
        List<int[]> colBands = Fit(colCnt, Bands(colCnt, 12, (int)(H * 0.004)), cols.Length);
        log.AppendLine("Spalten: " + colBands.Count);

        Directory.CreateDirectory(outDir);
        Directory.CreateDirectory(sdDir);

        for (int ci = 0; ci < colBands.Count; ci++)
        {
            int cx0 = colBands[ci][0], cx1 = colBands[ci][1];
            string[] names = cols[ci].Split(',');

            int[] rowCnt = new int[H];
            for (int y = 0; y < H; y++)
                for (int x = cx0; x <= cx1; x++)
                    if (P[Idx(x, y) + 3] > 24) rowCnt[y]++;
            List<int[]> rowBands = Fit(rowCnt, Bands(rowCnt, 14, (int)((cx1 - cx0) * 0.012)), names.Length);
            log.AppendLine("Spalte " + (ci + 1) + ": " + rowBands.Count + " Zeilen / " + names.Length + " Namen");

            for (int ri = 0; ri < rowBands.Count; ri++)
            {
                string name = ri < names.Length ? names[ri].Trim() : ("spalte" + (ci + 1) + "-zeile" + (ri + 1));
                int ry0 = rowBands[ri][0], ry1 = rowBands[ri][1];

                int x0 = W, y0 = H, x1 = -1, y1 = -1;
                for (int y = ry0; y <= ry1; y++)
                    for (int x = cx0; x <= cx1; x++)
                        if (P[Idx(x, y) + 3] > 24)
                        {
                            if (x < x0) x0 = x; if (x > x1) x1 = x;
                            if (y < y0) y0 = y; if (y > y1) y1 = y;
                        }
                if (x1 < 0) continue;

                int bw = x1 - x0 + 1, bh = y1 - y0 + 1;
                int side = (int)Math.Round(Math.Max(bw, bh) * 1.10);
                Rectangle cell = new Rectangle(x0, y0, bw, bh);
                int outSide = Math.Min(side, outMax);

                Save(img, cell, side, outSide, 1.0, 0.0, Path.Combine(outDir, name + ".png"));
                Save(img, cell, side, sdSize, 0.66, 0.03, Path.Combine(sdDir, name + ".png"));
                log.AppendLine("  " + name + ": " + bw + "x" + bh + " -> " + outSide + "px");
            }
        }
        img.Dispose();
        return log.ToString();
    }

    // zeichnet nur den Zellinhalt zentriert auf eine quadratische, transparente Flaeche
    static void Save(Bitmap img, Rectangle cell, int side, int outSize, double fill, double topPad, string path)
    {
        double scale = outSize * fill / side;
        double cx = cell.X + cell.Width / 2.0, cy = cell.Y + cell.Height / 2.0;
        double destW = cell.Width * scale, destH = cell.Height * scale;
        double centerY = fill < 1.0 ? outSize * topPad + outSize * fill / 2.0 : outSize / 2.0;
        double dx = outSize / 2.0 - destW / 2.0;
        double dy = centerY - destH / 2.0;

        using (Bitmap bmp = new Bitmap(outSize, outSize, PixelFormat.Format32bppArgb))
        {
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Transparent);
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.CompositingQuality = CompositingQuality.HighQuality;
                g.DrawImage(img, new RectangleF((float)dx, (float)dy, (float)destW, (float)destH),
                            new RectangleF(cell.X, cell.Y, cell.Width, cell.Height), GraphicsUnit.Pixel);
            }
            bmp.Save(path, ImageFormat.Png);
        }
    }

    static byte Unmix(byte obs, int bgv, double a)
    {
        double v = (obs - (1 - a) * bgv) / a;
        if (v < 0) v = 0; if (v > 255) v = 255;
        return (byte)Math.Round(v);
    }

    static int Idx(int x, int y) { return y * Stride + x * 4; }

    // Hintergrund = sehr nah an der Hintergrundfarbe, ODER neutralgrau und nur maessig abweichend
    // (das erfasst die weichen Schlagschatten, laesst aber farbige Flaechen wie Hauttoene stehen)
    static void Push(Stack<int> st, bool[] bg, int x, int y, int tol)
    {
        int p = y * W + x;
        if (bg[p]) return;
        int i = Idx(x, y);
        int d = Dist(i);
        if (d > 30)
        {
            if (d > tol) return;
            int b = P[i], g = P[i + 1], r = P[i + 2];
            int max = Math.Max(r, Math.Max(g, b)), min = Math.Min(r, Math.Min(g, b));
            if (max - min > 14) return;
        }
        bg[p] = true; st.Push(p);
    }

    static List<int[]> Fit(int[] cnt, List<int[]> bands, int want)
    {
        while (bands.Count < want)
        {
            int big = 0;
            for (int i = 1; i < bands.Count; i++)
                if (bands[i][1] - bands[i][0] > bands[big][1] - bands[big][0]) big = i;
            int a = bands[big][0], b = bands[big][1];
            int lo = a + (b - a) / 3, hi = b - (b - a) / 3;
            int best = lo;
            for (int i = lo; i <= hi; i++) if (cnt[i] < cnt[best]) best = i;
            if (best <= a || best >= b) break;
            bands.RemoveAt(big);
            bands.Insert(big, new int[] { best + 1, b });
            bands.Insert(big, new int[] { a, best - 1 });
        }
        return bands;
    }

    static List<int[]> Bands(int[] cnt, int minGap, int empty)
    {
        List<int[]> res = new List<int[]>();
        int start = -1, gap = 0;
        for (int i = 0; i < cnt.Length; i++)
        {
            if (cnt[i] > empty)
            {
                if (start < 0) start = i;
                gap = 0;
            }
            else if (start >= 0)
            {
                gap++;
                if (gap >= minGap) { res.Add(new int[] { start, i - gap }); start = -1; gap = 0; }
            }
        }
        if (start >= 0) res.Add(new int[] { start, cnt.Length - 1 });
        return res;
    }
}
'@

Add-Type -TypeDefinition $code -ReferencedAssemblies System.Drawing

# je Spalte von oben nach unten
$cols = @(
    'ball,strafe,meldungen,highlights',
    'coach,spieler,kommentar-b,resultat',
    'timeout,bestplayer,schiedsrichter-b,opener',
    'aufstellung,starting6,einblender-ein',
    'schiedsrichter,kommentar,drittel,einblender-aus'
)

[Slicer]::Run($Src, $OutDir, $SdDir, $cols, 512, 144)
