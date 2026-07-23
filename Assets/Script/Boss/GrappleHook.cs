using Challenge2.TerrainPrototype;
using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-100)]
[RequireComponent(typeof(Collider2D))]
public class GrappleHook : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private Rigidbody2D playerBody;
    [SerializeField] private heroscrip hero;
    [SerializeField, Min(1f)] private float pullSpeed = 55f;
    [SerializeField, Min(0.1f)] private float attachDistance = 1.5f;
    [SerializeField, Min(0f)] private float hangDistance = 1f;

    private static GrappleHook activeHook;

    private Collider2D hookCollider;
    private Collider2D playerCollider;
    private readonly ContactPoint2D[] contacts = new ContactPoint2D[12];
    private float previousGravityScale;
    private bool pulling;
    private bool attached;

    private void Awake()
    {
        hookCollider = GetComponent<Collider2D>();
        FindPlayerIfNeeded();
    }

    private void Update()
    {
        if (!pulling)
        {
            if (Mouse.current != null &&
                Mouse.current.leftButton.wasPressedThisFrame &&
                WasThisHookClicked())
            {
                BeginGrapple();
            }

            return;
        }

        if (Keyboard.current != null &&
            Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            Release();
        }
    }

    private void FixedUpdate()
    {
        if (!pulling || playerBody == null)
        {
            return;
        }

        if (IsTouchingStoneWall())
        {
            Release();
            return;
        }

        Vector2 attachPoint =
            (Vector2)transform.position + Vector2.down * hangDistance;
        Vector2 toHook = attachPoint - playerBody.position;

        if (!attached && toHook.magnitude <= attachDistance)
        {
            attached = true;
        }

        if (attached)
        {
            playerBody.velocity = Vector2.zero;
            playerBody.MovePosition(attachPoint);
        }
        else
        {
            playerBody.velocity = toHook.normalized * pullSpeed;
        }
    }

    private bool WasThisHookClicked()
    {
        FindPlayerIfNeeded();

        Camera camera = Camera.main;
        if (camera == null)
        {
            camera = FindObjectOfType<Camera>();
        }

        if (camera == null || hookCollider == null)
        {
            return false;
        }

        Vector2 screenPosition = Mouse.current.position.ReadValue();
        Vector2 worldPosition = camera.ScreenToWorldPoint(screenPosition);
        return hookCollider.OverlapPoint(worldPosition);
    }

    private void BeginGrapple()
    {
        FindPlayerIfNeeded();
        if (playerBody == null || hero == null)
        {
            return;
        }

        if (activeHook != null && activeHook != this)
        {
            activeHook.Release();
        }

        activeHook = this;
        pulling = true;
        attached = false;
        previousGravityScale = playerBody.gravityScale;
        playerBody.gravityScale = 0f;
        playerBody.velocity = Vector2.zero;
        hero.IsGrappling = true;
    }

    public void Release()
    {
        if (!pulling)
        {
            return;
        }

        pulling = false;
        attached = false;

        if (playerBody != null)
        {
            playerBody.gravityScale = previousGravityScale;
            playerBody.velocity = Vector2.zero;
        }

        if (hero != null)
        {
            hero.IsGrappling = false;
        }

        if (activeHook == this)
        {
            activeHook = null;
        }
    }

    private bool IsTouchingStoneWall()
    {
        if (playerCollider == null)
        {
            return false;
        }

        int count = playerCollider.GetContacts(contacts);
        for (int i = 0; i < count; i++)
        {
            Collider2D other = contacts[i].otherCollider;
            if (other == null)
            {
                continue;
            }

            TerrainSegment segment =
                other.GetComponentInParent<TerrainSegment>();
            TerrainEntity terrain = segment != null
                ? segment.ParentTerrain
                : other.GetComponentInParent<TerrainEntity>();

            if (terrain is FallingStoneWallTerrain)
            {
                return true;
            }
        }

        return false;
    }

    private void FindPlayerIfNeeded()
    {
        if (player == null)
        {
            GameObject playerObject = GameObject.Find("Hero");
            player = playerObject != null ? playerObject.transform : null;
        }

        if (player == null)
        {
            return;
        }

        if (playerBody == null)
        {
            playerBody = player.GetComponent<Rigidbody2D>();
        }

        if (hero == null)
        {
            hero = player.GetComponent<heroscrip>();
        }

        if (playerCollider == null)
        {
            playerCollider = player.GetComponent<Collider2D>();
        }
    }

    private void OnDisable()
    {
        Release();
    }
}
