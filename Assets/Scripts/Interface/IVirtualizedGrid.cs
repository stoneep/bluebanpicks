using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IVirtualizedGrid
{
    void Refresh(float scrollY);
    
    void ForceRefresh();
}