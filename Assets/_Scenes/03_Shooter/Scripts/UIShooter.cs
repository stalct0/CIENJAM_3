using UnityEngine;

public class UIShooter : MonoBehaviour
{
    [Header("References")] 
    public GameManager GameManager;
    public CanvasGroup CanvasGroup;

    private void Update()
    {
        // GameManager에서 로컬 플레이어 권한을 가진 객체가 할당되었는지 확인
        var player = GameManager.LocalPlayer;

        if (player == null)
        {
            CanvasGroup.alpha = 0f;
            CanvasGroup.interactable = false;
            CanvasGroup.blocksRaycasts = false;
        }
        else
        {
            // 플레이어가 존재하면 UI를 표시
            CanvasGroup.alpha = 1f;
            CanvasGroup.interactable = true;
            CanvasGroup.blocksRaycasts = true;
        }
    }
}