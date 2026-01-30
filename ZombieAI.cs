using UnityEngine;
using UnityEngine.AI;
using System.Collections;
[RequireComponent(typeof(AudioSource))]
public class ZombieAI : MonoBehaviour
{
    [Header("=== OYUNCU REFERANSI ===")]
    [Tooltip("Oyuncu Transform'unu buraya ata (veya 'Player' tagı ile otomatik bulunur)")]
    public Transform player;
    [Header("=== ALGILAMA ALANLARI ===")]
    [Tooltip("Dış alan - Oyuncu girince uyarı sesi çalar")]
    [SerializeField] private float outerDetectionRadius = 15f;
    [Tooltip("İç alan - Oyuncu girince zombi takip başlar")]
    [SerializeField] private float innerDetectionRadius = 7f;
    [Tooltip("Saldırı mesafesi")]
    [SerializeField] private float attackRange = 2f;
    [Header("=== HAREKET AYARLARI ===")]
    [Tooltip("Yürüme hızı")]
    [SerializeField] private float walkSpeed = 1.5f;
    [Tooltip("Koşma hızı (opsiyonel)")]
    [SerializeField] private float runSpeed = 4f;
    [Tooltip("Dönme hızı")]
    [SerializeField] private float rotationSpeed = 2f;
    [Tooltip("Koşmaya başlama mesafesi (0 = asla koşma)")]
    [SerializeField] private float runTriggerDistance = 5f;
    [Header("=== SES AYARLARI ===")]
    [Tooltip("Dış alana girildiğinde çalan müzik/ses")]
    [SerializeField] private AudioClip outerAreaMusic;
    [Tooltip("İç alana girildiğinde çalan müzik/ses (dış müzik üstüne eklenir)")]
    [SerializeField] private AudioClip innerAreaMusic;
    [Tooltip("Boşta zombi sesleri")]
    [SerializeField] private AudioClip[] idleSounds;
    [Tooltip("Takip sırasında zombi sesleri")]
    [SerializeField] private AudioClip[] chaseSounds;
    [Tooltip("Saldırı sesi")]
    [SerializeField] private AudioClip attackSound;
    [Tooltip("Müzik geçiş süresi (saniye)")]
    [SerializeField] private float musicTransitionDuration = 2f;
    [Tooltip("Dış alan müzik ses seviyesi")]
    [SerializeField] private float musicVolume = 0.7f;
    [Tooltip("İç alan müzik ses çarpanı (3 = %300)")]
    [SerializeField] private float innerMusicVolumeMultiplier = 3f;
    [Tooltip("Normal müzik hızı (dış alan)")]
    [SerializeField] private float normalMusicSpeed = 1f;
    [Tooltip("Hızlı müzik hızı (iç alan - takip)")]
    [SerializeField] private float fastMusicSpeed = 1.3f;
    [Header("=== ANIMATOR (Opsiyonel) ===")]
    [Tooltip("Animator varsa buraya ata")]
    public Animator zombieAnimator;
    [Header("=== NAV MESH (Opsiyonel) ===")]
    [Tooltip("NavMeshAgent varsa otomatik kullanılır")]
    public NavMeshAgent navAgent;
    [Header("=== DEBUG GÖRSELLEŞTİRME ===")]
    [Tooltip("Oyun içinde algılama alanlarını göster")]
    [SerializeField] private bool showDebugCircles = true;
    [Tooltip("Dış alan çizgi rengi")]
    [SerializeField] private Color outerCircleColor = new Color(1f, 0.3f, 0f, 0.8f);
    [Tooltip("İç alan çizgi rengi")]
    [SerializeField] private Color innerCircleColor = new Color(1f, 0f, 0f, 0.8f);
    [Tooltip("Saldırı alanı çizgi rengi")]
    [SerializeField] private Color attackCircleColor = new Color(0.8f, 0f, 0f, 1f);
    [Tooltip("Çizgi kalınlığı")]
    [SerializeField] private float lineWidth = 0.1f;
    private LineRenderer outerCircleLine;
    private LineRenderer innerCircleLine;
    private LineRenderer attackCircleLine;
    public enum ZombieState
    {
        Idle,
        Alert,
        Chasing,
        Attacking
    }
    private AudioSource outerMusicSource;
    private AudioSource innerMusicSource;
    private AudioSource sfxSource;
    private ZombieState currentState = ZombieState.Idle;
    private ZombieState previousState = ZombieState.Idle;
    private float distanceToPlayer;
    private bool isTransitioningMusic = false;
    private float idleSoundTimer = 0f;
    private float chaseSoundTimer = 0f;
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int IsAlertHash = Animator.StringToHash("IsAlert");
    private static readonly int IsChasingHash = Animator.StringToHash("IsChasing");
    private static readonly int AttackTriggerHash = Animator.StringToHash("Attack");
    void Awake()
    {
        SetupAudioSources();
        if (navAgent == null)
            navAgent = GetComponent<NavMeshAgent>();
        if (showDebugCircles)
            SetupDebugCircles();
    }
    void SetupAudioSources()
    {
        outerMusicSource = GetComponent<AudioSource>();
        if (outerMusicSource == null)
            outerMusicSource = gameObject.AddComponent<AudioSource>();
        outerMusicSource.playOnAwake = false;
        outerMusicSource.loop = true;
        outerMusicSource.spatialBlend = 0.5f;
        outerMusicSource.pitch = normalMusicSpeed;
        innerMusicSource = gameObject.AddComponent<AudioSource>();
        innerMusicSource.playOnAwake = false;
        innerMusicSource.loop = true;
        innerMusicSource.spatialBlend = 0.5f;
        innerMusicSource.pitch = normalMusicSpeed;
        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        sfxSource.loop = false;
        sfxSource.spatialBlend = 1f;
    }
    void SetupDebugCircles()
    {
        outerCircleLine = CreateCircle("OuterDetectionCircle", outerDetectionRadius, outerCircleColor);
        innerCircleLine = CreateCircle("InnerDetectionCircle", innerDetectionRadius, innerCircleColor);
        attackCircleLine = CreateCircle("AttackRangeCircle", attackRange, attackCircleColor);
    }
    LineRenderer CreateCircle(string name, float radius, Color color)
    {
        GameObject circleObj = new GameObject(name);
        circleObj.transform.SetParent(transform);
        circleObj.transform.localPosition = Vector3.up * 0.05f;
        LineRenderer lr = circleObj.AddComponent<LineRenderer>();
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = color;
        lr.endColor = color;
        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;
        lr.useWorldSpace = false;
        lr.loop = true;
        int segments = 64;
        lr.positionCount = segments;
        float angleStep = 360f / segments;
        for (int i = 0; i < segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            float x = Mathf.Cos(angle) * radius;
            float z = Mathf.Sin(angle) * radius;
            lr.SetPosition(i, new Vector3(x, 0f, z));
        }
        return lr;
    }
    void UpdateDebugCircles()
    {
        if (!showDebugCircles) return;
        if (outerCircleLine != null)
        {
            Color outerColor = (currentState == ZombieState.Alert || currentState == ZombieState.Chasing || currentState == ZombieState.Attacking) 
                ? new Color(1f, 0.5f, 0f, 1f)
                : outerCircleColor;
            outerCircleLine.startColor = outerColor;
            outerCircleLine.endColor = outerColor;
        }
        if (innerCircleLine != null)
        {
            Color innerColor = (currentState == ZombieState.Chasing || currentState == ZombieState.Attacking)
                ? new Color(1f, 0f, 0f, 1f)
                : innerCircleColor;
            innerCircleLine.startColor = innerColor;
            innerCircleLine.endColor = innerColor;
        }
        if (attackCircleLine != null)
        {
            Color attackColor = (currentState == ZombieState.Attacking)
                ? new Color(1f, 0f, 0f, 1f)
                : attackCircleColor;
            attackCircleLine.startColor = attackColor;
            attackCircleLine.endColor = attackColor;
        }
    }
    public void ToggleDebugCircles(bool show)
    {
        showDebugCircles = show;
        if (outerCircleLine != null) outerCircleLine.gameObject.SetActive(show);
        if (innerCircleLine != null) innerCircleLine.gameObject.SetActive(show);
        if (attackCircleLine != null) attackCircleLine.gameObject.SetActive(show);
    }
    void Start()
    {
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
                Debug.Log("ZombieAI: Oyuncu 'Player' tag'ı ile otomatik bulundu.");
            }
            else
            {
                Debug.LogError("ZombieAI: Oyuncu bulunamadı! 'Player' tag'ı ekle veya Inspector'dan ata.");
            }
        }
        if (navAgent != null)
        {
            navAgent.speed = walkSpeed;
            navAgent.stoppingDistance = attackRange;
            navAgent.isStopped = true;
        }
    }
    void Update()
    {
        if (player == null) return;
        distanceToPlayer = Vector3.Distance(transform.position, player.position);
        DetermineState();
        HandleState();
        UpdateAnimator();
        UpdateDebugCircles();
    }
    void DetermineState()
    {
        previousState = currentState;
        if (distanceToPlayer <= attackRange)
        {
            currentState = ZombieState.Attacking;
        }
        else if (distanceToPlayer <= innerDetectionRadius)
        {
            currentState = ZombieState.Chasing;
        }
        else if (distanceToPlayer <= outerDetectionRadius)
        {
            currentState = ZombieState.Alert;
        }
        else
        {
            currentState = ZombieState.Idle;
        }
        if (currentState != previousState)
        {
            OnStateChanged(previousState, currentState);
        }
    }
    void OnStateChanged(ZombieState from, ZombieState to)
    {
        Debug.Log($"ZombieAI: Durum değişti: {from} -> {to}");
        switch (to)
        {
            case ZombieState.Idle:
                StopChasing();
                StartCoroutine(FadeOutAllMusic());
                break;
            case ZombieState.Alert:
                Debug.Log("ZombieAI: Oyuncu dış alanda - sadece dış müzik çalıyor");
                StartCoroutine(StartOuterMusic());
                StopChasing();
                break;
            case ZombieState.Chasing:
                Debug.Log("ZombieAI: Oyuncu iç alanda - müzik hızlanıyor, zombi takibe başlıyor!");
                StartCoroutine(StartInnerMusicLayer());
                StartChasing();
                break;
            case ZombieState.Attacking:
                Attack();
                break;
        }
    }
    void HandleState()
    {
        switch (currentState)
        {
            case ZombieState.Idle:
                HandleIdle();
                break;
            case ZombieState.Alert:
                HandleAlert();
                break;
            case ZombieState.Chasing:
                HandleChasing();
                break;
            case ZombieState.Attacking:
                HandleAttacking();
                break;
        }
    }
    void HandleIdle()
    {
        idleSoundTimer += Time.deltaTime;
        if (idleSoundTimer >= Random.Range(5f, 10f))
        {
            idleSoundTimer = 0f;
            PlayRandomSound(idleSounds);
        }
    }
    void HandleAlert()
    {
    }
    void HandleChasing()
    {
        LookAtPlayer(rotationSpeed);
        if (navAgent != null && navAgent.enabled)
        {
            navAgent.SetDestination(player.position);
            if (runTriggerDistance > 0 && distanceToPlayer <= runTriggerDistance)
            {
                navAgent.speed = runSpeed;
            }
            else
            {
                navAgent.speed = walkSpeed;
            }
        }
        else
        {
            MoveTowardsPlayer();
        }
        chaseSoundTimer += Time.deltaTime;
        if (chaseSoundTimer >= Random.Range(3f, 6f))
        {
            chaseSoundTimer = 0f;
            PlayRandomSound(chaseSounds);
        }
    }
    void HandleAttacking()
    {
        LookAtPlayer(rotationSpeed * 2f);
        if (navAgent != null)
            navAgent.isStopped = true;
    }
    void LookAtPlayer(float speed)
    {
        Vector3 direction = (player.position - transform.position).normalized;
        direction.y = 0;
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, speed * Time.deltaTime);
        }
    }
    void MoveTowardsPlayer()
    {
        Vector3 direction = (player.position - transform.position).normalized;
        direction.y = 0;
        float speed = (runTriggerDistance > 0 && distanceToPlayer <= runTriggerDistance) ? runSpeed : walkSpeed;
        transform.position += direction * speed * Time.deltaTime;
    }
    void StartChasing()
    {
        if (navAgent != null)
        {
            navAgent.isStopped = false;
        }
    }
    void StopChasing()
    {
        if (navAgent != null)
        {
            navAgent.isStopped = true;
        }
    }
    void Attack()
    {
        if (zombieAnimator != null)
        {
            zombieAnimator.SetTrigger(AttackTriggerHash);
        }
        if (attackSound != null)
        {
            sfxSource.PlayOneShot(attackSound);
        }
    }
    #region Müzik Sistemi
    IEnumerator StartOuterMusic()
    {
        if (outerAreaMusic == null) yield break;
        if (innerMusicSource.isPlaying)
        {
            yield return StartCoroutine(FadeVolume(innerMusicSource, innerMusicSource.volume, 0f, musicTransitionDuration));
            innerMusicSource.Stop();
        }
        if (outerMusicSource.isPlaying)
        {
            yield return StartCoroutine(FadePitch(outerMusicSource, outerMusicSource.pitch, normalMusicSpeed, musicTransitionDuration));
            yield break;
        }
        outerMusicSource.clip = outerAreaMusic;
        outerMusicSource.volume = 0f;
        outerMusicSource.pitch = normalMusicSpeed;
        outerMusicSource.Play();
        yield return StartCoroutine(FadeVolume(outerMusicSource, 0f, musicVolume, musicTransitionDuration));
    }
    IEnumerator StartInnerMusicLayer()
    {
        if (!outerMusicSource.isPlaying && outerAreaMusic != null)
        {
            outerMusicSource.clip = outerAreaMusic;
            outerMusicSource.volume = musicVolume;
            outerMusicSource.pitch = normalMusicSpeed;
            outerMusicSource.Play();
        }
        StartCoroutine(FadePitch(outerMusicSource, outerMusicSource.pitch, fastMusicSpeed, musicTransitionDuration));
        if (innerAreaMusic != null && !innerMusicSource.isPlaying)
        {
            innerMusicSource.clip = innerAreaMusic;
            innerMusicSource.volume = 0f;
            innerMusicSource.pitch = fastMusicSpeed;
            innerMusicSource.Play();
            yield return StartCoroutine(FadeVolume(innerMusicSource, 0f, musicVolume * innerMusicVolumeMultiplier, musicTransitionDuration));
        }
        else if (innerMusicSource.isPlaying)
        {
            yield return StartCoroutine(FadePitch(innerMusicSource, innerMusicSource.pitch, fastMusicSpeed, musicTransitionDuration));
        }
    }
    IEnumerator FadeOutAllMusic()
    {
        if (outerMusicSource.isPlaying)
        {
            StartCoroutine(FadeVolume(outerMusicSource, outerMusicSource.volume, 0f, musicTransitionDuration));
        }
        if (innerMusicSource.isPlaying)
        {
            StartCoroutine(FadeVolume(innerMusicSource, innerMusicSource.volume, 0f, musicTransitionDuration));
        }
        yield return new WaitForSeconds(musicTransitionDuration);
        outerMusicSource.Stop();
        innerMusicSource.Stop();
        outerMusicSource.pitch = normalMusicSpeed;
        innerMusicSource.pitch = normalMusicSpeed;
    }
    IEnumerator FadeVolume(AudioSource source, float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            source.volume = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        source.volume = to;
    }
    IEnumerator FadePitch(AudioSource source, float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            source.pitch = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        source.pitch = to;
    }
    #endregion
    #region Ses Sistemi
    void PlayRandomSound(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0) return;
        int index = Random.Range(0, clips.Length);
        sfxSource.PlayOneShot(clips[index]);
    }
    #endregion
    #region Animator
    void UpdateAnimator()
    {
        if (zombieAnimator == null) return;
        float speed = 0f;
        if (navAgent != null && !navAgent.isStopped)
        {
            speed = navAgent.velocity.magnitude / runSpeed;
        }
        zombieAnimator.SetFloat(SpeedHash, speed);
        zombieAnimator.SetBool(IsAlertHash, currentState == ZombieState.Alert);
        zombieAnimator.SetBool(IsChasingHash, currentState == ZombieState.Chasing);
    }
    #endregion
    #region Public Metodlar
    public ZombieState CurrentState => currentState;
    public float DistanceToPlayer => distanceToPlayer;
    public void SetDetectionRadii(float outer, float inner)
    {
        outerDetectionRadius = outer;
        innerDetectionRadius = inner;
    }
    #endregion
    #region Gizmos (Editor'da görselleştirme)
#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, outerDetectionRadius);
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, innerDetectionRadius);
        Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
#endif
    #endregion
}
