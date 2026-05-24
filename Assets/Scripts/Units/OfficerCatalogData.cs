using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "OfficerCatalog_New",
    menuName = "Turan Strateji/Officers/Officer Catalog")]
public class OfficerCatalogData : ScriptableObject
{
    public List<OfficerData> officers = new List<OfficerData>();
}
