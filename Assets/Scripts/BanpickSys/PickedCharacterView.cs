using UnityEngine;
using UnityEngine.UI;

public sealed class PickedCharacterView : MonoBehaviour
{
    [SerializeField] private Image portraitImage;
    [SerializeField] private CharacterCut cut = CharacterCut.Large;

    [Header("Next Turn Overlay (다음 차례 표시)")]
    [SerializeField] private GameObject nextTurnOverlay;

    private readonly CharacterArtProvider artProvider = new();
    private string currentId;
    private int loadToken;
    private bool isPending;
    
    private void OnDestroy() => artProvider.ReleaseAll();

    public void Show(CharacterViewData data) => Show(data.Id);

    public void Show(string characterId)
    {
        if (!portraitImage || string.IsNullOrEmpty(characterId)) return;

        isPending = false; // 확정 표시로 전환되면 프리뷰 상태는 해제
        currentId = characterId;
        int token = ++loadToken;
        
        if (portraitImage) { var c = portraitImage.color; c.a = 1f; portraitImage.color = c; } // alpha 원복
        
        var handle = artProvider.LoadSprite(characterId, cut);
        if (handle.IsDone) Apply(token, characterId, handle.Result);
        else handle.Completed += h => Apply(token, characterId, h.Result);
    }

    public void Clear()
    {
        currentId = null;
        loadToken++; 
        if (portraitImage)
        {
            portraitImage.sprite = null;
            portraitImage.enabled = false;
        }
        SetNextTurnHighlight(false);
    }
    
    public void SetNextTurnHighlight(bool on)
    {
        if (nextTurnOverlay) nextTurnOverlay.SetActive(on);
    }

    private void Apply(int token, string characterId, Sprite sprite)
    {
        
        if (token != loadToken || currentId != characterId || !portraitImage) return;

        portraitImage.sprite = sprite;
        portraitImage.enabled = (sprite != null);
        portraitImage.preserveAspect = true;
    }
    
    public void ShowPending(string characterId)
    {
        Show(characterId);
        isPending = true;
        if (portraitImage) { var c = portraitImage.color; c.a = 0.5f; portraitImage.color = c; }
    }

    public void ClearPending()
    {
        // 이미 확정 픽으로 덮어써졌다면(isPending == false) 아무것도 하지 않는다.
        // 서버의 "확정"과 "프리뷰 해제" 알림이 근접한 타이밍에 도착해도 확정 픽을 실수로 지우지 않기 위한 안전장치.
        if (!isPending) return;

        isPending = false;
        currentId = null;
        loadToken++;
        if (portraitImage)
        {
            portraitImage.sprite = null;
            portraitImage.enabled = false;
            var c = portraitImage.color; c.a = 1f; portraitImage.color = c;
        }
    }
}