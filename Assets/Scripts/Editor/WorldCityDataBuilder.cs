#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

public static class WorldCityDataBuilder
{
    private const string Root = "Assets/Resources/Data/WorldCities";

    [MenuItem("Turan Strateji/Data/Build World City Data")]
    public static void Build()
    {
        Directory.CreateDirectory(Root);

        CreateCity(
            "WC_AnkaraMerkez",
            "ankara_merkez",
            "Ankara Merkez",
            "Anadolu Komuta Hatti",
            5,
            5,
            240f,
            2,
            25000,
            WorldCityBonusType.ResearchSpeed,
            0.08f
        );

        CreateCity(
            "WC_OtukenGecidi",
            "otuken_gecidi",
            "Otuken Gecidi",
            "Bozkir Strateji Koridoru",
            4,
            4,
            210f,
            2,
            18000,
            WorldCityBonusType.AllianceOccupationSpeed,
            0.07f
        );

        CreateCity(
            "WC_HazarKoprusu",
            "hazar_koprusu",
            "Hazar Koprusu",
            "Hazar Lojistik Hatti",
            3,
            3,
            180f,
            1,
            12000,
            WorldCityBonusType.LogisticsSpeed,
            0.06f
        );

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("World city data olusturuldu: " + Root);
    }

    private static void CreateCity(
        string assetName,
        string cityId,
        string displayName,
        string regionName,
        int level,
        int influenceRadius,
        float occupationSeconds,
        int requiredAllianceLevel,
        int requiredPower,
        WorldCityBonusType bonusType,
        float bonusPercent)
    {
        string path = $"{Root}/{assetName}.asset";
        WorldCityData data = AssetDatabase.LoadAssetAtPath<WorldCityData>(path);
        if (data == null)
        {
            data = ScriptableObject.CreateInstance<WorldCityData>();
            AssetDatabase.CreateAsset(data, path);
        }

        data.cityId = cityId;
        data.displayName = displayName;
        data.regionName = regionName;
        data.level = level;
        data.influenceRadius = influenceRadius;
        data.occupationSeconds = occupationSeconds;
        data.requiredAllianceLevel = requiredAllianceLevel;
        data.requiredPower = requiredPower;
        data.bonusType = bonusType;
        data.bonusPercent = bonusPercent;

        EditorUtility.SetDirty(data);
    }
}
#endif
