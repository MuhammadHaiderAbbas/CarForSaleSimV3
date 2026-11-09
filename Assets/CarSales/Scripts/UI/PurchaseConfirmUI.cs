using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CarSales
{
    /// <summary>"Purchase Successful!" confirmation with a big check mark and confetti.</summary>
    public class PurchaseConfirmUI : UIPanel
    {
        TextMeshProUGUI message, detail;
        RawImage photo;

        public override void Build()
        {
            var body = UIFactory.Window(Root, "Purchase Confirmation", new Vector2(620f, 640f), CloseAll);
            var v = UIFactory.Vertical(body, 0, 10f, TextAnchor.UpperCenter);
            v.childForceExpandWidth = true;

            // confetti
            var colors = new[] { UIFactory.Gold, UIFactory.Good, UIFactory.Accent, UIFactory.Warn, UIFactory.Bad };
            for (int i = 0; i < 26; i++)
            {
                var c = UIFactory.Fill(body, "Confetti", colors[i % colors.Length]);
                c.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
                var rt = c.rectTransform;
                rt.anchorMin = rt.anchorMax = new Vector2(Random.value, Random.Range(0.55f, 1f));
                rt.sizeDelta = new Vector2(Random.Range(6f, 12f), Random.Range(3f, 6f));
                rt.localEulerAngles = new Vector3(0f, 0f, Random.Range(0f, 180f));
            }

            var iconRow = UIFactory.HRow(body, 120, 0, "IconRow", false);
            iconRow.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleCenter;
            UIFactory.Icon(iconRow, UIFactory.Good, "", 110f, true);
            var title = UIFactory.Text(body, "Purchase Successful!", 36, UIFactory.TextMain, TextAlignmentOptions.Center, true);
            UIFactory.LE(title, h: 50);
            photo = UIFactory.CarPicture(body, 200f);
            message = UIFactory.Text(body, "", 21, UIFactory.TextMain, TextAlignmentOptions.Center);
            detail = UIFactory.Text(body, "", 16, UIFactory.TextDim, TextAlignmentOptions.Center);
            UIFactory.Spacer(body);
            var row = UIFactory.HRow(body, 54, 12);
            UIFactory.Btn(row, "Keep Shopping", UIFactory.Neutral, () => CarSalesServices.UI.marketplace.Show(), 52f);
            UIFactory.Btn(row, "Continue", UIFactory.Accent, CloseAll, 52f);
        }

        public void Show(CarInstance car, int price, int spotIndex, string extra)
        {
            OpenSelf();
            var def = CarSalesServices.Database.Get(car.carId);
            UIFactory.SetCarPicture(photo, car);
            message.text = $"You bought the <b>{def.year} {def.brand} {def.displayName}</b>\nfor <color={UIFactory.Hex(UIFactory.Gold)}><b>{UIFactory.Money(price)}</b></color>.";
            detail.text = (spotIndex >= 0 ? $"It's waiting at {CarSalesServices.Vehicles.LocationText(car)}.\n" : "") + extra;
            CarSalesServices.PlaySfx(Sfx.Success);
        }
    }
}
