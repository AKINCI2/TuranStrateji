using UnityEngine;

[RequireComponent(typeof(Collider))]
public class WorldResourceNode : MonoBehaviour
{
    public WorldResourceType resourceType = WorldResourceType.Steel;
    public int level = 1;
    public int amount = 100;
    public int remainingAmount = 100;
    public HexCell hex;
    public bool isCollected;

    [Header("Gathering")]
    public bool requiresTimedGathering = true;
    public float gatherDurationSeconds = 20f;
    public int gatherPowerPerSecond = 10;
    public bool isGathering;

    [Header("Guard")]
    public bool hasGuard;
    public bool firstClearRewardGranted;
    public int guardMaxHealth = 60;
    public int guardHealth = 60;
    public int guardDamage = 8;
    public ResourceCost firstClearReward;

    private Renderer[] renderers;
    private GameObject guardRoot;
    private GameObject collectorRoot;
    private UnitController activeGatherer;
    private float gatherTimer;

    void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>(true);
    }

    void Update()
    {
        TickGathering();
    }

    public ResourceCost GetReward()
    {
        return GetRewardForAmount(amount);
    }

    public ResourceCost GetRewardForAmount(int rewardAmount)
    {
        ResourceCost reward = new ResourceCost();

        switch (resourceType)
        {
            case WorldResourceType.Gold:
                reward.gold = rewardAmount;
                break;
            case WorldResourceType.TuranCoin:
                reward.turanCoin = rewardAmount;
                break;
            case WorldResourceType.Steel:
            case WorldResourceType.Wood:
            case WorldResourceType.Concrete:
            case WorldResourceType.Cement:
            case WorldResourceType.Brick:
                reward.steel = rewardAmount;
                break;
            case WorldResourceType.Oil:
                reward.oil = rewardAmount;
                break;
            case WorldResourceType.Bor:
                reward.bor = rewardAmount;
                break;
        }

        return reward;
    }

    public bool TryCollect(UnitController unit)
    {
        return BeginInteraction(unit);
    }

    public bool BeginInteraction(UnitController unit)
    {
        if (isCollected || unit == null || BaseManager.Instance == null)
            return false;

        if (isGathering)
            return activeGatherer == unit;

        if (!ResolveGuardCombat(unit))
            return false;

        GrantFirstClearRewardIfNeeded();

        if (!requiresTimedGathering)
            return CollectAllNow();

        StartGathering(unit);
        return true;
    }

    public void ConfigureCollection(int totalAmount, float durationSeconds)
    {
        amount = Mathf.Max(1, totalAmount);
        remainingAmount = amount;
        gatherDurationSeconds = Mathf.Max(1f, durationSeconds);
        gatherPowerPerSecond = Mathf.Max(1, Mathf.CeilToInt(amount / gatherDurationSeconds));
    }

    public void ConfigureGuard(bool enabled, int health, int damage, ResourceCost firstReward)
    {
        hasGuard = enabled;
        guardMaxHealth = enabled ? Mathf.Max(0, health) : 0;
        guardHealth = guardMaxHealth;
        guardDamage = enabled ? Mathf.Max(0, damage) : 0;
        firstClearReward = firstReward;
        RefreshGuardVisual();
    }

    public void SetGuardRoot(GameObject root)
    {
        guardRoot = root;
        RefreshGuardVisual();
    }

    private bool ResolveGuardCombat(UnitController unit)
    {
        if (!hasGuard)
            return true;

        if (guardHealth <= 0)
            return true;

        int unitDamage = GetUnitDamage(unit);
        Health unitHealth =
            unit.GetComponent<Health>();

        int rounds = 0;
        while (guardHealth > 0 && rounds < 8)
        {
            guardHealth -= unitDamage;
            rounds++;

            if (guardHealth > 0 && unitHealth != null && guardDamage > 0)
                unitHealth.TakeDamage(guardDamage);

            if (unitHealth != null && unitHealth.currentHealth <= 0)
                break;
        }

        Debug.Log(
            $"Kaynak korumasiyla catisma: {resourceType} Lv.{level} | " +
            $"Tur: {rounds} | Koruma HP: {Mathf.Max(0, guardHealth)}"
        );

        if (guardHealth <= 0)
        {
            RefreshGuardVisual();
            Debug.Log($"Kaynak korumasi temizlendi: {resourceType} Lv.{level}");
            return true;
        }

        return false;
    }

    private void GrantFirstClearRewardIfNeeded()
    {
        if (!hasGuard || firstClearRewardGranted || firstClearReward.IsZero())
            return;

        firstClearRewardGranted = true;
        BaseManager.Instance.AddResources(firstClearReward);
        Debug.Log($"Ilk temizleme odulu alindi: {resourceType} Lv.{level} | {firstClearReward.ToDisplayString()}");
    }

    private bool CollectAllNow()
    {
        int collectedAmount = Mathf.Max(0, remainingAmount);
        if (collectedAmount <= 0)
            return false;

        remainingAmount = 0;
        isCollected = true;
        BaseManager.Instance.AddResources(GetRewardForAmount(collectedAmount));
        SetVisualActive(false);
        Debug.Log($"Kaynak toplandi: {resourceType} Lv.{level} +{collectedAmount}");
        return true;
    }

    private void StartGathering(UnitController unit)
    {
        if (remainingAmount <= 0)
        {
            isCollected = true;
            SetVisualActive(false);
            return;
        }

        activeGatherer = unit;
        isGathering = true;
        RefreshCollectorVisual(true);
        Debug.Log($"Toplama basladi: {resourceType} Lv.{level} | Kalan: {remainingAmount} | Sure: {gatherDurationSeconds:0}s");
    }

    private void TickGathering()
    {
        if (!isGathering || isCollected || activeGatherer == null)
            return;

        if (!activeGatherer.gameObject.activeInHierarchy ||
            activeGatherer.state == UnitState.Death ||
            activeGatherer.state == UnitState.Move ||
            activeGatherer.currentHex != hex ||
            Vector3.Distance(activeGatherer.transform.position, transform.position) > 3.5f)
        {
            StopGathering("Toplayici ayrildi");
            return;
        }

        gatherTimer += Time.deltaTime;
        if (gatherTimer < 1f)
            return;

        int seconds = Mathf.FloorToInt(gatherTimer);
        gatherTimer -= seconds;

        int collectAmount =
            Mathf.Min(remainingAmount, gatherPowerPerSecond * seconds);

        if (collectAmount <= 0)
            return;

        remainingAmount -= collectAmount;
        BaseManager.Instance.AddResources(GetRewardForAmount(collectAmount));

        int collected = amount - remainingAmount;
        Debug.Log($"Kaynak toplaniyor: {resourceType} Lv.{level} | +{collectAmount} | Toplanan: {collected}/{amount}");

        if (remainingAmount <= 0)
        {
            isCollected = true;
            isGathering = false;
            RefreshCollectorVisual(false);
            SetVisualActive(false);
            Debug.Log($"Kaynak tamamen toplandi: {resourceType} Lv.{level}");
        }
    }

    private void StopGathering(string reason)
    {
        isGathering = false;
        activeGatherer = null;
        gatherTimer = 0f;
        RefreshCollectorVisual(false);
        Debug.Log($"{reason}: {resourceType} Lv.{level} | Kalan: {remainingAmount}/{amount}");
    }

    private int GetUnitDamage(UnitController unit)
    {
        UnitCombat combat =
            unit.GetComponent<UnitCombat>();

        if (combat != null)
            return Mathf.Max(1, combat.damage);

        return 25;
    }

    private void RefreshGuardVisual()
    {
        if (guardRoot != null)
            guardRoot.SetActive(hasGuard && guardHealth > 0 && !isCollected);
    }

    public void SetCollectorRoot(GameObject root)
    {
        collectorRoot = root;
        RefreshCollectorVisual(false);
    }

    private void RefreshCollectorVisual(bool active)
    {
        if (collectorRoot != null)
            collectorRoot.SetActive(active && !isCollected);
    }

    public void SetVisualActive(bool active)
    {
        if (renderers == null || renderers.Length == 0)
            renderers = GetComponentsInChildren<Renderer>(true);

        foreach (Renderer renderer in renderers)
        {
            if (renderer != null)
                renderer.enabled = active;
        }

        Collider nodeCollider = GetComponent<Collider>();
        if (nodeCollider != null)
            nodeCollider.enabled = active;
    }
}

