using System;
using System.Collections.Generic;
using UnityEngine;

namespace Common.Pooling
{
    public sealed class UIComponentPool<T> where T : Component
    {
        private readonly T prefab;
        private readonly RectTransform parent;
        private readonly List<T> items = new List<T>(128);

        private readonly Vector2 size;
        private readonly Vector2 anchorMin;
        private readonly Vector2 anchorMax;
        private readonly Vector2 pivot;

        public IReadOnlyList<T> Items => items;

        public UIComponentPool(T prefab, RectTransform parent, Vector2 size, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot)
        {
            if (!prefab) throw new ArgumentNullException(nameof(prefab));
            if (!parent) throw new ArgumentNullException(nameof(parent));

            this.prefab = prefab;
            this.parent = parent;
            this.size = size;
            this.anchorMin = anchorMin;
            this.anchorMax = anchorMax;
            this.pivot = pivot;
        }

        public static UIComponentPool<T> CreateTopLeft(T prefab, RectTransform parent, Vector2 size)
        {
            return new UIComponentPool<T>(
                prefab,
                parent,
                size,
                anchorMin: new Vector2(0, 1),
                anchorMax: new Vector2(0, 1),
                pivot: new Vector2(0, 1)
            );
        }

        public void Ensure(int count, Action<T> onCreated = null)
        {
            // if (count < 0) count = 0;
            //
            // while (items.Count < count)
            // {
            //     var inst = UnityEngine.Object.Instantiate(prefab, parent);
            //     ApplyRectDefaults(inst);
            //     SafeReturn(inst);
            //     items.Add(inst);
            //     onCreated?.Invoke(inst);
            // } --> 생성직후 숨겨버리는코드
            
            while (items.Count < count)
            {
                var inst = UnityEngine.Object.Instantiate(prefab, parent);
                ApplyRectDefaults(inst);

                // SafeReturn(inst) 대신: 시각 정리는 하되 SetActive는 건드리지 않음
                if (inst is IUIReusable reusable) reusable.OnReturn();
                inst.gameObject.SetActive(true);   // ← 처음부터 열어둠

                items.Add(inst);
                onCreated?.Invoke(inst);
            }
        }

        public T RentAt(int index)
        {
            if (index < 0 || index >= items.Count)
                throw new ArgumentOutOfRangeException(nameof(index), index, "Index out of range. Call Ensure() first.");

            var item = items[index];
            if (!item.gameObject.activeSelf)
                item.gameObject.SetActive(true);

            SetVisible(index, true);
            
            if (item is IUIReusable reusable) reusable.OnRent();
            
            return item;
        }

        public void ReturnUnusedFrom(int fromIndex)
        {
            if (fromIndex < 0) fromIndex = 0;
            if (fromIndex > items.Count) fromIndex = items.Count;

            for (int i = fromIndex; i < items.Count; i++)
                SafeReturn(items[i]);
        }

        public void ReturnAll()
        {
            ReturnUnusedFrom(0);
        }

        private void SafeReturn(T item)
        {
            if (!item) return;

            if (item is IUIReusable reusable)
                reusable.OnReturn();
            
            int idx = items.IndexOf(item);
            if (idx >= 0) SetVisible(idx, false);
            
            // if (item.gameObject.activeSelf)
            //     item.gameObject.SetActive(false);
        }

        private void ApplyRectDefaults(T item)
        {
            if (!item) return;
            if (item.transform is not RectTransform rt) return;

            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.sizeDelta = size;
        }
        
        /// <summary>
        /// 모든 풀링된 슬롯의 크기를 업데이트
        /// </summary>
        public void UpdateCellSize(Vector2 newSize)
        {
            // size 필드는 readonly라서 직접 변경 불가
            // 대신 모든 아이템의 RectTransform을 업데이트
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                if (!item) continue;
                if (item.transform is not RectTransform rt) continue;
                
                rt.sizeDelta = newSize;
            }
        }
        
        public void SetVisible(int index, bool visible)
        {
            if (index < 0 || index >= items.Count) return;
            var item = items[index];
            if (!item) return;

            if (item.gameObject.activeSelf == visible) return;
            item.gameObject.SetActive(visible);
        }
    }
}
