using System;
using UnityEngine;
using Common.Pooling;

public abstract class BaseVirtualizedGrid<T> : MonoBehaviour, IVirtualizedGrid
    where T : MonoBehaviour
{
    [Header("Layout Settings")]
    [SerializeField] protected RectTransform content;
    [SerializeField] protected T slotPrefab;
    
    [Tooltip("레이아웃 모드\n- Vertical: 세로 스크롤, columns 고정\n- Horizontal: 가로 스크롤, rows 고정")]
    [SerializeField] protected GridLayoutMode layoutMode = GridLayoutMode.Vertical;
    
    [Tooltip("Vertical 모드: 열 개수 고정 (GridLayoutGroup의 Constraint Count)")]
    [SerializeField] protected int columns = 5;
    
    [Tooltip("Horizontal 모드: 행 개수 고정 (GridLayoutGroup의 Constraint Count)")]
    [SerializeField] protected int rows = 3;
    
    [SerializeField] protected Vector2 cellSize = new Vector2(220, 380);
    [SerializeField] protected Vector2 spacing = new Vector2(10, 10);
    [SerializeField] protected Vector2 padding = new Vector2(10, 10);
    
    [Tooltip("버퍼 개수 (Vertical: 버퍼 행 수 / Horizontal: 버퍼 열 수)")]
    [SerializeField] protected int bufferCount = 2;
    
    [Header("Position & Range Settings")]
    [Tooltip("슬롯 배치의 시작 오프셋 (절댓값 적용, 예: X=100, Y=50)")]
    [SerializeField] protected Vector2 startOffset = Vector2.zero;

    [Tooltip("슬롯 배치의 종료 오프셋 (상대적 여백)\n마지막 슬롯 이후 추가할 여유 공간 (예: X=100 → 오른쪽에 100px 여백)")]
    [SerializeField] protected Vector2 endOffset = Vector2.zero;
    
    [Tooltip("스크롤 범위 제한 사용 여부")]
    [SerializeField] protected bool useScrollRangeLimit = false;
    
    [Tooltip("스크롤 범위 제한 (Min/Max). useScrollRangeLimit가 true일 때만 적용")]
    [SerializeField] protected Vector2 scrollRange = new Vector2(0, -1000);
    
    [Header("Content Size Settings")]
    [Tooltip("Content의 Width를 자동 계산할지 여부 (Horizontal 모드에서 자동 활성화)")]
    [SerializeField] protected bool autoCalculateWidth = false;
    
    [Tooltip("Content의 최소 Width (autoCalculateWidth가 true일 때 적용)")]
    [SerializeField] protected float minContentWidth = 0f;
    
    [Header("Culling Settings")]
    [Tooltip("슬롯이 사라지는 경계선 오프셋. 양수면 viewport 밖으로 더 나간 후 사라짐 (예: X=-10, Y=-10)")]
    [SerializeField] protected Vector2 cullingOffset = Vector2.zero;
    
    [Tooltip("Culling Offset 사용 여부")]
    [SerializeField] protected bool useCullingOffset = false;

    protected UIComponentPool<T> slotPool;
    protected int itemCount;
    protected int lastStartIndex = -1; // Vertical: startRow / Horizontal: startColumn
    
    // Viewport 캐싱
    protected RectTransform cachedViewport;

    public event Action<int, T> OnRequestBind;

    protected virtual void Awake()
    {
        slotPool = UIComponentPool<T>.CreateTopLeft(slotPrefab, content, cellSize);
        CacheViewport();
        
        // Content Anchor를 Top-Left로 강제 설정 (Stretch 모드 방지)
        if (content != null)
        {
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(0, 1);
            content.pivot = new Vector2(0, 1);
        }
    }

    public void SetTotalCount(int count)
    {
        itemCount = count;
        RecalcContentSize();
        UpdatePoolSize();
        ForceRefresh();
    }

    /// <summary>
    /// IVirtualizedGrid 인터페이스 구현
    /// Vertical 모드: scrollY는 Y축 스크롤 값
    /// Horizontal 모드: scrollY는 X축 스크롤 값으로 해석
    /// </summary>
    public void Refresh(float scrollValue)
    {
        if (slotPool == null) return;

        // 스크롤 범위 제한 적용
        if (useScrollRangeLimit)
        {
            scrollValue = Mathf.Clamp(scrollValue, scrollRange.y, scrollRange.x);
        }

        if (layoutMode == GridLayoutMode.Vertical)
        {
            RefreshVertical(scrollValue);
        }
        else
        {
            RefreshHorizontal(scrollValue);
        }
    }

    /// <summary>
    /// Vertical 모드 Refresh (기존 로직)
    /// </summary>
    protected void RefreshVertical(float scrollY)
    {
        float rowH = cellSize.y + spacing.y;
        int startRow = Mathf.Max(0, Mathf.FloorToInt((scrollY - padding.y) / rowH) - bufferCount);

        if (startRow == lastStartIndex) return;
        lastStartIndex = startRow;

        int startIndex = startRow * columns;
        int poolCount = slotPool.Items.Count;

        for (int i = 0; i < poolCount; i++)
        {
            int dataIndex = startIndex + i;
            var slot = slotPool.RentAt(i);

            if (dataIndex >= 0 && dataIndex < itemCount)
            {
                int r = dataIndex / columns;
                int c = dataIndex % columns;
                
                // startOffset 적용
                float x = startOffset.x + padding.x + c * (cellSize.x + spacing.x);
                float y = startOffset.y - (padding.y + r * (cellSize.y + spacing.y));

                var rectTransform = (RectTransform)slot.transform;
                rectTransform.anchoredPosition = new Vector2(x, y);
                
                // Culling Offset 적용
                bool isVisible = useCullingOffset ?
                    IsSlotVisible(rectTransform) : true;
                
                slot.gameObject.SetActive(isVisible);
                
                if (isVisible)
                {
                    OnRequestBind?.Invoke(dataIndex, slot);
                }
            }
            else
            {
                slot.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Horizontal 모드 Refresh (새로운 로직)
    /// </summary>
    protected void RefreshHorizontal(float scrollX)
    {
        float colW = cellSize.x + spacing.x;
        int startCol = Mathf.Max(0, Mathf.FloorToInt((scrollX - padding.x) / colW) - bufferCount);

        if (startCol == lastStartIndex) return;
        lastStartIndex = startCol;

        int startIndex = startCol * rows;
        int poolCount = slotPool.Items.Count;

        for (int i = 0; i < poolCount; i++)
        {
            int dataIndex = startIndex + i;
            var slot = slotPool.RentAt(i);

            if (dataIndex >= 0 && dataIndex < itemCount)
            {
                int c = dataIndex / rows;      // 열 인덱스
                int r = dataIndex % rows;      // 행 인덱스
                
                // startOffset 적용
                float x = startOffset.x + padding.x + c * (cellSize.x + spacing.x);
                float y = startOffset.y - (padding.y + r * (cellSize.y + spacing.y));

                var rectTransform = (RectTransform)slot.transform;
                rectTransform.anchoredPosition = new Vector2(x, y);
                
                // Culling Offset 적용
                bool isVisible = useCullingOffset ?
                    IsSlotVisible(rectTransform) : true;
                
                //slot.gameObject.SetActive(isVisible); <-제어권x
                
                slotPool.SetVisible(i, isVisible);
                
                if (isVisible) OnRequestBind?.Invoke(dataIndex, slot);
            }
            else
            {
                //slot.gameObject.SetActive(false); <-제어권x
                slotPool.SetVisible(i, false);
            }
        }
    }
    
    /// <summary>
    /// Culling Offset을 고려하여 슬롯이 보이는지 확인
    /// </summary>
    protected bool IsSlotVisible(RectTransform slotRect)
    {
        if (cachedViewport == null)
        {
            CacheViewport();
            if (cachedViewport == null) return true; // viewport 없으면 항상 표시
        }

        // 슬롯의 월드 좌표 범위
        Vector3[] slotCorners = new Vector3[4];
        slotRect.GetWorldCorners(slotCorners);
        
        // Viewport의 월드 좌표 범위
        Vector3[] viewportCorners = new Vector3[4];
        cachedViewport.GetWorldCorners(viewportCorners);
        
        // Culling Offset 적용 (로컬 좌표계에서)
        float viewportMinX = viewportCorners[0].x + cullingOffset.x;
        float viewportMaxX = viewportCorners[2].x - cullingOffset.x;
        float viewportMinY = viewportCorners[0].y + cullingOffset.y;
        float viewportMaxY = viewportCorners[2].y - cullingOffset.y;
        
        // 슬롯 범위
        float slotMinX = slotCorners[0].x;
        float slotMaxX = slotCorners[2].x;
        float slotMinY = slotCorners[0].y;
        float slotMaxY = slotCorners[2].y;
        
        // AABB 충돌 검사
        bool isOverlapping = 
            slotMaxX >= viewportMinX && slotMinX <= viewportMaxX &&
            slotMaxY >= viewportMinY && slotMinY <= viewportMaxY;
        
        return isOverlapping;
    }
    
    /// <summary>
    /// Viewport 캐싱
    /// </summary>
    protected void CacheViewport()
    {
        if (cachedViewport == null)
        {
            cachedViewport = transform.parent as RectTransform;
            if (content.parent != null)
            {
                cachedViewport = content.parent as RectTransform;
            }
        }
    }

    public void ForceRefresh() => lastStartIndex = -1;

    protected void UpdatePoolSize()
    {
        CacheViewport();
        
        if (layoutMode == GridLayoutMode.Vertical)
        {
            // Vertical: 보이는 행 수 계산
            float viewportH = (cachedViewport != null) ?
                cachedViewport.rect.height : 1000f;
            int visibleRows = Mathf.CeilToInt(viewportH / (cellSize.y + spacing.y));
            int totalPoolCount = columns * (visibleRows + bufferCount * 2);
            
            // ⭐ itemCount보다 작으면 itemCount만큼 생성 (최소 보장)
            totalPoolCount = Mathf.Min(totalPoolCount, itemCount);
            
            slotPool.Ensure(totalPoolCount);
        }
        else
        {
            // Horizontal: 보이는 열 수 계산
            float viewportW = (cachedViewport != null) ?
                cachedViewport.rect.width : 1000f;
            int visibleCols = Mathf.CeilToInt(viewportW / (cellSize.x + spacing.x));
            int totalPoolCount = rows * (visibleCols + bufferCount * 2);
            
            // ⭐ itemCount보다 작으면 itemCount만큼 생성 (최소 보장)
            totalPoolCount = Mathf.Max(totalPoolCount, itemCount);
            
            slotPool.Ensure(totalPoolCount);
        }
    }

    /// <summary>
    /// Content 크기 계산 (Height + Width)
    /// </summary>
    protected void RecalcContentSize()
    {
        float h, w;

        if (layoutMode == GridLayoutMode.Vertical)
        {
            // Vertical 모드: Height 자동 계산, Width 선택적
            int totalRows = Mathf.Max(1, Mathf.CeilToInt(itemCount / (float)columns));
            
            h = padding.y * 2 + totalRows * cellSize.y + (totalRows - 1) * spacing.y;
            h += Mathf.Abs(startOffset.y) + Mathf.Abs(endOffset.y);
            
            // Width 계산
            if (autoCalculateWidth)
            {
                w = padding.x * 2 + columns * cellSize.x + (columns - 1) * spacing.x;
                w += Mathf.Abs(startOffset.x) + Mathf.Abs(endOffset.x);
                
                if (minContentWidth > 0)
                {
                    w = Mathf.Max(w, minContentWidth);
                }
            }
            else
            {
                w = content.sizeDelta.x; // 기존 width 유지
            }
        }
        else
        {
            // Horizontal 모드: Width 자동 계산, Height 선택적
            int totalCols = Mathf.Max(1, Mathf.CeilToInt(itemCount / (float)rows));
            
            // 실제 콘텐츠 너비 계산
            w = padding.x * 2 + totalCols * cellSize.x + (totalCols - 1) * spacing.x;
            w += Mathf.Abs(startOffset.x);
            
            float calculatedWidth = w;
            
            // endOffset을 "마지막 슬롯 이후 여유 공간"으로 해석
            // 예: endOffset.x = 100 → 마지막 슬롯 뒤에 100px 추가
            w += Mathf.Abs(endOffset.x);
            
            Debug.Log($"[BaseVirtualizedGrid.Horizontal] calculatedWidth={calculatedWidth}, endOffset={endOffset.x}, finalWidth={w}");
            
            // Viewport 기반 최소 너비 보장 (슬롯이 적을 때)
            if (cachedViewport != null && Mathf.Abs(endOffset.x) == 0)
            {
                float minRequiredWidth = cachedViewport.rect.width + Mathf.Abs(startOffset.x);
                w = Mathf.Max(w, minRequiredWidth);
                
                Debug.Log($"[BaseVirtualizedGrid.Horizontal] 자동 최소 너비 적용: minRequired={minRequiredWidth}, finalWidth={w}");
            }
            
            if (minContentWidth > 0)
            {
                w = Mathf.Max(w, minContentWidth);
            }
            
            // Height는 rows 기반으로 계산
            h = padding.y * 2 + rows * cellSize.y + (rows - 1) * spacing.y;
            h += Mathf.Abs(startOffset.y) + Mathf.Abs(endOffset.y);
        }
        
        content.sizeDelta = new Vector2(w, h);
    }
    
    /// <summary>
    /// [레거시] Height만 계산 (하위 호환성)
    /// </summary>
    [System.Obsolete("Use RecalcContentSize() instead")]
    protected void RecalcContentHeight()
    {
        RecalcContentSize();
    }
    
    #region Public API for Runtime Configuration
    
    /// <summary>
    /// 레이아웃 모드 변경 (런타임)
    /// </summary>
    public void SetLayoutMode(GridLayoutMode mode)
    {
        if (layoutMode == mode) return;
        
        layoutMode = mode;
        RecalcContentSize();
        UpdatePoolSize();
        ForceRefresh();
    }
    
    /// <summary>
    /// Columns 설정 (Vertical 모드에서 사용)
    /// </summary>
    public void SetColumns(int count)
    {
        columns = Mathf.Max(1, count);
        if (layoutMode == GridLayoutMode.Vertical)
        {
            RecalcContentSize();
            UpdatePoolSize();
            ForceRefresh();
        }
    }
    
    /// <summary>
    /// Rows 설정 (Horizontal 모드에서 사용)
    /// </summary>
    public void SetRows(int count)
    {
        rows = Mathf.Max(1, count);
        if (layoutMode == GridLayoutMode.Horizontal)
        {
            RecalcContentSize();
            UpdatePoolSize();
            ForceRefresh();
        }
    }
    
    /// <summary>
    /// Cell Size 설정
    /// </summary>
    public void SetCellSize(Vector2 size)
    {
        cellSize = size;
        slotPool?.UpdateCellSize(size);
        RecalcContentSize();
        UpdatePoolSize();
        ForceRefresh();
    }
    
    /// <summary>
    /// Spacing 설정
    /// </summary>
    public void SetSpacing(Vector2 space)
    {
        spacing = space;
        RecalcContentSize();
        ForceRefresh();
    }
    
    /// <summary>
    /// Padding 설정
    /// </summary>
    public void SetPadding(Vector2 pad)
    {
        padding = pad;
        RecalcContentSize();
        ForceRefresh();
    }
    
    /// <summary>
    /// 시작 오프셋 설정 (런타임에서도 변경 가능)
    /// </summary>
    public void SetStartOffset(Vector2 offset)
    {
        startOffset = offset;
        ForceRefresh();
    }
    
    /// <summary>
    /// 종료 오프셋 설정 (런타임에서도 변경 가능)
    /// </summary>
    public void SetEndOffset(Vector2 offset)
    {
        endOffset = offset;
        RecalcContentSize();
        ForceRefresh();
    }
    
    /// <summary>
    /// 스크롤 범위 설정 (런타임에서도 변경 가능)
    /// </summary>
    public void SetScrollRange(float min, float max, bool enable = true)
    {
        scrollRange = new Vector2(max, min); // x가 max, y가 min
        useScrollRangeLimit = enable;
        ForceRefresh();
    }
    
    /// <summary>
    /// Culling Offset 설정 (런타임에서도 변경 가능)
    /// 양수 값: viewport 밖으로 더 나간 후 사라짐 (예: X=-10이면 왼쪽으로 10픽셀 더 나가야 사라짐)
    /// 음수 값: viewport 안쪽에서 미리 사라짐
    /// </summary>
    public void SetCullingOffset(Vector2 offset, bool enable = true)
    {
        cullingOffset = offset;
        useCullingOffset = enable;
        ForceRefresh();
    }
    
    /// <summary>
    /// Content Width 자동 계산 설정
    /// </summary>
    public void SetAutoCalculateWidth(bool enable, float minWidth = 0f)
    {
        autoCalculateWidth = enable;
        minContentWidth = minWidth;
        RecalcContentSize();
    }
    
    /// <summary>
    /// 현재 설정 값 조회
    /// </summary>
    public (GridLayoutMode mode, int columns, int rows, Vector2 cellSize, Vector2 spacing, Vector2 padding) GetLayoutSettings()
    {
        return (layoutMode, columns, rows, cellSize, spacing, padding);
    }
    
    /// <summary>
    /// 현재 오프셋 및 범위 설정 조회
    /// </summary>
    public (Vector2 startOffset, Vector2 endOffset, Vector2 scrollRange, bool useLimit, Vector2 cullingOffset, bool useCulling) GetCurrentSettings()
    {
        return (startOffset, endOffset, scrollRange, useScrollRangeLimit, cullingOffset, useCullingOffset);
    }
    
    #endregion
}
