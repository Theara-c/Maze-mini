using UnityEngine;

// Sits on the trigger at the bottom of the exit pit. Created automatically by MazeFrame.
public class ExitPit : MonoBehaviour
{
    [Tooltip("ON: falling into the exit pit finishes the maze (win). OFF: it counts as a loss.")]
    public bool winOnFall = true;

    [Tooltip("Turn ON later if the player must hold a key before the pit counts as the exit.")]
    public bool requireKey = false;

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        var gm = GameManager.Instance;
        if (gm == null || !gm.IsPlaying) return;

        if (!winOnFall)
        {
            gm.Lose("You fell into the pit!");
            return;
        }

        if (requireKey && !gm.hasKey)
        {
            gm.Lose("You jumped into the pit without the key!");
            return;
        }

        gm.Win("YOU ESCAPED!\nYou reached the end of the maze.");
    }
}