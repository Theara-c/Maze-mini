using UnityEngine;

// Lets the player shrink a maze wall so it can be jumped over.
// Look at a wall (the centre of the screen) and left-click: the wall is scaled down to a low block
// (its bottom stays on the floor). The number of shrinks is limited so the maze cannot be skipped.
//   - Only walls inside the maze can be shrunk, not the outer border (you would walk off the map).
//   - Ghosts still treat a shrunk wall as a wall (their path-finding uses the cell's wall flags), so
//     only the player can hop over it.
// Created automatically by MazeFrame (on the Player). Needs a Collider on the wall prefab.
public class WallShrinker : MonoBehaviour
{
    [Header("Shrink")]
    [Tooltip("How far away a wall can be targeted (world units).")]
    public float reach = 1.5f;
    [Tooltip("Height of a shrunk wall (world units). Keep it under the player's jumpHeight (0.35).")]
    public float shrunkHeight = 0.2f;
    [Tooltip("How many walls can be shrunk per game.")]
    public int maxShrinks = 10;

    int used;
    Camera cam;
    MazeCell[] allCells;

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm != null && !gm.IsPlaying) return;
        if (!GameInput.ShrinkPressed()) return;

        if (used >= maxShrinks)
        {
            if (gm != null) gm.ShowHint("No wall shrinks left!", 2f);
            return;
        }

        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        if (!Physics.Raycast(cam.transform.position, cam.transform.forward, out RaycastHit hit, reach,
                             ~0, QueryTriggerInteraction.Ignore))
            return;

        Transform wall = FindWall(hit.collider.transform, out MazeCell cell);
        if (wall == null) return;                                   // not a maze wall
        if (wall.GetComponent<ShrunkWall>() != null) return;        // already shrunk
        if (IsOuterWall(wall, cell))
        {
            if (gm != null) gm.ShowHint("The outer wall can't be shrunk.", 2f);
            return;
        }

        ShrinkWithNeighbour(wall);
        used++;
        if (gm != null) gm.ShowHint("Wall shrunk! Jump over it. (" + (maxShrinks - used) + " left)", 2.5f);
    }

    // the wall is the direct child of a MazeCell that contains the thing we hit
    static Transform FindWall(Transform t, out MazeCell cell)
    {
        while (t != null && t.parent != null)
        {
            cell = t.parent.GetComponent<MazeCell>();
            if (cell != null) return t.name.ToLower().Contains("wall") ? t : null;   // skips the floor block
            t = t.parent;
        }
        cell = null;
        return null;
    }

    // true if the wall sits on the outside edge of the maze
    bool IsOuterWall(Transform wall, MazeCell cell)
    {
        if (allCells == null || allCells.Length == 0)
            allCells = FindObjectsByType<MazeCell>(FindObjectsSortMode.None);

        Vector2 min = new Vector2(float.MaxValue, float.MaxValue), max = new Vector2(float.MinValue, float.MinValue);
        foreach (var c in allCells)
        {
            Vector3 p = c.transform.position;
            min = Vector2.Min(min, new Vector2(p.x, p.z));
            max = Vector2.Max(max, new Vector2(p.x, p.z));
        }

        Vector3 cellPos = cell.transform.position;
        Vector3 off = wall.position - cellPos;
        off.y = 0f;
        if (off.sqrMagnitude < 0.0001f) return false;

        // which side of the cell is this wall on?
        Vector2 side = Mathf.Abs(off.x) > Mathf.Abs(off.z)
            ? new Vector2(Mathf.Sign(off.x), 0f)
            : new Vector2(0f, Mathf.Sign(off.z));

        return (side.x > 0 && cellPos.x >= max.x - 0.01f) || (side.x < 0 && cellPos.x <= min.x + 0.01f) ||
               (side.y > 0 && cellPos.z >= max.y - 0.01f) || (side.y < 0 && cellPos.z <= min.y + 0.01f);
    }

    // A wall between two cells exists twice (one copy per cell, in the same spot), so shrink every
    // wall at this position. Otherwise the second copy would stay tall and need another click.
    void ShrinkWithNeighbour(Transform wall)
    {
        Vector3 centre = GetBounds(wall).center;
        var toShrink = new System.Collections.Generic.List<Transform> { wall };

        foreach (var c in allCells)
        {
            if ((c.transform.position - centre).sqrMagnitude > 2.5f * 2.5f) continue;   // only nearby cells
            foreach (Transform child in c.transform)
            {
                if (child == wall || !child.name.ToLower().Contains("wall")) continue;
                if (child.GetComponent<ShrunkWall>() != null || !child.gameObject.activeInHierarchy) continue;

                Vector3 d = GetBounds(child).center - centre;
                d.y = 0f;
                if (d.magnitude < 0.25f) toShrink.Add(child);
            }
        }

        foreach (var w in toShrink) Shrink(w);
    }

    void Shrink(Transform wall)
    {
        Bounds b = GetBounds(wall);
        if (b.size.y < 0.001f) return;

        wall.gameObject.AddComponent<ShrunkWall>();

        float oldBottom = b.min.y;
        float factor = Mathf.Clamp(shrunkHeight / b.size.y, 0.05f, 1f);

        Vector3 s = wall.localScale;
        wall.localScale = new Vector3(s.x, s.y * factor, s.z);

        // scaling happens around the pivot, so move the wall back down onto the floor
        float newBottom = GetBounds(wall).min.y;
        wall.position += Vector3.up * (oldBottom - newBottom);
    }

    static Bounds GetBounds(Transform t)
    {
        var rends = t.GetComponentsInChildren<Renderer>();
        if (rends.Length == 0) return new Bounds(t.position, Vector3.zero);
        Bounds b = rends[0].bounds;
        for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
        return b;
    }
}

// marker so a wall can only be shrunk once
public class ShrunkWall : MonoBehaviour { }
