using UnityEngine;

// 지나가면 체크포인트를 기록합니다. 죽으면 respawnPoint(없으면 이 위치)에서 다시 시작합니다.
// order가 큰 체크포인트일수록 뒤쪽입니다. 앞 체크포인트를 다시 지나도 덮어쓰지 않습니다.
public class Checkpoint : PlayerTrigger
{
    [SerializeField] int order = 1;
    [SerializeField] Transform respawnPoint;
    // 깃발 등 '켜짐'을 보여 줄 렌더러(선택)
    [SerializeField] Renderer flagRenderer;
    [SerializeField] Color activeColor = new Color(0.35f, 1f, 0.45f);

    void Start()
    {
        if (GameSession.HasCheckpoint && GameSession.CheckpointOrder >= order)
        {
            SetFlagActive();
        }
    }

    protected override void OnPlayerEnter(Mover mover)
    {
        Vector3 position = respawnPoint != null ? respawnPoint.position : transform.position;

        if (GameSession.SetCheckpoint(order, position, mover.IsViewFlipped))
        {
            FeedbackFX.Checkpoint(position);
            SetFlagActive();
        }
    }

    void SetFlagActive()
    {
        if (flagRenderer == null)
        {
            return;
        }

        MaterialPropertyBlock block = new MaterialPropertyBlock();
        block.SetColor("_BaseColor", activeColor);
        block.SetColor("_EmissionColor", activeColor * 2f);
        flagRenderer.SetPropertyBlock(block);
    }
}
