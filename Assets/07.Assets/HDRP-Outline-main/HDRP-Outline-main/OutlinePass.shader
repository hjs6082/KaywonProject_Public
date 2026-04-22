Shader "Hidden/Shader/OutlinePass"
{
    HLSLINCLUDE

    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/RenderPass/CustomPass/CustomPassCommon.hlsl"

    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/NormalBuffer.hlsl"
    #pragma vertex Vert

    TEXTURE2D_X(OutlineBuffer);
    TEXTURE2D_X(OutlineDepthBuffer);
    float4 OutlineColor;
    float2 TexelSize;
    float Threshold;
    int Thickness;
    float OutlineIntensity;

    #define v2 1.41421
    #define c45 0.707107
    #define c225 0.9238795
    #define s225 0.3826834

    #define MaxSamples 8
    // Neighbour pixel positions
    static float2 SamplePoints[MaxSamples] =
    {
        float2( 1,  1),
        float2( 0,  1),
        float2(-1,  1),
        float2(-1,  0),
        float2(-1, -1),
        float2( 0, -1),
        float2( 1, -1),
        float2( 1, 0),
    };

    static float2 BlurPoints[5] =
    {
        float2(1,0),
        float2(0,1),
        float2(1,0),
        float2(0,1),
        float2(1,1)
    };

    float GaussSamples[32];

    float SampleTexture(float2 UV)
    {
        return SAMPLE_TEXTURE2D_X_LOD(OutlineBuffer,s_linear_clamp_sampler,UV,0);
    }

    float4 FullScreenPass(Varyings Input) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(Varyings);

        float2 UV = Input.positionCS * _ScreenSize.zw * _RTHandleScale.xy;
        float4 Outline = SAMPLE_TEXTURE2D_X_LOD(OutlineBuffer, s_linear_clamp_sampler, UV, 0);
        Outline.a = 0;

        if (Luminance(Outline.rgb) < Threshold)
        {
            for (int i = 0; i < MaxSamples; i++)
            {
                for(int j = 1; j <= Thickness; j++)
                {

                    float2 UVN = UV + _ScreenSize.zw * _RTHandleScale.xy * SamplePoints[i] * j;
                    float4 Neighbour = SAMPLE_TEXTURE2D_X_LOD(OutlineBuffer, s_linear_clamp_sampler, UVN, 0);

                    
                    if (Luminance(Neighbour) > Threshold)
                    {
                        // 아웃라인 소스(이웃 픽셀)의 깊이와 현재 씬 깊이를 비교
                        // 씬에서 아웃라인 소스보다 앞에 뭔가 있으면 (캐릭터 등) 그리지 않음
                        float outlineSourceDepth = SAMPLE_TEXTURE2D_X_LOD(OutlineDepthBuffer, s_point_clamp_sampler, UVN, 0).r;
                        float sceneDepth = LoadCameraDepth(Input.positionCS.xy);

                        // HDRP는 Reversed-Z: 가까울수록 값이 큼
                        // sceneDepth > outlineSourceDepth = 씬이 아웃라인 소스보다 앞 = 캐릭터가 막고 있음
                        if (sceneDepth <= outlineSourceDepth + 0.0001)
                        {
                            Outline.rgb = OutlineColor.rgb;
                            Outline.a = 1;
                        }
                        break;
                    }
                }
            }
        }

        return Outline;
    }

    ENDHLSL

    SubShader
    {
        Pass
        {
            Name "Custom Pass 0"

            ZWrite Off
            ZTest Always
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off

            HLSLPROGRAM
                #pragma fragment FullScreenPass
            ENDHLSL
        }
    }
    Fallback Off
}
