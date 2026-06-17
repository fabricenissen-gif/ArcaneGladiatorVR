using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Health))]
public class SwarmerAI : MonoBehaviour
{
    private enum SwarmState
    {
        Chase,
        Orbit,
        Telegraph,
        Dive
    }

    [Header("References")]
    public Transform player;
    public PlayerHealth playerHealth;
    public MeshRenderer enemyRenderer;

    [Header("Movement")]
    [SerializeField] private float chaseSpeed = 3f;
    [SerializeField] private float orbitSpeed = 2f;
    [SerializeField] private float orbitDistance = 1.2f;

    [Header("Attack (Dive)")]
    [SerializeField] private float diveSpeed = 8f;
    [SerializeField] private float diveDamage = 10f;
    [SerializeField] private float minAttackCooldown = 3f;
    [SerializeField] private float maxAttackCooldown = 5f;
    [SerializeField] private float telegraphDuration = 0.6f;

    private SwarmState currentState = SwarmState.Chase;
    private float lastAttackTime;
    private float currentCooldown;

    private float orbitAngle;
    private int orbitDirection = 1;

    private static float globalLastAttackTime = -999f;
    private static float globalAttackSpacing = 1.5f;

    private Vector3 diveTargetPosition;
    private Material originalMaterial;

    private void Start()
    {
        if (player == null && Camera.main != null) player = Camera.main.transform;
        if (playerHealth == null && player != null) playerHealth = player.GetComponentInParent<PlayerHealth>();

        if (enemyRenderer != null) originalMaterial = enemyRenderer.material;

        orbitDirection = Random.value > 0.5f ? 1 : -1;
        orbitAngle = Random.Range(0f, 360f);

        currentCooldown = Random.Range(minAttackCooldown, maxAttackCooldown);
        lastAttackTime = Time.time + Random.Range(0f, 2f);
    }

    private void Update()
    {
        if (player == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        switch (currentState)
        {
            case SwarmState.Chase:
                HandleChase(distanceToPlayer);
                break;
            case SwarmState.Orbit:
                HandleOrbit(distanceToPlayer);
                break;
            case SwarmState.Telegraph:
                break;
            case SwarmState.Dive:
                HandleDive(distanceToPlayer);
                break;
        }

        if (currentState != SwarmState.Dive)
        {
            LookAtPlayer();
        }
    }

    private void HandleChase(float distance)
    {
        if (distance <= orbitDistance)
        {
            currentState = SwarmState.Orbit;
            return;
        }

        transform.position = Vector3.MoveTowards(transform.position, player.position, chaseSpeed * Time.deltaTime);
    }

    private void HandleOrbit(float distance)
    {
        if (distance > orbitDistance * 2f)
        {
            currentState = SwarmState.Chase;
            return;
        }

        orbitAngle += orbitSpeed * orbitDirection * Time.deltaTime;

        float hoverOffset = Mathf.Sin(Time.time * 3f) * 0.4f;
        Vector3 orbitOffset = new Vector3(Mathf.Sin(orbitAngle), hoverOffset, Mathf.Cos(orbitAngle)) * orbitDistance;
        Vector3 targetPos = player.position + orbitOffset;

        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * 5f);

        if (Time.time >= lastAttackTime + currentCooldown && Time.time >= globalLastAttackTime + globalAttackSpacing)
        {
            Vector3 directionToEnemy = (transform.position - player.position).normalized;
            directionToEnemy.y = 0f;

            Vector3 playerForward = player.forward;
            playerForward.y = 0f;

            float angle = Vector3.Angle(playerForward, directionToEnemy);

            if (angle <= 45f)
            {
                globalLastAttackTime = Time.time;
                StartCoroutine(TelegraphAndDive());
            }
            else
            {
                orbitSpeed = 4f;
            }
        }
        else
        {
            orbitSpeed = 2f;
        }
    }

    private IEnumerator TelegraphAndDive()
    {
        currentState = SwarmState.Telegraph;

        if (enemyRenderer != null)
            enemyRenderer.material.color = Color.red;

        Vector3 pullbackPos = transform.position + (transform.position - player.position).normalized * 0.2f;

        float timer = 0f;
        while (timer < telegraphDuration)
        {
            transform.position = Vector3.Lerp(transform.position, pullbackPos, Time.deltaTime * 5f);
            timer += Time.deltaTime;
            yield return null;
        }

        diveTargetPosition = player.position;

        if (enemyRenderer != null)
            enemyRenderer.material = originalMaterial;

        currentState = SwarmState.Dive;
    }

    private void HandleDive(float distance)
    {
        transform.position = Vector3.MoveTowards(transform.position, diveTargetPosition, diveSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, player.position) < 0.5f)
        {
            if (playerHealth != null)
            {
                playerHealth.TakeDamage((int)diveDamage, transform.forward);
            }

            ResetAfterDive();
        }
        else if (Vector3.Distance(transform.position, diveTargetPosition) < 0.1f)
        {
            Debug.Log("Spieler ist dem Swarmer ausgewichen!");
            ResetAfterDive();
        }
    }

    private void ResetAfterDive()
    {
        lastAttackTime = Time.time;
        currentCooldown = Random.Range(minAttackCooldown, maxAttackCooldown);
        orbitDirection *= -1;
        currentState = SwarmState.Orbit;
    }

    private void LookAtPlayer()
    {
        Vector3 direction = (player.position - transform.position).normalized;
        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
        }
    }
}