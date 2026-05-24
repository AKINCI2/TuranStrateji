using UnityEngine;

[CreateAssetMenu(
    fileName = "OfficerData_New",
    menuName = "Turan Strateji/Officers/Officer Data")]
public class OfficerData : ScriptableObject
{
    [Header("Identity")]
    public string officerId = "officer_id";
    public string displayName = "Subay";
    public Sprite portrait;
    public OfficerLegacyType legacyType = OfficerLegacyType.FictionalTuranHero;
    [TextArea(2, 5)]
    public string shortBiography;

    [Header("Role")]
    public TuranForceBranch branch = TuranForceBranch.LandForces;
    public TuranUnitKind preferredUnitKind = TuranUnitKind.Infantry;
    public OfficerAssignment assignment = OfficerAssignment.Field;
    public OfficerTendency tendency = OfficerTendency.Attack;

    [Header("Progression")]
    public int maxLevel = 60;
    public int unlockSkill2Level = 10;
    public int unlockSkill3Level = 25;
    public int unlockSkill4Level = 45;

    [Header("Bonuses")]
    public int attackBonusPercent;
    public int defenseBonusPercent;
    public int healthBonusPercent;
    public int gatheringBonusPercent;
    public int trainingSpeedBonusPercent;

    public bool IsSkillUnlocked(int skillIndex, int currentLevel)
    {
        if (skillIndex <= 1)
            return true;

        if (skillIndex == 2)
            return currentLevel >= unlockSkill2Level;

        if (skillIndex == 3)
            return currentLevel >= unlockSkill3Level;

        if (skillIndex == 4)
            return currentLevel >= unlockSkill4Level;

        return false;
    }
}

