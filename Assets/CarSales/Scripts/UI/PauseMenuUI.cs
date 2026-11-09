using CarSales.Foundation;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CarSales
{
    public class PauseMenuUI : UIPanel
    {
        [SerializeField] string mainMenuScene = "MainMenu";
        Button mainMenuButton;

        public override void Build()
        {
            // Phase 12 V2 (UI board): a big pause badge, the title, then icon buttons (Resume highlighted).
            var body = UIFactory.Window(Root, Loc.T("pause.title", "Paused"), new Vector2(520f, 760f), CloseAll);
            UIFactory.Vertical(body, 0, 12f);
            var badgeRow = UIFactory.HRow(body, 104f, 0f);
            UIFactory.Spacer(badgeRow, 0f, 1f);
            var badge = UIFactory.Panel(badgeRow, "Badge", UIFactory.CardLight, 0f);
            badge.sprite = UIFactory.Circle; badge.type = Image.Type.Simple;
            UIFactory.LE(badge, h: 96, w: 96, flexW: 0);
            var pauseIcon = UIFactory.IconImage(badge.transform, UIIcon.Pause, UIFactory.TextMain, 56f);
            UIFactory.Place(pauseIcon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(56f, 56f));
            UIFactory.Spacer(badgeRow, 0f, 1f);
            var title = UIFactory.Text(body, Loc.T("pause.heading", "Pause Menu"), 30, UIFactory.TextMain, TextAlignmentOptions.Center, true);
            UIFactory.LE(title, h: 44);
            UIFactory.IconBtn(body, UIIcon.PlayArrow, Loc.T("pause.resume", "Resume"), UIFactory.Accent, CloseAll, 56f, 21f);
            UIFactory.IconBtn(body, UIIcon.Save, Loc.T("pause.save", "Save Game"), UIFactory.CardLight, () => { CarSalesServices.Save.SaveGame(); CarSalesServices.UI.Success("Saved", "Your progress has been saved."); }, 56f, 21f);
            UIFactory.IconBtn(body, UIIcon.Settings, Loc.T("menu.settings", "Settings"), UIFactory.CardLight, () => CarSalesServices.UI.settings.Show(), 56f, 21f);
            UIFactory.IconBtn(body, UIIcon.Info, Loc.T("menu.credits", "Credits"), UIFactory.CardLight, () => CreditsUI.Show(transform.root), 56f, 21f);
            mainMenuButton = UIFactory.IconBtn(body, UIIcon.Home, Loc.T("pause.mainmenu", "Save & Main Menu"), UIFactory.CardLight, GoToMainMenu, 56f, 21f);
            UIFactory.IconBtn(body, UIIcon.ExitToApp, Loc.T("pause.quit", "Save & Quit to Desktop"), UIFactory.CardLight, () => { CarSalesServices.Save.SaveGame(); QuitGame(); }, 56f, 21f);
        }

        public void Show()
        {
            OpenSelf();
            mainMenuButton.interactable = Application.CanStreamedLevelBeLoaded(mainMenuScene);
        }

        void GoToMainMenu()
        {
            if (!(CarSalesServices.Progress && CarSalesServices.Progress.IsGameOver)) CarSalesServices.Save.SaveGame();
            Time.timeScale = 1f;
            SceneManager.LoadScene(mainMenuScene);
        }

        public static void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
