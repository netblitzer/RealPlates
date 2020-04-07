// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Unlit/BasicHeightmapShader"
{
    Properties
    {
        [NoScaleOffset] _MainTex ("Texture", 2D) = "white" {}
		// Color property for material inspector, default to white
		_Color("Main Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

			// vertex shader inputs
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
				fixed4 color : COLOR;
            };

			// vertex shader outputs ("vertex to fragment")
            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

			// vertex shader
			v2f vert(appdata v)
			{
				v2f o;
				// transform position to clip space
				// (multiply with model*view*projection matrix)

				float heightMult = (12.5 * v.color.y * (1 - (21 * v.color.x / 50)) / 70) - (2.5 * (1 + (0.1 * v.color.x)));
				heightMult = 1 + heightMult / 10.0;

				o.vertex = UnityObjectToClipPos(v.vertex * heightMult);
				// just pass the texture coordinate
				o.uv = v.uv;
				return o;
			}

			// texture we will sample
			sampler2D _MainTex;

			// color from the material
			fixed4 _Color;

			// pixel shader; returns low precision ("fixed4" type)
			// color ("SV_Target" semantic)
			fixed4 frag(v2f i) : SV_Target
			{
				// sample texture and return it
				//fixed4 col = tex2D(_MainTex, i.uv);
				//return col;

				return _Color; // just return it
			}
			ENDCG
        }
    }
}
