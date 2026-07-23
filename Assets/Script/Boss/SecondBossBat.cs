using Challenge2.TerrainPrototype;
using UnityEngine;

[RequireComponent(typeof(PrototypeDamageable))]
public class SecondBossBat : MonoBehaviour
{
    [Header("Contact Damage")]
    [SerializeField, Min(0.1f)] private float damageInterval = 2f;

    [Header("Health Bar")]
    [SerializeField] private Vector2 healthBarSize = new Vector2(46f, 6f);
    [SerializeField, Min(0f)] private float healthBarWorldGap = 0.15f;

    private Transform player;
    private Rigidbody2D body;
    private PrototypeDamageable health;
    private SpriteRenderer spriteRenderer;
    private Camera worldCamera;
    private float moveSpeed;
    private int damage;
    private float dieAt;
    private float nextDamageTime;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        health = GetComponent<PrototypeDamageable>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        worldCamera = Camera.main;
    }

    private void OnEnable()
    {
        if (health == null)
        {
            health = GetComponent<PrototypeDamageable>();
        }

        if (health != null)
        {
            health.HealthChanged += HandleHealthChanged;
            HandleHealthChanged(
                health.CurrentHealth,
                health.MaximumHealth);
        }
    }

    private void OnDisable()
    {
        if (health != null)
        {
            health.HealthChanged -= HandleHealthChanged;
        }
    }

    public void Initialize(
        Transform target,
        float speed,
        int contactDamage,
        float lifeTime)
    {
        player = target;
        moveSpeed = Mathf.Max(0.1f, speed);
        damage = Mathf.Max(1, contactDamage);
        dieAt = Time.time + Mathf.Max(0.1f, lifeTime);
        nextDamageTime = 0f;

        if (body != null)
        {
            body.gravityScale = 0f;
            body.bodyType = RigidbodyType2D.Kinematic;
        }
    }

    private void FixedUpdate()
    {
        if (player == null || Time.time >= dieAt)
        {
            Destroy(gameObject);
            return;
        }

        Vector2 direction =
            ((Vector2)player.position - (Vector2)transform.position).normalized;

        if (body != null)
        {
            body.MovePosition(body.position + direction * moveSpeed * Time.fixedDeltaTime);
        }
        else
        {
            transform.position +=
                (Vector3)(direction * moveSpeed * Time.fixedDeltaTime);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        DamagePlayer(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        DamagePlayer(other);
    }

    private void DamagePlayer(Collider2D other)
    {
        if (other == null || Time.time < nextDamageTime)
        {
            return;
        }

        PrototypeDamageable target =
            other.GetComponentInParent<PrototypeDamageable>();

        if (target == null || target.Owner != TerrainOwner.Player)
        {
            return;
        }

        if (target.TryApplyDamage(damage, TerrainOwner.Boss, transform))
        {
            nextDamageTime = Time.time + damageInterval;
        }
    }

    private void HandleHealthChanged(
        int currentHealth,
        int maximumHealth)
    {
        if (currentHealth <= 0)
        {
            Destroy(gameObject);
        }
    }

    private void OnGUI()
    {
        if (health == null || !health.IsAlive)
        {
            return;
        }

        if (worldCamera == null)
        {
            worldCamera = Camera.main;
        }

        if (worldCamera == null)
        {
            return;
        }

        Vector3 barWorldPosition = transform.position;
        if (spriteRenderer != null)
        {
            barWorldPosition.y =
                spriteRenderer.bounds.max.y + healthBarWorldGap;
        }

        Vector3 screenPosition =
            worldCamera.WorldToScreenPoint(barWorldPosition);
        if (screenPosition.z <= 0f)
        {
            return;
        }

        float healthRate = health.MaximumHealth > 0
            ? health.CurrentHealth / (float)health.MaximumHealth
            : 0f;
        Rect background = new Rect(
            screenPosition.x - healthBarSize.x * 0.5f,
            Screen.height - screenPosition.y,
            healthBarSize.x,
            healthBarSize.y);

        GUI.color = Color.black;
        GUI.DrawTexture(background, Texture2D.whiteTexture);
        GUI.color = Color.red;
        GUI.DrawTexture(
            new Rect(
                background.x + 1f,
                background.y + 1f,
                (background.width - 2f) * healthRate,
                background.height - 2f),
            Texture2D.whiteTexture);
        GUI.color = Color.white;
    }
}
