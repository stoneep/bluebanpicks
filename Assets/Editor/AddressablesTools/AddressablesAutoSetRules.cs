#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Tools/Addressables/Auto Set Rules", fileName = "AddressablesAutoSetRules")]
public class AddressablesAutoSetRules : ScriptableObject
{
    public List<Rule> rules = new();

    [Serializable]
    public class Rule
    {
        [Header("Scan")]
        public string rootFolder = "Assets/Art";
        public string groupName = "UIIcons";

        [Header("Address")]
        public RuleMode mode = RuleMode.ByFileNameLower;
        public string addressPrefix = "icon/role/";

        [Header("Filters (optional)")]
        public string onlyThisFileNameNoExt = "";
    }

    public enum RuleMode
    {
        /// <summary>파일명(소문자) → addressPrefix + filename</summary>
        ByFileNameLower,

        /// <summary>지정 파일명 하나만 매칭</summary>
        OnlyThisFileName,

        /// <summary>
        /// 캐릭터 초상화 규칙:
        /// Student_Portrait_{Id}            → char/{Id}/portrait_large
        /// Student_Portrait_{Id}_Small      → char/{Id}/portrait_small
        /// Student_Portrait_{Id}_Collection → char/{Id}/portrait_collection
        /// Student_Portrait_{Id}_Slot       → char/{Id}/portrait_slot
        /// </summary>
        CharacterPortraitByFolderAndCut
    }
}
#endif