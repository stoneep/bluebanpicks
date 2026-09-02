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

    private void OnDestroy() => artProvider.ReleaseAll();

    public void Show(CharacterViewData data) => Show(data.Id);

    public void Show(string characterId)
    {
        if (!portraitImage || string.IsNullOrEmpty(characterId)) return;

        currentId = characterId;
        int token = ++loadToken;

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
}