using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CarSales.Foundation;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

namespace CarSales
{
    /// <summary>
    /// Full-screen town map (M): top-down render, category filters, zoom, click a place to set the GPS, the GPS
    /// route drawn along the walkable/drivable NavMesh, and a list of places with GPS and taxi buttons.
    /// </summary>
    public class MapUI : UIPanel
    {
        public enum PlaceKind { All, Dealerships, Services, CarMarkets, Homes, Properties, Other }
        static readonly string[] KindNames = { "All", "Dealers", "Services", "Markets", "Homes", "Property", "Other" };

        [SerializeField] Vector3 mapCenter = new Vector3(0f, 0f, 85f);
        [SerializeField] float mapHalfSize = 235f;
        float MapPixels = 940f;   // set from the screen size when the map opens
        static readonly float[] Zooms = { 1f, 1.6f, 2.5f, 4f };

        Camera mapCam;
        RenderTexture rt;
        RawImage mapImage;
        RectTransform iconLayer, routeLayer, list, frameRt, sideRt;
        Image playerArrow;
        TextMeshProUGUI zoomText, routeText;
        UIFactory.TabBar filters;
        int zoomIndex;
        Vector3 viewCenter;
        PlaceKind kind = PlaceKind.All;

        /// <summary>Map category of a place (from its glyph; buildings made by the setup wizard use fixed letters).</summary>
        public static PlaceKind KindOf(MapPlace p)
        {
            switch (p.glyph)
            {
                case "D": return PlaceKind.Dealerships;
                case "F": case "W": case "C": case "P": case "S": case "X": case "R": return PlaceKind.Services;
                case "U": case "A": return PlaceKind.CarMarkets;
                case "H": return PlaceKind.Homes;
                case "$": return PlaceKind.Properties;
                default: return PlaceKind.Other;   // "Y" construction plots, "O" offices
            }
        }

        /// <summary>Phase 12: the icon shown for a place on the map (the board's markers).</summary>
        public static UIIcon IconOf(MapPlace p)
        {
            string n = (p.placeName ?? "").ToLowerInvariant();
            if (n.Contains("park")) return UIIcon.Park;
            if (n.Contains("town centre")) return UIIcon.Storefront;
            if (n.Contains("industrial")) return UIIcon.Factory;
            if (n.Contains("service centre")) return UIIcon.CarRepair;
            if (n.Contains("parts")) return UIIcon.TireRepair;
            if (n.Contains("tyre") || n.Contains("tire")) return UIIcon.TireRepair;
            if (n.Contains("patrol")) return UIIcon.Radio;
            if (n.Contains("rentgo") || n.Contains("rent-a-car")) return UIIcon.Handshake;
            if (n.Contains("office")) return UIIcon.Apartment;
            if (n.Contains("close")) return UIIcon.Person;
            switch (p.glyph)
            {
                case "D": return UIIcon.Storefront;
                case "F": return UIIcon.LocalGasStation;
                case "W": return UIIcon.Build;
                case "C": return UIIcon.LocalCarWash;
                case "P": return UIIcon.ShoppingCart;
                case "S": return UIIcon.CarRepair;
                case "X": return UIIcon.Radio;
                case "R": return UIIcon.Handshake;
                case "O": return UIIcon.Apartment;
                case "Y": return UIIcon.Construction;
                case "U": return UIIcon.DirectionsCar;
                case "A": return UIIcon.Gavel;
                case "H": return UIIcon.Home;
                case "$": return UIIcon.Apartment;
                case "M": return UIIcon.Person;
                default: return UIIcon.Place;
            }
        }

        public override void Build()
        {
            // Frame Harbour City.
            var (cx, cz, half) = CarSales.Sim.Town.MapFrame;
            mapCenter = new Vector3(cx, 0f, cz);
            mapHalfSize = half;
            var bg = Root.gameObject.AddComponent<Image>();
            bg.color = UIFactory.WindowBg;

            var head = UIFactory.Fill(Root, "Header", UIFactory.HeaderBg);
            var hr = head.rectTransform;
            hr.anchorMin = new Vector2(0f, 1f); hr.anchorMax = Vector2.one; hr.pivot = new Vector2(0.5f, 1f);
            hr.sizeDelta = new Vector2(0f, 64f); hr.anchoredPosition = Vector2.zero;
            var titleIcon = UIFactory.IconImage(hr, UIIcon.Map, UIFactory.Accent, 30f);
            UIFactory.Place(titleIcon.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(32f, 0f), new Vector2(30f, 30f));
            var title = UIFactory.Text(hr, "World Map", 26, UIFactory.TextMain, TextAlignmentOptions.Left, true);
            UIFactory.Stretch(title.rectTransform);
            title.rectTransform.offsetMin = new Vector2(74f, 0f);
            var hint = UIFactory.Text(hr, "Click a place to set the GPS   -   M or Esc to close", 16, UIFactory.TextDim, TextAlignmentOptions.Center);
            UIFactory.Stretch(hint.rectTransform);
            var close = UIFactory.Btn(hr, "", UIFactory.Ghost, CloseAll, 40f, 18f);
            UIFactory.Place((RectTransform)close.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-16f, 0f), new Vector2(44f, 40f));
            var cross = UIFactory.IconImage(close.transform, UIIcon.Close, UIFactory.TextMain, 24f);
            UIFactory.Place(cross.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(24f, 24f));

            // Map
            var frame = UIFactory.Panel(Root, "MapFrame", UIFactory.MapFrame, 1.2f);
            frameRt = frame.rectTransform;
            UIFactory.Place(frameRt, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(40f, -32f), new Vector2(MapPixels, MapPixels));
            frame.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            mapImage = UIFactory.Rect("Map", frame.transform).gameObject.AddComponent<RawImage>();
            UIFactory.Stretch(mapImage.rectTransform);
            mapImage.raycastTarget = false;
            routeLayer = UIFactory.Rect("Route", frame.transform);
            UIFactory.Stretch(routeLayer);
            iconLayer = UIFactory.Rect("Icons", frame.transform);
            UIFactory.Stretch(iconLayer);
            playerArrow = UIFactory.SpriteImage(frame.transform, "You", UIFactory.Arrow, Color.white, 28f);

            // Zoom controls (on the map)
            var zoom = UIFactory.Panel(frame.transform, "Zoom", UIFactory.HudBgStrong, 2f);
            UIFactory.Place(zoom.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(14f, -14f), new Vector2(64f, 200f));
            UIFactory.Vertical(zoom, 8, 6f);
            UIFactory.Btn(zoom.transform, "+", UIFactory.CardLight, () => SetZoom(zoomIndex + 1), 44f, 24f);

            // Legend along the bottom of the map (phase 12 V2 board).
            var legend = UIFactory.Panel(frame.transform, "Legend", UIFactory.HudBgStrong, 2f);
            UIFactory.Place(legend.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(14f, 14f), new Vector2(620f, 40f));
            var lh = UIFactory.Horizontal(legend, 6, 12f);
            lh.padding = new RectOffset(12, 12, 6, 6);
            foreach (var (ic, name, col) in new[] { (UIIcon.MyLocation, "Player", UIFactory.TextMain), (UIIcon.Storefront, "Dealership", UIFactory.Accent),
                                                     (UIIcon.Gavel, "Auction", UIFactory.Gold), (UIIcon.Build, "Garage", UIFactory.Info),
                                                     (UIIcon.LocalGasStation, "Fuel", UIFactory.Good), (UIIcon.ShoppingCart, "Shop", UIFactory.Warn) })
            {
                UIFactory.IconImage(legend.transform, ic, col, 22f);
                var lt = UIFactory.Text(legend.transform, name, 14, UIFactory.TextMain, TextAlignmentOptions.Left, true);
                UIFactory.LE(lt, w: 70, flexW: 0);
            }
            zoomText = UIFactory.Text(zoom.transform, "", 14, UIFactory.TextDim, TextAlignmentOptions.Center, true);
            UIFactory.LE(zoomText, h: 20);
            UIFactory.Btn(zoom.transform, "-", UIFactory.CardLight, () => SetZoom(zoomIndex - 1), 44f, 24f);
            UIFactory.Btn(zoom.transform, "Me", UIFactory.Accent, () => { viewCenter = PlayerPos(); RenderMap(); Rebuild(); }, 44f, 14f);

            // Side panel
            var side = sideRt = UIFactory.Rect("Side", Root);
            side.anchorMin = new Vector2(0f, 0f); side.anchorMax = new Vector2(1f, 1f);
            side.offsetMin = new Vector2(40f + MapPixels + 32f, 36f); side.offsetMax = new Vector2(-40f, -96f);
            UIFactory.Vertical(side, 0, 10f);
            UIFactory.SectionTitle(side, "Show");
            filters = UIFactory.Tabs(side, KindNames, i => { kind = (PlaceKind)i; Rebuild(); }, 38f, 0f, 14f);
            var routeCard = UIFactory.Panel(side, "Route", UIFactory.CardBg, 1.6f);
            UIFactory.LE(routeCard, h: 74);
            var rv = UIFactory.Vertical(routeCard, 14, 4f);
            rv.padding = new RectOffset(16, 16, 10, 10);
            routeText = UIFactory.Text(routeCard.transform, "", 17, UIFactory.TextMain, TextAlignmentOptions.Left, true);
            UIFactory.LE(routeText, flexH: 1);
            UIFactory.SectionTitle(side, "Places");
            list = UIFactory.ScrollList(side, 8f);
            UIFactory.Btn(side, "Clear GPS", UIFactory.Neutral, () => { GpsSystem.Clear(); Rebuild(); }, 44f, 17f);
            filters.Select(0, false);
        }

        public void Show()
        {
            OpenSelf();
            Layout();
            viewCenter = zoomIndex == 0 ? mapCenter : PlayerPos();
            RenderMap();
            Rebuild();
        }

        /// <summary>Square map as tall as the screen allows (the interface-size setting changes the room available).</summary>
        void Layout()
        {
            Canvas.ForceUpdateCanvases();
            var r = Root.rect;
            if (r.height < 10f) return;
            MapPixels = Mathf.Floor(Mathf.Min(r.height - 140f, r.width * 0.56f));
            frameRt.sizeDelta = new Vector2(MapPixels, MapPixels);
            sideRt.offsetMin = new Vector2(40f + MapPixels + 32f, 36f);
        }

        float HalfSize => mapHalfSize / Zooms[zoomIndex];

        void SetZoom(int index)
        {
            int z = Mathf.Clamp(index, 0, Zooms.Length - 1);
            if (z == zoomIndex) return;
            zoomIndex = z;
            viewCenter = z == 0 ? mapCenter : PlayerPos();
            RenderMap();
            Rebuild();
        }

        static Vector3 PlayerPos()
        {
            if (Camera.main) return Camera.main.transform.position;
            var player = FindAnyObjectByType<PlayerInteractor>();
            return player ? player.transform.position : Vector3.zero;
        }

        void RenderMap()
        {
            if (!rt) rt = new RenderTexture(2048, 2048, 16) { name = "TownMap" };   // phase 12: the town is ~1 km across
            if (!mapCam)
            {
                var go = new GameObject("TownMapCamera");
                mapCam = go.AddComponent<Camera>();
                mapCam.orthographic = true;
                mapCam.clearFlags = CameraClearFlags.SolidColor;
                mapCam.backgroundColor = UIFactory.MapBg;
                mapCam.cullingMask = ~((1 << ThumbnailService.StudioLayer) | (1 << 5));
                mapCam.nearClipPlane = 1f;
                mapCam.farClipPlane = 600f;
                mapCam.aspect = 1f;
                mapCam.enabled = false;
            }
            mapCam.orthographicSize = HalfSize;
            mapCam.transform.SetPositionAndRotation(new Vector3(viewCenter.x, 300f, viewCenter.z), Quaternion.Euler(90f, 0f, 0f));
            mapCam.targetTexture = rt;
            mapImage.texture = rt;
            zoomText.text = $"x{Zooms[zoomIndex]:0.#}";
            StartCoroutine(RenderOnce());
        }

        IEnumerator RenderOnce()
        {
            mapCam.enabled = true;
            yield return new WaitForEndOfFrame();
            mapCam.enabled = false;
        }

        Vector2 ToMap(Vector3 world) =>
            new Vector2((world.x - viewCenter.x) / HalfSize, (world.z - viewCenter.z) / HalfSize) * (MapPixels / 2f);

        bool OnMap(Vector2 p) => Mathf.Abs(p.x) < MapPixels / 2f - 6f && Mathf.Abs(p.y) < MapPixels / 2f - 6f;

        void Rebuild()
        {
            UIFactory.ClearChildren(iconLayer);
            UIFactory.ClearChildren(list);
            Vector3 me = PlayerPos();

            var places = MapPlace.All.Where(p => p && p.IsVisible).OrderBy(p => Vector3.Distance(me, p.transform.position)).ToList();
            for (int i = 1; i < KindNames.Length; i++)
            {
                int count = places.Count(p => (int)KindOf(p) == i);
                filters.SetLabel(i, $"{KindNames[i]} {count}");
                filters.SetInteractable(i, count > 0);
            }
            foreach (var place in places)
            {
                bool gps = GpsSystem.Target == place;
                bool match = kind == PlaceKind.All || KindOf(place) == kind;
                if (!match && !gps) continue;
                var p = place;
                Vector2 pos = ToMap(place.transform.position);
                if (OnMap(pos))
                {
                    float size = gps ? 36f : 30f;
                    var icon = UIFactory.IconBadge(iconLayer, gps ? UIFactory.Gold : place.color, IconOf(place), size);
                    icon.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
                    icon.raycastTarget = true;
                    var b = icon.gameObject.AddComponent<Button>();
                    b.targetGraphic = icon;
                    b.onClick.AddListener(() => { GpsSystem.SetTarget(p); Rebuild(); });
                    UIFactory.Place(icon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, new Vector2(size, size));
                    if (zoomIndex > 0 || gps)
                    {
                        var label = UIFactory.Text(iconLayer, place.placeName, 13, UIFactory.TextMain, TextAlignmentOptions.Center, true);
                        label.gameObject.AddComponent<Shadow>().effectColor = UITheme.Current.shadow;
                        UIFactory.Place(label.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 1f), pos + new Vector2(0f, -20f), new Vector2(170f, 20f));
                    }
                }
                if (match) ListRow(place, me, gps);
            }
            if (list.childCount == 0) UIFactory.EmptyState(list, "No places of this kind", null);

            DrawRoute(me);
            playerArrow.transform.SetAsLastSibling();
            Vector2 mp = ToMap(me);
            playerArrow.gameObject.SetActive(OnMap(mp));
            UIFactory.Place(playerArrow.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), mp, new Vector2(28f, 28f));
            float yaw = Camera.main ? Camera.main.transform.eulerAngles.y : 0f;
            playerArrow.rectTransform.localEulerAngles = new Vector3(0f, 0f, -yaw);
        }

        void ListRow(MapPlace place, Vector3 me, bool gps)
        {
            var p = place;
            var row = UIFactory.Panel(list, place.placeName, gps ? UIFactory.AccentDark : UIFactory.CardBg, 1.8f);
            UIFactory.LE(row, h: 60);
            var h = UIFactory.Horizontal(row, 8, 10f);
            h.padding = new RectOffset(10, 10, 8, 8);
            h.childForceExpandHeight = false;
            UIFactory.IconBadge(row.transform, place.color, IconOf(place), 36f);
            var col = UIFactory.VCol(row.transform, 0f);
            UIFactory.Text(col, place.placeName, 18, UIFactory.TextMain, TextAlignmentOptions.Left, true);
            UIFactory.Text(col, $"{KindNames[(int)KindOf(place)]}  -  {Vector3.Distance(me, place.transform.position):0} m away", 14, UIFactory.TextDim, TextAlignmentOptions.Left);
            var gpsBtn = UIFactory.Btn(row.transform, gps ? "GPS on" : "GPS", gps ? UIFactory.Gold : UIFactory.Accent, () => { GpsSystem.SetTarget(p); Rebuild(); }, 40f, 15f);
            UIFactory.LE(gpsBtn, h: 40, w: 84, flexW: 0);
            int fare = CarSalesServices.Tow ? CarSalesServices.Tow.TaxiCost(me, p.ArrivalPosition) : 0;
            var taxi = UIFactory.Btn(row.transform, $"Taxi {UIFactory.Money(fare)}", UIFactory.Warn, () => TakeTaxi(p), 40f, 14f);
            UIFactory.LE(taxi, h: 40, w: 116, flexW: 0);
        }

        /// <summary>Dots along the NavMesh path to the GPS target (a straight line if there is no NavMesh path).</summary>
        void DrawRoute(Vector3 me)
        {
            UIFactory.ClearChildren(routeLayer);
            var target = GpsSystem.Target;
            if (!target) { routeText.text = $"<color={UIFactory.Hex(UIFactory.TextDim)}>No GPS route. Click a place to set one.</color>"; return; }

            var points = new List<Vector3> { me, target.ArrivalPosition };
            var path = new NavMeshPath();
            if (NavMesh.SamplePosition(me, out var a, 8f, NavMesh.AllAreas) &&
                NavMesh.SamplePosition(target.ArrivalPosition, out var b, 12f, NavMesh.AllAreas) &&
                NavMesh.CalculatePath(a.position, b.position, NavMesh.AllAreas, path) && path.corners.Length > 1)
                points = new List<Vector3>(path.corners);

            float length = 0f;
            for (int i = 1; i < points.Count; i++) length += Vector3.Distance(points[i - 1], points[i]);
            float spacing = Mathf.Max(4f, HalfSize / 40f);
            for (int i = 1; i < points.Count; i++)
            {
                Vector3 p0 = points[i - 1], p1 = points[i];
                float seg = Vector3.Distance(p0, p1);
                for (float d = 0f; d < seg; d += spacing)
                {
                    Vector2 mp = ToMap(Vector3.Lerp(p0, p1, d / seg));
                    if (!OnMap(mp)) continue;
                    var dot = UIFactory.SpriteImage(routeLayer, "Dot", UIFactory.Circle, UIFactory.Gold, 7f);
                    UIFactory.Place(dot.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), mp, new Vector2(7f, 7f));
                }
            }
            routeText.text = $"To <color={UIFactory.Hex(UIFactory.Gold)}>{target.placeName}</color>\n" +
                             $"<size=85%><color={UIFactory.Hex(UIFactory.TextDim)}>{(length >= 1000f ? $"{length / 1000f:0.0} km" : $"{length:0} m")} along the roads</color></size>";
        }

        void TakeTaxi(MapPlace place)
        {
            if (CarSalesServices.Tow.Taxi(place, out string msg)) { CloseAll(); CarSalesServices.UI.Success("Taxi", msg); }
            else CarSalesServices.UI.Error(msg);
        }

        void OnDestroy()
        {
            if (mapCam) Destroy(mapCam.gameObject);
            if (rt) { rt.Release(); Destroy(rt); }
        }
    }
}
