using UnityEngine;

public sealed class WaypointPath : MonoBehaviour
{
    [SerializeField] private Transform[] waypoints;

    public int Count => waypoints == null ? 0 : waypoints.Length;

    public Vector3 this[int index] => waypoints[index].position;

    public Vector3 StartPosition => Count > 0 ? waypoints[0].position : transform.position;

    private void OnDrawGizmos()
    {
        if (waypoints == null || waypoints.Length == 0)
        {
            return;
        }

        Gizmos.color = Color.yellow;
        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] == null)
            {
                continue;
            }

            Gizmos.DrawSphere(waypoints[i].position, 0.12f);
            if (i + 1 < waypoints.Length && waypoints[i + 1] != null)
            {
                Gizmos.DrawLine(waypoints[i].position, waypoints[i + 1].position);
            }
        }
    }
}
