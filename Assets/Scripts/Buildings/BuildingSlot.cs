using UnityEngine;

public class BuildingSlot : MonoBehaviour
{
    public string slotId = "slot_01";
    public BuildingType allowedType = BuildingType.Barracks;
    public int requiredHeadquartersLevel = 1;
    public BaseBuilding placedBuilding;

    public bool IsEmpty =>
        placedBuilding == null;

    public bool CanPlace(BuildingData buildingData)
    {
        if (buildingData == null || !IsEmpty)
            return false;

        return buildingData.type == allowedType;
    }

    public bool Place(BaseBuilding building)
    {
        if (building == null || building.data == null)
            return false;

        if (!IsEmpty && placedBuilding != building)
            return false;

        if (building.data.type != allowedType)
        {
            return false;
        }

        placedBuilding = building;
        building.transform.SetParent(transform);
        building.transform.localPosition = Vector3.zero;
        building.transform.localRotation = Quaternion.identity;

        return true;
    }

    public bool CanUse(int headquartersLevel)
    {
        return headquartersLevel >= requiredHeadquartersLevel;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = IsEmpty
            ? new Color(0.15f, 0.75f, 0.95f, 0.45f)
            : new Color(0.25f, 0.9f, 0.35f, 0.35f);

        Gizmos.DrawWireCube(transform.position + Vector3.up * 0.05f, new Vector3(3.4f, 0.1f, 3.4f));
    }
}

