using UnityEngine;
using UnityEngine.AI;

public class PlayerMovement : MonoBehaviour
{
    public Camera cam;
    public LayerMask groundMask;

    private NavMeshAgent agent;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (cam == null) cam = Camera.main;
    }

    void Update()
    {
        if (!Input.GetMouseButtonDown(1)) return;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 500f, groundMask, QueryTriggerInteraction.Ignore))
        {
            agent.SetDestination(hit.point);
        }
    }
}
