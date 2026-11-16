
using UnityEngine;

public class TreasureChest : MonoBehaviour
{
    private bool collected = false;

    private void OnTriggerEnter(Collider other)
    {
        if (collected || !other.CompareTag("Player"))
            return;

        if (GameManager.Instance == null)
            return;

        collected = true;

        GameManager.Instance.CollectKey();

        GameManager.Instance.ShowHint("You collected the coin! You can now open the door!");

        Debug.Log("Coin collected! Key unlocked.");

        Destroy(gameObject);
    }
}
