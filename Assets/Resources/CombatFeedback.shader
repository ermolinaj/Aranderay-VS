Shader "Aranderay/Combat Feedback"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _RimWidth ("Sprite Rim in Pixels", Float) = 0
        [HideInInspector] _RendererColor ("Renderer Color", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="True" }
        Cull Off
        ZWrite Off
        ZTest LEqual
        Blend One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex SpriteVert
            #pragma fragment FeedbackFrag
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "UnitySprites.cginc"

            float _RimWidth;

            fixed4 FeedbackFrag(v2f input) : SV_Target
            {
                fixed4 tint = input.color;
                if (_RimWidth > 0)
                {
                    float2 uv = input.texcoord;
                    float2 dx = ddx(uv) * _RimWidth;
                    float2 dy = ddy(uv) * _RimWidth;
                    fixed alpha = SampleSpriteTexture(uv).a;
                    fixed interior = min(SampleSpriteTexture(uv - dx).a, SampleSpriteTexture(uv + dx).a);
                    interior = min(interior, min(SampleSpriteTexture(uv - dy).a, SampleSpriteTexture(uv + dy).a));
                    float2 diagonal = (dx + dy) * 0.7071;
                    interior = min(interior, min(SampleSpriteTexture(uv - diagonal).a, SampleSpriteTexture(uv + diagonal).a));
                    diagonal = (dx - dy) * 0.7071;
                    interior = min(interior, min(SampleSpriteTexture(uv - diagonal).a, SampleSpriteTexture(uv + diagonal).a));
                    tint.a *= saturate((alpha - interior) * 1.4 + alpha * 0.065);
                }
                return fixed4(tint.rgb * tint.a, tint.a);
            }
            ENDCG
        }
    }
}
