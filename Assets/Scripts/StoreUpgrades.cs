using System;
using CodeClicker;
using UnityEngine;
using UnityEngine.UI;

public enum UpgradeEffect
{
    CoinsPerKey,
    PassiveIncome,
    KeysPerLine
}

public class StoreUpgrades : MonoBehaviour
{
    [SerializeField] private Text nameText;
    [SerializeField] private Text priceText;
    [SerializeField] private Text incomeInfoText;
    [SerializeField] private Button buyButton;

    [SerializeField] private string upgradeName;
    [SerializeField] private string description;
    [SerializeField] private double price;
    [SerializeField] private double effectAmount;
    [SerializeField] private UpgradeEffect effect;
    [SerializeField] private double priceMultiplier = 1.15;
    [SerializeField] private bool oneTimePurchase;
    private MoneySystem moneySystem;
    private WorkspaceVisuals workspaceVisuals;
    private int purchaseCount;

    private bool IsOneTime
    {
        get
        {
            return oneTimePurchase || upgradeName == "CoPilot" || upgradeName == "AI Assistant";
        }
    }

    private bool IsMaxed
    {
        get
        {
            if (IsOneTime && purchaseCount > 0)
            {
                return true;
            }

            return effect == UpgradeEffect.KeysPerLine && purchaseCount >= 3;
        }
    }

    private double CurrentPrice
    {
        get { return price * Math.Pow(priceMultiplier, purchaseCount); }
    }

    public string UpgradeName { get { return upgradeName; } }
    public int PurchaseCount { get { return purchaseCount; } }

    public void Configure(
        string newName,
        string newDescription,
        double newPrice,
        UpgradeEffect newEffect,
        double newEffectAmount,
        Text newNameText,
        Text newPriceText,
        Text newIncomeInfoText,
        Button newBuyButton)
    {
        upgradeName = newName;
        description = newDescription;
        price = newPrice;
        effect = newEffect;
        effectAmount = newEffectAmount;
        oneTimePurchase = newName == "CoPilot" || newName == "AI Assistant";
        nameText = newNameText;
        priceText = newPriceText;
        incomeInfoText = newIncomeInfoText;
        buyButton = newBuyButton;
    }

    private void Start()
    {
        CodeClickerFont.Apply(nameText, 16);
        CodeClickerFont.Apply(incomeInfoText, 13);
        CodeClickerFont.Apply(priceText, 13);
        moneySystem = FindFirstObjectByType<MoneySystem>();
        workspaceVisuals = FindFirstObjectByType<WorkspaceVisuals>();
        if (buyButton != null)
        {
            buyButton.onClick.AddListener(BuyUpgrade);
        }
        UpdateUI();
    }

    private void Update()
    {
        if (moneySystem != null && buyButton != null)
        {
            buyButton.interactable = !IsMaxed && moneySystem.Coins >= CurrentPrice;
        }
    }

    public void BuyUpgrade()
    {
        if (IsMaxed || moneySystem == null)
        {
            return;
        }

        if (!moneySystem.TrySpend(CurrentPrice))
        {
            return;
        }

        purchaseCount++;

        if (workspaceVisuals != null && upgradeName == "Coffee machine")
        {
            workspaceVisuals.ShowCoffeeMachine();
        }

        double upgradeAmount = effectAmount;
        if (effect == UpgradeEffect.KeysPerLine && !IsOneTime)
        {
            upgradeAmount = effectAmount - purchaseCount + 1;
        }

        moneySystem.ApplyUpgrade(effect, upgradeAmount);

        UpdateUI();
        SaveSystem.RequestSave();
    }

    public void RestorePurchaseCount(int savedPurchaseCount)
    {
        int maximum = 1000;
        if (IsOneTime)
        {
            maximum = 1;
        }
        else if (effect == UpgradeEffect.KeysPerLine)
        {
            maximum = 3;
        }

        purchaseCount = Mathf.Clamp(savedPurchaseCount, 0, maximum);

        if (workspaceVisuals == null)
        {
            workspaceVisuals = FindFirstObjectByType<WorkspaceVisuals>();
        }

        if (purchaseCount > 0 && workspaceVisuals != null && upgradeName == "Coffee machine")
        {
            workspaceVisuals.ShowCoffeeMachine();
        }

        UpdateUI();
    }

    private void UpdateUI()
    {
        if (nameText != null)
        {
            nameText.text = upgradeName;
            if (!IsOneTime && purchaseCount > 0)
            {
                nameText.text = $"{upgradeName} x{purchaseCount}";
            }
        }

        if (incomeInfoText != null)
        {
            incomeInfoText.text = description;
        }

        if (priceText != null)
        {
            if (IsMaxed)
            {
                priceText.text = "Maxed";
            }
            else
            {
                priceText.text = MoneySystem.FormatCoins(CurrentPrice);
            }
        }
    }
}
