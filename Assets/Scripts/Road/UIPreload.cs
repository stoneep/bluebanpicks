using UnityEngine;

public sealed class UIPreload : MonoBehaviour
{
    private const string ATLAS_COMMON = AtlasAddressConfig.COMBAT;
    private const string ATLAS_AFFILIATION = AtlasAddressConfig.AFFILIATION;

    private void Awake()
    {
        var svc = UIIconAtlasService.Instance;
        if (svc == null)
        {
            Debug.LogError("[UIPreload] UIIconAtlasService.Instance is null. Ensure UIIconAtlasBootstrap is enabled.");
            return;
        }

        svc.LoadAtlas(ATLAS_COMMON);
        svc.LoadAtlas(ATLAS_AFFILIATION);
    }
}
