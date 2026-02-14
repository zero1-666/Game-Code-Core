using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerInteraction : MonoBehaviour
{
    [Header("搬运设置")]
    public float interactRange = 1.5f;     // 探测点距离玩家正前方的距离
    public float detectRadius = 0.8f;      // 判定球体的半径
    public Transform holdPoint;            // 物品吸附点
    public LayerMask itemLayer;           // 建议在 Inspector 中选为 "Crate" 所在的层

    [Header("牺牲设置")]
    public float sacrificeHoldTime = 1.0f;
    public GameObject bridgePrefab;
    public Transform spawnPoint;

    [Header("实时状态")]
    [SerializeField] private GameObject grabbedItem;
    [SerializeField] public int hp = 3;
    public float currentHoldTime = 0;
    public bool isSacrificing = false;

    //-----------新增------------
    [Header("蓄力投掷设置")]
    public float minThrowForce = 2.0f;       // 基础放下力度
    public float maxThrowForce = 15.0f;      // 最大投掷力度
    public float maxChargeTime = 2.0f;       // 蓄力满额所需时间
    public LineRenderer aimLine;             // 抛物线预览组件 (需手动拖入)

    // --- 新增变量 ---
    public float currentThrowCharge = 0f;   // 当前蓄力计时
    public bool isChargingThrow = false;    // 是否正在蓄力
    //------------------------

    void Start()
    {
        if (spawnPoint == null) Debug.LogError("请分配 Spawn Point (重生点)！");
        if (holdPoint == null) Debug.LogError("请分配 Hold Point (吸附点)！");
    }

    void Update()
    {
        // --- 1. 搬运逻辑 (F键) ---
        // 调用独立的处理函数，保持 Update 清洁
        HandleThrowInput();

            // --- 2. 牺牲逻辑 (空格键) ---
            HandleSacrificeInput();
    }

    // --- 搬运功能核心：正前方范围检测版 ---
    void TryPickUp()
    {
        // 计算检测中心点：位于玩家正前方
        Vector3 detectCenter = transform.position + transform.forward * interactRange;

        // 使用物理重叠球检测范围内的所有物体
        Collider[] hitColliders = Physics.OverlapSphere(detectCenter, detectRadius);

        foreach (var hit in hitColliders)
        {
            // 检查是否带有 Crate 标签
            if (hit.CompareTag("Crate"))
            {
                grabbedItem = hit.gameObject;

                Rigidbody rb = grabbedItem.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.isKinematic = true;
                    rb.useGravity = false;
                }

                // 禁用碰撞，防止搬运时产生物理冲突导致玩家“飞天”
                if (grabbedItem.TryGetComponent<Collider>(out Collider col))
                {
                    col.enabled = false;
                }

                // 父子级关联
                grabbedItem.transform.SetParent(holdPoint);
                grabbedItem.transform.localPosition = Vector3.zero;
                grabbedItem.transform.localRotation = Quaternion.identity;

                Debug.Log("<color=cyan>【系统】已捡起物体：" + grabbedItem.name + "</color>");
                break; // 确保一次只捡一个
            }
        }
    }


    // --- 牺牲功能核心 ---
    void HandleSacrificeInput()
    {
        // 只有在 HP 足够、没拿东西且靠近坑位时才能触发
        if (hp >= 2 && grabbedItem == null && IsNearVoid())
        {
            if (Input.GetKey(KeyCode.Space))
            {
                isSacrificing = true;
                currentHoldTime += Time.deltaTime;
                Debug.Log("牺牲中... " + (currentHoldTime / sacrificeHoldTime * 100f).ToString("F0") + "%");

                if (currentHoldTime >= sacrificeHoldTime)
                {
                    ExecuteSacrifice();
                }
            }
            else
            {
                currentHoldTime = 0;
                isSacrificing = false;
            }
        }
    }

    bool IsNearVoid()
    {
        // 在角色周围 1.5 米搜索是否有 Void 标签的物体
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, 1.5f);
        foreach (var hit in hitColliders)
        {
            if (hit.CompareTag("Void")) return true;
        }
        return false;
    }

    void ExecuteSacrifice()
    {
        hp -= 1;

        Collider[] voids = Physics.OverlapSphere(transform.position, 2.0f);
        foreach (var v in voids)
        {
            if (v.CompareTag("Void"))
            {
                if (bridgePrefab != null)
                {
                    Instantiate(bridgePrefab, v.transform.position, v.transform.rotation);
                }
                Destroy(v.gameObject);
                break;
            }
        }

        if (spawnPoint != null) transform.position = spawnPoint.position;

        currentHoldTime = 0;
        isSacrificing = false;
        Debug.Log("<color=red>【核心】牺牲完成！</color> 剩余存在值: " + hp);
    }

    // 负责人在 Scene 窗口可视化调试范围
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 detectCenter = transform.position + transform.forward * interactRange;
        Gizmos.DrawWireSphere(detectCenter, detectRadius);

        // 画出牺牲判定范围
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 1.5f);
    }

    //------------------新增---------------
    // 处理交互输入的逻辑划分
    // 逻辑划分：严格区分拾取与投掷蓄力的两个阶段
    private void HandleThrowInput()
    {
        // 情况 A: 手上没东西 -> 只能捡起
        if (grabbedItem == null)
        {
            if (Input.GetKeyDown(KeyCode.F))
            {
                TryPickUp();
            }
            return; // 捡起后直接结束本帧，防止同一帧触发下面的逻辑
        }

        // 情况 B: 手上有东西 -> 等待玩家"再次"按下 F
        // Input.GetKeyDown 只有在玩家松开 F 并再次按下时才会触发
        if (Input.GetKeyDown(KeyCode.F))
        {
            isChargingThrow = true; // 开始蓄力标记
            currentThrowCharge = 0f;
            if (aimLine != null) aimLine.enabled = true; // 开启抛物线
        }

        // 情况 C: 正在蓄力中 (按住 F 不放)
        if (isChargingThrow && Input.GetKey(KeyCode.F))
        {
            currentThrowCharge += Time.deltaTime;
            currentThrowCharge = Mathf.Clamp(currentThrowCharge, 0, maxChargeTime);

            // 实时更新抛物线
            if (aimLine != null) UpdateAimLine();
        }

        // 情况 D: 松开 F 键 -> 执行投掷
        if (isChargingThrow && Input.GetKeyUp(KeyCode.F))
        {
            ExecuteThrow();
        }
    }

    // 执行投掷逻辑：应用物理冲量
    private void ExecuteThrow()
    {
        if (grabbedItem != null)
        {
            Rigidbody rb = grabbedItem.GetComponent<Rigidbody>();
            Collider col = grabbedItem.GetComponent<Collider>();

            // 1. 解除父子关系
            grabbedItem.transform.SetParent(null);

            // 2. 【关键修复】强制恢复碰撞体
            // 如果 IsTrigger 是 true，箱子就是幽灵，会穿地。必须设为 false！
            if (col != null)
            {
                col.isTrigger = false;
                col.enabled = true;
            }

            // 3. 【关键修复】恢复刚体物理
            if (rb != null)
            {
                rb.isKinematic = false; // 让物理引擎接管
                rb.useGravity = true;   // 开启重力
                rb.collisionDetectionMode = CollisionDetectionMode.Continuous; // 防止速度太快穿地

                // 4. 施加投掷力
                float chargePercent = currentThrowCharge / maxChargeTime;
                float finalForce = Mathf.Lerp(minThrowForce, maxThrowForce, chargePercent);
                Vector3 throwDir = (transform.forward + transform.up * 0.5f).normalized;

                rb.AddForce(throwDir * finalForce, ForceMode.Impulse);
            }

            grabbedItem = null; // 清空引用
        }

        // 状态重置
        isChargingThrow = false;
        currentThrowCharge = 0;
        if (aimLine != null) aimLine.enabled = false;
    }

    // 逻辑划分：物理预测逻辑，用于实时绘制投掷路径
    private void UpdateAimLine()
    {
        int resolution = 30; // 抛物线的精细度（点数）
        aimLine.positionCount = resolution;

        // 计算初始速度向量（模拟 ExecuteThrow 里的逻辑）
        float chargePercent = currentThrowCharge / maxChargeTime;
        float finalForce = Mathf.Lerp(minThrowForce, maxThrowForce, chargePercent);
        // ForceMode.Impulse 下，速度变化量 = 力 / 质量 (假设质量为1)
        Vector3 velocity = (transform.forward + Vector3.up * 0.5f).normalized * finalForce;

        Vector3 startPosition = holdPoint.position;
        float timeStep = 0.1f; // 每个点之间的时间间隔

        for (int i = 0; i < resolution; i++)
        {
            float t = i * timeStep;
            // 物理公式：位移 = 初始速度 * 时间 + 0.5 * 重力 * 时间的平方
            Vector3 point = startPosition + velocity * t + 0.5f * Physics.gravity * t * t;
            aimLine.SetPosition(i, point);
        }
    }
    //-------------------------------------

}