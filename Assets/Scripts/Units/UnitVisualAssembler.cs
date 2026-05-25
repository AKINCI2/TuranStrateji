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
    }

    private void SetHierarchyActive(Transform root, bool active)
    {
        if (root == null)
            return;

        root.gameObject.SetActive(active);

        for (int i = 0; i < root.childCount; i++)
            SetHierarchyActive(root.GetChild(i), active);
    }
}
