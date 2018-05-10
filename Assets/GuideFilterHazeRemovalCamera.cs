using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GuideFilterHazeRemovalCamera : MonoBehaviour 
{
    public bool showTransmissTex = false;
    [Space(20)]
    public bool useGuidanceFilter = true;

    [Range(0,1)]
    public float minGrayScale = 0.9f;// 防止图像太黑时大气颜色变黑
    [Range(0,1)]
    public float maxGrayScale = 0.95f;// 防止大气太亮, 天空出现色块


    [Range(0,5)]
    public int darkChannelDownSample = 3;   // 降低分辨率采样
    [Range(1,17)]
    public int minFilterWidth = 2;          // 最小值滤波采样宽度


    [Space(20)]
    [Header("大气颜色")]
    public float atmosSphereUpdatePeriod = 10f;   // 大气颜色更新周期
    // 获得前maxCount个最亮的值, 取平均
    [Range(0.00001f,0.0015f)]
    public float maxAtmosCountRatio = 0.001f;
    public float lastUpdateTime = -1000f;
    bool atmosUpdateCompleted = true;
    public Color atmosColor = Color.white;

    [Space(20)]
    public Material hazeRemovalMat;
    public Material gammaMat; // 暂时不用


    #region 大气颜色 A 计算
    // 用于计算大气颜色(强度, 位置)
    [System.Serializable]
    public class AtmosPos
    {
        public float strength;
        public Vector2Int pos;
        public AtmosPos(float s,Vector2Int p)
        {
            this.strength = s;
            this.pos = p;
        }
    }
    // 计算大气颜色
    IEnumerator ReCountAtmosphereColor(Texture2D darkChannelTex2D,Texture2D originTex2D)
    {
        int darkWidth = darkChannelTex2D.width;
        int darkHight = darkChannelTex2D.height;
        int originWidth = originTex2D.width;
        int originHeight = originTex2D.height;

        Debug.Log("1. 获得暗通道前0.1%的值");
        // ======================= 1. 获得暗通道前0.1%的值 ==========================
        // 从暗通道中获得maxCount个最亮的点
        int maxCount = (int)((float)darkWidth * (float)darkHight * maxAtmosCountRatio);
        // 最亮的几个点, 升序排列
        AtmosPos[] lightestPoses = new AtmosPos[maxCount];
        // 暗通道上的所有像素
        var darkPixels = darkChannelTex2D.GetPixels();
        // 初始化
        for (int i = 0; i < maxCount; i++)
        {
            lightestPoses[i] = new AtmosPos(0, Vector2Int.zero);
        }
        // 遍历以找出暗通道中最亮的几个点
        for (int i = 0; i < darkWidth; i++)
        {
            for (int j = 0; j < darkHight; j++)
            {
                float curStrength = darkPixels[i + j*darkWidth].r;
                // 选择合适位置插入这个颜色(或者不插入)
                for (int index = 0; index < maxCount; index++)
                {
                    if (curStrength > lightestPoses[index].strength)
                    {
                        // 前移
                        if (index != 0)
                            lightestPoses[index - 1] = lightestPoses[index];
                        if (index == maxCount - 1)
                            lightestPoses[index] = new AtmosPos(curStrength, new Vector2Int(i, j));
                    }
                    else
                    {
                        if (index == 0)
                            break;
                        else
                            lightestPoses[index - 1] = new AtmosPos(curStrength,new Vector2Int(i,j));
                    }
                }
            }
            yield return null;
        }

        Debug.Log("2. 从前0.1%个暗通道值中获得原图最亮的一个像素");
        // ======================= 2. 从前0.1%个暗通道值中获得原图最亮的一个像素 ==========================
        Color maxColor = Color.black;
        int maxIndex = 0;
        // 在这些点中取最亮的一个点
        for (int i = 0; i < maxCount; i++)
        {
            // 将darkChannel坐标映射到originTex
            int targetPosX = (int)((float)lightestPoses[i].pos.x*((float)originWidth/(float)darkWidth));
            int targetPosY = (int)((float)lightestPoses[i].pos.y*((float)originHeight/(float)darkHight));

            var curPixel = originTex2D.GetPixel(targetPosX, targetPosY);
            if (curPixel.grayscale > maxColor.grayscale)
            {
                maxIndex = i;
                maxColor = curPixel;
            }
        }
        Debug.Log("3. 插值渐变大气颜色");
        // ======================= 3. 插值渐变大气颜色 ==========================
        atmosColor = ClampAtmosColor(maxColor);
        atmosUpdateCompleted = true;
    }
    Color ClampAtmosColor(Color originAtmos)
    {
        if (originAtmos.grayscale < minGrayScale)
        {
            float transRatio = minGrayScale / originAtmos.grayscale;
            return originAtmos * transRatio;
        }
        else if (originAtmos.grayscale > maxGrayScale)
        {
            float transRatio = maxGrayScale / originAtmos.grayscale;
            return originAtmos * transRatio;
        }
        return originAtmos;
    }
    #endregion

    // 更新大气光照
    void UpdateAtmosphere(RenderTexture originTex)
    {
        // 检查是否应该计算大气颜色
        if (atmosUpdateCompleted)
        {
            float costTime = Time.time - lastUpdateTime;
            if (costTime > atmosSphereUpdatePeriod)
            {
                RenderTexture darkChannelTex = RenderTexture.GetTemporary(originTex.width >> darkChannelDownSample,originTex.height >> darkChannelDownSample,0,originTex.format);
                hazeRemovalMat.SetColor("_AtmosColor", Color.white);
                BlitDarkChannel(originTex, darkChannelTex);
                //============================
                lastUpdateTime = Time.time;
                atmosUpdateCompleted = false;
                var darkChannelTex2D = RTtoTex2D(darkChannelTex);
                var originTex2D = RTtoTex2D(originTex);
                StartCoroutine(ReCountAtmosphereColor(darkChannelTex2D,originTex2D));
                //============================
                RenderTexture.ReleaseTemporary(darkChannelTex);
            }
        }
    }
    // 获得暗通道
    void BlitDarkChannel(RenderTexture originTex, RenderTexture dest)
    {
        RenderTexture tempTex = RenderTexture.GetTemporary(originTex.width >> darkChannelDownSample,originTex.height >> darkChannelDownSample,0,originTex.format);
        // 1. 暗通道: 变灰
        Graphics.Blit(originTex,tempTex, hazeRemovalMat,0);
        hazeRemovalMat.SetColor("_AtmosColor", Color.white);
        // 1. 暗通道: 最小值滤波
        Graphics.Blit(tempTex, dest, hazeRemovalMat,1);
        RenderTexture.ReleaseTemporary(tempTex);
    }
   

    // 错误 : 应将导向滤波应用于暗通道而不是透射图 !!
    // 每帧渲染
    void OnRenderImageOrigin(RenderTexture source, RenderTexture dest)
    {  
        hazeRemovalMat.SetInt("_CoreWidth", minFilterWidth);
        RenderTexture darkChannelTex = RenderTexture.GetTemporary(source.width >> darkChannelDownSample,source.height >> darkChannelDownSample,0,source.format);
        RenderTexture transmissTex = RenderTexture.GetTemporary(source.width >> darkChannelDownSample,source.height >> darkChannelDownSample,0,source.format);
        RenderTexture guideTransmissTex = RenderTexture.GetTemporary(source.width >> darkChannelDownSample,source.height >> darkChannelDownSample,0,source.format);


        UpdateAtmosphere(source);

        // 求带有大气值的暗通道(以获得透射率)
        hazeRemovalMat.SetColor("_AtmosColor", atmosColor);
        BlitDarkChannel(source,darkChannelTex);
        hazeRemovalMat.SetTexture("_DarkChannelTex",darkChannelTex);
        // 计算透射率
        Graphics.Blit(darkChannelTex,transmissTex, hazeRemovalMat,2);
        // 是否使用导向滤波?
        if (useGuidanceFilter)
        {
            GuideFilter.Instance.Filter(transmissTex, source, guideTransmissTex);
            hazeRemovalMat.SetTexture("_TransmissTex", guideTransmissTex);
        }
        else
        {
            hazeRemovalMat.SetTexture("_TransmissTex",transmissTex);
        }
       
        // 最终结果
        Graphics.Blit(source, dest, hazeRemovalMat, 3);

        if (showTransmissTex)
        {
            if (useGuidanceFilter)
                Graphics.Blit(guideTransmissTex, dest);
            else
                Graphics.Blit(transmissTex, dest);
        }

        RenderTexture.ReleaseTemporary(darkChannelTex);
        RenderTexture.ReleaseTemporary(transmissTex);
        RenderTexture.ReleaseTemporary(guideTransmissTex);
    }

    // 每帧渲染
    void OnRenderImage(RenderTexture source, RenderTexture dest)
    {  
        hazeRemovalMat.SetInt("_CoreWidth", minFilterWidth);
        RenderTexture darkChannelTex = RenderTexture.GetTemporary(source.width >> darkChannelDownSample,source.height >> darkChannelDownSample,0,source.format);
        RenderTexture guideDarkChannelTex = RenderTexture.GetTemporary(source.width >> darkChannelDownSample,source.height >> darkChannelDownSample,0,source.format);

        RenderTexture transmissTex = RenderTexture.GetTemporary(source.width >> darkChannelDownSample,source.height >> darkChannelDownSample,0,source.format);
        //RenderTexture guideTransmissTex = RenderTexture.GetTemporary(source.width >> darkChannelDownSample,source.height >> darkChannelDownSample,0,source.format);


        UpdateAtmosphere(source);

        // 求带有大气值的暗通道(以获得透射率)
        hazeRemovalMat.SetColor("_AtmosColor", atmosColor);
        BlitDarkChannel(source,darkChannelTex);
        hazeRemovalMat.SetTexture("_DarkChannelTex",darkChannelTex);
        // 是否使用导向滤波?
        if (useGuidanceFilter)
        {
            GuideFilter.Instance.Filter(darkChannelTex, source, guideDarkChannelTex);
            hazeRemovalMat.SetTexture("_DarkChannelTex", guideDarkChannelTex);
        }

        // 计算透射率
        Graphics.Blit(guideDarkChannelTex,transmissTex, hazeRemovalMat,2);

        hazeRemovalMat.SetTexture("_TransmissTex",transmissTex);

        // 最终结果
        Graphics.Blit(source, dest, hazeRemovalMat, 3);

        if (showTransmissTex)
        {
            Graphics.Blit(transmissTex, dest);
        }

        RenderTexture.ReleaseTemporary(darkChannelTex);
        RenderTexture.ReleaseTemporary(transmissTex);
        RenderTexture.ReleaseTemporary(guideDarkChannelTex);
    }



    void OnDisable()
    {
        StopAllCoroutines();
        atmosUpdateCompleted = true;
    }
    // RenderTex转Tex2D
    public Texture2D RTtoTex2D(RenderTexture temp)
    {
        Debug.Assert(temp != null);
        Texture2D myTexture2D = new Texture2D(temp.width,temp.height);
        RenderTexture tempActive = RenderTexture.active;
        RenderTexture.active = temp;
        myTexture2D.ReadPixels(new Rect(0, 0, temp.width, temp.height), 0, 0);
        myTexture2D.Apply();
        RenderTexture.active = tempActive;
        RenderTexture.active = null;
        return myTexture2D;
    }
}
