using UnityEditor;

public class TuranAnimationImportSettings : AssetPostprocessor
{
    void OnPreprocessModel()
    {
        if (!assetPath.EndsWith(".fbx"))
            return;

        string lowerPath =
            assetPath.ToLowerInvariant();

        bool shouldLoop =
            lowerPath.Contains("walking") ||
            lowerPath.Contains("run") ||
            lowerPath.Contains("rifleidle") ||
            lowerPath.Contains("idle");

        if (!shouldLoop)
            return;

        ModelImporter importer =
            assetImporter as ModelImporter;

        if (importer == null)
            return;

        ModelImporterClipAnimation[] clips =
            importer.defaultClipAnimations;

        for (int i = 0; i < clips.Length; i++)
        {
            clips[i].loopTime = true;
            clips[i].loopPose = true;
            clips[i].lockRootHeightY = true;
            clips[i].lockRootPositionXZ = true;
        }

        importer.clipAnimations = clips;
    }
}

