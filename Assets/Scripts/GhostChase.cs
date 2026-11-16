// using System.Collections.Generic;
// using UnityEngine;

// // Makes a ghost chase the player through the maze, cell by cell, so it never walks through walls.
// // Put this on the ghost prefab (the one assigned to _ghostPrefab in Obstaclerandom).
// // Needs: MazeCell with the HasXxxWall properties, and GameManager.
// //   - Catching is distance-based: no Hazard script, tag or trigger collider is needed on the ghost.
// //   - The ghost freezes while the game is in the menu, paused, or over.
// public class GhostChase : MonoBehaviour
// {
//     [Header("Chase")]
//     [Tooltip("Keep this lower than the player's moveSpeed (1.5) so the player can escape.")]
//     public float moveSpeed = 1f;
//     [Tooltip("Seconds of play time before the ghost starts moving.")]
//     public float startDelay = 5f;
//     [Tooltip("Horizontal distance at which the ghost catches the player.")]
//     public float catchDistance = 0.35f;
//     [Tooltip("Only chase when the player is within this many cells along the maze path. 0 = always chase.")]
//     public int detectionRange = 0;

//     [Header("Look")]
//     public float bobHeight = 0.05f;
//     public float bobSpeed = 2f;
//     public bool faceMovement = true;
//     public float turnSpeed = 8f;
//     [Tooltip("Add 90, 180 or 270 if the model faces the wrong way when it moves.")]
//     public float facingOffset = 0f;

//     [Header("Setup")]
//     [Tooltip("The ghost's own colliders are not needed (catching uses distance) and could push the player.")]
//     public bool disableColliders = true;

//     static readonly Vector2Int[] Dirs = { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down }; // up = +Z, down = -Z

//     readonly Dictionary<Vector2Int, MazeCell> cells = new Dictionary<Vector2Int, MazeCell>();
//     readonly Dictionary<Vector2Int, int> dist = new Dictionary<Vector2Int, int>();   // path distance to the player's cell
//     readonly Queue<Vector2Int> queue = new Queue<Vector2Int>();

//     Transform player;
//     Vector2Int min, max;
//     Vector2Int ghostCell, nextCell, lastPlayerCell;
//     bool moving, haveDistances;
//     float baseY, playTime;

//     void Awake()
//     {
//         // The free ghost asset has its own keyboard script: Space teleports the ghost to (0,0,0),
//         // arrow keys move it, A / S play animations. Turn it off so only GhostChase controls the ghost.
//         var keyboardScript = GetComponent<Sample.GhostScript>();
//         if (keyboardScript != null) keyboardScript.enabled = false;

//         var rb = GetComponent<Rigidbody>();
//         if (rb != null) { rb.isKinematic = true; rb.useGravity = false; }

//         if (disableColliders)
//             foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = false;
//     }

//     void Start()
//     {
//         baseY = transform.position.y;

//         foreach (var c in FindObjectsByType<MazeCell>(FindObjectsSortMode.None))
//             cells[ToCell(c.transform.position)] = c;

//         if (cells.Count == 0)
//         {
//             Debug.LogWarning("[GhostChase] No MazeCell found, the ghost will not move.");
//             enabled = false;
//             return;
//         }

//         bool first = true;
//         foreach (var key in cells.Keys)
//         {
//             if (first) { min = max = key; first = false; continue; }
//             min = Vector2Int.Min(min, key);
//             max = Vector2Int.Max(max, key);
//         }

//         ghostCell = ClampToMaze(ToCell(transform.position));
//     }

//     void Update()
//     {
//         var gm = GameManager.Instance;
//         if (gm != null && !gm.IsPlaying) return;   // menu, paused or game over

//         if (player == null)
//         {
//             player = FindPlayer();
//             if (player == null) return;
//         }

//         playTime += Time.deltaTime;
//         Vector3 pos = transform.position;

//         if (playTime >= startDelay)
//         {
//             // the player may be outside the grid (entrance / exit corridor): use the nearest maze cell
//             Vector2Int playerCell = ClampToMaze(ToCell(player.position));
//             if (!haveDistances || playerCell != lastPlayerCell)
//             {
//                 ComputeDistances(playerCell);
//                 lastPlayerCell = playerCell;
//                 haveDistances = true;
//             }

//             Chase(ref pos);
//         }

//         pos.y = baseY + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
//         transform.position = pos;

//         CheckCatch(gm);
//     }

//     // ---------- Chasing ----------

//     void Chase(ref Vector3 pos)
//     {
//         Vector3 target;

//         if (moving)
//         {
//             target = CellCenter(nextCell);
//         }
//         else
//         {
//             if (!dist.TryGetValue(ghostCell, out int d)) return;    // cell is cut off from the player
//             if (detectionRange > 0 && d > detectionRange) return;  // player is too far away

//             if (d == 0)
//             {
//                 target = player.position;                          // same cell: go straight for the player
//             }
//             else
//             {
//                 nextCell = BestNeighbour(ghostCell, d);
//                 moving = true;
//                 target = CellCenter(nextCell);
//             }
//         }

//         Vector3 flatTarget = new Vector3(target.x, pos.y, target.z);
//         Vector3 before = pos;
//         pos = Vector3.MoveTowards(pos, flatTarget, moveSpeed * Time.deltaTime);

//         if (faceMovement)
//         {
//             Vector3 dir = pos - before;
//             dir.y = 0f;
//             if (dir.sqrMagnitude > 0.000001f)
//             {
//                 Quaternion look = Quaternion.LookRotation(dir) * Quaternion.Euler(0f, facingOffset, 0f);
//                 transform.rotation = Quaternion.Slerp(transform.rotation, look, turnSpeed * Time.deltaTime);
//             }
//         }

//         // arrived at the next cell centre: it becomes the current cell
//         if (moving && (flatTarget - pos).sqrMagnitude < 0.0001f)
//         {
//             ghostCell = nextCell;
//             moving = false;
//         }
//     }

//     // Breadth-first search outward from the player's cell. Afterwards dist[cell] is the number of
//     // steps from that cell to the player, so the ghost just steps to the neighbour with a smaller number.
//     void ComputeDistances(Vector2Int target)
//     {
//         dist.Clear();
//         queue.Clear();
//         dist[target] = 0;
//         queue.Enqueue(target);

//         while (queue.Count > 0)
//         {
//             Vector2Int c = queue.Dequeue();
//             foreach (var dir in Dirs)
//             {
//                 Vector2Int n = c + dir;
//                 if (dist.ContainsKey(n) || !CanPass(c, dir)) continue;
//                 dist[n] = dist[c] + 1;
//                 queue.Enqueue(n);
//             }
//         }
//     }

//     Vector2Int BestNeighbour(Vector2Int from, int currentDist)
//     {
//         Vector2Int best = from;
//         int bestDist = currentDist;

//         foreach (var dir in Dirs)
//         {
//             Vector2Int n = from + dir;
//             if (!CanPass(from, dir)) continue;
//             if (dist.TryGetValue(n, out int nd) && nd < bestDist)
//             {
//                 best = n;
//                 bestDist = nd;
//             }
//         }
//         return best;
//     }

//     // true if there is no wall between the cell and its neighbour in that direction
//     bool CanPass(Vector2Int from, Vector2Int dir)
//     {
//         if (!cells.TryGetValue(from, out MazeCell a)) return false;
//         if (!cells.TryGetValue(from + dir, out MazeCell b)) return false;

//         if (dir == Vector2Int.right) return !a.HasRightWall && !b.HasLeftWall;
//         if (dir == Vector2Int.left)  return !a.HasLeftWall  && !b.HasRightWall;
//         if (dir == Vector2Int.up)    return !a.HasFrontWall && !b.HasBackWall;
//         return !a.HasBackWall && !b.HasFrontWall;
//     }

//     // ---------- Catching ----------

//     void CheckCatch(GameManager gm)
//     {
//         if (player == null) return;

//         Vector3 diff = player.position - transform.position;
//         float flat = new Vector2(diff.x, diff.z).magnitude;

//         if (flat < catchDistance && Mathf.Abs(diff.y) < 1f)
//         {
//             // a ghost that takes a coin is used up and disappears
//             if (gm != null) { if (gm.HitByGhost()) Destroy(gameObject); }
//             else Debug.Log("[GhostChase] Caught the player (no GameManager in scene).");
//         }
//     }

//     // ---------- Helpers ----------

//     Transform FindPlayer()
//     {
//         var gm = GameManager.Instance;
//         if (gm != null && gm.player != null) return gm.player;

//         var go = GameObject.FindGameObjectWithTag("Player");
//         return go != null ? go.transform : null;
//     }

//     static Vector2Int ToCell(Vector3 p)
//     {
//         return new Vector2Int(Mathf.RoundToInt(p.x), Mathf.RoundToInt(p.z));
//     }

//     static Vector3 CellCenter(Vector2Int c)
//     {
//         return new Vector3(c.x, 0f, c.y);
//     }

//     Vector2Int ClampToMaze(Vector2Int c)
//     {
//         return new Vector2Int(Mathf.Clamp(c.x, min.x, max.x), Mathf.Clamp(c.y, min.y, max.y));
//     }
// }
using System.Collections.Generic;
using UnityEngine;

// Makes a ghost chase the player through the maze, cell by cell, so it never walks through walls.
// Put this on the ghost prefab (the one assigned to _ghostPrefab in Obstaclerandom).
// Needs: MazeCell with the HasXxxWall properties, and GameManager.
//   - Catching is distance-based: no Hazard script, tag or trigger collider is needed on the ghost.
//   - The ghost freezes while the game is in the menu, paused, or over.
public class GhostChase : MonoBehaviour
{
    [Header("Chase")]
    [Tooltip("Keep this lower than the player's moveSpeed (1.5) so the player can escape.")]
    public float moveSpeed = 1f;
    [Tooltip("Seconds of play time before the ghost starts moving.")]
    public float startDelay = 5f;
    [Tooltip("Horizontal distance at which the ghost catches the player.")]
    public float catchDistance = 0.35f;
    [Tooltip("Only chase when the player is within this many cells along the maze path. 0 = always chase.")]
    public int detectionRange = 0;

    [Header("Look")]
    public float bobHeight = 0.5f;
    public float bobSpeed = 2f;
    public bool faceMovement = true;
    public float turnSpeed = 8f;
    [Tooltip("Add 90, 180 or 270 if the model faces the wrong way when it moves.")]
    public float facingOffset = 0f;

    [Header("Photo Face")]
    [Tooltip("Picture shown as the ghost (PNG with a transparent background). Leave empty to load Assets/Resources/GhostFace.png.")]
    public Texture2D faceTexture;
    [Tooltip("How tall the picture is, in world units. The maze cells are 1 wide.")]
    public float faceHeight = 1.5f;
    [Tooltip("Hide the original ghost model and show only the picture.")]
    public bool hideOriginalModel = true;

    [Header("Setup")]
    [Tooltip("The ghost's own colliders are not needed (catching uses distance) and could push the player.")]
    public bool disableColliders = true;

    static readonly Vector2Int[] Dirs = { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down }; // up = +Z, down = -Z

    readonly Dictionary<Vector2Int, MazeCell> cells = new Dictionary<Vector2Int, MazeCell>();
    readonly Dictionary<Vector2Int, int> dist = new Dictionary<Vector2Int, int>();   // path distance to the player's cell
    readonly Queue<Vector2Int> queue = new Queue<Vector2Int>();

    Transform player;
    Vector2Int min, max;
    Vector2Int ghostCell, nextCell, lastPlayerCell;
    bool moving, haveDistances;
    float baseY, playTime;

    void Awake()
    {
        // The free ghost asset has its own keyboard script: Space teleports the ghost to (0,0,0),
        // arrow keys move it, A / S play animations. Turn it off so only GhostChase controls the ghost.
        var keyboardScript = GetComponent<Sample.GhostScript>();
        if (keyboardScript != null) keyboardScript.enabled = false;

        SetupFace();

        var rb = GetComponent<Rigidbody>();
        if (rb != null) { rb.isKinematic = true; rb.useGravity = false; }

        if (disableColliders)
            foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = false;
    }

    void Start()
    {
        baseY = transform.position.y;

        foreach (var c in FindObjectsByType<MazeCell>(FindObjectsSortMode.None))
            cells[ToCell(c.transform.position)] = c;

        if (cells.Count == 0)
        {
            Debug.LogWarning("[GhostChase] No MazeCell found, the ghost will not move.");
            enabled = false;
            return;
        }

        bool first = true;
        foreach (var key in cells.Keys)
        {
            if (first) { min = max = key; first = false; continue; }
            min = Vector2Int.Min(min, key);
            max = Vector2Int.Max(max, key);
        }

        ghostCell = ClampToMaze(ToCell(transform.position));
    }

    // ---------- Photo face ----------

    Transform face;

    void SetupFace()
    {
        if (faceTexture == null) faceTexture = Resources.Load<Texture2D>("GhostFace");
        if (faceTexture == null) return;   // no picture found: keep the normal ghost model

        if (hideOriginalModel)
            foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = false;

        var go = new GameObject("Ghost Face");
        go.transform.SetParent(transform, false);
        // the picture is centred on the ghost's position; ppu sets how tall it is in world units
        float ppu = faceTexture.height / Mathf.Max(0.01f, faceHeight);
        var sprite = Sprite.Create(faceTexture, new Rect(0, 0, faceTexture.width, faceTexture.height),
                                   new Vector2(0.5f, 0.5f), ppu);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        face = go.transform;
    }

    // the picture always turns to face the player's camera (stays upright)
    void LateUpdate()
    {
        if (face == null) return;
        var cam = Camera.main;
        if (cam == null) return;

        Vector3 f = cam.transform.forward;
        f.y = 0f;
        if (f.sqrMagnitude > 0.0001f) face.rotation = Quaternion.LookRotation(f);
    }

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm != null && !gm.IsPlaying) return;   // menu, paused or game over

        if (player == null)
        {
            player = FindPlayer();
            if (player == null) return;
        }

        playTime += Time.deltaTime;
        Vector3 pos = transform.position;

        if (playTime >= startDelay)
        {
            // the player may be outside the grid (entrance / exit corridor): use the nearest maze cell
            Vector2Int playerCell = ClampToMaze(ToCell(player.position));
            if (!haveDistances || playerCell != lastPlayerCell)
            {
                ComputeDistances(playerCell);
                lastPlayerCell = playerCell;
                haveDistances = true;
            }

            Chase(ref pos);
        }

        pos.y = baseY + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = pos;

        CheckCatch(gm);
    }

    // ---------- Chasing ----------

    void Chase(ref Vector3 pos)
    {
        Vector3 target;

        if (moving)
        {
            target = CellCenter(nextCell);
        }
        else
        {
            if (!dist.TryGetValue(ghostCell, out int d)) return;    // cell is cut off from the player
            if (detectionRange > 0 && d > detectionRange) return;  // player is too far away

            if (d == 0)
            {
                target = player.position;                          // same cell: go straight for the player
            }
            else
            {
                nextCell = BestNeighbour(ghostCell, d);
                moving = true;
                target = CellCenter(nextCell);
            }
        }

        Vector3 flatTarget = new Vector3(target.x, pos.y, target.z);
        Vector3 before = pos;
        pos = Vector3.MoveTowards(pos, flatTarget, moveSpeed * Time.deltaTime);

        if (faceMovement)
        {
            Vector3 dir = pos - before;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.000001f)
            {
                Quaternion look = Quaternion.LookRotation(dir) * Quaternion.Euler(0f, facingOffset, 0f);
                transform.rotation = Quaternion.Slerp(transform.rotation, look, turnSpeed * Time.deltaTime);
            }
        }

        // arrived at the next cell centre: it becomes the current cell
        if (moving && (flatTarget - pos).sqrMagnitude < 0.0001f)
        {
            ghostCell = nextCell;
            moving = false;
        }
    }

    // Breadth-first search outward from the player's cell. Afterwards dist[cell] is the number of
    // steps from that cell to the player, so the ghost just steps to the neighbour with a smaller number.
    void ComputeDistances(Vector2Int target)
    {
        dist.Clear();
        queue.Clear();
        dist[target] = 0;
        queue.Enqueue(target);

        while (queue.Count > 0)
        {
            Vector2Int c = queue.Dequeue();
            foreach (var dir in Dirs)
            {
                Vector2Int n = c + dir;
                if (dist.ContainsKey(n) || !CanPass(c, dir)) continue;
                dist[n] = dist[c] + 1;
                queue.Enqueue(n);
            }
        }
    }

    Vector2Int BestNeighbour(Vector2Int from, int currentDist)
    {
        Vector2Int best = from;
        int bestDist = currentDist;

        foreach (var dir in Dirs)
        {
            Vector2Int n = from + dir;
            if (!CanPass(from, dir)) continue;
            if (dist.TryGetValue(n, out int nd) && nd < bestDist)
            {
                best = n;
                bestDist = nd;
            }
        }
        return best;
    }

    // true if there is no wall between the cell and its neighbour in that direction
    bool CanPass(Vector2Int from, Vector2Int dir)
    {
        if (!cells.TryGetValue(from, out MazeCell a)) return false;
        if (!cells.TryGetValue(from + dir, out MazeCell b)) return false;

        if (dir == Vector2Int.right) return !a.HasRightWall && !b.HasLeftWall;
        if (dir == Vector2Int.left)  return !a.HasLeftWall  && !b.HasRightWall;
        if (dir == Vector2Int.up)    return !a.HasFrontWall && !b.HasBackWall;
        return !a.HasBackWall && !b.HasFrontWall;
    }

    // ---------- Catching ----------

    void CheckCatch(GameManager gm)
    {
        if (player == null) return;

        Vector3 diff = player.position - transform.position;
        float flat = new Vector2(diff.x, diff.z).magnitude;

        if (flat < catchDistance && Mathf.Abs(diff.y) < 1f)
        {
            // a ghost that takes a coin is used up and disappears
            if (gm != null) { if (gm.HitByGhost()) Destroy(gameObject); }
            else Debug.Log("[GhostChase] Caught the player (no GameManager in scene).");
        }
    }

    // ---------- Helpers ----------

    Transform FindPlayer()
    {
        var gm = GameManager.Instance;
        if (gm != null && gm.player != null) return gm.player;

        var go = GameObject.FindGameObjectWithTag("Player");
        return go != null ? go.transform : null;
    }

    static Vector2Int ToCell(Vector3 p)
    {
        return new Vector2Int(Mathf.RoundToInt(p.x), Mathf.RoundToInt(p.z));
    }

    static Vector3 CellCenter(Vector2Int c)
    {
        return new Vector3(c.x, 0f, c.y);
    }

    Vector2Int ClampToMaze(Vector2Int c)
    {
        return new Vector2Int(Mathf.Clamp(c.x, min.x, max.x), Mathf.Clamp(c.y, min.y, max.y));
    }
}