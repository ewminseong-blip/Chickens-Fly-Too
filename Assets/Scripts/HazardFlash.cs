using System.Collections;
using UnityEngine;

// 플레이어를 죽인 물체를 흰색 ↔ 원래 색으로 몇 번 깜빡여 '무엇에 죽었는지' 보여 줍니다.
public class HazardFlash : MonoBehaviour
{
    public static void Flash(GameObject target)
    {
        if (target == null)
        {
            return;
        }

        Renderer r = target.GetComponentInChildren<Renderer>();
        if (r == null)
        {
            r = target.GetComponentInParent<Renderer>();
        }

        if (r == null)
        {
            return;
        }

        HazardFlash flash = r.gameObject.AddComponent<HazardFlash>();
        flash.StartCoroutine(flash.Run(r));
    }

    IEnumerator Run(Renderer r)
    {
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        Color baseColor = r.sharedMaterial != null && r.sharedMaterial.HasProperty("_BaseColor")
            ? r.sharedMaterial.GetColor("_BaseColor")
            : Color.gray;

        for (int i = 0; i < 4; i++)
        {
            block.SetColor("_BaseColor", Color.white);
            r.SetPropertyBlock(block);
            yield return new WaitForSecondsRealtime(0.08f);
            block.SetColor("_BaseColor", baseColor);
            r.SetPropertyBlock(block);
            yield return new WaitForSecondsRealtime(0.08f);
        }

        r.SetPropertyBlock(null);
        Destroy(this);
    }
}
