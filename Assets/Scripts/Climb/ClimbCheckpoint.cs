using UnityEngine;

// 체크포인트(드물게 3개): 둥지에 닿으면 켜짐. 켜진 뒤 그 높이보다 checkpointCatchDepth(20m) 넘게 떨어지면 여기로 돌아옴.
public class ClimbCheckpoint : MonoBehaviour
{
    [SerializeField] int index = 1;
    [SerializeField] Transform respawnPoint;
    [SerializeField] Renderer eggRenderer;
    [SerializeField] Color litColor = new Color(0.45f, 1f, 0.4f);

    public int Index => index;
    public Vector3 RespawnPosition => respawnPoint != null ? respawnPoint.position : transform.position + Vector3.up;
    public float RespawnYaw => respawnPoint != null ? respawnPoint.eulerAngles.y : transform.eulerAngles.y;

    public void SetLit(bool lit)
    {
        if (eggRenderer == null) return;
        MaterialPropertyBlock b = new MaterialPropertyBlock();
        eggRenderer.GetPropertyBlock(b);
        if (lit)
        {
            b.SetColor("_BaseColor", litColor);
            b.SetColor("_EmissionColor", litColor * 1.6f);
        }
        else
        {
            b.Clear();
        }
        eggRenderer.SetPropertyBlock(b);
    }

    void OnTriggerEnter(Collider other)
    {
        ClimbController p = other.GetComponentInParent<ClimbController>();
        if (p == null || ClimbGame.Instance == null) return;
        ClimbGame.Instance.ReachCheckpoint(this);
    }
}
