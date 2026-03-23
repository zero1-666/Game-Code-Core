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
    public Vector3 spawnPoint; // 保留字段，但牺牲后不再使用（死亡时仍用）

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
        // 检查点初始化（保留，死亡时仍需要）
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
        if (isDead) return;
        if (UIManager.Instance != null && UIManager.Instance.gameOverPanel.activeSelf) return;

        // 防御性检查：Void 被销毁后重置状态
        if (currentVoidCollider == null && isInVoidZone)
        {
            isInVoidZone = false;
            ResetSacrificeProgress();
        }

        HandleThrowInput();
        HandleSacrificeInput();
    }

    // 检查点系统（保留，死亡时仍需要）
    public void UpdateSpawnPoint(Vector3 newPosition)
    {
        spawnPoint = newPosition;
        Debug.Log($"<color=green>重生点已更新至：{newPosition}</color>");
    }

    // Trigger 检测（Void 区域）
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Void"))
        {
            isInVoidZone = true;
            currentVoidCollider = other;

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

    // 生命值逻辑
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

        CharacterController cc = GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        if (UIManager.Instance != null)
        {
            UIManager.Instance.TriggerGameOver();
        }
    }

    // 牺牲逻辑
    void HandleSacrificeInput()
    {
        // 防御性检查：Void 被销毁后强制退出
        if (currentVoidCollider == null && isInVoidZone)
        {
            isInVoidZone = false;
            ResetSacrificeProgress();
            return;
        }

        if (isInVoidZone && grabbedItem == null)
        {
            if (Input.GetKey(KeyCode.Space))
            {
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

    // 🔴🔴🔴 核心修改：ExecuteSacrifice 🔴🔴🔴
    void ExecuteSacrifice()
    {
        TakeDamage(1);

        // 生成桥梁（在原地生成，玩家不传送）
        if (currentVoidCollider != null)
        {
            if (bridgePrefab != null)
            {
                // 可选：稍微抬高一点生成，避免与玩家碰撞穿模
                Vector3 bridgePos = currentVoidCollider.transform.position;
                // 如果需要可以取消下面这行的注释，让桥在玩家脚下生成
                // bridgePos.y = transform.position.y - 0.1f; 

                Instantiate(bridgePrefab, bridgePos, currentVoidCollider.transform.rotation);
            }

            Destroy(currentVoidCollider.gameObject);
            isInVoidZone = false;
            currentVoidCollider = null;
        }
        else
        {
            // 备用方案
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

        // 🔴🔴🔴 删除的代码：传送回重生点 🔴🔴🔴
        // 以下代码已被删除：
        // if (hp > 0)
        // {
        //     CharacterController cc = GetComponent<CharacterController>();
        //     if (cc != null) cc.enabled = false;
        //     transform.position = spawnPoint;  // ❌ 不再传送！
        //     Physics.SyncTransforms();
        //     if (cc != null) cc.enabled = true;
        // }
        // else
        // {
        //     Debug.Log("玩家以最后的生命为代价建造了桥梁");
        // }

        // 保留：如果死亡（hp<=0），TakeDamage 中已经调用了 Die()
        // 但这里可以加个提示



        if (hp <= 0)
        {
            Debug.Log("玩家以最后的生命为代价建造了桥梁，留在原地");
        }

        currentHoldTime = 0;
        isSacrificing = false;
    }

    // 拾取与投掷系统逻辑（保持不变）
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

        if (isInVoidZone && currentVoidCollider != null)
        {
            StartCoroutine(DelayedVoidCheck());
        }

        isChargingThrow = false;
        if (aimLine != null) aimLine.enabled = false;
    }

    System.Collections.IEnumerator DelayedVoidCheck()
    {
        yield return null;
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

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(spawnPoint, 0.5f);
        Gizmos.DrawLine(transform.position, spawnPoint);
    }
}