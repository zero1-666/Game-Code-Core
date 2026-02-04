using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerInteraction : MonoBehaviour
{
    [Header("搬运设置")]
    public float interactRange = 5.0f;     // 探测距离
    public float detectRadius = 0.5f;     // 判定半径
    public Transform holdPoint;           // 物品吸附点
    public float carrySpeedMultiplier = 0.8f;

    [Header("牺牲设置")]
    public float sacrificeHoldTime = 1.0f;
    public GameObject bridgePrefab;
    public Transform spawnPoint;

    [Header("实时状态")]
    [SerializeField] private GameObject grabbedItem;
    [SerializeField] private int hp = 3;
    private float currentHoldTime = 0;
    private bool isSacrificing = false;
    private Camera mainCam;

    void Start()
    {
        mainCam = Camera.main;
        if (spawnPoint == null) Debug.LogError("请分配 Spawn Point (重生点)！");
        if (holdPoint == null) Debug.LogError("请分配 Hold Point (吸附点)！");
    }

    void Update()
    {
        // --- 1. 搬运逻辑 (F键) ---
        if (Input.GetKeyDown(KeyCode.F))
        {
            if (grabbedItem == null) TryPickUp();
            else DropItem();
        }

        // --- 2. 牺牲逻辑 (空格键) ---
        HandleSacrificeInput();

        // --- 3. 调试射线 (Scene窗口可见) ---
        if (mainCam != null)
        {
            Vector3 rayStart = mainCam.transform.position + mainCam.transform.forward * 2.5f;
            Debug.DrawRay(rayStart, mainCam.transform.forward * interactRange, Color.green);
        }
    }

    // --- 搬运功能核心 ---
    void TryPickUp()
    {
        if (mainCam == null) return;

        // 关键：从相机前方 2.5 米处发射，彻底跳过 Player 身体
        Vector3 origin = mainCam.transform.position + mainCam.transform.forward * 2.5f;
        RaycastHit hit;

        if (Physics.SphereCast(origin, detectRadius, mainCam.transform.forward, out hit, interactRange))
        {
            if (hit.collider.CompareTag("Crate"))
            {
                grabbedItem = hit.collider.gameObject;
                Rigidbody rb = grabbedItem.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.isKinematic = true;
                    rb.useGravity = false;
                }

                // 禁用碰撞防止搬运时把人弹飞
                if (grabbedItem.TryGetComponent<Collider>(out Collider col)) col.enabled = false;

                grabbedItem.transform.SetParent(holdPoint);
                grabbedItem.transform.localPosition = Vector3.zero;
                grabbedItem.transform.localRotation = Quaternion.identity;

                Debug.Log("已拾取箱子：速度降至80%");
            }
        }
    }

    void DropItem()
    {
        if (grabbedItem != null)
        {
            if (grabbedItem.TryGetComponent<Collider>(out Collider col)) col.enabled = true;

            Rigidbody rb = grabbedItem.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.useGravity = true;
            }

            grabbedItem.transform.SetParent(null);
            grabbedItem = null;
            Debug.Log("箱子已放下");
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

        // 寻找附近的坑位并生成桥梁
        Collider[] voids = Physics.OverlapSphere(transform.position, 2.0f);
        foreach (var v in voids)
        {
            if (v.CompareTag("Void"))
            {
                if (bridgePrefab != null)
                {
                    Instantiate(bridgePrefab, v.transform.position, v.transform.rotation);
                }
                Destroy(v.gameObject); // 坑位消失，路面生成
                break;
            }
        }

        // 重生逻辑
        if (spawnPoint != null) transform.position = spawnPoint.position;

        currentHoldTime = 0;
        isSacrificing = false;
        Debug.Log("<color=red>牺牲完成！</color> 剩余存在值: " + hp);
    }
}