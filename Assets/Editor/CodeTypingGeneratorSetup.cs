using CodeClicker;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CodeClickerEditor
{
    [InitializeOnLoad]
    public static class CodeTypingGeneratorSetup
    {
        private const string MenuPath = "Tools/Code Clicker/Add Live Code Screen To Selected Computer";
        private const string MoneyMenuPath = "Tools/Code Clicker/Add Money HUD";
        private const string ShopMenuPath = "Tools/Code Clicker/Add Starter Shop";
        private const string RepairWorkspaceMenuPath = "Tools/Code Clicker/Repair Coffee Machine Visual";
        private const string AutoSetupSessionKey = "CodeClicker.LiveCodeScreenAutoSetupCompleteV5";
        private const string CoffeeMachineAssetPath = "Assets/Art/CodeClickerUI/icon_coffee_machine.png";

        private static readonly UpgradeData[] ShopItems =
        {
            new UpgradeData("Studeren", "+0.10 coins per key", 1, UpgradeEffect.CoinsPerKey, 0.1),
            new UpgradeData("Forums bekijken", "+0.30 coins per key", 3, UpgradeEffect.CoinsPerKey, 0.3),
            new UpgradeData("Online cursus", "+1 coin per key", 25, UpgradeEffect.CoinsPerKey, 1),
            new UpgradeData("Junior developer", "+1 coin per second", 500, UpgradeEffect.PassiveIncome, 1),
            new UpgradeData("Mechanical keyboard", "+5 coins per key", 2_500, UpgradeEffect.CoinsPerKey, 5),
            new UpgradeData("CoPilot", "4 keys per code line", 10_000, UpgradeEffect.KeysPerLine, 4),
            new UpgradeData("Dual monitors", "+25 coins per second", 50_000, UpgradeEffect.PassiveIncome, 25),
            new UpgradeData("Coffee machine", "+100 coins per second", 125_000, UpgradeEffect.PassiveIncome, 100),
            new UpgradeData("Betere muis", "3 keys per code line", 1_000_000, UpgradeEffect.KeysPerLine, 3),
            new UpgradeData("Senior developer", "+10K coins per second", 30_000_000, UpgradeEffect.PassiveIncome, 10_000),
            new UpgradeData("AI Assistant", "+50K coins per second", 100_000_000, UpgradeEffect.PassiveIncome, 50_000)
        };

        static CodeTypingGeneratorSetup()
        {
            EditorApplication.delayCall += TryAutomaticSetup;
            EditorApplication.hierarchyChanged += TryAutomaticSetup;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorSceneManager.sceneOpened += OnSceneOpened;
        }

        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            SessionState.EraseBool(AutoSetupSessionKey);
            EditorApplication.delayCall += TryAutomaticSetup;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                EditorApplication.delayCall += TryAutomaticSetup;
            }
        }

        [MenuItem(MenuPath, false, 100)]
        private static void AddLiveCodeScreen()
        {
            GameObject computer = Selection.activeGameObject;
            if (computer == null || computer.GetComponent<RectTransform>() == null)
            {
                EditorUtility.DisplayDialog(
                    "Code Clicker",
                    "Selecteer eerst de UI Image van de computer in de Hierarchy.",
                    "OK");
                return;
            }

            if (computer.GetComponentInParent<Canvas>() == null)
            {
                EditorUtility.DisplayDialog(
                    "Code Clicker",
                    "Het geselecteerde object moet onderdeel zijn van een Canvas.",
                    "OK");
                return;
            }

            Transform existing = computer.transform.Find("LiveCodeScreen");
            if (existing != null)
            {
                Selection.activeGameObject = existing.gameObject;
                EditorGUIUtility.PingObject(existing.gameObject);
                EditorUtility.DisplayDialog(
                    "Code Clicker",
                    "Deze computer heeft al een LiveCodeScreen. Het bestaande object is geselecteerd.",
                    "OK");
                return;
            }

            CreateLiveCodeScreen(computer, true);
        }

        [MenuItem(MoneyMenuPath, false, 101)]
        private static void AddMoneyHudFromMenu()
        {
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                EditorUtility.DisplayDialog("Code Clicker", "Er is geen Canvas in de scène gevonden.", "OK");
                return;
            }

            EnsureMoneyHud(canvas);
            Transform moneyHud = canvas.transform.Find("MoneyHUD");
            if (moneyHud != null)
            {
                Selection.activeGameObject = moneyHud.gameObject;
                EditorGUIUtility.PingObject(moneyHud.gameObject);
            }
        }

        [MenuItem(ShopMenuPath, false, 102)]
        private static void AddStarterShopFromMenu()
        {
            EnsureStarterShop();
        }

        [MenuItem(RepairWorkspaceMenuPath, false, 103)]
        private static void RepairCoffeeMachineVisualFromMenu()
        {
            Image computer = FindComputerWorkstation();
            if (computer == null)
            {
                EditorUtility.DisplayDialog(
                    "Code Clicker",
                    "De computer_workstation Image is niet gevonden in de geopende scène.",
                    "OK");
                return;
            }

            EnsureCoffeeMachineVisual(computer.gameObject);
            Transform machine = computer.transform.Find("CoffeeMachine");
            Selection.activeGameObject = computer.gameObject;
            if (machine != null)
            {
                Selection.activeGameObject = machine.gameObject;
            }
            EditorGUIUtility.PingObject(Selection.activeGameObject);
        }

        private static void TryAutomaticSetup()
        {
            if (SessionState.GetBool(AutoSetupSessionKey, false) ||
                EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            Image computer = FindComputerWorkstation();
            if (computer == null)
            {
                return;
            }

            if (computer.transform.Find("LiveCodeScreen") == null)
            {
                CreateLiveCodeScreen(computer.gameObject, false);
                Debug.Log("[Code Clicker] LiveCodeScreen automatisch aan computer_workstation toegevoegd.", computer);
            }

            EnsureCoffeeMachineVisual(computer.gameObject);
            EnsureMoneyHud(computer.GetComponentInParent<Canvas>());
            EnsureStarterShop();
            SessionState.SetBool(AutoSetupSessionKey, true);
        }

        private static Image FindComputerWorkstation()
        {
            foreach (Image image in Resources.FindObjectsOfTypeAll<Image>())
            {
                if (image == null || image.sprite == null || EditorUtility.IsPersistent(image))
                {
                    continue;
                }

                if (image.gameObject.scene.IsValid() &&
                    image.sprite.name.StartsWith("computer_workstation", System.StringComparison.Ordinal))
                {
                    return image;
                }
            }

            return null;
        }

        private static void EnsureCoffeeMachineVisual(GameObject computer)
        {
            WorkspaceVisuals visuals = computer.GetComponent<WorkspaceVisuals>();
            if (visuals == null)
            {
                visuals = Undo.AddComponent<WorkspaceVisuals>(computer);
            }

            RectTransform coffeeMachine = EnsureRawImage(
                computer.transform,
                "CoffeeMachine",
                CoffeeMachineAssetPath,
                new Vector2(0.14f, 0.30f),
                new Vector2(0.34f, 0.58f));

            Texture2D coffeeMachinePicture = AssetDatabase.LoadAssetAtPath<Texture2D>(CoffeeMachineAssetPath);
            visuals.Configure(coffeeMachine, coffeeMachinePicture);

            coffeeMachine.SetAsLastSibling();
            RectTransform liveScreen = computer.transform.Find("LiveCodeScreen") as RectTransform;
            if (liveScreen != null)
            {
                liveScreen.SetAsLastSibling();
            }

            coffeeMachine.gameObject.SetActive(false);
            EditorUtility.SetDirty(visuals);
            EditorUtility.SetDirty(computer);
            EditorSceneManager.MarkSceneDirty(computer.scene);
            Debug.Log("[Code Clicker] Coffee machine image is gekoppeld.", computer);
        }

        private static RectTransform EnsureRawImage(
            Transform parent,
            string objectName,
            string assetPath,
            Vector2 minimum,
            Vector2 maximum)
        {
            Transform existing = parent.Find(objectName);
            GameObject imageObject;
            bool created = existing == null;
            if (existing == null)
            {
                imageObject = new GameObject(objectName, typeof(RectTransform));
                Undo.RegisterCreatedObjectUndo(imageObject, $"Add {objectName}");
                imageObject.transform.SetParent(parent, false);
                imageObject.layer = parent.gameObject.layer;
            }
            else
            {
                imageObject = existing.gameObject;
            }

            RectTransform rect = imageObject.GetComponent<RectTransform>();
            if (created)
            {
                rect.anchorMin = minimum;
                rect.anchorMax = maximum;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }

            RawImage image = imageObject.GetComponent<RawImage>();
            if (image == null)
            {
                Graphic otherGraphic = imageObject.GetComponent<Graphic>();
                if (otherGraphic != null)
                {
                    Undo.DestroyObjectImmediate(otherGraphic);
                }

                image = Undo.AddComponent<RawImage>(imageObject);
            }

            image.texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            image.color = Color.white;
            image.raycastTarget = false;
            EditorUtility.SetDirty(image);
            return rect;
        }

        private static void CreateLiveCodeScreen(GameObject computer, bool showCompletionDialog)
        {

            GameObject screen = new GameObject("LiveCodeScreen", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(screen, "Add Live Code Screen");
            screen.transform.SetParent(computer.transform, false);
            screen.transform.SetAsLastSibling();

            RectTransform screenRect = screen.GetComponent<RectTransform>();
            screenRect.anchorMin = new Vector2(0.235f, 0.495f);
            screenRect.anchorMax = new Vector2(0.82f, 0.935f);
            screenRect.offsetMin = Vector2.zero;
            screenRect.offsetMax = Vector2.zero;

            Image background = Undo.AddComponent<Image>(screen);
            background.color = new Color(0.015f, 0.035f, 0.075f, 0.96f);
            background.raycastTarget = false;
            Undo.AddComponent<RectMask2D>(screen);

            GameObject textObject = new GameObject("CodeText", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(textObject, "Add Code Text");
            textObject.transform.SetParent(screen.transform, false);

            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(8f, 6f);
            textRect.offsetMax = new Vector2(-8f, -6f);

            Text codeText = Undo.AddComponent<Text>(textObject);
            codeText.text = "Type to code\n|";
            CodeClickerFont.Apply(codeText, 18, true);
            codeText.color = new Color(0.83f, 0.83f, 0.83f, 1f);
            codeText.fontSize = 18;
            codeText.alignment = TextAnchor.UpperLeft;
            codeText.horizontalOverflow = HorizontalWrapMode.Wrap;
            codeText.verticalOverflow = VerticalWrapMode.Truncate;
            codeText.lineSpacing = 0.9f;
            codeText.raycastTarget = false;
            codeText.supportRichText = true;

            CodeTypingGenerator generator = Undo.AddComponent<CodeTypingGenerator>(screen);
            generator.CodeDisplay = codeText;

            EnsureMoneyHud(computer.GetComponentInParent<Canvas>());
            EnsureStarterShop();

            EditorUtility.SetDirty(computer);
            EditorSceneManager.MarkSceneDirty(computer.scene);

            if (showCompletionDialog)
            {
                Selection.activeGameObject = screen;
                EditorGUIUtility.PingObject(screen);
                EditorUtility.DisplayDialog(
                    "Code Clicker",
                    "Het live codescherm is toegevoegd. Druk op Play en klik daarna in de Game-tab om te typen.\n\nJe kunt LiveCodeScreen met de Rect Tool nog preciezer over het monitorscherm leggen.",
                    "Klaar");
            }
        }

        private static void EnsureMoneyHud(Canvas canvas)
        {
            if (canvas == null)
            {
                return;
            }

            Transform existing = canvas.transform.Find("MoneyHUD");
            if (existing != null)
            {
                return;
            }

            GameObject moneyHud = new GameObject("MoneyHUD", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(moneyHud, "Add Money HUD");
            moneyHud.transform.SetParent(canvas.transform, false);
            moneyHud.transform.SetAsLastSibling();

            RectTransform hudRect = moneyHud.GetComponent<RectTransform>();
            hudRect.anchorMin = Vector2.one;
            hudRect.anchorMax = Vector2.one;
            hudRect.pivot = Vector2.one;
            hudRect.anchoredPosition = new Vector2(-30f, -30f);
            hudRect.sizeDelta = new Vector2(240f, 64f);

            Image background = Undo.AddComponent<Image>(moneyHud);
            background.color = new Color(0.015f, 0.035f, 0.075f, 0.94f);
            background.raycastTarget = false;

            GameObject textObject = new GameObject("MoneyText", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(textObject, "Add Money Text");
            textObject.transform.SetParent(moneyHud.transform, false);

            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10f, 4f);
            textRect.offsetMax = new Vector2(-10f, -4f);

            Text moneyText = Undo.AddComponent<Text>(textObject);
            moneyText.text = "€ 0,00";
            moneyText.color = new Color(1f, 0.72f, 0.23f, 1f);
            moneyText.fontSize = 30;
            moneyText.fontStyle = FontStyle.Bold;
            moneyText.alignment = TextAnchor.MiddleCenter;
            moneyText.raycastTarget = false;

            MoneySystem moneySystem = Undo.AddComponent<MoneySystem>(moneyHud);
            moneySystem.MoneyText = moneyText;

            EditorUtility.SetDirty(canvas.gameObject);
            EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
            Debug.Log("[Code Clicker] MoneyHUD toegevoegd.", moneyHud);
        }

        private static void EnsureStarterShop()
        {
            Image shopPanel = null;

            foreach (Image image in Resources.FindObjectsOfTypeAll<Image>())
            {
                if (image == null || image.sprite == null || EditorUtility.IsPersistent(image))
                {
                    continue;
                }

                if (image.gameObject.scene.IsValid() &&
                    image.sprite.name.StartsWith("shop_panel", System.StringComparison.Ordinal))
                {
                    shopPanel = image;
                    break;
                }
            }

            if (shopPanel == null)
            {
                EditorUtility.DisplayDialog("Code Clicker", "De shop_panel Image is niet gevonden.", "OK");
                return;
            }

            Transform existing = shopPanel.transform.Find("ShopViewport");
            if (existing != null)
            {
                return;
            }

            Font font = Font.CreateDynamicFontFromOSFont(
                new[] { "Segoe UI", "Arial", "Liberation Sans" }, 16);

            GameObject viewport = new GameObject("ShopViewport", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(viewport, "Add Starter Shop");
            viewport.transform.SetParent(shopPanel.transform, false);
            viewport.transform.SetAsLastSibling();

            RectTransform viewportRect = viewport.GetComponent<RectTransform>();
            SetAnchors(viewportRect, new Vector2(0.07f, 0.07f), new Vector2(0.93f, 0.86f));

            Image viewportImage = Undo.AddComponent<Image>(viewport);
            viewportImage.color = new Color(0f, 0f, 0f, 0.01f);
            Undo.AddComponent<RectMask2D>(viewport);
            ScrollRect scrollRect = Undo.AddComponent<ScrollRect>(viewport);
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 28f;

            GameObject content = new GameObject("ShopContent", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(content, "Add Shop Content");
            content.transform.SetParent(viewport.transform, false);

            RectTransform contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = Vector2.one;
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0f, ShopItems.Length * 78f);

            scrollRect.viewport = viewportRect;
            scrollRect.content = contentRect;

            for (int index = 0; index < ShopItems.Length; index++)
            {
                CreateShopRow(content.transform, ShopItems[index], index, font);
            }

            EditorUtility.SetDirty(shopPanel.gameObject);
            EditorSceneManager.MarkSceneDirty(shopPanel.gameObject.scene);
            Selection.activeGameObject = viewport;
            Debug.Log("[Code Clicker] Starter shop met 11 upgrades toegevoegd.", viewport);
        }

        private static void CreateShopRow(Transform parent, UpgradeData item, int index, Font font)
        {
            GameObject row = new GameObject($"Upgrade_{index + 1}_{item.Name}", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(row, "Add Shop Upgrade");
            row.transform.SetParent(parent, false);

            RectTransform rowRect = row.GetComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0f, 1f);
            rowRect.anchorMax = Vector2.one;
            rowRect.pivot = new Vector2(0.5f, 1f);
            rowRect.anchoredPosition = new Vector2(0f, -index * 78f);
            rowRect.sizeDelta = new Vector2(0f, 72f);

            Image rowImage = Undo.AddComponent<Image>(row);
            if (index % 2 == 0)
            {
                rowImage.color = new Color(0.035f, 0.09f, 0.14f, 0.92f);
            }
            else
            {
                rowImage.color = new Color(0.025f, 0.07f, 0.12f, 0.92f);
            }

            Text nameText = CreateText(row.transform, "Name", item.Name, font, 16, FontStyle.Bold,
                TextAnchor.MiddleLeft, new Vector2(0.04f, 0.48f), new Vector2(0.66f, 0.96f), Color.white);

            Text infoText = CreateText(row.transform, "Effect", item.Description, font, 13, FontStyle.Normal,
                TextAnchor.MiddleLeft, new Vector2(0.04f, 0.05f), new Vector2(0.68f, 0.52f),
                new Color(0.45f, 0.9f, 1f, 1f));

            GameObject buttonObject = new GameObject("BuyButton", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(buttonObject, "Add Buy Button");
            buttonObject.transform.SetParent(row.transform, false);
            RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
            SetAnchors(buttonRect, new Vector2(0.70f, 0.16f), new Vector2(0.97f, 0.84f));

            Image buttonImage = Undo.AddComponent<Image>(buttonObject);
            buttonImage.color = new Color(0.95f, 0.55f, 0.12f, 1f);
            Button button = Undo.AddComponent<Button>(buttonObject);
            button.targetGraphic = buttonImage;

            Text priceText = CreateText(buttonObject.transform, "Price", MoneySystem.FormatCoins(item.Price),
                font, 13, FontStyle.Bold, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one,
                new Color(0.08f, 0.06f, 0.04f, 1f));

            StoreUpgrades upgrade = Undo.AddComponent<StoreUpgrades>(row);
            upgrade.Configure(item.Name, item.Description, item.Price, item.Effect, item.Amount,
                nameText, priceText, infoText, button);
            EditorUtility.SetDirty(upgrade);
        }

        private static Text CreateText(
            Transform parent,
            string objectName,
            string value,
            Font font,
            int fontSize,
            FontStyle fontStyle,
            TextAnchor alignment,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Color color)
        {
            GameObject textObject = new GameObject(objectName, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(textObject, "Add Shop Text");
            textObject.transform.SetParent(parent, false);

            RectTransform rect = textObject.GetComponent<RectTransform>();
            SetAnchors(rect, anchorMin, anchorMax);

            Text text = Undo.AddComponent<Text>(textObject);
            text.text = value;
            text.font = font;
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        private static void SetAnchors(RectTransform rect, Vector2 minimum, Vector2 maximum)
        {
            rect.anchorMin = minimum;
            rect.anchorMax = maximum;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private sealed class UpgradeData
        {
            public readonly string Name;
            public readonly string Description;
            public readonly double Price;
            public readonly UpgradeEffect Effect;
            public readonly double Amount;

            public UpgradeData(string name, string description, double price, UpgradeEffect effect, double amount)
            {
                Name = name;
                Description = description;
                Price = price;
                Effect = effect;
                Amount = amount;
            }
        }

        [MenuItem(MenuPath, true)]
        private static bool ValidateAddLiveCodeScreen()
        {
            GameObject selected = Selection.activeGameObject;
            return selected != null &&
                   selected.GetComponent<RectTransform>() != null &&
                   selected.GetComponentInParent<Canvas>() != null;
        }
    }
}
