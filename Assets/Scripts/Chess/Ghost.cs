using System.Collections.Generic;
using UnityEngine;

public class Ghost : MonoBehaviour
{
    [SerializeField, Range(0f, 1f)] float ghostAlpha = 0.4f;

    readonly Dictionary<MoveOrder, GameObject> ghosts = new();
    GameManager gm;

    void Start()
    {
        gm = GameManager.Instance;
        gm.Movement.StepStarted += OnStepStarted;
        gm.Movement.StepCompleted += OnStepCompleted;
        gm.Movement.OrderEnded += OnOrderEnded;
    }

    void OnDestroy()
    {
        if (gm == null) return;
        gm.Movement.StepStarted -= OnStepStarted;
        gm.Movement.StepCompleted -= OnStepCompleted;
        gm.Movement.OrderEnded -= OnOrderEnded;
    }

    void OnStepStarted(MoveOrder order)
    {
        RemoveGhost(order);
        ghosts[order] = CreateGhost(order.Piece);
    }

    // La pieza real "salta" a la casilla cuando el fantasma llega
    void OnStepCompleted(MoveOrder order, ChessPiece captured)
        => order.Piece.transform.position = gm.Space.GridToWorld(order.Piece.Position);

    void OnOrderEnded(MoveOrder order, MoveEndReason reason) => RemoveGhost(order);

    GameObject CreateGhost(ChessPiece piece)
    {
        GameObject ghost = Instantiate(piece.gameObject);
        Destroy(ghost.GetComponent<ChessPiece>());

        foreach (SpriteRenderer sr in ghost.GetComponentsInChildren<SpriteRenderer>())
        {
            Color c = sr.color;
            c.a = ghostAlpha;
            sr.color = c;
        }

        return ghost;
    }

    void RemoveGhost(MoveOrder order)
    {
        if (!ghosts.TryGetValue(order, out GameObject ghost)) return;
        Destroy(ghost);
        ghosts.Remove(order);
    }

    void Update()
    {
        foreach (var pair in ghosts)
        {
            MoveOrder order = pair.Key;
            float time = Mathf.InverseLerp(order.StepStartTime, order.StepArrivalTime, gm.Clock.Now);
            Vector3 from = gm.Space.GridToWorld(order.Piece.Position);
            Vector3 to = gm.Space.GridToWorld(order.NextCell);
            pair.Value.transform.position = Vector3.Lerp(from, to, time);
        }
    }
}
