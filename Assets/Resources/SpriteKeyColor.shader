// 키 색상 배경에서 반투명 가장자리 잔상 제거용 셰이더
// clip() 으로 알파가 낮은 픽셀을 폐기 → 배경(키 색상)이 그대로 보임 → 투명 처리됨
Shader "Custom/SpriteKeyColor"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        [PerRendererData] _Color   ("Tint",           Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags
        {
            "Queue"           = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType"      = "Transparent"
            "PreviewType"     = "Plane"
            "CanUseSpriteAtlas" = "True"
        }
        Cull     Off
        Lighting Off
        ZWrite   Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color  : COLOR;
                float2 uv     : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color  : COLOR;
                float2 uv     : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4    _Color;

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv     = v.uv;
                o.color  = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, i.uv) * i.color;
                // 알파 50% 미만 픽셀 폐기
                // 0.01 임계값으로는 가장자리 반투명 픽셀(0.05~0.5)이 살아남아
                // 키 색상(녹색)과 블렌딩 → 초록색 잔상 발생
                // 0.5 로 높이면 가장자리가 선명해지는 대신 잔상 완전 제거
                clip(c.a - 0.5);
                // 살아남은 픽셀을 완전 불투명으로 강제
                // → Blend 수식에서 배경(키 색상)이 섞이지 않음 → 잔상 원천 차단
                c.a = 1.0;
                return c;
            }
            ENDCG
        }
    }
}
