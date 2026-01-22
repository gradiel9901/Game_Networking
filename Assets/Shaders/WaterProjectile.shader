Shader "Custom/WaterProjectile"
{
    Properties
    {
        _MainColor ("Water Color", Color) = (0.2, 0.8, 1, 0.6)
        _FresnelColor ("Fresnel Color", Color) = (1, 1, 1, 1)
        _FresnelPower ("Fresnel Power", Range(0.1, 10)) = 2.0
        
        [Header(Wobble Settings)]
        _WobbleSpeed ("Wobble Speed", Float) = 2.0
        _WobbleAmplitude ("Wobble Amplitude", Float) = 0.1
        _WobbleFrequency ("Wobble Frequency", Float) = 10.0
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
        Cull Back

        Pass
        {
            Name "Unlit"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            // Includes
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // Structs
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : NORMAL;
                float3 viewDirWS : TEXCOORD1;
            };

            // Properties
            CBUFFER_START(UnityPerMaterial)
                float4 _MainColor;
                float4 _FresnelColor;
                float _FresnelPower;
                float _WobbleSpeed;
                float _WobbleAmplitude;
                float _WobbleFrequency;
            CBUFFER_END

            // Vertex Shader
            Varyings vert (Attributes input)
            {
                Varyings output;

                // Vertex Wobble Animation
                float3 pos = input.positionOS.xyz;
                
                // Simple sine wave displacement based on time and position
                float time = _Time.y * _WobbleSpeed;
                float wobble = sin(pos.x * _WobbleFrequency + time) * 
                               cos(pos.z * _WobbleFrequency + time) * 
                               _WobbleAmplitude;
                
                pos += input.normalOS * wobble;

                // Transform positions
                VertexPositionInputs vertexInput = GetVertexPositionInputs(pos);
                output.positionCS = vertexInput.positionCS;
                
                // Transform Normal to World Space
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);
                output.normalWS = normalInput.normalWS;

                // Calculate View Direction in World Space
                output.viewDirWS = GetWorldSpaceViewDir(vertexInput.positionWS);

                return output;
            }

            // Fragment Shader
            half4 frag (Varyings input) : SV_Target
            {
                // Normalize inputs
                float3 normal = normalize(input.normalWS);
                float3 viewDir = normalize(input.viewDirWS);

                // Fresnel Effect
                // Dot product of Normal and ViewDir gives us the "facing ratio"
                // 1 = facing camera (center), 0 = perpendicular (edge)
                float NdotV = saturate(dot(normal, viewDir));
                
                // Invert NdotV so 1 is at the edge
                float fresnelFactor = pow(1.0 - NdotV, _FresnelPower);

                // Mix Main Color with Fresnel Color
                half3 finalColor = lerp(_MainColor.rgb, _FresnelColor.rgb, fresnelFactor);
                
                // Opacity: Base alpha increased by fresnel strength at edges
                half alpha = saturate(_MainColor.a + fresnelFactor);

                return half4(finalColor, alpha);
            }
            ENDHLSL
        }
    }
}
