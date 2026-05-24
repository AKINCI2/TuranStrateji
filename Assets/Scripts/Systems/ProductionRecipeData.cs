using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "ProductionRecipes", menuName = "Turan Strateji/Production/Recipe Data")]
public class ProductionRecipeData : ScriptableObject
{
    public List<ProductionRecipe> recipes = new List<ProductionRecipe>();
}