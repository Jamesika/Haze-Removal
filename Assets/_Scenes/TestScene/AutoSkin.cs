using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoSkin : MonoBehaviour 
{
    [Range(0,5)]
    public int downSample = 1;
    void OnRenderImage(RenderTexture source, RenderTexture dest)
    {
        RenderTexture sourceDownSample = RenderTexture.GetTemporary(source.width>>downSample,source.height>>downSample ,0,source.format);
        Graphics.Blit(source, sourceDownSample);
        GuideFilter.Instance.Filter(sourceDownSample, sourceDownSample, dest);
        RenderTexture.ReleaseTemporary(sourceDownSample);
    }
}
