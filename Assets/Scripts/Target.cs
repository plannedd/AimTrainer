using UnityEngine;

public class Target : MonoBehaviour
{

    [SerializeField] public float health = 10f;
    
    public GameObject TargetBox;

    void Start()
    {
        TargetBox = GameObject.Find("Target");
    }

    public void TakeDamage(float amount)
    {
        health -= amount;
        if (health <= 0f)
        {
            Die();
        }
    }

    void Die()
    {
            TargetBox.transform.position = new Vector3(Random.Range(-25.0f, -35.0f), Random.Range(4.0f, 10.0f), Random.Range(-23.0f, -24.0f));
    }
}
