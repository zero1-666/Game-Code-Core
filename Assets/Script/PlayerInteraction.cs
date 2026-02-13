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
    [SerializeField] private int hp = 3;
    private float currentHoldTime = 0;
    private bool isSacrificing = false;

    void Start()
    {
        if (spawnPoint == null) Debug.LogError("请分配 Spawn Point (重生点)！");
        if (holdPoint == null) Debug.LogError("请分配 Hold Point (吸附点)！");
    }

    void Update()
    {
        // --- 1. 搬运逻辑 (F键) ---
        if (Input.GetKeyDown(KeyCode.F))
        {
            if (grabbedItem == null)
            {
                TryPickUp();
            }
            else
            {
                DropItem();
            }
        }

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

    void DropItem()
    {
        if (grabbedItem != null)
        {
            // 恢复碰撞
            if (grabbedItem.TryGetComponent<Collider>(out Collider col))
            {
                col.enabled = true;
            }

            // 恢复物理模拟
            Rigidbody rb = grabbedItem.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.useGravity = true;
            }

            grabbedItem.transform.SetParent(null);
            grabbedItem = null;
            Debug.Log("<color=yellow>【系统】物体已放下</color>");
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
}