using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CarSales
{
    public class EndScreenUI : UIPanel
    {
        [SerializeField] string mainMenuScene = "MainMenu";
        TextMeshProUGUI titleText, bodyText;
        Image icon;
        TextMeshProUGUI iconGlyph;
        Button continueButton;

        public override void Build()
        {
            var body = UIFactory.Window(Root, "AUTO WORLD", new Vector2(780f, 640f), null);
            UIFactory.Vertical(body, 0, 12f, TextAnchor.UpperCenter);
            var iconRow = UIFactory.HRow(body, 120, 0, "IconRow", false);
            iconRow.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleCenter;
            icon = UIFactory.Icon(iconRow, UIFactory.Good, "!", 110f);
            iconGlyph = icon.GetComponentInChildren<TextMeshProUGUI>();
            titleText = UIFactory.Text(body, "", 54, UIFactory.TextMain, TextAlignmentOptions.Center, true);
            UIFactory.LE(titleText, h: 72);
            bodyText = UIFactory.Text(body, "", 21, UIFactory.TextMain, TextAlignmentOptions.Center);
            UIFactory.Spacer(body);
            var row = UIFactory.HRow(body, 56, 12);
            continueButton = UIFactory.Btn(row, "Keep Playing", UIFactory.Good, CloseAll, 54f);
            UIFactory.Btn(row, "New Game", UIFactory.Accent, () => { CloseAll(); CarSalesServices.Save.NewGame(); }, 54f);
            UIFactory.Btn(row, "Main Menu", UIFactory.Neutral, () =>
            {
                Time.timeScale = 1f;
                if (Application.CanStreamedLevelBeLoaded(mainMenuScene)) SceneManager.LoadScene(mainMenuScene);
                else { CloseAll(); CarSalesServices.Save.NewGame(); }
            }, 54f);
        }

        public void ShowWin()
        {
            OpenSelf();
            icon.color = UIFactory.Gold;
            iconGlyph.text = "$";
            titleText.text = "YOU WON!";
            titleText.color = UIFactory.Gold;
            bodyText.text = $"Your dealership is worth <b>{UIFactory.Money(CarSalesServices.Progress.NetWorth)}</b>!\n\n" + Summary() +
                            "\n\nKeep playing to grow even bigger, or start a new game.";
            continueButton.gameObject.SetActive(true);
            CarSalesServices.Save.SaveGame();
        }

        public void ShowGameOver()
        {
            OpenSelf();
            icon.color = UIFactory.Bad;
            iconGlyph.text = "!";
            titleText.text = "BANKRUPT";
            titleText.color = UIFactory.Bad;
            bodyText.text = $"You ended {CarSalesServices.Progress.BankruptcyDays} days in a row in debt and the bank closed your dealership.\n\n" +
                            Summary() + "\n\nTip: don't overbuy, watch loan interest, and use Quick Sell when cash runs low.";
            continueButton.gameObject.SetActive(false);
        }

        static string Summary()
        {
            var s = CarSalesServices.Progress.Stats;
            return $"Days: {s.daysPlayed}    Cars sold: {s.carsSold}    Total profit: {UIFactory.Money(s.totalProfit)}";
        }
    }
}
