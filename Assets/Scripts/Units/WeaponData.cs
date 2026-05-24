using UnityEngine;

[CreateAssetMenu(
    fileName = "WeaponData_New",
    menuName = "Turan Strateji/Weapons/Weapon Data")]
public class WeaponData : ScriptableObject
{
    [Header("Identity")]
    public string weaponId = "weapon_id";
    public string displayName = "Silah";
    public TuranNation nation = TuranNation.TuranCommon;
    public TuranRarity rarity = TuranRarity.Common;
    public WeaponCategory category = WeaponCategory.Rifle;
    public TuranUnitEra era = TuranUnitEra.Modern;

    [Header("Prefab")]
    public GameObject weaponPrefab;
    public Sprite weaponIcon;
    public Vector3 socketLocalPosition;
    public Vector3 socketLocalRotation;
    public Vector3 socketLocalScale = Vector3.one;

    [Header("Combat")]
    public int attack;
    public float range = 5f;
    public float fireRate = 1f;
    public int armorPiercing;
}
