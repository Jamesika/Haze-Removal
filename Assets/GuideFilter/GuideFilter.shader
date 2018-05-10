// https://blog.csdn.net/zwlq1314521/article/details/51006144
Shader "Hidden/GuideFilter"
{
	Properties
	{
		_MainTex ("Guide Texture", 2D) = "white" {}
		// 1. ===
		_MeanITex("_MeanITex",2D) = "white" {}
		_MeanPTex("_MeanPTex",2D) = "white" {}
		_CorrITex("_CorrITex",2D) = "white" {}
		_CorrIPTex("_CorrIPTex",2D) = "white" {}
		// 2. ===
		_VarITex("_VarITex",2D) = "white" {}
		_CovIPTex("_CovIPTex",2D) = "white" {}
		// 3. ===
		_ATex("A Tex",2D) = "white" {}
		_BTex("B Tex",2D) = "white" {}
		_Regular("Regularization E", Range(0,1)) = 0.9
		// 4. ===
		_MeanATex("Mean A Tex",2D) = "white" {}
		_MeanBTex("Mean B Tex",2D) = "white" {}
	}
	SubShader
	{
		Cull Off ZWrite Off ZTest Always

		// ============== 1. 求 _VarITex ================
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

			sampler2D _MeanITex;
			sampler2D _CorrITex;

			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 meanI = tex2D(_MeanITex, i.uv);
				fixed4 corrI = tex2D(_CorrITex, i.uv);
				fixed4 col = corrI - meanI*meanI;
				return col;
			}
			ENDCG
		}
		// ============== 2. 求_CovIPTex ================
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

			sampler2D _CorrIPTex;
			sampler2D _MeanITex;
			sampler2D _MeanPTex;

			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 corrIP = tex2D(_CorrIPTex, i.uv);
				fixed4 meanI = tex2D(_MeanITex, i.uv);
				fixed4 meanP = tex2D(_MeanPTex, i.uv);
				fixed4 col = corrIP - meanI*meanP;
				return col;
			}
			ENDCG
		}
		// ============== 3. 求_ATex ================
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

			sampler2D _CovIPTex;
			sampler2D _VarITex;
			sampler2D _MeanPTex;
			fixed _Regular;

			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 covIP = tex2D(_CovIPTex, i.uv);
				fixed4 varI = tex2D(_VarITex, i.uv);
				fixed4 col = covIP/(varI + fixed4(_Regular,_Regular,_Regular,1));
				return col;
			}
			ENDCG
		}
		// ============== 4. 求_BTex ================
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

			sampler2D _ATex;
			sampler2D _MeanITex;
			sampler2D _MeanPTex;

			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 a = tex2D(_ATex, i.uv);
				fixed4 meanI = tex2D(_MeanITex, i.uv);
				fixed4 meanP = tex2D(_MeanPTex, i.uv);
				fixed4 col = meanP - a*meanI;
				return col;
			}
			ENDCG
		}
		// ============== 5. 求最终图像 ================
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

			sampler2D _MainTex;
			sampler2D _MeanATex;
			sampler2D _MeanBTex;

			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 meanA = tex2D(_MeanATex, i.uv);
				fixed4 meanB = tex2D(_MeanBTex, i.uv);
				fixed4 guide = tex2D(_MainTex, i.uv);
				fixed4 col = meanA*guide+ meanB;
				return col;
			}
			ENDCG
		}
	}
}
