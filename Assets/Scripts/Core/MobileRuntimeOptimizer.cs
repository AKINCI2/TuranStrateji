using UnityEngine;

public class MobileRuntimeOptimizer : MonoBehaviour
{
    [Header("Frame")]
    public int targetFps = 60;
    public bool disableVSync = true;

    [Header("Shadows")]
    public bool mobileShadowOptimization = true;
    public ShadowQuality shadowQuality = ShadowQuality.HardOnly;
    public float shadowDistance = 35f;

    [Header("Rendering")]
    public int antiAliasing = 2;
    public AnisotropicFiltering anisotropicFiltering = AnisotropicFiltering.Disable;
    public bool disableRealtimeReflectionProbes = true;

    [Header("Physics")]
    public bool syncTransformsOnApply = true;

    void Awake()
    {
        Apply();
    }

    public void Apply()
    {
        Application.targetFrameRate = Mathf.Clamp(targetFps, 30, 120);
        Screen.sleepTimeout = SleepTimeout.NeverSleep;

        if (disableVSync)
            QualitySettings.vSyncCount = 0;

        if (mobileShadowOptimization)
        {
            QualitySettings.shadows = shadowQuality;
            QualitySettings.shadowDistance = Mathf.Clamp(shadowDistance, 20f, 70f);
            QualitySettings.shadowResolution = ShadowResolution.Medium;
            QualitySettings.shadowCascades = 2;
        }

        QualitySettings.antiAliasing = antiAliasing;
        QualitySettings.anisotropicFiltering = anisotropicFiltering;

        if (disableRealtimeReflectionProbes)
            QualitySettings.realtimeReflectionProbes = false;

        if (syncTransformsOnApply)
            Physics.SyncTransforms();
    }
}
