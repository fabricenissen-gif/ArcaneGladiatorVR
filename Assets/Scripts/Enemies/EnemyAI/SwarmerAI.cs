using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Health))] 
public class SwarmerAI : MonoBehaviour
{
    private enum SwarmState
    {
        Chase,
        Orbit,
        Telegraph, // NEU: Warn-Phase
        Dive
    }

    [Header("References")]
    public Transform player;
    public PlayerHealth playerHealth;
    
    [Tooltip("Der MeshRenderer des Gegners, um ihn rot aufleuchten zu lassen")]
    public MeshRenderer enemyRenderer;

    [Header("Movement")]
    [SerializeField] private float chaseSpeed = 3f;      
    [SerializeField] private float orbitSpeed = 2f;      
    [Tooltip("Näher dran, damit du ihn mit dem Schwert erreichst!")]
    [SerializeField] private float orbitDistance = 1.2f; 
    
    [Header("Attack (Dive)")]
    [SerializeField] private float diveSpeed = 8f;
    [SerializeField] private float diveDamage = 10f;
    [SerializeField] private float minAttackCooldown = 3f;
    [SerializeField] private float maxAttackCooldown = 5f;
    [Tooltip("Wie lange warnt der Gegner vor, bevor er stürzt?")]
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
                // Bleibt auf der Stelle stehen und warnt vor!
                break;
            case SwarmState.Dive:
                HandleDive(distanceToPlayer);
                break;
        }
        
        if (currentState != SwarmState.Dive) 
        {
            LookAtPlayer(); // Im Dive-Modus schaut er stur auf sein Ziel, nicht mehr mitdrehen
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
        
        // Sanftes Schweben
        float hoverOffset = Mathf.Sin(Time.time * 3f) * 0.4f;

        Vector3 orbitOffset = new Vector3(Mathf.Sin(orbitAngle), hoverOffset, Mathf.Cos(orbitAngle)) * orbitDistance;
        Vector3 targetPos = player.position + orbitOffset;
        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * 5f);

        // --- VR FAIRNESS CHECK ---
        // Darf der Swarmer überhaupt angreifen?
        if (Time.time >= lastAttackTime + currentCooldown && Time.time >= globalLastAttackTime + globalAttackSpacing)
        {
            // Berechne den Winkel zwischen dem Spieler-Blick und dem Gegner
            Vector3 directionToEnemy = (transform.position - player.position).normalized;
            
            // Ignoriere die Y-Achse (Höhe) für den Winkel
            directionToEnemy.y = 0; 
            Vector3 playerForward = player.forward;
            playerForward.y = 0;

            float angle = Vector3.Angle(playerForward, directionToEnemy);

            // GANZ WICHTIG: Nur angreifen, wenn er in einem 120-Grad-Feld vor dem Spieler ist!
            if (angle <= 45f) 
            {
                globalLastAttackTime = Time.time; 
                StartCoroutine(TelegraphAndDive());
            }
            else
            {
                // Wenn er hinter dem Spieler ist, kreist er etwas schneller, um nach vorne zu kommen
                orbitSpeed = 4f; 
            }
        }
        else
        {
            orbitSpeed = 2f; // Normale Geschwindigkeit, wenn kein Angriff ansteht
        }
    }

    private IEnumerator TelegraphAndDive()
    {
        currentState = SwarmState.Telegraph;
        
        // 1. Visuelle Warnung (Rot aufleuchten)
        if (enemyRenderer != null) enemyRenderer.material.color = Color.red;

        // Er zieht sich ein klitzekleines Stück zurück, um Schwung zu holen
        Vector3 pullbackPos = transform.position + (transform.position - player.position).normalized * 0.2f;
        
        float timer = 0;
        while (timer < telegraphDuration)
        {
            transform.position = Vector3.Lerp(transform.position, pullbackPos, Time.deltaTime * 5f);
            timer += Time.deltaTime;
            yield return null;
        }

        // 2. Dive-Ziel berechnen (Wir speichern, wo der Kopf des Spielers in DIESEM Moment ist)
        diveTargetPosition = player.position;
        
        // Farbe zurücksetzen
        if (enemyRenderer != null) enemyRenderer.material = originalMaterial;

        currentState = SwarmState.Dive;
    }

    private void HandleDive(float distance)
    {
        // Wir fliegen nicht mehr mitlaufend auf den Spieler, sondern auf die gespeicherte diveTargetPosition!
        // Das erlaubt dem Spieler, durch einen Schritt zur Seite "auszuweichen".
        transform.position = Vector3.MoveTowards(transform.position, diveTargetPosition, diveSpeed * Time.deltaTime);

        // Prüfen, ob er den Spieler getroffen hat (Abstand zum ECHTEN Spieler, nicht zur Zielposition)
        if (Vector3.Distance(transform.position, player.position) < 0.5f)
        {
            if (playerHealth != null)
            {
                playerHealth.TakeDamage((int)diveDamage, transform.forward);
            }
            ResetAfterDive();
        }
        // Oder wenn er seine Zielposition in der Luft erreicht hat (Spieler ist ausgewichen!)
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