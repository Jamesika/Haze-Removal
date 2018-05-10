Shader "Hidden/MeanFilter"
{
	Properties
	{
		_MainTex ("Main Texture", 2D) = "white" {}
		_Radius("Radius", int) = 1
	}
	SubShader
	{
		Cull Off ZWrite Off ZTest Always

		// ============== 1. 均值滤波(由于BoxFilter需要在CPU中计算, 所以这里使用普通方法均值滤波) ================
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}

			uniform fixed4 _MainTex_TexelSize;
			sampler2D _MainTex;
			int _Radius;

			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 col = fixed4(0,0,0,0);
				int pixelCount = pow(_Radius*2+1,2);
				float pixelRatio = 1.0f/(float)pixelCount;
				for(int ti = -_Radius;ti<=_Radius;ti++)
				{
					for(int tj = -_Radius;tj<=_Radius;tj++)
					{
						fixed2 deltaUV = fixed2(_MainTex_TexelSize.x*ti, _MainTex_TexelSize.y*tj);
						col += pixelRatio*tex2D(_MainTex, i.uv+deltaUV);
					}
				}
				return fixed4(col.rgb,1);
			}
			ENDCG
		}
	}
}
