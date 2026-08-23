using System.Collections.Generic;

public interface IFilteredListView<TData>
{
    void SetData(List<TData> data);
    void Refresh(bool jumpToTop);
}