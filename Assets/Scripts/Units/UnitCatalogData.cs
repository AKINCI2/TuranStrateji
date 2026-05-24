using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "UnitCatalog_New",
    menuName = "Turan Strateji/Units/Unit Catalog")]
public class UnitCatalogData : ScriptableObject
{
    public List<TuranUnitData> units = new List<TuranUnitData>();

    public TuranUnitData FindById(string unitId)
    {
        foreach (TuranUnitData unit in units)
        {
            if (unit != null && unit.unitId == unitId)
                return unit;
        }

        return null;
    }

    public List<TuranUnitData> GetByKind(TuranUnitKind kind)
    {
        List<TuranUnitData> result = new List<TuranUnitData>();
        foreach (TuranUnitData unit in units)
        {
            if (unit != null && unit.kind == kind)
                result.Add(unit);
        }

        return result;
    }
}
