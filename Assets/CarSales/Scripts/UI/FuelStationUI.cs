using CarSales.Foundation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CarSales
{
    /// <summary>
    /// Phase 12 V2 (UI board) Fuel Station: pick the fuel, see the price per litre, choose how many litres and refuel.
    /// A car's tank is treated as 50 litres (fuel is stored as a percentage). Putting petrol in a diesel (or the other
    /// way round) is refused at the pump. Electric cars charge at the EV point for the same money per percent.
    /// Created on demand over the UI canvas.
    /// </summary>
    public class FuelStationUI : MonoBehaviour
    {
        public const float TankLitres = 50f;
        static FuelStationUI open;
        CarDisplay car;
        string fuel = "Petrol";
        int litres;
        TextMeshProUGUI carText, priceText, litresText, totalText, warnText;
        Button petrolBtn, dieselBtn;

        public static float PricePerLitre => CarSalesServices.Vehicles ? CarSalesServices.Vehicles.FuelPricePerPercent * 100f / TankLitres : 1.5f;

        public static void Show(CarDisplay target)
        {
            if (!target) return;
            var canvas = CarSalesServices.UI ? CarSalesServices.UI.transform : null;
            if (!canvas) { CarSalesServices.Vehicles.Refuel(target); return; }
            if (open) Destroy(open.gameObject);
            var root = UIFactory.Rect("FuelStation", canvas);
            UIFactory.Stretch(root);
            open = root.gameObject.AddComponent<FuelStationUI>();
            open.car = target;
            open.Build(root);
            CarSalesServices.UI.Open(root.gameObject);   // a real menu: frees the cursor, Esc goes back
        }

        int MaxLitres => Mathf.Max(0, Mathf.FloorToInt((100f - car.Data.fuel) / 100f * TankLitres));

        void Build(RectTransform root)
        {
            var body = UIFactory.Window(root, "Fuel Station", new Vector2(640f, 560f), Close);
            UIFactory.Vertical(body, 0, 12f);
            carText = UIFactory.Text(body, "", 18, UIFactory.TextDim, TextAlignmentOptions.Left);
            UIFactory.LE(carText, h: 30);
            UIFactory.SectionTitle(body, "Fuel Type");
            var types = UIFactory.HRow(body, 48f, 10f);
            petrolBtn = UIFactory.IconBtn(types, UIIcon.LocalGasStation, "Petrol", UIFactory.CardLight, () => { fuel = "Petrol"; Refresh(); }, 46f, 18f);
            dieselBtn = UIFactory.IconBtn(types, UIIcon.LocalGasStation, "Diesel", UIFactory.CardLight, () => { fuel = "Diesel"; Refresh(); }, 46f, 18f);
            priceText = UIFactory.Text(body, "", 20, UIFactory.TextMain, TextAlignmentOptions.Left, true);
            UIFactory.LE(priceText, h: 34);
            var amount = UIFactory.HRow(body, 52f, 10f);
            var lbl = UIFactory.Text(amount, "Fuel Amount (L):", 19, UIFactory.TextMain, TextAlignmentOptions.Left, true);
            UIFactory.LE(lbl, flexW: 1);
            var minus = UIFactory.Btn(amount, "-", UIFactory.CardLight, () => { litres = Mathf.Max(0, litres - 5); Refresh(); }, 46f, 24f);
            UIFactory.LE(minus, h: 46, w: 56, flexW: 0);
            litresText = UIFactory.Text(amount, "", 24, UIFactory.TextMain, TextAlignmentOptions.Center, true);
            UIFactory.LE(litresText, w: 90, flexW: 0);
            var plus = UIFactory.Btn(amount, "+", UIFactory.Accent, () => { litres = Mathf.Min(MaxLitres, litres + 5); Refresh(); }, 46f, 24f);
            UIFactory.LE(plus, h: 46, w: 56, flexW: 0);
            var full = UIFactory.Btn(amount, "Fill", UIFactory.CardLight, () => { litres = MaxLitres; Refresh(); }, 46f, 18f);
            UIFactory.LE(full, h: 46, w: 80, flexW: 0);
            totalText = UIFactory.Text(body, "", 22, UIFactory.Gold, TextAlignmentOptions.Left, true);
            UIFactory.LE(totalText, h: 34);
            warnText = UIFactory.Text(body, "", 16, UIFactory.Bad, TextAlignmentOptions.Left);
            UIFactory.LE(warnText, h: 26);
            UIFactory.Spacer(body);
            UIFactory.IconBtn(body, UIIcon.LocalGasStation, "Refuel", UIFactory.Good, Refuel, 54f, 21f);
            fuel = Needs();
            litres = MaxLitres;
            Refresh();
        }

        string Needs()
        {
            string t = car && car.Definition ? (car.Definition.fuelType ?? "Petrol") : "Petrol";
            return t.ToLowerInvariant().Contains("diesel") ? "Diesel" : "Petrol";
        }

        bool Electric => car && car.Definition && (car.Definition.fuelType ?? "").ToLowerInvariant().Contains("electric");

        void Refresh()
        {
            if (!car) { Close(); return; }
            carText.text = $"{car.Definition.displayName}  -  tank {car.Data.fuel:0}%  ({car.Definition.fuelType})";
            UIFactory.SetButtonColor(petrolBtn, fuel == "Petrol" ? UIFactory.Accent : UIFactory.CardLight);
            UIFactory.SetButtonColor(dieselBtn, fuel == "Diesel" ? UIFactory.Accent : UIFactory.CardLight);
            priceText.text = $"Price per litre: {UIFactory.Currency}{PricePerLitre:0.00}";
            litresText.text = litres.ToString();
            totalText.text = $"Total: {UIFactory.Money(Cost())}";
            warnText.text = Electric ? "Electric car: this charges the battery at the EV point." : fuel != Needs() ? $"Wrong fuel! The {car.Definition.displayName} takes {Needs().ToLower()}." : "";
        }

        int Cost() => Mathf.CeilToInt(litres * PricePerLitre);

        void Refuel()
        {
            if (!car) { Close(); return; }
            if (!Electric && fuel != Needs()) { CarSalesServices.UI.Error($"That's the wrong fuel for the {car.Definition.displayName}."); return; }
            if (litres <= 0) { CarSalesServices.UI.Notify("The tank is already full."); return; }
            int cost = Cost();
            if (!CarSalesServices.Economy.TrySpend(cost, TxCategory.Fuel, $"Fuel {litres} L - {car.Definition.displayName}", car.Data.instanceId))
            { CarSalesServices.UI.Error("Not enough money for that much fuel."); return; }
            car.Data.fuel = Mathf.Min(100f, car.Data.fuel + litres / TankLitres * 100f);
            CarSalesServices.PlaySfx(Sfx.Coin);
            CarSalesServices.UI.Success("Refuelled", $"{litres} L for {UIFactory.Money(cost)}.");
            Close();
        }

        void Close()
        {
            if (CarSalesServices.UI && CarSalesServices.UI.IsMenuOpen) CarSalesServices.UI.Close();
            else if (this) Destroy(gameObject);
        }

        // Closed by Esc / UIManager (the panel is just hidden): remove it.
        void OnDisable() { if (open == this) open = null; if (this && gameObject) Destroy(gameObject); }
    }
}
