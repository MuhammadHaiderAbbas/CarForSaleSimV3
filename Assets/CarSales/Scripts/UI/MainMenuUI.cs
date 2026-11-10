using System;
using CarSales.Foundation;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CarSales
{
    /// <summary>Main menu scene. Builds its own UI (put it on any GameObject; a Canvas is added if missing).</summary>
    public class MainMenuUI : MonoBehaviour
    {
        [SerializeField] string gameScene = "HarbourCity";
        GameObject mainPanel;
        SettingsUI settings;
        Button continueButton, loadButton;
        TextMeshProUGUI infoText;

        // Save slots
        SaveStore store;
        GameObject slotPanel;
        TextMeshProUGUI slotTitle, slotHint;
        readonly Button[] slotButtons = new Button[SaveStore.SlotCount];
        readonly TextMeshProUGUI[] slotLabels = new TextMeshProUGUI[SaveStore.SlotCount];
        readonly Button[] deleteButtons = new Button[SaveStore.SlotCount];
        readonly RawImage[] slotThumbs = new RawImage[SaveStore.SlotCount];
        readonly Texture2D[] thumbTextures = new Texture2D[SaveStore.SlotCount];
        bool slotModeNew;
        int confirmOverwrite = -1, confirmDelete = -1;

        void Awake()
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            GameSettings.Apply();
            EnsureCanvas();
            for (int i = transform.childCount - 1; i >= 0; i--) Destroy(transform.GetChild(i).gameObject);
            Build();
        }

        void EnsureCanvas()
        {
            if (!GetComponent<Canvas>()) gameObject.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = GetComponent<CanvasScaler>();
            if (!scaler) scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = GameSettings.UiReferenceResolution;
            scaler.matchWidthOrHeight = 0.5f;
            if (!GetComponent<GraphicRaycaster>()) gameObject.AddComponent<GraphicRaycaster>();
        }

        void Build()
        {
            // Phase 12 V2 (UI board): a dark band down the left over the showroom scene, the logo, a list of icon
            // menu items (Continue highlighted) and the strapline at the bottom.
            var panel = UIFactory.Panel(transform, "MainPanel", UIFactory.MenuBg, 0f);
            mainPanel = panel.gameObject;
            panel.rectTransform.anchorMin = new Vector2(0f, 0f);
            panel.rectTransform.anchorMax = new Vector2(0f, 1f);
            panel.rectTransform.pivot = new Vector2(0f, 0.5f);
            panel.rectTransform.anchoredPosition = Vector2.zero;
            panel.rectTransform.sizeDelta = new Vector2(640f, 0f);
            var edge = UIFactory.Fill(panel.transform, "Edge", UIFactory.Line);
            edge.rectTransform.anchorMin = new Vector2(1f, 0f); edge.rectTransform.anchorMax = new Vector2(1f, 1f);
            edge.rectTransform.pivot = new Vector2(1f, 0.5f); edge.rectTransform.sizeDelta = new Vector2(2f, 0f);
            var v = UIFactory.Vertical(panel, 0, 10f);
            v.padding = new RectOffset(70, 70, 90, 50);

            var logo = UIFactory.Text(panel.transform, "<i>Car For Sale</i>", 76, UIFactory.TextMain, TextAlignmentOptions.Left, true);
            UIFactory.LE(logo, h: 84);
            var logo2 = UIFactory.Text(panel.transform, $"<b>Simulator</b> <color={UIFactory.Hex(UIFactory.Bad)}>V2</color>", 58, UIFactory.TextMain, TextAlignmentOptions.Left, true);
            UIFactory.LE(logo2, h: 66);
            var tag = UIFactory.Text(panel.transform, Loc.T("menu.tagline", "Buy  ·  Sell  ·  Manage  ·  Grow"), 22, UIFactory.TextDim, TextAlignmentOptions.Left);
            UIFactory.LE(tag, h: 40);
            UIFactory.Spacer(panel.transform, 0f).GetComponent<LayoutElement>().minHeight = 30f;
            infoText = UIFactory.Text(panel.transform, "", 16, UIFactory.TextDim, TextAlignmentOptions.Left);
            UIFactory.LE(infoText, h: 28);

            continueButton = UIFactory.IconBtn(panel.transform, UIIcon.PlayArrow, Loc.T("menu.continue", "Continue"), UIFactory.Accent, ContinueLatest, 58f, 22f);
            UIFactory.IconBtn(panel.transform, UIIcon.Add, Loc.T("menu.new", "New Game"), UIFactory.Ghost, () => OpenSlots(true), 58f, 22f);
            loadButton = UIFactory.IconBtn(panel.transform, UIIcon.Save, Loc.T("menu.load", "Load Game"), UIFactory.Ghost, () => OpenSlots(false), 58f, 22f);
            UIFactory.IconBtn(panel.transform, UIIcon.Settings, Loc.T("menu.settings", "Settings"), UIFactory.Ghost, () => { mainPanel.SetActive(false); settings.Show(); }, 58f, 22f);
            UIFactory.IconBtn(panel.transform, UIIcon.Info, Loc.T("menu.credits", "Credits"), UIFactory.Ghost, () => { mainPanel.SetActive(false); CreditsUI.Show(transform, () => mainPanel.SetActive(true)); }, 58f, 22f);
            UIFactory.IconBtn(panel.transform, UIIcon.ExitToApp, Loc.T("menu.quit", "Quit"), UIFactory.Ghost, PauseMenuUI.QuitGame, 58f, 22f);
            UIFactory.Spacer(panel.transform);
            var strap = UIFactory.Text(panel.transform, Loc.T("menu.strap", "Build Your Dream Dealership"), 20, UIFactory.TextMain, TextAlignmentOptions.Left, true);
            UIFactory.LE(strap, h: 30);
            var version = UIFactory.Text(panel.transform, "v" + SaveManager.GameVersion, 14, UIFactory.TextFaint, TextAlignmentOptions.Left);
            UIFactory.LE(version, h: 20);

            var settingsRoot = UIFactory.Rect("Settings", transform);
            UIFactory.Stretch(settingsRoot);
            settings = settingsRoot.gameObject.AddComponent<SettingsUI>();
            settings.Build();
            settings.showOnBackInMenu = mainPanel;
            settingsRoot.gameObject.SetActive(false);

            BuildSlotPanel();
        }

        void BuildSlotPanel()
        {
            var panel = UIFactory.Panel(transform, "SlotPanel", UIFactory.MenuBg, 1f);
            slotPanel = panel.gameObject;
            UIFactory.Place(panel.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(90f, 0f), new Vector2(760f, 840f));
            panel.gameObject.AddComponent<Outline>().effectColor = UIFactory.Line;
            UIFactory.Vertical(panel, 40, 18f);
            slotTitle = UIFactory.Text(panel.transform, "", 44, UIFactory.TextMain, TextAlignmentOptions.Center, true);
            UIFactory.LE(slotTitle, h: 60);
            slotHint = UIFactory.Text(panel.transform, "", 17, UIFactory.TextDim, TextAlignmentOptions.Center);
            UIFactory.LE(slotHint, h: 44);
            for (int i = 0; i < SaveStore.SlotCount; i++)
            {
                int slot = i + 1;
                var row = UIFactory.HRow(panel.transform, 132, 12, "Slot" + slot);
                slotButtons[i] = UIFactory.Btn(row, "Slot", UIFactory.CardLight, () => PickSlot(slot), out slotLabels[i], 132f, 20f);
                UIFactory.LE(slotButtons[i], flexW: 1);
                slotLabels[i].alignment = TextAlignmentOptions.MidlineLeft;
                slotLabels[i].margin = new Vector4(232f, 0f, 10f, 0f);
                var frame = UIFactory.Panel(slotButtons[i].transform, "Thumb", UIFactory.PictureBg, 2f);
                UIFactory.Place(frame.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(10f, 0f), new Vector2(200f, 112f));
                frame.raycastTarget = false;
                frame.gameObject.AddComponent<Mask>().showMaskGraphic = true;
                slotThumbs[i] = UIFactory.Rect("Image", frame.transform).gameObject.AddComponent<RawImage>();
                UIFactory.Stretch(slotThumbs[i].rectTransform);
                slotThumbs[i].raycastTarget = false;
                deleteButtons[i] = UIFactory.Btn(row, Loc.T("common.delete", "Delete"), UIFactory.Bad, () => DeleteSlot(slot), 132f, 18f);
                UIFactory.LE(deleteButtons[i], w: 130);
            }
            UIFactory.Spacer(panel.transform);
            UIFactory.Btn(panel.transform, Loc.T("common.back", "Back"), UIFactory.Neutral, CloseSlots, 64f, 22f);
            slotPanel.SetActive(false);
        }

        void OpenSlots(bool newGame)
        {
            slotModeNew = newGame;
            confirmOverwrite = confirmDelete = -1;
            mainPanel.SetActive(false);
            slotPanel.SetActive(true);
            RefreshSlots();
        }

        void CloseSlots()
        {
            slotPanel.SetActive(false);
            mainPanel.SetActive(true);
            RefreshMain();
        }

        void RefreshSlots()
        {
            slotTitle.text = slotModeNew ? "NEW GAME" : "LOAD GAME";
            slotHint.text = slotModeNew ? "Pick a slot for your new career. Each slot keeps 3 automatic backups."
                                        : "Pick a save to continue. The game saves every night when you sleep.";
            for (int i = 0; i < SaveStore.SlotCount; i++)
            {
                int slot = i + 1;
                var info = store.Peek(slot);
                string head = $"<size=110%><b>SLOT {slot}</b></size>";
                string body;
                if (!info.Exists) body = $"<color={UIFactory.Hex(UIFactory.TextDim)}>Empty</color>";
                else if (!info.Readable) body = $"<color={UIFactory.Hex(UIFactory.Bad)}>Damaged - cannot be loaded</color>";
                else
                {
                    string when = info.SavedAtUtc > DateTime.MinValue ? info.SavedAtUtc.ToLocalTime().ToString("dd MMM yyyy  HH:mm") : "unknown time";
                    body = $"{info.Label}   <color={UIFactory.Hex(UIFactory.Gold)}>{UIFactory.Money(info.Money)}</color>\n" +
                           $"<size=85%><color={UIFactory.Hex(UIFactory.TextDim)}>Saved {when}{(info.FromBackup ? "  (restored from backup)" : "")}</color></size>";
                }
                if (confirmOverwrite == slot) body = $"<color={UIFactory.Hex(UIFactory.Warn)}>Overwrite this save? Click again to confirm.</color>";
                if (confirmDelete == slot) body = $"<color={UIFactory.Hex(UIFactory.Bad)}>Delete this save and its backups? Click Delete again.</color>";
                slotLabels[i].text = head + "\n" + body;
                if (thumbTextures[i]) Destroy(thumbTextures[i]);
                thumbTextures[i] = info.Readable ? SaveThumbnail.Load(store.ThumbnailFile(slot)) : null;
                slotThumbs[i].texture = thumbTextures[i];
                slotThumbs[i].color = thumbTextures[i] ? Color.white : UIFactory.Clear;
                slotButtons[i].interactable = slotModeNew || info.Readable;
                deleteButtons[i].gameObject.SetActive(info.Exists);
            }
        }

        void PickSlot(int slot)
        {
            confirmDelete = -1;
            if (slotModeNew)
            {
                if (store.Exists(slot) && confirmOverwrite != slot) { confirmOverwrite = slot; RefreshSlots(); return; }
                StartGame(slot, true);
            }
            else StartGame(slot, false);
        }

        void DeleteSlot(int slot)
        {
            confirmOverwrite = -1;
            if (confirmDelete != slot) { confirmDelete = slot; RefreshSlots(); return; }
            store.Delete(slot);
            confirmDelete = -1;
            RefreshSlots();
        }

        void ContinueLatest()
        {
            int slot = store.MostRecentSlot();
            if (slot > 0) StartGame(slot, false);
        }

        void RefreshMain()
        {
            int latest = store.MostRecentSlot();
            bool any = false;
            for (int s = 1; s <= SaveStore.SlotCount; s++) any |= store.Peek(s).Readable;
            continueButton.interactable = latest > 0;
            loadButton.interactable = any;
            if (latest > 0)
            {
                var info = store.Peek(latest);
                infoText.text = $"Continue: slot {latest} - {info.Label}, {UIFactory.Money(info.Money)}";
            }
            else infoText.text = "No saved game yet - start a New Game!";
        }

        void OnEnable() => GameSettings.OnUiScaleChanged += ApplyUiScale;
        void OnDisable() => GameSettings.OnUiScaleChanged -= ApplyUiScale;
        void OnDestroy() { foreach (var t in thumbTextures) if (t) Destroy(t); }
        void ApplyUiScale(float _) { var s = GetComponent<CanvasScaler>(); if (s) s.referenceResolution = GameSettings.UiReferenceResolution; }

        void Start()
        {
            store = SaveManager.OpenStore();
            RefreshMain();
        }

        void StartGame(int slot, bool newGame)
        {
            if (!Application.CanStreamedLevelBeLoaded(gameScene))
            {
                infoText.text = slotHint.text = $"Scene '{gameScene}' is not in the build scene list.";
                return;
            }
            GameSettings.ActiveSlot = slot;
            if (newGame) PlayerPrefs.SetInt(GameSettings.NewGameFlagKey, 1);
            SceneManager.LoadScene(gameScene);
        }
    }
}
