using UnityEngine;

// A pickup: walk into it to get +1 coin. Each coin is one life (a ghost touch costs one coin).
// Obstaclerandom adds this to every coin it spawns, so you do not need to edit the prefab.
public class Coin : MonoBehaviour
{
    public float spinSpeed = 120f;   // degrees per second, set 0 to stop spinning
    public int value = 1;

    void Awake()
    {
        // make every collider on the coin a trigger so the player walks through it instead of bumping it
        foreach (var c in GetComponentsInChildren<Collider>()) c.isTrigger = true;
        transform.rotation = Quaternion.Euler(90f, 0f, 0f);
    }

    void Update()
    {
        transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<PlayerController>() == null) return;

        if (GameManager.Instance != null) GameManager.Instance.AddCoin(value);
        Destroy(gameObject);
    }
}
