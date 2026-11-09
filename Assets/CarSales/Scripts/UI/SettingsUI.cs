using System.Collections.Generic;
using CarSales.Foundation;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace CarSales
{
    /// <summary>
    /// Works in-game (through UIManager) and in the main menu (no UIManager).
    /// Phase 10: tabs - General, Display, Audio, Driving, Controls (key rebinding).
    /// </summary>
    public class SettingsUI : UIPanel
    {
        Slider sensitivity, volume;
        TextMeshProUGUI sensitivityLabel, volumeLabel, drivingLabel, gearboxLabel, absLabel, tcsLabel, escLabel, themeLabel, scaleLabel, trafficLabel, perfLabel;
        TextMeshProUGUI subtitlesLabel, languageLabel, difficultyLabel, viewLabel;
        Slider uiScale;
        readonly Slider[] groupSliders = new Slider[5];
        readonly TextMeshProUGUI[] groupLabels = new TextMeshProUGUI[5];
        readonly List<RectTransform> pages = new List<RectTransform>();
        RectTransform controlsList;
        InputActionRebindingExtensions.RebindingOperation rebinding;
        InputAction rebindAction;
        bool rebindWasEnabled;
        public GameObject showOnBackInMenu;

        public static readonly string[] GroupNames = { "Music", "Sound effects", "Ambience", "Vehicles", "Interface" };
        public static readonly string[] DifficultyNames = { "EASY", "NORMAL", "HARD", "REALISTIC", "CUSTOM" };

        UIFactory.TabBar nav;
        TextMeshProUGUI invertLabel, hintsLabel, autoSaveLabel, qualityLabel, vsyncLabel, fullscreenLabel, resolutionLabel, frameCapLabel;

        /// <summary>
        /// Phase 12 V2 (UI board): icon navigation down the left - Gameplay, Graphics, Audio, Controls, Language - the
        /// page on the right, and Reset to Default / Apply along the bottom.
        /// </summary>
        public override void Build()
        {
            var body = UIFactory.Window(Root, Loc.T("settings.title", "Settings"), new Vector2(980f, 860f), Back);
            UIFactory.Vertical(body, 0, 12f);
            var columns = UIFactory.HRow(body, 0f, 18f);
            UIFactory.LE(columns, flexH: 1);
            columns.GetComponent<HorizontalLayoutGroup>().childForceExpandHeight = true;
            var left = UIFactory.VCol(columns, 6f, "Nav", 210f, 0f);
            var right = UIFactory.VCol(columns, 10f, "Pages");
            UIFactory.LE(right, flexH: 1);

            var items = new (UIIcon icon, string name)[]
            {
                (UIIcon.VideogameAsset, Loc.T("settings.tab.gameplay", "Gameplay")), (UIIcon.Monitor, Loc.T("settings.tab.graphics", "Graphics")),
                (UIIcon.VolumeUp, Loc.T("settings.tab.audio", "Audio")), (UIIcon.Keyboard, Loc.T("settings.tab.controls", "Controls")),
                (UIIcon.Language, Loc.T("settings.tab.language", "Language")),
            };
            nav = new UIFactory.TabBar();
            for (int i = 0; i < items.Length; i++)
            {
                int index = i;
                var b = UIFactory.IconBtn(left, items[i].icon, items[i].name, UIFactory.CardLight, () => nav.Select(index), out var lbl, 50f, 18f);
                nav.buttons.Add(b); nav.labels.Add(lbl);
            }
            nav.OnChanged += ShowPage;
            UIFactory.Spacer(left);

            for (int i = 0; i < items.Length; i++)
            {
                var page = UIFactory.VCol(right, 10f, "Page" + i);
                UIFactory.LE(page, flexH: 1);
                pages.Add(page);
            }
            BuildGameplay(pages[0]);
            BuildDisplay(pages[1]);
            BuildAudio(pages[2]);
            BuildControlsPage(pages[3]);
            BuildLanguage(pages[4]);

            var bottom = UIFactory.HRow(body, 50f, 12f);
            UIFactory.Spacer(bottom, 0f, 1f);
            var reset = UIFactory.Btn(bottom, Loc.T("settings.reset", "Reset to Default"), UIFactory.CardLight, () =>
            {
                CancelRebind();
                GameSettings.ResetToDefaults();
                OnEnable();
            }, 50f, 18f);
            UIFactory.LE(reset, h: 50, w: 220, flexW: 0);
            var apply = UIFactory.Btn(bottom, Loc.T("settings.apply", "Apply"), UIFactory.Accent, Back, 50f, 18f);
            UIFactory.LE(apply, h: 50, w: 180, flexW: 0);
            nav.Select(0);
        }

        /// <summary>A setting row: name on the left, a value button on the right (the board's toggles and drop-downs).</summary>
        TextMeshProUGUI SettingRow(Transform p, string name, UnityEngine.Events.UnityAction onClick, float w = 230f)
        {
            var row = UIFactory.HRow(p, 50f, 12f);
            var t = UIFactory.Text(row, name, 19, UIFactory.TextMain, TextAlignmentOptions.Left, true);
            UIFactory.LE(t, flexW: 1);
            var b = UIFactory.Btn(row, "", UIFactory.CardLight, () => { onClick(); UpdateLabels(); }, out var lbl, 44f, 17f);
            UIFactory.LE(b, h: 44, w: w, flexW: 0);
            return lbl;
        }

        // Fonts may lack symbol glyphs, so the switch is shown in words and colour.
        static string Toggle(bool on) => on ? $"<color={UIFactory.Hex(UIFactory.Good)}>" + Loc.T("common.on", "ON") + "</color>" : Loc.T("common.off", "OFF");

        void BuildGameplay(Transform p)
        {
            UIFactory.SectionTitle(p, Loc.T("settings.gameplay", "Gameplay Settings"));
            difficultyLabel = SettingRow(p, Loc.T("settings.difficulty", "Difficulty (new games)"), () => GameSettings.Difficulty = (GameSettings.Difficulty + 1) % (BalanceSettings.HasCustom ? 5 : 4));
            invertLabel = SettingRow(p, Loc.T("settings.invert", "Invert Y Axis"), () => GameSettings.InvertY = !GameSettings.InvertY);
            hintsLabel = SettingRow(p, Loc.T("settings.hints", "Show Hints"), () => GameSettings.ShowHints = !GameSettings.ShowHints);
            autoSaveLabel = SettingRow(p, Loc.T("settings.autosave", "Auto Save"), () => GameSettings.AutoSave = !GameSettings.AutoSave);
            subtitlesLabel = SettingRow(p, Loc.T("settings.subtitles", "Subtitles"), () => GameSettings.Subtitles = !GameSettings.Subtitles);
            viewLabel = SettingRow(p, Loc.T("settings.view", "Camera view (V)"), () =>
            {
                GameSettings.ThirdPerson = !GameSettings.ThirdPerson;
                var me = FindAnyObjectByType<PlayerAvatar>();
                if (me) me.SetView(GameSettings.ThirdPerson);
            });
            sensitivityLabel = UIFactory.Text(p, "", 19, UIFactory.TextMain, TextAlignmentOptions.Left, true);
            sensitivity = UIFactory.MakeSlider(p, UIFactory.Accent, 30f);
            sensitivity.minValue = 0.02f; sensitivity.maxValue = 0.5f;
            sensitivity.onValueChanged.AddListener(v => { GameSettings.MouseSensitivity = v; UpdateLabels(); });
            var note = UIFactory.Text(p, Loc.T("settings.difficulty.note", "Difficulty applies when you start a new game (starting money, customer budgets, repair costs, footfall). Language applies to screens opened after the change."), 15, UIFactory.TextDim);
            note.textWrappingMode = TextWrappingModes.Normal;
            UIFactory.LE(note, h: 48);
            UIFactory.Spacer(p);
        }

        void BuildLanguage(Transform p)
        {
            UIFactory.SectionTitle(p, Loc.T("settings.tab.language", "Language"));
            languageLabel = SettingRow(p, Loc.T("settings.language", "Language"), () =>
            {
                var langs = Loc.Available();
                int i = System.Array.IndexOf(langs, GameSettings.Language);
                GameSettings.Language = langs[(i + 1) % langs.Length];
            });
            var note = UIFactory.Text(p, Loc.T("settings.language.note", "Add a translation by putting a file such as de.json next to Resources/Localization/en.json."), 15, UIFactory.TextDim);
            note.textWrappingMode = TextWrappingModes.Normal;
            UIFactory.LE(note, h: 44);
            UIFactory.Spacer(p);
        }

        void BuildControlsPage(Transform p)
        {
            UIFactory.SectionTitle(p, Loc.T("settings.tab.driving", "Driving"));
            BuildDriving(p);
            UIFactory.SectionTitle(p, Loc.T("settings.keys", "Key bindings"));
            BuildControls(p);
        }

        void ShowPage(int index)
        {
            CancelRebind();
            for (int i = 0; i < pages.Count; i++) pages[i].gameObject.SetActive(i == index);
            if (index == 3) RefreshControls();
        }

        // ------------------------------------------------------------------ DISPLAY
        void BuildDisplay(Transform p)
        {
            scaleLabel = UIFactory.Text(p, "", 19, UIFactory.TextMain, TextAlignmentOptions.Left, true);
            uiScale = UIFactory.MakeSlider(p, UIFactory.Accent, 28f);
            uiScale.minValue = 0.8f; uiScale.maxValue = 1.3f;
            uiScale.onValueChanged.AddListener(v => { GameSettings.UiScale = Mathf.Round(v * 20f) / 20f; UpdateLabels(); });
            qualityLabel = SettingRow(p, Loc.T("settings.quality", "Quality"), () =>
            {
                int n = QualitySettings.names.Length;
                int cur = GameSettings.QualityLevel < 0 ? QualitySettings.GetQualityLevel() : GameSettings.QualityLevel;
                GameSettings.QualityLevel = n == 0 ? -1 : (cur + 1) % n;
            });
            resolutionLabel = SettingRow(p, Loc.T("settings.resolution", "Resolution"), () =>
            {
                var res = Screen.resolutions;
                if (res.Length == 0) return;
                int i = System.Array.FindIndex(res, r => r.width == Screen.width && r.height == Screen.height);
                var next = res[(i + 1) % res.Length];
                Screen.SetResolution(next.width, next.height, Screen.fullScreenMode);
            });
            fullscreenLabel = SettingRow(p, Loc.T("settings.fullscreen", "Display mode"), () =>
                Screen.fullScreenMode = Screen.fullScreenMode == FullScreenMode.Windowed ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
            vsyncLabel = SettingRow(p, Loc.T("settings.vsync", "V-Sync"), () => GameSettings.VSync = !GameSettings.VSync);
            frameCapLabel = SettingRow(p, Loc.T("settings.framecap", "Frame-rate limit"), () =>
            {
                int[] caps = { 0, 30, 60, 120, 144 };
                int i = System.Array.IndexOf(caps, GameSettings.FrameCap);
                GameSettings.FrameCap = caps[(i + 1) % caps.Length];
            });
            UIFactory.Btn(p, "", UIFactory.AccentDark, () =>
            {
                GameSettings.Theme = (UIThemeChoice)(((int)GameSettings.Theme + 1) % 5);
                UpdateLabels();
            }, out themeLabel, 48f, 17f);
            UIFactory.Btn(p, "", UIFactory.AccentDark, () => { GameSettings.TrafficLevel = (GameSettings.TrafficLevel + 1) % 4; UpdateLabels(); }, out trafficLabel, 46f, 17f);
            UIFactory.Btn(p, "", UIFactory.Neutral, () => { GameSettings.ShowPerformance = !GameSettings.ShowPerformance; UpdateLabels(); }, out perfLabel, 46f, 17f);
            UIFactory.Spacer(p);
        }

        // ------------------------------------------------------------------ AUDIO
        void BuildAudio(Transform p)
        {
            volumeLabel = UIFactory.Text(p, "", 21, UIFactory.TextMain, TextAlignmentOptions.Left, true);
            volume = UIFactory.MakeSlider(p, UIFactory.Good, 32f);
            volume.minValue = 0f; volume.maxValue = 1f;
            volume.onValueChanged.AddListener(v => { GameSettings.Volume = v; UpdateLabels(); });
            for (int g = 0; g < GroupNames.Length; g++)
            {
                int group = g;
                groupLabels[g] = UIFactory.Text(p, "", 18, UIFactory.TextMain, TextAlignmentOptions.Left, true);
                groupSliders[g] = UIFactory.MakeSlider(p, UIFactory.Accent, 26f);
                groupSliders[g].minValue = 0f; groupSliders[g].maxValue = 1f;
                groupSliders[g].onValueChanged.AddListener(v => { GameSettings.SetGroupVolume(group, v); AudioManager.ApplyGroupVolumes(); UpdateLabels(); });
            }
            UIFactory.Spacer(p);
        }

        // ------------------------------------------------------------------ DRIVING
        void BuildDriving(Transform p)
        {
            UIFactory.Btn(p, "", UIFactory.AccentDark, () =>
            {
                GameSettings.Driving = GameSettings.Driving == DrivingModel.Realistic ? DrivingModel.Arcade : DrivingModel.Realistic;
                UpdateLabels();
            }, out drivingLabel, 52f, 19f);
            UIFactory.Btn(p, "", UIFactory.AccentDark, () => { GameSettings.ManualGearbox = !GameSettings.ManualGearbox; UpdateLabels(); }, out gearboxLabel, 52f, 19f);
            var assists = UIFactory.HRow(p, 52f, 10f);
            UIFactory.Btn(assists, "", UIFactory.Neutral, () => { GameSettings.Abs = !GameSettings.Abs; UpdateLabels(); }, out absLabel, 52f, 18f);
            UIFactory.Btn(assists, "", UIFactory.Neutral, () => { GameSettings.TractionControl = !GameSettings.TractionControl; UpdateLabels(); }, out tcsLabel, 52f, 18f);
            UIFactory.Btn(assists, "", UIFactory.Neutral, () => { GameSettings.StabilityControl = !GameSettings.StabilityControl; UpdateLabels(); }, out escLabel, 52f, 18f);
            var note = UIFactory.Text(p, Loc.T("settings.driving.note", "Applies the next time you get into a car. F3 while driving shows telemetry."), 15, UIFactory.TextDim);
            UIFactory.LE(note, h: 24);
            UIFactory.Spacer(p);
        }

        // ------------------------------------------------------------------ CONTROLS
        void BuildControls(Transform p)
        {
            var hint = UIFactory.Text(p, Loc.T("settings.controls.hint", "Click a key to change it, then press the new key (Escape cancels). Saved for every save game."), 15, UIFactory.TextDim);
            hint.textWrappingMode = TextWrappingModes.Normal;
            UIFactory.LE(hint, h: 44);
            controlsList = UIFactory.ScrollList(p, 6f, "Bindings");
            UIFactory.Btn(p, Loc.T("settings.controls.reset", "Reset all keys to default"), UIFactory.Bad, () =>
            {
                CancelRebind();
                CarSalesInput.ResetOverrides();
                RefreshControls();
            }, 46f, 17f);
        }

        void RefreshControls()
        {
            if (!controlsList) return;
            UIFactory.ClearChildren(controlsList);
            int shown = 0;
            foreach (var action in CarSalesInput.AllActions())
            {
                if (action == null) continue;
                var bindings = action.bindings;
                for (int i = 0; i < bindings.Count; i++)
                {
                    var b = bindings[i];
                    if (b.isComposite) continue;
                    string partName = b.isPartOfComposite ? $" ({b.name})" : "";
                    var row = UIFactory.HRow(controlsList, 40f, 10f);
                    var name = UIFactory.Text(row, Pretty(action.name) + partName, 17, UIFactory.TextMain, TextAlignmentOptions.Left, true);
                    UIFactory.LE(name, flexW: 1);
                    string keyText = action.GetBindingDisplayString(i);
                    if (string.IsNullOrEmpty(keyText)) keyText = "-";
                    int index = i;
                    var act = action;
                    TextMeshProUGUI keyLabel = null;
                    var btn = UIFactory.Btn(row, keyText, UIFactory.CardLight, () => StartRebind(act, index, keyLabel), out keyLabel, 40f, 16f);
                    UIFactory.LE(btn, h: 40, w: 220, flexW: 0);
                    shown++;
                }
            }
            if (shown == 0) UIFactory.EmptyState(controlsList, Loc.T("settings.controls.none", "No controls found"), Loc.T("settings.controls.none.hint", "Load a game once so the input actions are linked."));
        }

        static string Pretty(string actionName)
        {
            if (string.IsNullOrEmpty(actionName)) return "";
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < actionName.Length; i++)
            {
                char c = actionName[i];
                if (i > 0 && char.IsUpper(c) && !char.IsUpper(actionName[i - 1])) sb.Append(' ');
                sb.Append(c);
            }
            return Loc.T("action." + actionName, sb.ToString());
        }

        void StartRebind(InputAction action, int bindingIndex, TextMeshProUGUI label)
        {
            CancelRebind();
            if (action == null) return;
            if (label) label.text = Loc.T("settings.controls.press", "Press a key...");
            bool wasEnabled = action.enabled;
            rebindAction = action;
            rebindWasEnabled = wasEnabled;
            action.Disable();
            rebinding = action.PerformInteractiveRebinding(bindingIndex);
            if (rebinding == null) { if (wasEnabled) action.Enable(); RefreshControls(); return; }
            rebinding.WithControlsExcluding("<Mouse>/position")
                     .WithControlsExcluding("<Mouse>/delta")
                     .WithCancelingThrough("<Keyboard>/escape")
                     .OnMatchWaitForAnother(0.1f)
                     .OnComplete(op => { FinishRebind(action, wasEnabled); CarSalesInput.SaveOverrides(); })
                     .OnCancel(op => FinishRebind(action, wasEnabled))
                     .Start();
        }

        void FinishRebind(InputAction action, bool enable)
        {
            if (rebinding != null) { rebinding.Dispose(); rebinding = null; }
            rebindAction = null;
            if (enable && action != null) action.Enable();
            RefreshControls();
        }

        void CancelRebind()
        {
            if (rebinding == null) return;
            var op = rebinding;
            rebinding = null;
            op.Dispose();
            if (rebindAction != null && rebindWasEnabled) rebindAction.Enable();
            rebindAction = null;
        }

        void OnDisable() => CancelRebind();

        // ------------------------------------------------------------------ COMMON
        void OnEnable()
        {
            if (!sensitivity) return;
            sensitivity.SetValueWithoutNotify(GameSettings.MouseSensitivity);
            volume.SetValueWithoutNotify(GameSettings.Volume);
            if (uiScale) uiScale.SetValueWithoutNotify(GameSettings.UiScale);
            for (int g = 0; g < groupSliders.Length; g++) if (groupSliders[g]) groupSliders[g].SetValueWithoutNotify(GameSettings.GroupVolume(g));
            UpdateLabels();
        }

        public void Show()
        {
            if (CarSalesServices.UI) OpenSelf();
            else gameObject.SetActive(true);
        }

        void Back()
        {
            CancelRebind();
            if (CarSalesServices.UI && CarSalesServices.UI.pause) { CarSalesServices.UI.pause.Show(); return; }
            gameObject.SetActive(false);
            if (showOnBackInMenu) showOnBackInMenu.SetActive(true);
        }

        static string OnOff(bool on) => on ? Loc.T("common.on", "ON") : Loc.T("common.off", "OFF");

        void UpdateLabels()
        {
            sensitivityLabel.text = $"{Loc.T("settings.sensitivity", "Mouse sensitivity")}: {GameSettings.MouseSensitivity:0.00}";
            volumeLabel.text = $"{Loc.T("settings.volume", "Master volume")}: {GameSettings.Volume * 100f:0}%";
            if (!drivingLabel) return;
            for (int g = 0; g < groupLabels.Length; g++)
                if (groupLabels[g]) groupLabels[g].text = $"{Loc.T("settings.group." + g, GroupNames[g])}: {GameSettings.GroupVolume(g) * 100f:0}%";
            subtitlesLabel.text = Toggle(GameSettings.Subtitles);
            invertLabel.text = Toggle(GameSettings.InvertY);
            hintsLabel.text = Toggle(GameSettings.ShowHints);
            autoSaveLabel.text = Toggle(GameSettings.AutoSave);
            vsyncLabel.text = Toggle(GameSettings.VSync);
            int q = GameSettings.QualityLevel < 0 ? QualitySettings.GetQualityLevel() : GameSettings.QualityLevel;
            qualityLabel.text = QualitySettings.names.Length > q && q >= 0 ? QualitySettings.names[q] : "-";
            resolutionLabel.text = $"{Screen.width} × {Screen.height}";
            fullscreenLabel.text = Screen.fullScreenMode == FullScreenMode.Windowed ? Loc.T("settings.windowed", "Windowed") : Loc.T("settings.fullscreen.on", "Full screen");
            frameCapLabel.text = GameSettings.FrameCap <= 0 ? Loc.T("settings.unlimited", "Unlimited") : GameSettings.FrameCap + " fps";
            viewLabel.text = GameSettings.ThirdPerson ? Loc.T("settings.view.third", "Third person") : Loc.T("settings.view.first", "First person");
            languageLabel.text = GameSettings.Language.ToUpper();
            difficultyLabel.text = DifficultyNames[GameSettings.Difficulty];
            scaleLabel.text = $"{Loc.T("settings.uiscale", "Interface size")}: {GameSettings.UiScale * 100f:0}%";
            string theme = GameSettings.Theme == UIThemeChoice.ColourBlind ? "COLOUR-BLIND SAFE" : GameSettings.Theme.ToString().ToUpper();
            themeLabel.text = $"{Loc.T("settings.theme", "Theme")}: {theme}  ({Loc.T("settings.theme.note", "applies after loading / main menu")})" +
                              (GameSettings.Theme == UIThemeChoice.Custom ? "  - Resources/CarSalesUITheme" : "");
            drivingLabel.text = GameSettings.Driving == DrivingModel.Realistic ? Loc.T("settings.physics.realistic", "Physics: REALISTIC (wheels, gearbox, weight)") : Loc.T("settings.physics.arcade", "Physics: ARCADE (simple)");
            gearboxLabel.text = GameSettings.ManualGearbox ? Loc.T("settings.gearbox.manual", "Gearbox: MANUAL (Left Shift up, Left Ctrl down)") : Loc.T("settings.gearbox.auto", "Gearbox: AUTOMATIC");
            absLabel.text = "ABS: " + OnOff(GameSettings.Abs);
            tcsLabel.text = $"{Loc.T("settings.tcs", "Traction control")}: {OnOff(GameSettings.TractionControl)}";
            escLabel.text = $"{Loc.T("settings.esc", "Stability")}: {OnOff(GameSettings.StabilityControl)}";
            string[] traffic = { "OFF", "LOW", "NORMAL", "BUSY" };
            trafficLabel.text = $"{Loc.T("settings.traffic", "Traffic & people")}: {traffic[GameSettings.TrafficLevel]}";
            perfLabel.text = $"{Loc.T("settings.perf", "Frame-rate overlay")}: {OnOff(GameSettings.ShowPerformance)}";
        }
    }
}
