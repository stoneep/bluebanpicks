using System.Collections.Generic;
using UnityEngine;

public sealed class CharacterListBootstrap : MonoBehaviour
{
    [SerializeField] private CharacterListPanelController listController;
    [SerializeField] private TextAsset charDatabaseJson;
    [SerializeField] private GameLanguage language = GameLanguage.Korean;
    [SerializeField] private TextAsset patchJson; // optional
    
    private void Start()
    {
        var list = CharDatabaseLoader.LoadFromJson(charDatabaseJson, patchJson, language);

        if (listController != null) listController.SetAllCharacters(list);
    }
}