using UnityEngine;

public class BoardSpace : MonoBehaviour
{
    [SerializeField] Vector3 boardOrigin;
    [SerializeField] float cellSize = 1f;
    [SerializeField] bool drawGizmos = true;

    public Vector3 GridToWorld(Vector2Int cell)
        => new (boardOrigin.x + (cell.x + 0.5f) * cellSize, boardOrigin.y, boardOrigin.z + (cell.y + 0.5f) * cellSize);

    public Vector2Int WorldToGrid(Vector3 world)
        => new (Mathf.FloorToInt((world.x - boardOrigin.x) / cellSize), Mathf.FloorToInt((world.z - boardOrigin.z) / cellSize));

    public bool TryRaycastBoard(Ray ray, out Vector3 point)
    {
        Plane plane = new Plane(Vector3.up, boardOrigin);

        if (plane.Raycast(ray, out float distance))
        {
            point = ray.GetPoint(distance);
            return true;
        }

        point = default;
        return false;
    }

    void OnDrawGizmos()
    {
        if (!drawGizmos) return;

        // Líneas de la grilla
        Gizmos.color = Color.red;
        float size = cellSize * 8;
        for (int i = 0; i <= 8; i++)
        {
            float offset = i * cellSize;
            Gizmos.DrawLine(boardOrigin + new Vector3(offset, 0, 0), boardOrigin + new Vector3(offset, 0, size));
            Gizmos.DrawLine(boardOrigin + new Vector3(0, 0, offset), boardOrigin + new Vector3(size, 0, offset));
        }

        // Centro de cada casilla
        Gizmos.color = Color.yellow;
        for (int x = 0; x < 8; x++)
            for (int y = 0; y < 8; y++)
                Gizmos.DrawSphere(GridToWorld(new Vector2Int(x, y)), cellSize * 0.05f);

        // Origen y esquinas de referencia
        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(boardOrigin, cellSize * 0.1f);
    }
}
