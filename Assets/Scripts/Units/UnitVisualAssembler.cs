using System.Collections.Generic;
using UnityEngine;

public class UnitVisualAssembler : MonoBehaviour
{
    public UnitController unitController;
    public FormationController formationController;

    private readonly List<GameObject> spawnedVisuals = new List<GameObject>();

    public void Assemble(TuranUnitData unitData)
    {
        if (unitData == null || unitData.visualProfile == null)
        {
            EnsureExistingChildrenVisible();
            return;
        }

        UnitVisualProfile profile = unitData.visualProfile;
        GameObject characterPrefab = profile.characterPrefab;
        if (characterPrefab == null)
        {
            EnsureExistingChildrenVisible();
            return;
        }

        Clear();
        RemoveLegacyPlaceholderChildren();

        if (formationController == null)
            formationController = GetComponent<FormationController>();

        if (unitController == null)
            unitController = GetComponent<UnitController>();

        int count = Mathf.Max(1, profile.visibleSoldierCount);
        if (unitData.kind == TuranUnitKind.Tank ||
            unitData.kind == TuranUnitKind.Artillery ||
            unitData.kind == TuranUnitKind.RocketArtillery ||
            unitData.kind == TuranUnitKind.Logistics)
        {
            count = 1;
        }

        for (int i = 0; i < count; i++)
        {
            GameObject visual = Instantiate(characterPrefab, transform);
            visual.name = characterPrefab.name + "_" + (i + 1);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;

            AttachWeapon(visual.transform, unitData.weaponData, profile);
            spawnedVisuals.Add(visual);
        }

        if (formationController != null)
        {
            formationController.soldiers.Clear();
            foreach (GameObject visual in spawnedVisuals)
                formationController.soldiers.Add(visual.transform);

            formationController.maxSoldiers = count;
            formationController.spacing = profile.spacing;
            formationController.hexFillRatio = profile.hexFillRatio;
        }
    }

    private void AttachWeapon(Transform visualRoot, WeaponData weaponData, UnitVisualProfile profile)
    {
        if (visualRoot == null || weaponData == null || weaponData.weaponPrefab == null)
            return;

        Transform socket = FindSocket(visualRoot, profile);
        if (socket == null)
            return;

        GameObject weapon = Instantiate(weaponData.weaponPrefab, socket);
        weapon.name = weaponData.weaponPrefab.name;
        weapon.transform.localPosition = weaponData.socketLocalPosition;
        weapon.transform.localRotation = Quaternion.Euler(weaponData.socketLocalRotation);
        weapon.transform.localScale = weaponData.socketLocalScale;
    }

    private Transform FindSocket(Transform visualRoot, UnitVisualProfile profile)
    {
        WeaponSocket[] sockets = visualRoot.GetComponentsInChildren<WeaponSocket>(true);
        foreach (WeaponSocket socket in sockets)
        {
            if (socket != null && socket.socketName == profile.weaponSocketName)
                return socket.transform;
        }

        Animator animator = visualRoot.GetComponentInChildren<Animator>();
        if (animator != null && animator.isHuman)
        {
            Transform hand = animator.GetBoneTransform(profile.fallbackHandBone);
            if (hand != null)
                return hand;
        }

        return visualRoot;
    }

    public void Clear()
    {
        for (int i = spawnedVisuals.Count - 1; i >= 0; i--)
        {
            if (spawnedVisuals[i] != null)
                Destroy(spawnedVisuals[i]);
        }

        spawnedVisuals.Clear();
    }

    private void RemoveLegacyPlaceholderChildren()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (child == null)
                continue;

            if (!child.name.StartsWith("Cube", System.StringComparison.OrdinalIgnoreCase))
                continue;

            if (child.GetComponent<UnitController>() != null ||
                child.GetComponent<FormationController>() != null ||
                child.GetComponentInChildren<SkinnedMeshRenderer>(true) != null)
            {
                continue;
            }

            Destroy(child.gameObject);
        }
    }

    public void EnsureExistingChildrenVisible()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            SetHierarchyActive(child, true);
        }

        bool clonedFallback = false;
        if (!HasCombatVisual(transform))
            clonedFallback = TryCloneSceneFallbackVisual();

        if (!HasCombatVisual(transform))
            clonedFallback = TryCloneLooseSoldierVisuals() || clonedFallback;

        if (formationController == null)
            formationController = GetComponent<FormationController>();

        if (formationController != null)
            formationController.RebuildSoldiersFromChildren(clonedFallback);
    }

    private void SetHierarchyActive(Transform root, bool active)
    {
        if (root == null)
            return;

        root.gameObject.SetActive(active);

        for (int i = 0; i < root.childCount; i++)
            SetHierarchyActive(root.GetChild(i), active);
    }

    private bool TryCloneSceneFallbackVisual()
    {
        UnitController[] controllers =
            FindObjectsByType<UnitController>(FindObjectsInactive.Include);

        foreach (UnitController controller in controllers)
        {
            if (controller == null || controller.transform == transform)
                continue;

            if (controller.unitData != null &&
                controller.unitData.kind == TuranUnitKind.Logistics)
            {
                continue;
            }

            if (!HasCombatVisual(controller.transform))
                continue;

            int cloned = CloneCombatVisualChildren(controller.transform);
            if (cloned > 0)
                return true;
        }

        return false;
    }

    private bool TryCloneLooseSoldierVisuals()
    {
        Transform[] transforms = Resources.FindObjectsOfTypeAll<Transform>();
        int cloned = 0;

        foreach (Transform candidate in transforms)
        {
            if (candidate == null || candidate == transform || candidate.root == transform)
                continue;

            if (!candidate.gameObject.scene.IsValid())
                continue;

            if (!IsCombatVisualRoot(candidate))
                continue;

            if (candidate.GetComponentInParent<UnitVisualAssembler>() != null)
                continue;

            string lowerName = candidate.name.ToLowerInvariant();
            bool looksLikeSoldier =
                lowerName.StartsWith("soldier") ||
                lowerName.Contains("asker") ||
                lowerName.Contains("piyade");

            if (!looksLikeSoldier && candidate.GetComponentInChildren<Animator>(true) == null)
                continue;

            GameObject clone = Instantiate(candidate.gameObject, transform);
            clone.name = candidate.name + "_Fallback";
            clone.transform.localPosition = Vector3.zero;
            clone.transform.localRotation = Quaternion.identity;
            clone.transform.localScale = Vector3.one;
            SetHierarchyActive(clone.transform, true);
            spawnedVisuals.Add(clone);
            cloned++;

            if (cloned >= 4)
                break;
        }

        return cloned > 0;
    }

    private int CloneCombatVisualChildren(Transform sourceRoot)
    {
        int clonedCount = 0;

        for (int i = 0; i < sourceRoot.childCount; i++)
        {
            Transform sourceChild = sourceRoot.GetChild(i);
            if (!IsCombatVisualRoot(sourceChild))
                continue;

            GameObject clone = Instantiate(sourceChild.gameObject, transform);
            clone.name = sourceChild.name;
            clone.transform.localPosition = sourceChild.localPosition;
            clone.transform.localRotation = sourceChild.localRotation;
            clone.transform.localScale = sourceChild.localScale;
            SetHierarchyActive(clone.transform, true);
            spawnedVisuals.Add(clone);
            clonedCount++;
        }

        return clonedCount;
    }

    private static bool HasCombatVisual(Transform root)
    {
        if (root == null)
            return false;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (IsCombatVisualRoot(child))
                return true;
        }

        return false;
    }

    private static bool IsCombatVisualRoot(Transform target)
    {
        if (target == null)
            return false;

        string lowerName = target.name.ToLowerInvariant();
        if (lowerName.Contains("canvas") ||
            lowerName.Contains("health") ||
            lowerName.Contains("bar") ||
            lowerName.Contains("badge") ||
            lowerName.Contains("selection") ||
            lowerName.Contains("ring") ||
            lowerName.Contains("firepoint") ||
            lowerName.Contains("muzzle") ||
            lowerName.Contains("bullet") ||
            lowerName.Contains("shadow"))
        {
            return false;
        }

        if (target.GetComponent<UnitController>() != null ||
            target.GetComponent<FormationController>() != null ||
            target.GetComponent<Canvas>() != null ||
            target.GetComponent<RectTransform>() != null)
        {
            return false;
        }

        if (lowerName.StartsWith("soldier") ||
            lowerName.Contains("asker"))
        {
            return true;
        }

        if (target.GetComponentInChildren<Animator>(true) != null)
            return true;

        if (target.GetComponentInChildren<SkinnedMeshRenderer>(true) != null)
            return true;

        MeshRenderer[] renderers = target.GetComponentsInChildren<MeshRenderer>(true);
        foreach (MeshRenderer renderer in renderers)
        {
            if (renderer == null)
                continue;

            if (renderer.GetComponentInParent<Canvas>() != null ||
                renderer.GetComponentInParent<RectTransform>() != null)
            {
                continue;
            }

            return true;
        }

        return false;
    }
}
