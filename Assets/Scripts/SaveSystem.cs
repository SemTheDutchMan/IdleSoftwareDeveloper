using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CodeClicker
{
    [DefaultExecutionOrder(1000)]
    public sealed class SaveSystem : MonoBehaviour
    {
        private const int CurrentSaveVersion = 1;
        private const float AutoSaveInterval = 60f;
        private const string SaveFileName = "code-clicker-save.json";

        private static SaveSystem instance;

        private MoneySystem moneySystem;
        private CodeTypingGenerator typingGenerator;
        private Text saveButtonText;
        private Text startupMessageText;
        private Button continueButton;
        private GameObject startupPrompt;
        private GameObject quitPrompt;
        private float secondsUntilAutoSave;
        private float timeScaleBeforeModal = 1f;
        private bool isInitialized;
        private bool hasChosenStartupMode;
        private bool isQuitting;

        public static bool IsModalOpen
        {
            get { return instance != null && instance.HasVisibleModal; }
        }

        private bool HasVisibleModal
        {
            get
            {
                bool startupIsOpen = startupPrompt != null && startupPrompt.activeSelf;
                bool quitIsOpen = quitPrompt != null && quitPrompt.activeSelf;
                return startupIsOpen || quitIsOpen;
            }
        }

        private string SavePath
        {
            get { return Path.Combine(Application.persistentDataPath, SaveFileName); }
        }

        private string BackupPath { get { return SavePath + ".bak"; } }
        private string TemporaryPath { get { return SavePath + ".tmp"; } }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureSaveSystemExists()
        {
            if (instance != null || FindFirstObjectByType<SaveSystem>() != null)
            {
                return;
            }

            GameObject saveObject = new GameObject("SaveSystem");
            saveObject.AddComponent<SaveSystem>();
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
            secondsUntilAutoSave = AutoSaveInterval;
        }

        private void Start()
        {
            InitializeCurrentScene();
        }

        private void Update()
        {
            if (!isInitialized)
            {
                return;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null &&
                keyboard.escapeKey.wasPressedThisFrame &&
                !TestCommandBar.EscapeHandledThisFrame)
            {
                if (quitPrompt != null && quitPrompt.activeSelf)
                {
                    CancelQuit();
                }
                else if (startupPrompt == null || !startupPrompt.activeSelf)
                {
                    ShowQuitPrompt();
                }
            }

            if (!hasChosenStartupMode)
            {
                return;
            }

            secondsUntilAutoSave -= Time.unscaledDeltaTime;
            if (secondsUntilAutoSave <= 0f)
            {
                SaveGame(false);
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused && isInitialized && hasChosenStartupMode)
            {
                SaveGame(false);
            }
        }

        private void OnApplicationQuit()
        {
            isQuitting = true;
            if (isInitialized && hasChosenStartupMode)
            {
                SaveGame(false);
            }
        }

        private void OnDestroy()
        {
            if (instance != this)
            {
                return;
            }

            SceneManager.sceneLoaded -= OnSceneLoaded;
            instance = null;
        }

        public static void RequestSave()
        {
            if (instance != null && instance.isInitialized && instance.hasChosenStartupMode)
            {
                instance.SaveGame(false);
            }
        }

        public static void RequestLoad()
        {
            if (instance != null)
            {
                instance.LoadGame();
            }
        }

        public void SaveGameFromButton()
        {
            if (hasChosenStartupMode)
            {
                SaveGame(true);
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!isQuitting)
            {
                StartCoroutine(InitializeNextFrame());
            }
        }

        private IEnumerator InitializeNextFrame()
        {
            yield return null;
            InitializeCurrentScene();
        }

        private void InitializeCurrentScene()
        {
            moneySystem = FindFirstObjectByType<MoneySystem>();
            typingGenerator = FindFirstObjectByType<CodeTypingGenerator>();
            if (moneySystem == null)
            {
                isInitialized = false;
                return;
            }

            CreateSaveButton();
            isInitialized = true;
            if (hasChosenStartupMode)
            {
                LoadGame();
            }
            else
            {
                CreateStartupPrompt();
            }

            secondsUntilAutoSave = AutoSaveInterval;
        }

        private void SaveGame(bool showConfirmation, bool replacePreviousSave = false)
        {
            if (!isInitialized || moneySystem == null)
            {
                return;
            }

            try
            {
                SaveData data = CaptureSaveData();
                string json = JsonUtility.ToJson(data, true);
                WriteSaveFile(json, replacePreviousSave);

                secondsUntilAutoSave = AutoSaveInterval;
                if (showConfirmation)
                {
                    SetButtonText($"SAVED  {DateTime.Now:HH:mm}");
                }
                else
                {
                    SetButtonText("SAVE GAME");
                }
            }
            catch (Exception exception)
            {
                Debug.LogError($"[Code Clicker] Saving failed: {exception.Message}", this);
                SetButtonText("SAVE FAILED");
            }
        }

        private void WriteSaveFile(string json, bool replacePreviousSave)
        {
            Directory.CreateDirectory(Application.persistentDataPath);
            File.WriteAllText(TemporaryPath, json);

            if (!File.Exists(SavePath))
            {
                File.Move(TemporaryPath, SavePath);
                return;
            }

            if (replacePreviousSave)
            {
                File.Copy(TemporaryPath, SavePath, true);
                File.Delete(TemporaryPath);
                if (File.Exists(BackupPath))
                {
                    File.Delete(BackupPath);
                }

                return;
            }

            if (File.Exists(BackupPath))
            {
                File.Delete(BackupPath);
            }

            File.Replace(TemporaryPath, SavePath, BackupPath);
        }

        private bool LoadGame()
        {
            SaveData data = TryReadSave(SavePath);
            if (data == null)
            {
                data = TryReadSave(BackupPath);
            }

            if (data == null)
            {
                SetButtonText("SAVE GAME");
                return false;
            }

            moneySystem.RestoreState(data.coins, data.coinsPerKey, data.passiveCoinsPerSecond);

            if (typingGenerator != null)
            {
                typingGenerator.RestoreState(
                    data.totalKeyPresses,
                    data.keysPerLine,
                    data.visibleCode,
                    data.pressedKeysForCurrentLine,
                    data.hasStartedTyping);
            }

            Dictionary<string, int> savedUpgradeCounts = new Dictionary<string, int>(StringComparer.Ordinal);
            if (data.upgrades != null)
            {
                foreach (UpgradeSaveData savedUpgrade in data.upgrades)
                {
                    if (savedUpgrade != null && !string.IsNullOrWhiteSpace(savedUpgrade.upgradeName))
                    {
                        savedUpgradeCounts[savedUpgrade.upgradeName] = savedUpgrade.purchaseCount;
                    }
                }
            }

            WorkspaceVisuals workspaceVisuals = FindFirstObjectByType<WorkspaceVisuals>();
            if (workspaceVisuals != null)
            {
                workspaceVisuals.ResetVisuals();
            }

            StoreUpgrades[] upgrades = FindObjectsByType<StoreUpgrades>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            foreach (StoreUpgrades upgrade in upgrades)
            {
                int count = 0;
                if (savedUpgradeCounts.TryGetValue(upgrade.UpgradeName, out int savedCount))
                {
                    count = savedCount;
                }

                upgrade.RestorePurchaseCount(count);
            }

            SetButtonText("SAVE GAME");
            secondsUntilAutoSave = AutoSaveInterval;
            Debug.Log($"[Code Clicker] Save loaded from {SavePath}", this);
            return true;
        }

        private void ContinueWithSave()
        {
            if (!LoadGame())
            {
                if (startupMessageText != null)
                {
                    startupMessageText.text = "Save kon niet worden geladen. Start een nieuwe game.";
                }

                if (continueButton != null)
                {
                    continueButton.interactable = false;
                }

                return;
            }

            hasChosenStartupMode = true;
            HideModal(startupPrompt);
        }

        private void StartNewGame()
        {
            moneySystem.RestoreState(0d, 0.01d, 0d);
            if (typingGenerator != null)
            {
                typingGenerator.StartNewGame();
            }

            StoreUpgrades[] upgrades = FindObjectsByType<StoreUpgrades>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            foreach (StoreUpgrades upgrade in upgrades)
            {
                upgrade.RestorePurchaseCount(0);
            }

            WorkspaceVisuals workspaceVisuals = FindFirstObjectByType<WorkspaceVisuals>();
            if (workspaceVisuals != null)
            {
                workspaceVisuals.ResetVisuals();
            }

            hasChosenStartupMode = true;
            HideModal(startupPrompt);
            SaveGame(false, true);
            SetButtonText("SAVE GAME");
        }

        private void ShowQuitPrompt()
        {
            if (!hasChosenStartupMode)
            {
                return;
            }

            if (quitPrompt == null)
            {
                CreateQuitPrompt();
            }

            ShowModal(quitPrompt);
        }

        private void CancelQuit()
        {
            HideModal(quitPrompt);
        }

        private void SaveAndQuit()
        {
            SaveGame(false);
            isQuitting = true;

#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void ShowModal(GameObject modal)
        {
            if (modal == null || modal.activeSelf)
            {
                return;
            }

            if (!HasVisibleModal)
            {
                timeScaleBeforeModal = Time.timeScale;
            }

            modal.SetActive(true);
            modal.transform.SetAsLastSibling();
            Time.timeScale = 0f;
        }

        private void HideModal(GameObject modal)
        {
            if (modal == null)
            {
                return;
            }

            modal.SetActive(false);
            if (!HasVisibleModal)
            {
                Time.timeScale = timeScaleBeforeModal;
            }
        }

        private SaveData CaptureSaveData()
        {
            SaveData data = new SaveData();
            data.version = CurrentSaveVersion;
            data.savedAtUtcTicks = DateTime.UtcNow.Ticks;
            data.coins = moneySystem.Coins;
            data.coinsPerKey = moneySystem.CoinsPerKey;
            data.passiveCoinsPerSecond = moneySystem.PassiveCoinsPerSecond;

            if (typingGenerator != null)
            {
                data.totalKeyPresses = typingGenerator.TotalKeyPresses;
                data.keysPerLine = typingGenerator.KeysPerLine;
                data.visibleCode = typingGenerator.VisibleCode;
                data.pressedKeysForCurrentLine = typingGenerator.PressedKeysForCurrentLine;
                data.hasStartedTyping = typingGenerator.HasStartedTyping;
            }

            StoreUpgrades[] upgrades = FindObjectsByType<StoreUpgrades>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            foreach (StoreUpgrades upgrade in upgrades)
            {
                data.upgrades.Add(new UpgradeSaveData
                {
                    upgradeName = upgrade.UpgradeName,
                    purchaseCount = upgrade.PurchaseCount
                });
            }

            return data;
        }

        private SaveData TryReadSave(string path)
        {
            if (!File.Exists(path))
            {
                return null;
            }

            try
            {
                SaveData data = JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
                if (data == null || data.version <= 0 || data.version > CurrentSaveVersion)
                {
                    throw new InvalidDataException("The save version is not supported.");
                }

                return data;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[Code Clicker] Could not read save '{path}': {exception.Message}", this);
                return null;
            }
        }

        private void CreateStartupPrompt()
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                if (LoadGame())
                {
                    hasChosenStartupMode = true;
                }
                else
                {
                    StartNewGame();
                }

                return;
            }

            startupPrompt = CreateModalRoot(canvas.transform, "StartupSavePrompt");
            RectTransform panel = CreateModalPanel(startupPrompt.transform, new Vector2(520f, 260f));

            CreateModalText(
                panel,
                "Title",
                "Doorgaan met save?",
                27,
                new Vector2(0.07f, 0.68f),
                new Vector2(0.93f, 0.91f),
                new Color(0.50f, 0.92f, 1f, 1f));

            bool hasUsableSave = TryReadSave(SavePath) != null || TryReadSave(BackupPath) != null;
            string message = "Er is nog geen save. Start een nieuwe game.";
            if (hasUsableSave)
            {
                message = "Kies DOORGAAN om je save te laden, of NIEUWE GAME om deze te overschrijven.";
            }

            startupMessageText = CreateModalText(
                panel,
                "Message",
                message,
                15,
                new Vector2(0.08f, 0.43f),
                new Vector2(0.92f, 0.67f),
                Color.white);

            continueButton = CreateModalButton(
                panel,
                "ContinueButton",
                "DOORGAAN",
                new Vector2(0.08f, 0.10f),
                new Vector2(0.48f, 0.34f),
                ContinueWithSave);
            continueButton.interactable = hasUsableSave;

            CreateModalButton(
                panel,
                "NewGameButton",
                "NIEUWE GAME",
                new Vector2(0.52f, 0.10f),
                new Vector2(0.92f, 0.34f),
                StartNewGame);

            ShowModal(startupPrompt);
        }

        private void CreateQuitPrompt()
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                return;
            }

            quitPrompt = CreateModalRoot(canvas.transform, "SaveAndQuitPrompt");
            RectTransform panel = CreateModalPanel(quitPrompt.transform, new Vector2(440f, 220f));

            CreateModalText(
                panel,
                "Title",
                "Save & quit?",
                28,
                new Vector2(0.08f, 0.65f),
                new Vector2(0.92f, 0.90f),
                new Color(1f, 0.72f, 0.23f, 1f));
            CreateModalText(
                panel,
                "Message",
                "Je voortgang wordt opgeslagen voordat de game sluit.",
                15,
                new Vector2(0.08f, 0.43f),
                new Vector2(0.92f, 0.65f),
                Color.white);

            CreateModalButton(
                panel,
                "SaveAndQuitButton",
                "SAVE & QUIT",
                new Vector2(0.08f, 0.10f),
                new Vector2(0.55f, 0.34f),
                SaveAndQuit);
            CreateModalButton(
                panel,
                "CancelButton",
                "ANNULEREN",
                new Vector2(0.59f, 0.10f),
                new Vector2(0.92f, 0.34f),
                CancelQuit);
        }

        private static GameObject CreateModalRoot(Transform parent, string name)
        {
            GameObject root = new GameObject(name, typeof(RectTransform), typeof(Image));
            root.transform.SetParent(parent, false);
            root.transform.SetAsLastSibling();

            RectTransform rect = root.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image overlay = root.GetComponent<Image>();
            overlay.color = new Color(0.005f, 0.015f, 0.03f, 0.88f);
            overlay.raycastTarget = true;
            root.SetActive(false);
            return root;
        }

        private static RectTransform CreateModalPanel(Transform parent, Vector2 size)
        {
            GameObject panelObject = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            panelObject.transform.SetParent(parent, false);

            RectTransform panel = panelObject.GetComponent<RectTransform>();
            panel.anchorMin = new Vector2(0.5f, 0.5f);
            panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.pivot = new Vector2(0.5f, 0.5f);
            panel.anchoredPosition = Vector2.zero;
            panel.sizeDelta = size;

            Image background = panelObject.GetComponent<Image>();
            background.color = new Color(0.025f, 0.08f, 0.13f, 1f);
            return panel;
        }

        private static Text CreateModalText(
            Transform parent,
            string name,
            string value,
            int fontSize,
            Vector2 minimum,
            Vector2 maximum,
            Color color)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);

            RectTransform rect = textObject.GetComponent<RectTransform>();
            SetAnchors(rect, minimum, maximum);

            Text text = textObject.GetComponent<Text>();
            CodeClickerFont.Apply(text, fontSize);
            text.text = value;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            return text;
        }

        private static Button CreateModalButton(
            Transform parent,
            string name,
            string label,
            Vector2 minimum,
            Vector2 maximum,
            UnityAction action)
        {
            GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            SetAnchors(rect, minimum, maximum);

            Image background = buttonObject.GetComponent<Image>();
            background.color = new Color(0.08f, 0.34f, 0.48f, 1f);

            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = background;
            button.onClick.AddListener(action);

            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.78f, 1f, 1f, 1f);
            colors.pressedColor = new Color(0.58f, 0.82f, 0.90f, 1f);
            colors.disabledColor = new Color(0.35f, 0.38f, 0.40f, 0.65f);
            button.colors = colors;

            CreateModalText(
                buttonObject.transform,
                "Text",
                label,
                16,
                Vector2.zero,
                Vector2.one,
                Color.white);
            return button;
        }

        private static void SetAnchors(RectTransform rect, Vector2 minimum, Vector2 maximum)
        {
            rect.anchorMin = minimum;
            rect.anchorMax = maximum;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private void CreateSaveButton()
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                return;
            }

            Transform existing = canvas.transform.Find("SaveGameButton");
            if (existing != null)
            {
                saveButtonText = existing.GetComponentInChildren<Text>(true);
                return;
            }

            GameObject buttonObject = new GameObject("SaveGameButton", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(canvas.transform, false);
            buttonObject.transform.SetAsLastSibling();

            RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
            buttonRect.anchorMin = Vector2.one;
            buttonRect.anchorMax = Vector2.one;
            buttonRect.pivot = Vector2.one;
            buttonRect.anchoredPosition = new Vector2(-30f, -104f);
            buttonRect.sizeDelta = new Vector2(150f, 42f);

            Image background = buttonObject.GetComponent<Image>();
            background.color = new Color(0.04f, 0.16f, 0.24f, 0.96f);

            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = background;
            button.onClick.AddListener(SaveGameFromButton);

            GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(buttonObject.transform, false);
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(8f, 3f);
            textRect.offsetMax = new Vector2(-8f, -3f);

            saveButtonText = textObject.GetComponent<Text>();
            CodeClickerFont.Apply(saveButtonText, 14);
            saveButtonText.alignment = TextAnchor.MiddleCenter;
            saveButtonText.color = new Color(0.50f, 0.92f, 1f, 1f);
            saveButtonText.raycastTarget = false;
            SetButtonText("SAVE GAME");
        }

        private void SetButtonText(string value)
        {
            if (saveButtonText != null)
            {
                saveButtonText.text = value;
            }
        }

        [Serializable]
        private sealed class SaveData
        {
            public int version;
            public long savedAtUtcTicks;
            public double coins;
            public double coinsPerKey;
            public double passiveCoinsPerSecond;
            public int totalKeyPresses;
            public int keysPerLine = 5;
            public string visibleCode = string.Empty;
            public int pressedKeysForCurrentLine;
            public bool hasStartedTyping;
            public List<UpgradeSaveData> upgrades = new List<UpgradeSaveData>();
        }

        [Serializable]
        private sealed class UpgradeSaveData
        {
            public string upgradeName;
            public int purchaseCount;
        }
    }
}
