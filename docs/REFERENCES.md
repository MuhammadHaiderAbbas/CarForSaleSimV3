# References

> The links below were written from knowledge and were **not** live-checked when this project was made
> (the build machine had no web access). If one has moved, search for the page title on the same site.

## A. Every file in this project

### Scripts (`Assets/CarSales/Scripts`, assembly `CarSales.Runtime`)

| File | Type | What it does |
|---|---|---|
| `Core/CarSalesServices.cs` | static class | Service locator: `Database`, `Economy`, `Inventory`, `Marketplace`, `DayCycle`, `Save`, `UI`, `Dealership`, `Spawner` |
| `Core/IInteractable.cs` | interface | `CanInteract()`, `GetInteractionText()`, `Interact(GameObject)` |
| `Core/CarSalesBootstrap.cs` | MonoBehaviour | Registers the `CarDatabase` asset |
| `Core/Billboard.cs` | MonoBehaviour | Makes world-space text face the camera |
| `Core/RuntimeNavMeshBaker.cs` | MonoBehaviour | Bakes the `NavMeshSurface` at runtime if it has no data |
| `Core/GameSettings.cs` | static class | Mouse sensitivity and volume (PlayerPrefs), new-game flag |
| `Core/ButtonSound.cs` | MonoBehaviour | Click sound on a Button |
| `Core/MenuShowcase.cs` | MonoBehaviour | Spinning, colour-changing car in the main menu |
| `Core/CarSalesInput.cs` | static class | Safe input lookup with built-in default keys |
| `Core/ThumbnailService.cs` | MonoBehaviour | Renders live car photos for the UI |
| `Core/MinimapMarker.cs` | MonoBehaviour | Coloured minimap dot above an object |
| `Data/CarParts.cs` | static class + enum | Per-part condition, labels, repair names, crash damage |
| `Data/GameMessage.cs` | serializable | Inbox message (offers, tips, bank, goals) |
| `Systems/MessageManager.cs` | MonoBehaviour | Inbox, daily private-seller offers |
| `Systems/VehicleRegistry.cs` | MonoBehaviour | All owned car objects: spawning, parking, refuelling, selling |
| `Systems/PropertyManager.cs` | MonoBehaviour | Homes, extra dealerships, investment properties |
| `Systems/TowService.cs` | MonoBehaviour | Tow trucks and taxis (distance-based prices), player teleport |
| `Systems/AuctionManager.cs` | MonoBehaviour | Daily auction lots and opening hours |
| `Core/MapPlace.cs`, `Core/GpsSystem.cs` | MonoBehaviour / static | Map places and the GPS target |
| `Core/ServiceZone.cs` | MonoBehaviour | Service areas with tow bays |
| `World/HomeBed.cs` | MonoBehaviour + IInteractable | Front door: sleep / move in |
| `World/PropertySign.cs` | MonoBehaviour + IInteractable | FOR SALE sign |
| `World/ServiceDesk.cs` | MonoBehaviour + IInteractable | Mechanic / car wash / fuel kiosk / auction desk |
| `World/UsedCarMarket.cs` | MonoBehaviour | Shows the lot's for-sale cars |
| `Data/PropertyInfo.cs` | serializable | Property definition |
| `UI/MapUI.cs`, `UI/PhoneUI.cs`, `UI/AuctionUI.cs`, `UI/RealEstateUI.cs` | UIPanel / ComputerPage | Map & GPS, phone, live auction, real estate |
| `UI/Framework/UIFactory.cs` | static class | Theme colours, generated sprites and all UI building helpers |
| `UI/Framework/UIPanel.cs` | base classes | `UIPanel` (full-screen window) and `ComputerPage` |
| `UI/Framework/NotificationToast.cs` | MonoBehaviour | Notification card |
| `UI/HomePageUI.cs`, `UI/InventoryUI.cs`, `UI/MessagesUI.cs` | ComputerPage | Dashboard, inventory tabs, inbox |
| `UI/VehicleDetailsUI.cs`, `UI/InspectionUI.cs`, `UI/PurchaseConfirmUI.cs` | UIPanel | Listing details, inspection report, purchase confirmation |
| `UI/GarageUI.cs`, `UI/CarWashUI.cs` | UIPanel | Repairs and washing |
| `Data/GameStats.cs` | serializable | Lifetime statistics (drive the goals) |
| `Data/Enums.cs` | enums | `CarCategory`, `CarState`, `CustomerState`, `NegotiationResult` |
| `Data/CarDefinition.cs` | ScriptableObject | One car model's static data (*Create → CarSales → Car Definition*) |
| `Data/CarDatabase.cs` | ScriptableObject | List of car definitions (*Create → CarSales → Car Database*) |
| `Data/CarInstance.cs` | serializable | One specific car (saved) |
| `Data/CustomerProfile.cs` | serializable | Customer budget, taste, patience, greed |
| `Data/DailyReport.cs` | serializable | Day statistics |
| `Data/SaveData.cs` | serializable | Root of the JSON save file |
| `Systems/EconomyManager.cs` | MonoBehaviour | Money, reputation, `TrySpend`, `AddMoney`, events |
| `Systems/CarValueCalculator.cs` | static class | Market value, repair cost, random car generation |
| `Systems/InventoryManager.cs` | MonoBehaviour | Owned cars (source of truth) |
| `Systems/MarketplaceManager.cs` | MonoBehaviour | Daily listings, `TryBuy` |
| `Systems/DayCycleManager.cs` | MonoBehaviour | Clock 08–18, open/close, rent, daily report, next day |
| `Systems/SaveManager.cs` | MonoBehaviour | JSON save/load (v2 format, reads v1), new game with starter cars |
| `Systems/UpgradeManager.cs` | MonoBehaviour | 5 upgrade types, levels, costs, effect properties |
| `Systems/BankManager.cs` | MonoBehaviour | Loans, limit, daily interest |
| `Systems/ProgressManager.cs` | MonoBehaviour | Stats, 13 goals with rewards, net worth, win, bankruptcy |
| `Systems/AudioManager.cs` | MonoBehaviour | Procedurally generated sound effects (+ optional clip overrides) |
| `Systems/TestDriveManager.cs` | MonoBehaviour | Test drive mode, chase camera, crash damage |
| `Systems/DayNightLighting.cs` | MonoBehaviour | Moves the sun with the shop clock |
| `Vehicles/ArcadeCarController.cs` | MonoBehaviour | Arcade driving on a CharacterController |
| `Dealership/DealershipManager.cs` | MonoBehaviour | Parking spots, spawn/despawn car visuals, `CompleteSale` |
| `Dealership/ParkingSpot.cs` | MonoBehaviour | One parking bay (index, occupant, gizmo) |
| `Dealership/CarDisplay.cs` | MonoBehaviour + IInteractable | A car on the lot; opens the car menu |
| `Dealership/CarVisual.cs` | MonoBehaviour | Applies paint colour and dirt to a model via MaterialPropertyBlock |
| `Dealership/DealerComputer.cs` | MonoBehaviour + IInteractable | Opens the marketplace |
| `Dealership/ShopToggleSign.cs` | MonoBehaviour + IInteractable | OPEN/CLOSED sign that toggles the shop |
| `Player/SimpleFirstPersonController.cs` | MonoBehaviour | WASD + mouse look, sprint, jump (CharacterController) |
| `Player/PlayerInteractor.cs` | MonoBehaviour | Raycast + E to interact; exposes `CurrentPrompt` |
| `Player/MenuInputBlocker.cs` | MonoBehaviour | Disables the listed player scripts while a menu is open |
| `Player/DebugCheats.cs` | MonoBehaviour | M money, F5 save, F9 load, F12 delete save |
| `Customers/CustomerAI.cs` | MonoBehaviour + IInteractable | Browse → Interested → Negotiate → Leave state machine |
| `Customers/CustomerSpawner.cs` | MonoBehaviour | Spawns customers with random profiles while open |
| `Customers/NegotiationSession.cs` | plain class | Haggling logic (hidden max price, patience, counters) |
| `UI/UIManager.cs` | MonoBehaviour | Open/close panels, cursor, notifications, Esc |
| `UI/HUDController.cs` | MonoBehaviour | Money, reputation, loan, clock, shop state, current goal, prompt / test-drive info |
| `UI/ComputerHubUI.cs` | MonoBehaviour | Computer start screen (Marketplace, Upgrades, Bank, Stats) |
| `UI/UpgradesUI.cs`, `UI/UpgradeRowUI.cs` | MonoBehaviour | Upgrade list and rows |
| `UI/BankUI.cs` | MonoBehaviour | Borrow / repay |
| `UI/StatsUI.cs` | MonoBehaviour | Statistics and net worth |
| `UI/PauseMenuUI.cs` | MonoBehaviour | Resume, save, settings, main menu, quit |
| `UI/SettingsUI.cs` | MonoBehaviour | Sensitivity and volume sliders (in-game and main menu) |
| `UI/EndScreenUI.cs` | MonoBehaviour | Win / bankrupt screen |
| `UI/MainMenuUI.cs` | MonoBehaviour | Main menu: Continue, New Game, Settings, Quit |
| `UI/MarketplaceUI.cs` | MonoBehaviour | Marketplace window |
| `UI/MarketListingItemUI.cs` | MonoBehaviour | One marketplace row |
| `UI/CarMenuUI.cs` | MonoBehaviour | Car details, repair, clean, open the price panel |
| `UI/PriceUI.cs` | MonoBehaviour | Price slider, list/unlist |
| `UI/NegotiationUI.cs` | MonoBehaviour | Negotiation window and chat log |
| `UI/DailyReportUI.cs` | MonoBehaviour | End-of-day report, Next Day button |

### Editor tools (`Assets/CarSales/Editor`)

| File | What it does |
|---|---|
| `CarSalesWorldBuilder.cs` | Builds the whole town (roads, 3 dealerships, services, auction, homes, properties, skyline) |
| `CarSalesSetupWizard.cs` | Menu **Tools → Car Sales**: *Build Everything*, *Open Test Scene*, *Open Main Menu Scene*, *Delete Save File*, *Show Save Folder* |
| `Installer/CarSalesDependencyInstaller.cs` | *1. Check & Install Packages*. Has no dependencies, so it always compiles |
| `CarSales.Editor.asmdef`, `Installer/CarSales.Installer.asmdef` | Editor-only assemblies |

### Other assets

| File | What it is |
|---|---|
| `Scripts/CarSales.Runtime.asmdef` | Runtime assembly. Only compiles when Input System, AI Navigation and uGUI are installed (*define constraints*) |
| `Input/CarSalesControls.inputactions` | Action map **CarSales**: Move, Look, Jump, Sprint, Interact, ToggleShop, CloseMenu |
| `Generated/…` | Created by the wizard: 15 materials, 8 car models, 8 `CarDefinition`s + `CarDatabase`, prefabs, `MainMenu` + `HarbourCity` scenes + NavMesh |

### Generated car list (fictional)

| carId | Name | Category | Year | Base price |
|---|---|---|---|---|
| hatch_velo | Kestrel Velo | Hatchback | 2016 | $12,000 |
| hatch_metro | Kestrel Metro S | Hatchback | 2019 | $15,500 |
| sedan_avenue | Orion Avenue | Sedan | 2014 | $18,000 |
| sedan_avenuegt | Orion Avenue GT | Sedan | 2020 | $26,000 |
| suv_ridge | Titan Ridge | SUV | 2017 | $32,000 |
| sports_stallion | Vortex Stallion R | Sports | 2018 | $55,000 |
| pickup_hauler | Titan Hauler | Pickup | 2015 | $28,000 |
| lux_regent | Aurelia Regent | Luxury | 2019 | $70,000 |

## B. Unity packages used

| Package | id | Why |
|---|---|---|
| Input System | `com.unity.inputsystem` | All controls, UI input module |
| AI Navigation | `com.unity.ai.navigation` | `NavMeshSurface` baking |
| Unity UI + TextMeshPro | `com.unity.ugui` | Canvas UI and all text (TMP is part of uGUI in Unity 6) |
| Universal RP | `com.unity.render-pipelines.universal` | Rendering (from the *Universal 3D* template) |

## C. Official Unity documentation

**Getting started**
- Unity Hub / download: https://unity.com/download
- Unity Manual: https://docs.unity3d.com/Manual/index.html
- Unity Learn (free courses, e.g. *Unity Essentials*, *Junior Programmer*): https://learn.unity.com

**Packages**
- Input System: https://docs.unity3d.com/Packages/com.unity.inputsystem@latest
- AI Navigation (NavMeshSurface): https://docs.unity3d.com/Packages/com.unity.ai.navigation@latest
- Unity UI (uGUI) & TextMeshPro: https://docs.unity3d.com/Packages/com.unity.ugui@latest
- Universal Render Pipeline: https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@latest

**Concepts used in this project**
- ScriptableObject: https://docs.unity3d.com/Manual/class-ScriptableObject.html
- JSON serialization: https://docs.unity3d.com/Manual/JSONSerialization.html
- Assembly definitions: https://docs.unity3d.com/Manual/ScriptCompilationAssemblyDefinitionFiles.html
- Layers: https://docs.unity3d.com/Manual/Layers.html
- Exporting packages: https://docs.unity3d.com/Manual/AssetPackagesCreate.html

**Scripting API**
- ScriptableObject: https://docs.unity3d.com/ScriptReference/ScriptableObject.html
- JsonUtility: https://docs.unity3d.com/ScriptReference/JsonUtility.html
- Application.persistentDataPath: https://docs.unity3d.com/ScriptReference/Application-persistentDataPath.html
- NavMeshAgent: https://docs.unity3d.com/ScriptReference/AI.NavMeshAgent.html
- NavMeshObstacle: https://docs.unity3d.com/ScriptReference/AI.NavMeshObstacle.html
- CharacterController: https://docs.unity3d.com/ScriptReference/CharacterController.html
- MaterialPropertyBlock: https://docs.unity3d.com/ScriptReference/MaterialPropertyBlock.html
- Physics.Raycast: https://docs.unity3d.com/ScriptReference/Physics.Raycast.html
- Coroutines: https://docs.unity3d.com/ScriptReference/Coroutine.html
- AudioClip.Create (procedural sounds): https://docs.unity3d.com/ScriptReference/AudioClip.Create.html
- PlayerPrefs (settings): https://docs.unity3d.com/ScriptReference/PlayerPrefs.html
- SceneManager.LoadScene (menus): https://docs.unity3d.com/ScriptReference/SceneManagement.SceneManager.LoadScene.html
- Time.timeScale (pause): https://docs.unity3d.com/ScriptReference/Time-timeScale.html
- PrefabUtility (used by the wizard): https://docs.unity3d.com/ScriptReference/PrefabUtility.html
- SerializedObject (used by the wizard): https://docs.unity3d.com/ScriptReference/SerializedObject.html
- PackageManager.Client (used by the installer): https://docs.unity3d.com/ScriptReference/PackageManager.Client.html

## D. Free assets to replace the placeholders

Always check each asset's licence before shipping. CC0 = free for any use, no credit needed.
**For a realistic look, follow [REALISM_GUIDE.md](REALISM_GUIDE.md)** and avoid the low-poly packs below (Kenney,
Quaternius): they suit a stylised game, not this one.

| What | Where | Notes |
|---|---|---|
| Low-poly cars | **Kenney – Car Kit**: https://kenney.nl/assets/car-kit | CC0 |
| City buildings | Kenney – City Kit (Commercial): https://kenney.nl/assets/city-kit-commercial | CC0 |
| Roads | Kenney – City Kit (Roads): https://kenney.nl/assets/city-kit-roads | CC0 |
| Characters | Kenney – Mini Characters: https://kenney.nl/assets/mini-characters | CC0 |
| UI sprites | Kenney – UI Pack: https://kenney.nl/assets/ui-pack | CC0 |
| UI sounds | Kenney – Interface Sounds: https://kenney.nl/assets/interface-sounds | CC0 |
| Vehicles, characters, props | Quaternius: https://quaternius.com | CC0 |
| Humanoid characters + walk/idle animations | Mixamo: https://www.mixamo.com | Free with an Adobe account |
| Textures, HDRI skies, models | Poly Haven: https://polyhaven.com | CC0 |
| PBR textures (asphalt, concrete, tiles) | ambientCG: https://ambientcg.com | CC0 |
| Sound effects | Freesound: https://freesound.org | Per-sound licence (CC0 / CC-BY) |
| Fonts | Google Fonts: https://fonts.google.com | Mostly OFL |
| Free Unity assets (search "free car", "city", "Starter Assets") | Unity Asset Store: https://assetstore.unity.com | Standard Unity EULA |
| More marketplaces | Fab: https://www.fab.com · Sketchfab free models: https://sketchfab.com/features/free-3d-models · OpenGameArt: https://opengameart.org | Check each licence |

**Using Kenney's Car Kit (recommended first swap)**
1. Download it and drag its `.fbx` (or `.glb` with a glTF importer) files into `Assets/CarSales/MyCars/Models`.
2. For each car, make a prefab → add `CarVisual` → assign the body renderer(s).
3. Point the matching `CarDefinition.displayPrefab` at it (see HOW_IT_WORKS §7).

## E. Tools
- Code editor: Visual Studio Community https://visualstudio.microsoft.com/vs/community/ ·
  VS Code https://code.visualstudio.com · JetBrains Rider https://www.jetbrains.com/rider/
- 3D modelling: Blender https://www.blender.org
- Version control: Git https://git-scm.com · GitHub Desktop https://desktop.github.com
  (a Unity `.gitignore` is included in this zip)
