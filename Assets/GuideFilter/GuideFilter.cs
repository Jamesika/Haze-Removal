using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GuideFilter : MonoBehaviour 
{
    public bool testMeanA = false;
    public bool testMeanB = false;

    static GuideFilter instance;
    public static GuideFilter Instance
    {
        get{ return instance; }
    }
    void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(this);
    }

    [Range(1,40)]
    public int radius = 1;                  // 导向滤波r
    [Range(0.01f,1f)]
    public float regularization = 0.01f;

    [Space(20)]
    public Material meanFilterMat;
    public Material texDotMat;
    public Material guideFilterMat;

    public void Mean(RenderTexture source, RenderTexture dest)
    {
        Graphics.Blit(source,dest,meanFilterMat);
    }
    public void Dot(RenderTexture source1,RenderTexture source2, RenderTexture dest)
    {
        texDotMat.SetTexture("_SubTex",source2);
        Graphics.Blit(source1, dest, texDotMat);
    }

    public void Filter(RenderTexture source,RenderTexture guide, RenderTexture dest)
    {
        // P ---> Source
        // I ---> guide
        // 设置值
        meanFilterMat.SetInt("_Radius", radius);
        guideFilterMat.SetFloat("_Regular", regularization);

        // 分配RT
        // 1 ===
        RenderTexture meanI = RenderTexture.GetTemporary(source.width,source.height ,0,source.format);
        RenderTexture meanP = RenderTexture.GetTemporary(source.width,source.height ,0,source.format);
        RenderTexture dotII = RenderTexture.GetTemporary(source.width,source.height ,0,source.format);
        RenderTexture dotIP = RenderTexture.GetTemporary(source.width ,source.height ,0,source.format);
        RenderTexture corrI = RenderTexture.GetTemporary(source.width ,source.height ,0,source.format);
        RenderTexture corrIP = RenderTexture.GetTemporary(source.width ,source.height ,0,source.format);
        // 2 ===
        RenderTexture varI = RenderTexture.GetTemporary(source.width,source.height,0,source.format);
        RenderTexture covIP = RenderTexture.GetTemporary(source.width,source.height,0,source.format);
        // 3 ===
        RenderTexture aTex = RenderTexture.GetTemporary(source.width,source.height,0,source.format);
        RenderTexture bTex = RenderTexture.GetTemporary(source.width,source.height,0,source.format);
        // 4 ===
        RenderTexture meanA = RenderTexture.GetTemporary(source.width,source.height,0,source.format);
        RenderTexture meanB = RenderTexture.GetTemporary(source.width,source.height,0,source.format);

        // 计算
        // 0 ===
        Mean(guide,meanI);
        Mean(source,meanP);
        Dot(guide,guide,dotII);
        Dot(guide, source, dotIP);
        Mean(dotII, corrI); 
        Mean(dotIP, corrIP);  
        // 1. ===
        guideFilterMat.SetTexture("_MeanITex",meanI);
        guideFilterMat.SetTexture("_MeanPTex",meanP);
        guideFilterMat.SetTexture("_CorrITex",corrI);
        guideFilterMat.SetTexture("_CorrIPTex",corrIP);
        Graphics.Blit(source,varI,guideFilterMat,0);
        // 2. ===
        Graphics.Blit(source,covIP,guideFilterMat,1);
        // 3. ===
        guideFilterMat.SetTexture("_CovIPTex",covIP);
        guideFilterMat.SetTexture("_VarITex",varI);
        Graphics.Blit(source,aTex,guideFilterMat,2);
        // 4. ===
        guideFilterMat.SetTexture("_ATex",aTex);
        Graphics.Blit(source,bTex,guideFilterMat,3);
        guideFilterMat.SetTexture("_BTex",bTex);


        Mean(aTex, meanA);
        Mean(bTex, meanB);
        guideFilterMat.SetTexture("_MeanATex", meanA);
        guideFilterMat.SetTexture("_MeanBTex", meanB);
      

        // 4. 最终!!===
        Graphics.Blit(guide,dest,guideFilterMat,4);

        // Problem : CovIP
        if (testMeanA)
            Graphics.Blit(meanA, dest);
        else if(testMeanB)
            Graphics.Blit(meanB,dest);

        //RenderTexture.ReleaseTemporary(guide);
        RenderTexture.ReleaseTemporary(meanI);
        RenderTexture.ReleaseTemporary(meanP);
        RenderTexture.ReleaseTemporary(dotII);
        RenderTexture.ReleaseTemporary(dotIP);

        RenderTexture.ReleaseTemporary(corrI);
        RenderTexture.ReleaseTemporary(corrIP);
        RenderTexture.ReleaseTemporary(varI);
        RenderTexture.ReleaseTemporary(covIP);
        RenderTexture.ReleaseTemporary(aTex);

        RenderTexture.ReleaseTemporary(bTex);
        RenderTexture.ReleaseTemporary(meanA);
        RenderTexture.ReleaseTemporary(meanB);
    }
}