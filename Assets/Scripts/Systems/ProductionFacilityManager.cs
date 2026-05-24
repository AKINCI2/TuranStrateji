using System;
using System.Collections.Generic;
using UnityEngine;

public class ProductionFacilityManager : MonoBehaviour
{
    public static ProductionFacilityManager Instance;

    [Header("Production")]
    public int slotCount = 2;
    public List<ProductionSlot> slots = new List<ProductionSlot>();

    [Header("Recipes")]
    public ProductionRecipeData recipeData;

    private readonly List<ProductionRecipe> recipes = new List<ProductionRecipe>();

    public event Action ProductionChanged;

    void Awake()
    {
        Instance = this;
        LoadRecipes();
        EnsureSlots();
    }

    private void LoadRecipes()
    {
        recipes.Clear();
        if (recipeData != null && recipeData.recipes != null && recipeData.recipes.Count > 0)
        {
            recipes.AddRange(recipeData.recipes);
            return;
        }

        AddDefaultRecipes();
    }

    void Update()
    {
        bool changed = false;

        foreach (ProductionSlot slot in slots)
        {
            if (slot == null || !slot.isProducing)
                continue;

            slot.remainingSeconds -= Time.deltaTime;
            if (slot.remainingSeconds > 0f)
                continue;

            slot.remainingSeconds = 0f;
            slot.isProducing = false;
            slot.isReadyToCollect = true;
            changed = true;
        }

        if (changed)
            ProductionChanged?.Invoke();
    }

    public bool StartProduction(ConstructionMaterialType type)
    {
        EnsureSlots();

        ProductionRecipe recipe = GetRecipe(type);
        if (recipe == null || BaseManager.Instance == null || MaterialInventoryManager.Instance == null)
            return false;

        ProductionSlot freeSlot = GetFreeSlot();
        if (freeSlot == null)
            return false;

        if (!BaseManager.Instance.TrySpend(recipe.cost))
            return false;

        freeSlot.isProducing = true;
        freeSlot.isReadyToCollect = false;
        freeSlot.recipeType = type;
        freeSlot.remainingSeconds = recipe.seconds;
        freeSlot.totalSeconds = recipe.seconds;
        ProductionChanged?.Invoke();
        return true;
    }

    public int CollectReadyProducts()
    {
        EnsureSlots();

        int collectedSlots = 0;
        foreach (ProductionSlot slot in slots)
        {
            if (slot == null || !slot.isReadyToCollect)
                continue;

            ProductionRecipe recipe = GetRecipe(slot.recipeType);
            if (recipe == null || MaterialInventoryManager.Instance == null)
                continue;

            MaterialInventoryManager.Instance.Add(recipe.type, recipe.outputAmount);
            slot.isReadyToCollect = false;
            slot.remainingSeconds = 0f;
            slot.totalSeconds = 0f;
            collectedSlots++;
        }

        if (collectedSlots > 0)
            ProductionChanged?.Invoke();

        return collectedSlots;
    }

    public bool HasReadyProducts()
    {
        EnsureSlots();
        foreach (ProductionSlot slot in slots)
        {
            if (slot != null && slot.isReadyToCollect)
                return true;
        }

        return false;
    }

    public string GetStatusText()
    {
        EnsureSlots();

        string text = "Atolye Slotlari";
        for (int i = 0; i < slots.Count; i++)
        {
            ProductionSlot slot = slots[i];
            if (slot == null)
                continue;

            if (slot.isReadyToCollect)
            {
                ProductionRecipe recipe = GetRecipe(slot.recipeType);
                int amount = recipe != null ? recipe.outputAmount : 0;
                text += $"\nSlot {i + 1}: {GetMaterialName(slot.recipeType)} hazir (+{amount})";
                continue;
            }

            if (slot.isProducing)
            {
                text += $"\nSlot {i + 1}: {GetMaterialName(slot.recipeType)} {slot.remainingSeconds:0}s";
                continue;
            }

            text += $"\nSlot {i + 1}: Bos";
        }

        return text;
    }

    public ProductionRecipe GetRecipe(ConstructionMaterialType type)
    {
        if (recipes.Count == 0)
            LoadRecipes();

        foreach (ProductionRecipe recipe in recipes)
        {
            if (recipe != null && recipe.type == type)
                return recipe;
        }

        return null;
    }

    public string GetMaterialName(ConstructionMaterialType type)
    {
        if (type == ConstructionMaterialType.Wood)
            return "Kalas";
        if (type == ConstructionMaterialType.Concrete)
            return "Beton";
        if (type == ConstructionMaterialType.Cement)
            return "Cimento";
        return "Tugla";
    }

    private ProductionSlot GetFreeSlot()
    {
        foreach (ProductionSlot slot in slots)
        {
            if (slot != null && !slot.isProducing && !slot.isReadyToCollect)
                return slot;
        }

        return null;
    }

    private void EnsureSlots()
    {
        while (slots.Count < slotCount)
            slots.Add(new ProductionSlot());
    }

    public bool HasFreeSlot()
    {
        EnsureSlots();
        return GetFreeSlot() != null;
    }

    private void AddDefaultRecipes()
    {
        recipes.Add(new ProductionRecipe
        {
            type = ConstructionMaterialType.Wood,
            outputAmount = 10,
            seconds = 8f,
            cost = new ResourceCost { steel = 5 }
        });

        recipes.Add(new ProductionRecipe
        {
            type = ConstructionMaterialType.Concrete,
            outputAmount = 6,
            seconds = 18f,
            cost = new ResourceCost { steel = 12, oil = 2 }
        });

        recipes.Add(new ProductionRecipe
        {
            type = ConstructionMaterialType.Cement,
            outputAmount = 4,
            seconds = 32f,
            cost = new ResourceCost { steel = 18, oil = 4, bor = 1 }
        });
    }
}

[Serializable]
public class ProductionRecipe
{
    public ConstructionMaterialType type;
    public int outputAmount = 1;
    public float seconds = 10f;
    public ResourceCost cost;
}

[Serializable]
public class ProductionSlot
{
    public bool isProducing;
    public bool isReadyToCollect;
    public ConstructionMaterialType recipeType;
    public float remainingSeconds;
    public float totalSeconds;
}

