using UnityEngine;

// Builds everything around the generated maze:
//   - a floor under the maze (so there is no giant plane to hide pits)
//   - an ENTRANCE corridor on the back wall of the start cell
//   - an EXIT ledge on the front wall of the far corner cell, ending in a deep PIT
//   - the first-person player (the scene's Main Camera is moved onto the player)
//   - a GameManager, if the scene does not already have one
public static class MazeFrame
{
    const float Thick = 0.2f;   // wall thickness, same as the maze cell walls
    const float High = 1f;      // wall height, same as the maze cell walls

    public static void Build(MazeCell[,] grid, int width, int depth, MazeCell cellPrefab, Material floorMaterial)
    {
        // the big 1000x1000 ground plane would make a pit impossible
        var plane = GameObject.Find("Plane");
        if (plane != null) plane.SetActive(false);

        Material wallMat = FindWallMaterial(cellPrefab);

        var root = new GameObject("Maze Frame").transform;

        // ---------- Floor under the maze ----------
        Box("Maze Floor", root,
            new Vector3((width - 1) / 2f, -0.1f, (depth - 1) / 2f),
            new Vector3(width, 0.2f, depth),
            floorMaterial, new Color(0.45f, 0.45f, 0.5f));

        // ---------- ENTRANCE (back wall of cell 0,0) ----------
        grid[0, 0].ClearBackWall();

        Box("Entrance Floor", root, new Vector3(0f, -0.1f, -1.55f), new Vector3(1f, 0.2f, 2.1f),
            floorMaterial, new Color(0.3f, 0.7f, 0.35f));                                   // green = start
        Box("Entrance Left Wall", root, new Vector3(-0.5f, 0.5f, -1.55f), new Vector3(Thick, High, 2.1f), wallMat, Color.gray);
        Box("Entrance Right Wall", root, new Vector3(0.5f, 0.5f, -1.55f), new Vector3(Thick, High, 2.1f), wallMat, Color.gray);
        Box("Entrance Back Wall", root, new Vector3(0f, 0.5f, -2.5f), new Vector3(1.2f, High, Thick), wallMat, Color.gray);

        // ---------- EXIT (front wall of the far corner cell) ----------
        grid[width - 1, depth - 1].ClearFrontWall();

        float x = width - 1;       // x of the exit corridor
        float z = depth;           // first row outside the maze (maze edge is at z - 0.5)

        // short ledge to walk out on (red = danger)
        Box("Exit Ledge", root, new Vector3(x, -0.1f, z - 0.1f), new Vector3(1f, 0.2f, 0.8f),
            null, new Color(0.85f, 0.2f, 0.2f));

        // tall walls that make a shaft: the pit starts where the ledge ends
        Box("Pit Left Wall", root, new Vector3(x - 0.5f, -4f, z + 0.65f), new Vector3(Thick, 10f, 2.3f), wallMat, Color.gray);
        Box("Pit Right Wall", root, new Vector3(x + 0.5f, -4f, z + 0.65f), new Vector3(Thick, 10f, 2.3f), wallMat, Color.gray);
        Box("Pit Back Wall", root, new Vector3(x, -4f, z + 1.8f), new Vector3(1.2f, 10f, Thick), wallMat, Color.gray);
        Box("Pit Bottom", root, new Vector3(x, -9.1f, z + 1.0f), new Vector3(1f, 0.2f, 1.4f), null, Color.black);

        // trigger partway down the pit: touching it ends the game
        var trigger = new GameObject("Exit Pit Trigger");
        trigger.transform.SetParent(root, false);
        trigger.transform.position = new Vector3(x, -2f, z + 1.0f);
        var box = trigger.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(0.9f, 1f, 1.3f);
        trigger.AddComponent<ExitPit>();

        // ---------- GameManager ----------
        var gm = Object.FindFirstObjectByType<GameManager>();
        if (gm == null) gm = new GameObject("GameManager").AddComponent<GameManager>();

        // ---------- First-person player ----------
        Transform player = CreatePlayer(new Vector3(0f, 0.32f, -1.8f));
        gm.player = player;
    }

    static Transform CreatePlayer(Vector3 position)
    {
        var player = new GameObject("Player");
        player.tag = "Player";
        player.transform.position = position;   // faces +Z, into the maze

        // The cells are 1 unit wide, so the player is small (0.3 radius is too wide).
        var capsule = player.AddComponent<CapsuleCollider>();
        capsule.radius = 0.15f;
        capsule.height = 0.6f;
        capsule.sharedMaterial = new PhysicsMaterial("PlayerNoFriction")
        {
            dynamicFriction = 0f,
            staticFriction = 0f,
            bounciness = 0f,
            frictionCombine = PhysicsMaterialCombine.Minimum,
            bounceCombine = PhysicsMaterialCombine.Minimum
        };

        player.AddComponent<Rigidbody>();
        player.AddComponent<PlayerController>();

        // Reuse the scene's Main Camera as the first-person camera.
        Camera cam = Camera.main;
        if (cam == null)
        {
            var camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            cam = camObj.AddComponent<Camera>();
            camObj.AddComponent<AudioListener>();
        }

        cam.transform.SetParent(player.transform, false);
        cam.transform.localPosition = new Vector3(0f, 0.18f, 0f);   // eye height
        cam.transform.localRotation = Quaternion.identity;
        cam.nearClipPlane = 0.02f;                                  // lets you stand close to thin walls
        cam.fieldOfView = 75f;

        var look = cam.gameObject.AddComponent<PlayerLook>();
        look.playerBody = player.transform;

        return player.transform;
    }

    static Material FindWallMaterial(MazeCell cellPrefab)
    {
        var renderers = cellPrefab.GetComponentsInChildren<MeshRenderer>(true);
        foreach (var r in renderers)
        {
            if (r.transform.parent != null && r.transform.parent.name.Contains("Wall"))
                return r.sharedMaterial;
        }
        return renderers.Length > 0 ? renderers[0].sharedMaterial : null;
    }

    static GameObject Box(string name, Transform parent, Vector3 center, Vector3 size, Material mat, Color fallbackColor)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = center;
        go.transform.localScale = size;

        var r = go.GetComponent<Renderer>();
        if (mat != null) r.sharedMaterial = mat;
        else r.material.color = fallbackColor;

        return go;
    }
}