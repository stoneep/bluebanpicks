using TMPro;
using UnityEngine;
using UnityEngine.UI;






public sealed class DraftResultRowView : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private TMP_Text orderText;   
    [SerializeField] private TMP_Text sideText;    
    [SerializeField] private TMP_Text typeText;    
    [SerializeField] private TMP_Text nameText;    
    [SerializeField] private Image portraitImage;
    [SerializeField] private CharacterCut cut = CharacterCut.Slot;

    [Header("Type Colors")]
    [SerializeField] private Color banColor = new(0.85f, 0.3f, 0.3f);
    [SerializeField] private Color pickColor = new(0.3f, 0.55f, 0.9f);

    private readonly CharacterArtProvider artProvider = new();
    private string currentId;
    private int loadToken;

    
    
    
    
    public void Bind(int order, DraftSide side, DraftResultType type, string characterId, string sideLabel)
    {
        if (orderText) orderText.text = order.ToString();
        if (sideText) sideText.text = sideLabel;

        if (typeText)
        {
            typeText.text = type == DraftResultType.Ban ? "BAN" : "PICK";
            typeText.color = type == DraftResultType.Ban ? banColor : pickColor;
        }

        if (nameText) nameText.text = CharDatabaseLoader.GetDisplayName(characterId);

        LoadPortrait(characterId);
    }

    private void LoadPortrait(string characterId)
    {
        if (!portraitImage || string.IsNullOrEmpty(characterId)) return;

        currentId = characterId;
        int token = ++loadToken;

        var handle = artProvider.LoadSprite(characterId, cut);

        if (handle.IsDone) Apply(token, characterId, handle.Result);
        else handle.Completed += h => Apply(token, characterId, h.Result);
    }

    private void Apply(int token, string characterId, Sprite sprite)
    {
        
        if (token != loadToken || currentId != characterId || !portraitImage) return;

        portraitImage.sprite = sprite;
        portraitImage.enabled = (sprite != null);
        portraitImage.preserveAspect = true;
    }

    private void OnDestroy() => artProvider.ReleaseAll();
}
