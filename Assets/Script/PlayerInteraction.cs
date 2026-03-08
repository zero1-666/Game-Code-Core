/*
using UnityEngine;
using UnityEngine.UI; // 必须引用 UI 命名空间

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

    // 20260303新增

    [Header("UI 提示设置")]
    public Text hintText;               // 在 Inspector 中拖入你的 UI Text 物件
    public float hintDuration = 2.0f;   // 提示显示持续时间
    private float hintTimer = 0f;
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
    // --- 牺牲逻辑核心修改版 ---
    void HandleSacrificeInput()
    {
        // 步骤 A: 首先判断是否靠近坑位
        bool nearVoid = IsNearVoid();

        if (nearVoid && grabbedItem == null)
        {
            // 步骤 B: 如果靠近坑位但 HP 只有 1 (根据你的要求，HP 为 1 时无法牺牲)
            if (hp <= 1)
            {
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    ShowHint("生命值不足，无法牺牲");
                }
            }
            // 步骤 C: 正常牺牲逻辑 (只有 HP > 1 才能触发)
            else
            {
                if (Input.GetKey(KeyCode.Space))
                {
                    isSacrificing = true;
                    currentHoldTime += Time.deltaTime;
                    // ... 进度日志 ...

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
        }
        else
        {
            ResetSacrificeProgress();
        }

        // 处理提示文字的消失计时
        UpdateHintTimer();
    }

    // --- 新增add辅助函数 ---

    // 显示提示文字
    void ShowHint(string message)
    {
        if (hintText != null)
        {
            hintText.text = message;
            hintText.gameObject.SetActive(true);
            hintTimer = hintDuration;
        }
        Debug.Log("<color=yellow>【系统提示】</color> " + message);
    }

    // 更新提示计时器
    void UpdateHintTimer()
    {
        if (hintTimer > 0)
        {
            hintTimer -= Time.deltaTime;
            if (hintTimer <= 0 && hintText != null)
            {
                hintText.gameObject.SetActive(false);
            }
        }
    }

    void ResetSacrificeProgress()
    {
        currentHoldTime = 0;
        isSacrificing = false;
    }

    //-----新增：
    // 这个函数就是解决“不存在名称IsNearVoid”报错的关键
    bool IsNearVoid()
    {
        // 在角色位置周围 1.5 米半径内搜索碰撞体
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, 1.5f);

        foreach (var hit in hitColliders)
        {
            // 检查碰撞体是否有 "Void" 标签
            if (hit.CompareTag("Void"))
            {
                return true; // 找到了坑位，返回“是”
            }
        }
        return false; // 没找到，返回“否”
    }
    //ending新增，与下面的void ExecuteSacrifice()函数是不冲突！


    //----original funtion
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

*/

//以上是之前的代码
//以下是现在 的代码


using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement; //

[RequireComponent(typeof(CharacterController))]
public class PlayerInteraction : MonoBehaviour
{
    [Header("搬运设置")]
    public float interactRange = 1.5f;
    public float detectRadius = 0.8f;
    public Transform holdPoint;
    public LayerMask itemLayer;

    [Header("牺牲设置")]
    public float sacrificeHoldTime = 1.0f;
    public GameObject bridgePrefab;

    [Header("检查点系统")]
    // 将类型改为 Vector3 以适配 Checkpoint.cs
    public Vector3 spawnPoint;

    [Header("实时状态")]
    [SerializeField] private GameObject grabbedItem;
    [SerializeField] public int hp = 3;
    public float currentHoldTime = 0;
    public bool isSacrificing = false;

    [Header("蓄力投掷设置")]
    public float minThrowForce = 2.0f;
    public float maxThrowForce = 15.0f;
    public float maxChargeTime = 2.0f;
    public LineRenderer aimLine;

    public float currentThrowCharge = 0f;
    public bool isChargingThrow = false;

    [Header("UI 提示与结束设置")]
    public Text hintText;
    public float hintDuration = 2.0f;
    private float hintTimer = 0f;
    // --- 新增：死亡界面引用 ---
    public GameObject gameOverPanel;

    void Start()
    {
        // 初始化重生点为当前起始位置
        spawnPoint = transform.position;

        if (holdPoint == null) Debug.LogError("请分配 Hold Point！");
        if (gameOverPanel != null) gameOverPanel.SetActive(false); // 初始隐藏死亡界面
    }

    void Update()
    {
        // 如果死亡界面显示中，禁止操作
        if (gameOverPanel != null && gameOverPanel.activeSelf) return;

        HandleThrowInput();
        HandleSacrificeInput();
    }

    // --- 整合功能：生命值管理与死亡显示 ---
    public void TakeDamage(int amount)
    {
        hp -= amount; //
        if (hp <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Debug.Log("<color=red>玩家死亡！</color>");
        // 修改：不再直接 LoadScene，而是弹出界面
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            // 如果有鼠标控制，可以在这里解锁鼠标
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            // 如果负责人忘记分配面板，则作为兜底重置场景
            SceneManager.LoadScene(SceneManager.GetActiveScene().name); //
        }
    }

    // --- 给 UI 按钮调用的重试函数 ---
    public void RetryGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name); //
    }

    // --- 搬运功能核心 ---
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

    // --- 牺牲逻辑核心 ---
    void HandleSacrificeInput()
    {
        // 1. 先判定是否靠近坑位
        bool nearVoid = IsNearVoid();

        // 2. 只有靠近坑位且没拿东西，才处理牺牲逻辑
        if (nearVoid && grabbedItem == null)
        {
            // --- 关键修复点：当 HP 等于 1 时 ---
            if (hp <= 1)
            {
                // 使用 GetKeyDown 确保只在按下那一瞬间触发提示，防止刷屏
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    Debug.Log("生命值不足，无法牺牲");
                    //ShowHint("生命值不足，无法牺牲");
                }

                // 只要 HP 不足，就必须重置进度，防止进度条一直涨
                ResetSacrificeProgress();
            }
            else
            {
                // 正常牺牲逻辑 (HP > 1)
                if (Input.GetKey(KeyCode.Space))
                {
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
        }
        else
        {
            ResetSacrificeProgress();
        }

        UpdateHintTimer();
    }
    void ExecuteSacrifice()
    {
        TakeDamage(1); // 扣血

        if (hp > 0)
        {
            // 1. 寻找附近的坑位并生成桥梁
            Collider[] voids = Physics.OverlapSphere(transform.position, 2.0f);
            foreach (var v in voids)
            {
                if (v.CompareTag("Void"))
                {
                    if (bridgePrefab != null) Instantiate(bridgePrefab, v.transform.position, v.transform.rotation);
                    Destroy(v.gameObject);
                    break;
                }
            }

            // --- 【核心修复：解决不回重生点的问题】 ---
            // 获取控制器组件
            CharacterController cc = GetComponent<CharacterController>();

            if (cc != null) cc.enabled = false; // 第一步：必须先关掉控制器，否则它会把玩家锁在原地

            transform.position = spawnPoint;    // 第二步：执行瞬间移动
            Physics.SyncTransforms();           // 第三步：强制物理同步（保险开关）

            if (cc != null) cc.enabled = true;  // 第四步：重新开启控制器，恢复正常行走
            // ------------------------------------------
            
            Debug.Log("<color=green>【系统】牺牲成功，已回到检查点：</color>" + spawnPoint);
        }

        // 状态重置
        currentHoldTime = 0;
        isSacrificing = false;
    }

    // --- 辅助功能 (判定, UI, 投掷等) ---
    bool IsNearVoid()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, 1.5f);
        foreach (var hit in hitColliders) { if (hit.CompareTag("Void")) return true; }
        return false;
    }

    void ShowHint(string message)
    {
        if (hintText != null) { hintText.text = message; hintText.gameObject.SetActive(true); hintTimer = hintDuration; }
    }

    void UpdateHintTimer()
    {
        if (hintTimer > 0) { hintTimer -= Time.deltaTime; if (hintTimer <= 0 && hintText != null) hintText.gameObject.SetActive(false); }
    }

    void ResetSacrificeProgress() { currentHoldTime = 0; isSacrificing = false; }

    private void HandleThrowInput()
    {
        if (grabbedItem == null) { if (Input.GetKeyDown(KeyCode.F)) TryPickUp(); return; }
        if (Input.GetKeyDown(KeyCode.F)) { isChargingThrow = true; currentThrowCharge = 0f; if (aimLine != null) aimLine.enabled = true; }
        if (isChargingThrow && Input.GetKey(KeyCode.F)) { currentThrowCharge += Time.deltaTime; currentThrowCharge = Mathf.Clamp(currentThrowCharge, 0, maxChargeTime); if (aimLine != null) UpdateAimLine(); }
        if (isChargingThrow && Input.GetKeyUp(KeyCode.F)) ExecuteThrow();
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
                rb.isKinematic = false; rb.useGravity = true;
                rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
                float finalForce = Mathf.Lerp(minThrowForce, maxThrowForce, currentThrowCharge / maxChargeTime);
                rb.AddForce((transform.forward + transform.up * 0.5f).normalized * finalForce, ForceMode.Impulse);
            }
            grabbedItem = null;
        }
        isChargingThrow = false; if (aimLine != null) aimLine.enabled = false;
    }

    private void UpdateAimLine()
    {
        int resolution = 30; aimLine.positionCount = resolution;
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

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position + transform.forward * interactRange, detectRadius);
        Gizmos.color = Color.red; Gizmos.DrawWireSphere(transform.position, 1.5f);
    }
}