using UnityEngine;

public class BuildingClickProxy : MonoBehaviour
{
    public BaseBuilding owner;

    public BaseBuilding ResolveOwner()
    {
        if (owner != null)
            return owner;

        owner = GetComponentInParent<BaseBuilding>();
        return owner;
    }
}
