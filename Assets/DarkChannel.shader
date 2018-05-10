Shader "Hidden/DarkChannel"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_DarkChannelTex ("_DarkChannelTex", 2D) = "white" {}
		_TransmissTex("_TransmissTex",2D) = "white" {}
		_CoreWidth("MinFilter Width", int) = 4
		_AtmosColor("Atmosphere Color",Color) = (1,1,1,1)
		_OriginTransmissivityRatio("_OriginTransmissivityRatio",Range(0,1)) = 0.95 // 原始透射率(保留一定程度的雾)
		_TransmissivityThreshold("_TransmissionThreshold",Range(0,1)) = 0.1 // 透射率阈值
	}
	SubShader
	{
		// No culling or depth
		Cull Off ZWrite Off ZTest Always

		// ============== Pass 1: 暗通道求min(R,G,B) ================
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
			fixed3 _AtmosColor;

			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 col = tex2D(_MainTex, i.uv);
				col.rgb /= _AtmosColor.rgb;// 当计算透射率时要除以大气颜色(第一次不用除, 大气设为白色)
				fixed rgbMin = min(col.r,col.g);
				rgbMin = min(rgbMin,col.b);
				col.rgb = fixed3(rgbMin,rgbMin,rgbMin);
				return col;
			}
			ENDCG
		}
		// ============== Pass 2: 暗通道最小值滤波 ================
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
			int _CoreWidth;

			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 col = tex2D(_MainTex, i.uv);
				fixed minValue = 1.0f;
				for(int ti = -_CoreWidth;ti<_CoreWidth;ti++)
				{
					for(int tj = -_CoreWidth;tj<_CoreWidth;tj++)
					{
						fixed2 deltaUV = fixed2(_MainTex_TexelSize.x*ti, _MainTex_TexelSize.y*tj);
						minValue = min(minValue,tex2D(_MainTex, i.uv+deltaUV).r);
					}
				}
				col.rgb = fixed3(minValue,minValue,minValue);
				return col;
			}
			ENDCG
		}
		// ============== Pass 3: 计算透射率 ================
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

			sampler2D _DarkChannelTex;
			fixed _OriginTransmissivityRatio;

			fixed4 frag (v2f i) : SV_Target
			{
				// 计算透射率
				fixed transmissivity = 1.0f - _OriginTransmissivityRatio*tex2D(_DarkChannelTex,i.uv).r;
				return fixed4(transmissivity,transmissivity,transmissivity,transmissivity);
			}
			ENDCG
		}
		// ============== Pass 4: 去雾(原图+含大气颜色的暗通道) ================
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
			sampler2D _DarkChannelTex;
			sampler2D _TransmissTex;
			int _CoreWidth;
			fixed _TransmissionThreshold;
			fixed _OriginTransmissivityRatio;
			fixed3 _AtmosColor;

			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 col = tex2D(_MainTex, i.uv);
				// 计算透射率
				fixed transmissivity = tex2D(_TransmissTex,i.uv).r;//1 - _OriginTransmissivityRatio*tex2D(_DarkChannelTex,i.uv).r;
				// Origin = (Haze - HazeColor)/Transmissivity + HazeColor
				col.rgb = (col.rgb - _AtmosColor.rgb)/max(_TransmissionThreshold, transmissivity)+_AtmosColor.rgb;
				return col;
			}
			ENDCG
		}


		
	}
}
