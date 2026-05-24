using System;
using System.Text;
using UnityEngine;

[Serializable]
public struct ResourceCost
{
    public int gold;
    public int turanCoin;
    public int steel;
    public int oil;
    public int bor;

    public bool CanAfford(ResourceCost wallet)
    {
        return wallet.gold >= gold &&
               wallet.turanCoin >= turanCoin &&
               wallet.steel >= steel &&
               wallet.oil >= oil &&
               wallet.bor >= bor;
    }

    public static ResourceCost operator -(ResourceCost wallet, ResourceCost cost)
    {
        return new ResourceCost
        {
            gold = wallet.gold - cost.gold,
            turanCoin = wallet.turanCoin - cost.turanCoin,
            steel = wallet.steel - cost.steel,
            oil = wallet.oil - cost.oil,
            bor = wallet.bor - cost.bor
        };
    }

    public static ResourceCost operator +(ResourceCost wallet, ResourceCost amount)
    {
        return new ResourceCost
        {
            gold = wallet.gold + amount.gold,
            turanCoin = wallet.turanCoin + amount.turanCoin,
            steel = wallet.steel + amount.steel,
            oil = wallet.oil + amount.oil,
            bor = wallet.bor + amount.bor
        };
    }

    public bool IsZero()
    {
        return gold == 0 &&
               turanCoin == 0 &&
               steel == 0 &&
               oil == 0 &&
               bor == 0;
    }

    public string ToDisplayString()
    {
        if (IsZero())
            return "Ucretsiz";

        StringBuilder builder =
            new StringBuilder();

        AppendCost(builder, "Altin", gold);
        AppendCost(builder, "Turan", turanCoin);
        AppendCost(builder, "Celik", steel);
        AppendCost(builder, "Petrol", oil);
        AppendCost(builder, "Bor", bor);

        return builder.ToString();
    }

    private static void AppendCost(StringBuilder builder, string label, int value)
    {
        if (value <= 0)
            return;

        if (builder.Length > 0)
            builder.Append("  ");

        builder.Append(label);
        builder.Append(": ");
        builder.Append(value);
    }
}

