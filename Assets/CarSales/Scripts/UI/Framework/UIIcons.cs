using System.Collections.Generic;
using CarSales.Foundation;
using UnityEngine;

namespace CarSales
{
    /// <summary>
    /// Phase 12 interface icons: Material Icons (Apache 2.0) rasterised in code on first use and cached as sprites.
    /// White shapes: tint them with the Image colour.
    /// </summary>
    public static class UIIcons
    {
        const int Size = 96;
        static readonly Dictionary<UIIcon, Sprite> cache = new Dictionary<UIIcon, Sprite>();

        public static Sprite Get(UIIcon icon)
        {
            if (icon == UIIcon.None) return null;
            if (cache.TryGetValue(icon, out var s) && s) return s;
            if (!IconLibrary.Svg.TryGetValue(icon, out var svg)) return null;
            var alpha = VectorIcon.Rasterize(svg, Size);
            var raw = new byte[Size * Size * 4];
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                {
                    int o = ((Size - 1 - y) * Size + x) * 4;   // Unity rows start at the bottom
                    raw[o] = raw[o + 1] = raw[o + 2] = 255;
                    raw[o + 3] = alpha[y * Size + x];
                }
            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, true) { name = "Icon_" + icon, filterMode = FilterMode.Trilinear, wrapMode = TextureWrapMode.Clamp };
            tex.SetPixelData(raw, 0);   // level 0 only; Apply builds the mip chain
            tex.Apply(true, true);
            s = Sprite.Create(tex, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), 100f);
            s.name = tex.name;
            cache[icon] = s;
            return s;
        }

        /// <summary>The icon that goes with a window or button title (so every window gets one without changing every screen).</summary>
        public static UIIcon ForTitle(string title)
        {
            string t = (title ?? "").ToLowerInvariant();
            (string key, UIIcon icon)[] map =
            {
                ("setting", UIIcon.Settings), ("pause", UIIcon.Pause), ("auction", UIIcon.Gavel), ("bid", UIIcon.Gavel), ("garage", UIIcon.Build),
                ("repair", UIIcon.Build), ("workshop", UIIcon.Build), ("inspect", UIIcon.CarRepair), ("mot", UIIcon.CarRepair), ("part", UIIcon.Build),
                ("tyre", UIIcon.TireRepair), ("fuel", UIIcon.LocalGasStation), ("wash", UIIcon.LocalCarWash), ("inventory", UIIcon.DirectionsCar),
                ("my cars", UIIcon.DirectionsCar), ("vehicle", UIIcon.DirectionsCar), ("car", UIIcon.DirectionsCar), ("dealership", UIIcon.Storefront),
                ("showroom", UIIcon.Storefront), ("upgrade", UIIcon.Upgrade), ("building", UIIcon.Apartment), ("real estate", UIIcon.Apartment),
                ("property", UIIcon.Apartment), ("bank", UIIcon.AccountBalance), ("loan", UIIcon.AccountBalance), ("account", UIIcon.ReceiptLong),
                ("income", UIIcon.BarChart), ("statistic", UIIcon.BarChart), ("business", UIIcon.TrendingUp), ("market", UIIcon.Storefront),
                ("customer", UIIcon.Groups), ("negotiat", UIIcon.Handshake), ("staff", UIIcon.Engineering), ("message", UIIcon.Mail),
                ("notification", UIIcon.Notifications), ("map", UIIcon.Map), ("phone", UIIcon.PhoneIphone), ("mission", UIIcon.EmojiEvents),
                ("goal", UIIcon.EmojiEvents), ("credit", UIIcon.Info), ("seller", UIIcon.Person), ("handover", UIIcon.Handshake),
                ("listing", UIIcon.LocalOffer), ("price", UIIcon.Sell), ("purchase", UIIcon.ShoppingCart), ("report", UIIcon.ReceiptLong),
                ("tow", UIIcon.LocalShipping), ("taxi", UIIcon.LocalShipping), ("save", UIIcon.Save), ("load", UIIcon.Save),
            };
            foreach (var (key, icon) in map) if (t.Contains(key)) return icon;
            return UIIcon.Info;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => cache.Clear();
    }
}
