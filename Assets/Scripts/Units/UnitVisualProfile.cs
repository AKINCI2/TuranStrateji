using UnityEngine;

[CreateAssetMenu(
    fileName = "UnitVisualProfile_New",
    menuName = "Turan Strateji/Units/Unit Visual Profile")]
public class UnitVisualProfile : ScriptableObject
{
    [Header("Identity")]
    public string profileId = "visual_profile_id";
    public string displayName = "Tim Gorseli";

    [Header("PRO MODEL TASARIMI (BURAYA KOYUN)")]
    [Tooltip("Kendi oluşturduğunuz 3D asker modelini buraya sürükleyin.")]
    public GameObject characterPrefab;
    
    [Header("Animasyon Ayarları")]
    public Avatar humanoidAvatar;
    public RuntimeAnimatorController animatorController;

    [Header("Formation")]
    public UnitFormationShape formationShape = UnitFormationShape.Fireteam;
    [Range(1, 6)]
    public int visibleSoldierCount = 4;
    public float spacing = 0.5f;
    public float hexFillRatio = 0.42f;

    [Header("Weapon Socket")]
    public string weaponSocketName = "WeaponSocket";
    public HumanBodyBones fallbackHandBone = HumanBodyBones.RightHand;
}
