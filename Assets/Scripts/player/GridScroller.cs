using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(ScrollRect))]
public sealed class GridScroller : MonoBehaviour
{
    [SerializeField] private VirtualizedCharacterGrid grid;
    private ScrollRect scrollRect;

    private void Awake()
    {
        scrollRect = GetComponent<ScrollRect>();
        scrollRect.onValueChanged.AddListener(OnScroll);
    }

    private void OnScroll(Vector2 pos)
    {
        grid.Refresh(scrollRect.content.anchoredPosition.y);
    }

    public void JumpToTop()
    {
        scrollRect.verticalNormalizedPosition = 1f;
        grid.ForceRefresh();
        grid.Refresh(0f);
    }
}