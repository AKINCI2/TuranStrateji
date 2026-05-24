using UnityEngine;

public class RaycastTester : MonoBehaviour
{
    public bool debugLogs = false;

    void Update()
    {
        if (!debugLogs)
            return;

        if (Input.GetMouseButtonDown(0))
        {
            Debug.Log("Mouse tıklandı!"); // BU SATIR ÇALIŞIYOR MU?

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, 1000f))
            {
                Debug.Log("Raycast hit: " + hit.collider.gameObject.name);
            }
            else
            {
                Debug.Log("Raycast boşta");
            }
        }
    }
}

