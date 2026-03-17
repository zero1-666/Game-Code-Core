using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerInteraction : MonoBehaviour
{
    [Header("交互设置")]
    public float interactRange = 1.5f;
    public float detectRadius = 0.8f;
    public Transform holdPoint;
    public LayerMask itemLayer;

    [Header("牺牲机制")]
    public float sacrificeHoldTime = 1.0f;
    public GameObject bridgePrefab;

    [Header("重生点系统")]
    public Vector3 spawnPoint;

    [Header("实时状态")]
    [SerializeField] private GameObject grabbedItem;
    [SerializeField] public int hp = 3;
    public float currentHoldTime = 0;
    public bool isSacrificing = false;

    // 虚空区域检测（使用Trigger）
    private bool isInVoidZone = false;
    private Collider currentVoidCollider = null;

    // 死亡状态标记
    private bool isDead = false;

    [Header("投掷蓄力系统")]
    public float minThrowForce = 2.0f;
    public float maxThrowForce = 15.0f;
    public float maxChargeTime = 2.0f;
    public LineRenderer aimLine;

    public float currentThrowCharge = 0f;
    public bool isChargingThrow = false;

    void Start()
    {
        // 如果启用了检查点系统且 spawnPoint 未设置，自动寻找起始检查点
        if (spawnPoint == Vector3.zero)
        {
            GameObject startPoint = GameObject.Find("Start_SpawnPoint")
                ?? GameObject.Find("Checkpoint_01")
                ?? GameObject.Find("Checkpoint_Start");

            if (startPoint != null)
            {
                spawnPoint = startPoint.transform.position;
                Debug.Log($"初始重生点设置自：{startPoint.name}");
            }
            else
            {
                // 如果没有找到检查点，使用当前位置
                spawnPoint = transform.position;
            }
        }

        isDead = false;
        isInVoidZone = false;
        currentVoidCollider = null;

        if (holdPoint == null) Debug.LogError("请设置 Hold Point");

        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateHPDisplay(hp);
        }
    }

    void Update()
    {
        // 如果已死亡，禁止所有操作
        if (isDead) return;

        // 如果GameOver面板已激活，也禁止操作（双重保险）
        if (UIManager.Instance != null && UIManager.Instance.gameOverPanel.activeSelf) return;

        // 🔴 关键修复：如果记录的 Void 已被销毁（如被箱子填补），但状态未重置，自动清理
        if (currentVoidCollider == null && isInVoidZone)
        {
            isInVoidZone = false;
            ResetSacrificeProgress();
        }

        HandleThrowInput();
        HandleSacrificeInput();
    }

    // --- 检查点系统核心方法 ---
    /// <summary>
    /// 更新重生点位置（供 Checkpoint.cs 调用）
    /// </summary>
    public void UpdateSpawnPoint(Vector3 newPosition)
    {
        spawnPoint = newPosition;
        Debug.Log($"<color=green>重生点已更新至：{newPosition}</color>");
    }

    // --- Trigger 检测（Void 区域）---
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Void"))
        {
            isInVoidZone = true;
            currentVoidCollider = other;

            // 可选：提示玩家可以牺牲
            if (UIManager.Instance != null && hp > 1)
            {
                UIManager.Instance.ShowHint("按住空格牺牲生命值建造桥梁");
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Void"))
        {
            isInVoidZone = false;
            currentVoidCollider = null;
            ResetSacrificeProgress();
        }
    }

    // --- 生命值逻辑 ---
    public void TakeDamage(int amount)
    {
        if (isDead) return;

        hp -= amount;

        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateHPDisplay(hp);
        }

        if (hp <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log("<color=red>玩家已死亡</color>");

        // 禁用角色控制器
        CharacterController cc = GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        // 如果有刚体，也禁用
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        // 触发延迟显示的GameOver
        if (UIManager.Instance != null)
        {
            UIManager.Instance.TriggerGameOver();
        }
    }

    // --- 牺牲逻辑（已修复）---
    void HandleSacrificeInput()
    {
        // 🔴 关键修复：如果 Void 被销毁了（currentVoidCollider为null），强制退出牺牲状态
        if (currentVoidCollider == null && isInVoidZone)
        {
            isInVoidZone = false;
            ResetSacrificeProgress();
            return; // 直接返回，不执行后续逻辑
        }

        if (isInVoidZone && grabbedItem == null)
        {
            if (Input.GetKey(KeyCode.Space))
            {
                // HP为1时的临终警告
                if (hp == 1 && currentHoldTime == 0)
                {
                    if (UIManager.Instance != null)
                    {
                        UIManager.Instance.ShowHint("警告：这将消耗你最后的生命！");
                    }
                }

                isSacrificing = true;
                currentHoldTime += Time.deltaTime;
                if (currentHoldTime >= sacrificeHoldTime)
                {
                    ExecuteSacrifice();
                }
            }
            else
            {
                ResetSacrificeProgress();
            }
        }
        else
        {
            ResetSacrificeProgress();
        }
    }

    void ExecuteSacrifice()
    {
        TakeDamage(1);

        // 使用记录的 currentVoidCollider 生成桥梁（精确匹配Void位置）
        if (currentVoidCollider != null)
        {
            if (bridgePrefab != null)
                Instantiate(bridgePrefab, currentVoidCollider.transform.position, currentVoidCollider.transform.rotation);

            // 销毁Void物体
            Destroy(currentVoidCollider.gameObject);

            // 🔴 重要：手动重置状态（因为Destroy不会触发OnTriggerExit）
            isInVoidZone = false;
            currentVoidCollider = null;
        }
        else
        {
            // 备用方案：如果Trigger没检测到但玩家按了（保险起见）
            Collider[] voids = Physics.OverlapSphere(transform.position, 2.0f);
            foreach (var v in voids)
            {
                if (v.CompareTag("Void"))
                {
                    if (bridgePrefab != null)
                        Instantiate(bridgePrefab, v.transform.position, v.transform.rotation);
                    Destroy(v.gameObject);
                    break;
                }
            }
        }

        // 只有活着的时候才传送
        if (hp > 0)
        {
            CharacterController cc = GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            transform.position = spawnPoint;
            Physics.SyncTransforms();
            if (cc != null) cc.enabled = true;
        }
        else
        {
            // HP=1时牺牲导致死亡：桥梁已生成，但不传送
            Debug.Log("玩家以最后的生命为代价建造了桥梁");
        }

        currentHoldTime = 0;
        isSacrificing = false;
    }

    // --- 拾取与投掷系统逻辑 ---
    void TryPickUp()
    {
        Vector3 detectCenter = transform.position + transform.forward * interactRange;
        Collider[] hitColliders = Physics.OverlapSphere(detectCenter, detectRadius, itemLayer);

        foreach (var hit in hitColliders)
        {
            if (hit.CompareTag("Crate"))
            {
                grabbedItem = hit.gameObject;
                Rigidbody rb = grabbedItem.GetComponent<Rigidbody>();
                if (rb != null) { rb.isKinematic = true; rb.useGravity = false; }
                if (grabbedItem.TryGetComponent<Collider>(out Collider col)) col.enabled = false;

                grabbedItem.transform.SetParent(holdPoint);
                grabbedItem.transform.localPosition = Vector3.zero;
                grabbedItem.transform.localRotation = Quaternion.identity;
                break;
            }
        }
    }

    private void HandleThrowInput()
    {
        if (grabbedItem == null)
        {
            if (Input.GetKeyDown(KeyCode.F)) TryPickUp();
            return;
        }

        if (Input.GetKeyDown(KeyCode.F))
        {
            isChargingThrow = true;
            currentThrowCharge = 0f;
            if (aimLine != null) aimLine.enabled = true;
        }

        if (isChargingThrow && Input.GetKey(KeyCode.F))
        {
            currentThrowCharge += Time.deltaTime;
            currentThrowCharge = Mathf.Clamp(currentThrowCharge, 0, maxChargeTime);
            if (aimLine != null) UpdateAimLine();
        }

        if (isChargingThrow && Input.GetKeyUp(KeyCode.F))
            ExecuteThrow();
    }

    private void ExecuteThrow()
    {
        if (grabbedItem != null)
        {
            Rigidbody rb = grabbedItem.GetComponent<Rigidbody>();
            Collider col = grabbedItem.GetComponent<Collider>();
            grabbedItem.transform.SetParent(null);
            if (col != null) { col.isTrigger = false; col.enabled = true; }
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.useGravity = true;
                rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
                float finalForce = Mathf.Lerp(minThrowForce, maxThrowForce, currentThrowCharge / maxChargeTime);
                rb.AddForce((transform.forward + transform.up * 0.5f).normalized * finalForce, ForceMode.Impulse);
            }
            grabbedItem = null;
        }

        // 🔴 可选：扔箱子后，如果之前在Void区域附近，强制刷新一次状态
        // 这防止箱子扔进Void后，由于物理碰撞延迟导致状态残留
        if (isInVoidZone && currentVoidCollider != null)
        {
            // 延迟一帧检查，确保物理碰撞先发生（箱子先进入Void）
            StartCoroutine(DelayedVoidCheck());
        }

        isChargingThrow = false;
        if (aimLine != null) aimLine.enabled = false;
    }

    // 辅助协程：延迟检查Void是否被箱子填补
    System.Collections.IEnumerator DelayedVoidCheck()
    {
        yield return null; // 等待一帧
        if (currentVoidCollider == null && isInVoidZone)
        {
            isInVoidZone = false;
            ResetSacrificeProgress();
        }
    }

    private void UpdateAimLine()
    {
        int resolution = 30;
        aimLine.positionCount = resolution;
        float finalForce = Mathf.Lerp(minThrowForce, maxThrowForce, currentThrowCharge / maxChargeTime);
        Vector3 velocity = (transform.forward + Vector3.up * 0.5f).normalized * finalForce;
        Vector3 startPosition = holdPoint.position;

        for (int i = 0; i < resolution; i++)
        {
            float t = i * 0.1f;
            Vector3 point = startPosition + velocity * t + 0.5f * Physics.gravity * t * t;
            aimLine.SetPosition(i, point);
        }
    }

    void ResetSacrificeProgress()
    {
        currentHoldTime = 0;
        isSacrificing = false;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position + transform.forward * interactRange, detectRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 1.5f);

        // 新增：在Scene视图中显示当前重生点位置
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(spawnPoint, 0.5f);
        Gizmos.DrawLine(transform.position, spawnPoint);
    }
}