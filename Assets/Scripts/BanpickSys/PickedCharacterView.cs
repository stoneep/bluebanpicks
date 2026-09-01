using UnityEngine;
using UnityEngine.UI;





public sealed class PickedCharacterView : MonoBehaviour
{
    [SerializeField] private Image portraitImage;
    [SerializeField] private CharacterCut cut = CharacterCut.Large;

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
    }

    private void Apply(int token, string characterId, Sprite sprite)
    {
        
        if (token != loadToken || currentId != characterId || !portraitImage) return;

        portraitImage.sprite = sprite;
        portraitImage.enabled = (sprite != null);
        portraitImage.preserveAspect = true;
    }
}