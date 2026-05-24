using UnityEngine;
using System.Collections.Generic;

public class SupplyManager : MonoBehaviour
{
    public static SupplyManager Instance { get; private set; }

    private List<SupplySource> sources = new List<SupplySource>();
    private List<SupplyConsumer> consumers = new List<SupplyConsumer>();

    [Header("Settings")]
    public float updateInterval = 0.5f;
    private float nextUpdateTime;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void RegisterSource(SupplySource source)
    {
        if (!sources.Contains(source)) sources.Add(source);
    }

    public void UnregisterSource(SupplySource source)
    {
        sources.Remove(source);
    }

    public void RegisterConsumer(SupplyConsumer consumer)
    {
        if (!consumers.Contains(consumer)) consumers.Add(consumer);
    }

    public void UnregisterConsumer(SupplyConsumer consumer)
    {
        consumers.Remove(consumer);
    }

    void Update()
    {
        if (Time.time >= nextUpdateTime)
        {
            UpdateSupplyStatus();
            nextUpdateTime = Time.time + updateInterval;
        }
    }

    private void UpdateSupplyStatus()
    {
        foreach (var consumer in consumers)
        {
            bool isSupplied = false;
            SupplySource activeSource = null;
            
            if (consumer.IsNearBase())
            {
                isSupplied = true;
            }
            else
            {
                foreach (var source in sources)
                {
                    if (source.IsInRange(consumer.transform.position))
                    {
                        isSupplied = true;
                        activeSource = source;
                        break;
                    }
                }
            }

            consumer.SetSupplied(isSupplied);
            UpdateVisualConnection(consumer, activeSource);
        }
    }

    private Dictionary<SupplyConsumer, LineRenderer> connectionLines = new Dictionary<SupplyConsumer, LineRenderer>();

    private void UpdateVisualConnection(SupplyConsumer consumer, SupplySource source)
    {
        if (source != null && consumer.isSupplied)
        {
            LineRenderer line = GetOrCreateLine(consumer);
            line.enabled = true;
            line.SetPosition(0, consumer.transform.position + Vector3.up * 0.5f);
            line.SetPosition(1, source.transform.position + Vector3.up * 0.5f);
        }
        else if (connectionLines.ContainsKey(consumer))
        {
            connectionLines[consumer].enabled = false;
        }
    }

    private LineRenderer GetOrCreateLine(SupplyConsumer consumer)
    {
        if (connectionLines.TryGetValue(consumer, out LineRenderer existing))
            return existing;

        GameObject lineObj = new GameObject("SupplyLine_" + consumer.name);
        lineObj.transform.SetParent(transform);
        LineRenderer line = lineObj.AddComponent<LineRenderer>();
        
        line.startWidth = 0.1f;
        line.endWidth = 0.1f;
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.startColor = new Color(0, 1, 0, 0.5f);
        line.endColor = new Color(0, 1, 0, 0.2f);
        line.positionCount = 2;

        connectionLines[consumer] = line;
        return line;
    }
    }

