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

    // 新增：死亡状态标记
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
        spawnPoint = transform.position;
        isDead = false;

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

        HandleThrowInput();
        HandleSacrificeInput();
    }

    // --- 生命值逻辑 ---
    public void TakeDamage(int amount)
    {
        // 死亡后不再扣血
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
        if (isDead) return; // 防止重复调用
        isDead = true;

        Debug.Log("<color=red>玩家已死亡</color>");

        // 禁用角色控制器（防止继续移动/坠落）
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

    // --- 牺牲逻辑（修改后）---
    void HandleSacrificeInput()
    {
        bool nearVoid = IsNearVoid();

        if (nearVoid && grabbedItem == null)
        {
            if (Input.GetKey(KeyCode.Space))
            {
                // HP为1时的特殊处理：允许牺牲，但显示临终警告
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
        TakeDamage(1); // 扣血并自动更新 UI 显示

        // 只有活着的时候才执行传送和造桥（如果扣血导致死亡，下面逻辑不会执行）
        if (hp > 0)
        {
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

            // 瞬移回重生点
            CharacterController cc = GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            transform.position = spawnPoint;
            Physics.SyncTransforms();
            if (cc != null) cc.enabled = true;
        }
        else
        {
            // HP=1时牺牲导致死亡：生成桥梁但不传送（因为已经死了）
            // 但仍然生成桥梁（作为玩家最后的贡献）
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
            Debug.Log("玩家以最后的生命为代价建造了桥梁");
        }

        currentHoldTime = 0;
        isSacrificing = false;
    }

    // --- 拾取与投掷系统逻辑（保持不变）---
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
        isChargingThrow = false;
        if (aimLine != null) aimLine.enabled = false;
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

    bool IsNearVoid()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, 1.5f);
        foreach (var hit in hitColliders)
        {
            if (hit.CompareTag("Void")) return true;
        }
        return false;
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
    }
}