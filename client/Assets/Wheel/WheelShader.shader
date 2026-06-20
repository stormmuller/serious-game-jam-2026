Shader "Custom/PrizeWheelURP"
{
    Properties
    {
        _Segments ("Number of Segments", Float) = 8
        _PaletteTex ("Color Palette (1D/Horizontal)", 2D) = "white" {}
        _BorderColor ("Border Color", Color) = (0, 0, 0, 1)
        _BorderWidth ("Border Width", Range(0, 0.05)) = 0.01
    }
    SubShader
    {
        Tags 
        { 
            "RenderType"="Transparent" 
            "Queue"="Transparent" 
            "RenderPipeline"="UniversalPipeline"
        }
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // Must match WheelSpinner.MaxSegments.
            #define MAX_SEGMENTS 32

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float2 uv           : TEXCOORD0;
            };

            float _Segments;
            Texture2D _PaletteTex;
            SamplerState sampler_PaletteTex;
            half4 _BorderColor;
            float _BorderWidth;
            // Cumulative normalized (0-1) angular boundary of each segment, parallel to _PaletteTex.
            // _Boundaries[i] is the angle where segment i ends; segment 0 starts at 0.
            float _Boundaries[MAX_SEGMENTS];

            Varyings vert (Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 frag (Varyings input) : SV_Target
            {
                float2 centerUV = input.uv - 0.5;
                
                float radius = length(centerUV);
                clip(0.5 - radius);

                float angle = atan2(centerUV.y, centerUV.x);
                
                float angleNormalized = (angle + 3.14159265359) / (2.0 * 3.14159265359);

                int segCount = (int)_Segments;
                int segmentIndex = max(segCount - 1, 0);
                float segStart = 0.0;
                float segEnd = 1.0;
                float prevBoundary = 0.0;

                for (int i = 0; i < MAX_SEGMENTS; i++)
                {
                    if (i >= segCount) break;

                    float boundary = _Boundaries[i];
                    if (angleNormalized < boundary)
                    {
                        segmentIndex = i;
                        segStart = prevBoundary;
                        segEnd = boundary;
                        break;
                    }
                    prevBoundary = boundary;
                }

                float uCoord = (segmentIndex + 0.5) / _Segments;

                half4 col = _PaletteTex.Sample(sampler_PaletteTex, float2(uCoord, 0.5));

                // Distance (in normalized-angle space) from the nearest divider of this segment.
                float distToBoundary = min(angleNormalized - segStart, segEnd - angleNormalized);
                float angularDist = max(distToBoundary, 0.0) * (2.0 * 3.14159265359);
                float arcDist = radius * angularDist;

                col = lerp(_BorderColor, col, step(_BorderWidth, arcDist));

                return col;
            }
            ENDHLSL
        }
    }
}