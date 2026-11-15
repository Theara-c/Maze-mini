
using UnityEngine;

public class TrapScript : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        // Player touches the trap = Game Over
        if (GameManager.Instance != null &&
            GameManager.Instance.IsPlaying)
        {
            GameManager.Instance.Lose("You touched a trap!");
        }
    }
}
