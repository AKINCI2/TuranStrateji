using UnityEngine;

public class WarpathZoomVisibility : MonoBehaviour
{
    public WarpathZoomLayer minLayer = WarpathZoomLayer.StrategicWorld;
    public WarpathZoomLayer maxLayer = WarpathZoomLayer.BaseInterior;
    public bool keepActiveInBaseInterior;

    public void ApplyLayer(WarpathZoomLayer layer)
    {
        bool visible = layer >= minLayer && layer <= maxLayer;
        if (layer == WarpathZoomLayer.BaseInterior && keepActiveInBaseInterior)
            visible = true;

        if (gameObject.activeSelf != visible)
            gameObject.SetActive(visible);
    }
}
