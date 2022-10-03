Shader "Custom/Disssk"
{
    Properties
    {
        _Tint("Tint", Color) = (0.5, 0.5, 0.5, 1)
        _PointSize("Point Size", Float) = 0.05
        _CircleResolution("Circle Resolution (2n-pointed circle)", Range (0, 36)) = 36
        _ReductionRatio("Reduction Ratio", Range (0, 1)) = 0.5
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
        }
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            // Uniforms
            half4 _Tint;
            half _PointSize;
            half _ReductionRatio;
            half _CircleResolution;
            float4x4 _MATRIX_M;

            struct Point
            {
                float3 position;
                float4 color;
            };

            StructuredBuffer<Point> _PointBuffer;

            // Hash function from H. Schechter & R. Bridson, goo.gl/RXiKaH
            uint Hash(uint s)
            {
                s ^= 2747636419u;
                s *= 2654435769u;
                s ^= s >> 16;
                s *= 2654435769u;
                s ^= s >> 16;
                s *= 2654435769u;
                return s;
            }

            // Random number (0-1)
            float Random(uint seed)
            {
                return float(Hash(seed)) / 4294967295.0; // 2^32-1
            }

            // Random point on unit sphere
            float3 RandomPoint(uint seed)
            {
                float u = Random(seed * 2 + 0) * UNITY_PI * 2;
                float z = Random(seed * 2 + 1) * 2 - 1;
                return float3(float2(cos(u), sin(u)) * sqrt(1 - z * z), z);
            }

            struct appdata
            {
                uint vertexID : SV_VertexID;
                float4 position : POSITION;
                half3 color : COLOR;
            };

            struct v2f
            {
                float4 position : SV_POSITION;
                float3 world_pos : W_POSITION;
                half3 color : COLOR;

                UNITY_FOG_COORDS(0)
            };

            v2f vert(appdata v)
            {
                v2f o;


                Point p = _PointBuffer[v.vertexID];

                o.world_pos = mul(_MATRIX_M, float4(p.position.xyz, 1.));
                o.position = UnityObjectToClipPos(o.world_pos);
                o.color = p.color;
                UNITY_TRANSFER_FOG(o, o.position);
                return o;
            }

            float random3(float3 pos)
            {
                return frac(sin(dot(pos, float3(12.9898, 78.233, 213.3499))) * 43758.5453123);
            }

            [maxvertexcount(36)]
            void geom(point v2f input[1], inout TriangleStream<v2f> outStream)
            {
                const float4 origin = input[0].position;
                const float2 p = origin.xy / origin.w;
                const float radius_from_center = p.x * p.x + p.y * p.y;
                const float3 world_pos = input[0].world_pos;
                float prob = random3(world_pos);
                prob += max((.8 - min(radius_from_center, 1)), 0); // offset for center of screen
                if (min(prob, 1) > _ReductionRatio)
                {
                    float2 extent = abs(UNITY_MATRIX_P._11_22 * _PointSize);

                    // Copy the basic information.
                    v2f o = input[0];

                    // Determine the number of slices based on the radius of the
                    // point on the screen.
                    const float radius = extent.y / origin.w * _ScreenParams.y;
                    const uint slices = min((radius + 1) / 5, 4) + 2;
                    // uint slices = _CircleResolution;

                    // Slightly enlarge quad points to compensate area reduction.
                    // Hopefully this line would be complied without branch.
                    if (slices == 2) extent *= 1.2;

                    // Top vertex
                    o.position.y = origin.y + extent.y;
                    o.position.xzw = origin.xzw;
                    outStream.Append(o);

                    UNITY_LOOP for (uint i = 1; i < slices; i++)
                    {
                        float sn, cs;
                        sincos(UNITY_PI / slices * i, sn, cs);

                        // Right side vertex
                        o.position.xy = origin.xy + extent * float2(sn, cs);
                        outStream.Append(o);

                        // Left side vertex
                        o.position.x = origin.x - extent.x * sn;
                        outStream.Append(o);
                    }

                    // Bottom vertex
                    o.position.x = origin.x;
                    o.position.y = origin.y - extent.y;

                    outStream.Append(o);

                    outStream.RestartStrip();
                }
            }

            fixed4 frag(v2f i) : SV_Target
            {
                half4 c = half4(i.color, _Tint.a);

                UNITY_APPLY_FOG(input.fogCoord, c);
                return c;
            }
            ENDCG
        }
    }
}