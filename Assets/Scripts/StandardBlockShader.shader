﻿Shader "Minecraft/Blocks" {

	Properties {
        
		_MainTex ("Block Texture Atlas", 2D) = "white" {}
		_Color ("Main Color", Color) = (1,1,1,1)
		
	}

	SubShader {

		Tags {"Queue"="AlphaTest" "IgnoreProjector"="True" "RenderType"="TransparentCutout"}

		LOD 100
		Lighting OFF
        
		Pass {

			CGPROGRAM
				#pragma vertex vertexFunction
				#pragma fragment fragmentFunction
				#pragma target 2.0

				#include "UnityCG.cginc"

				struct appdata {

					float4 vertex : POSITION;
					float2 uv : TEXCOORD0;
					float4 color : COLOR;

                };

				struct v2f {

					float4 vertex : SV_POSITION;
					float2 uv : TEXCOORD0;
					float4 color : COLOR;

                };

				sampler2D _MainTex;
				float GlobalLightLevel;

				v2f vertexFunction(appdata v) {

					v2f o;
					o.vertex = UnityObjectToClipPos(v.vertex);
					o.uv = v.uv;
					o.color = v.color;

					return o;

                }

				fixed4 fragmentFunction(v2f i) : SV_Target {

					fixed4 col = tex2D(_MainTex, i.uv);
					float LocalLightLevel = clamp(GlobalLightLevel + i.color.a, 0, 1);
					clip(col.a - 0.2);
					col = lerp(col, float4(0, 0, 0, 1), LocalLightLevel);

					return col;

                }

				ENDCG

		}
	}

}
