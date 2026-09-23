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
    private int purchaseCount;

    private bool IsOneTime => oneTimePurchase || upgradeName == "CoPilot" || upgradeName == "AI Assistant";
    private bool IsMaxed => (IsOneTime && purchaseCount > 0) ||
                            (effect == UpgradeEffect.KeysPerLine && !IsOneTime && purchaseCount >= 3);
    private double CurrentPrice => price * Math.Pow(priceMultiplier, purchaseCount);

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
        moneySystem = FindFirstObjectByType<MoneySystem>();
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
        if (IsMaxed || moneySystem == null || !moneySystem.TrySpend(CurrentPrice))
        {
            return;
        }

        purchaseCount++;

        if (effect == UpgradeEffect.KeysPerLine && !IsOneTime)
        {
            moneySystem.ApplyUpgrade(effect, effectAmount - purchaseCount + 1);
        }
        else
        {
            moneySystem.ApplyUpgrade(effect, effectAmount);
        }

        UpdateUI();
    }

    private void UpdateUI()
    {
        if (nameText != null)
        {
            nameText.text = IsOneTime || purchaseCount == 0
                ? upgradeName
                : $"{upgradeName} x{purchaseCount}";
        }

        if (incomeInfoText != null)
        {
            incomeInfoText.text = description;
        }

        if (priceText != null)
        {
            priceText.text = IsMaxed ? "Maxed" : MoneySystem.FormatCoins(CurrentPrice);
        }
    }
}
