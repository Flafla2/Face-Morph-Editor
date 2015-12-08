Shader "Custom/SubsurfaceScattering" {
	Properties{
		_MainTex("Albedo (RGB)", 2D) = "white" {}
		_Normal("Normal Map", 2D) = "bump" {}
		_Specular("Specular (RGB), Smoothness (A)", 2D) = "black" {}
		_Occulusion("Occlusion (RGB)", 2D) = "white" {}
	}

	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
		
		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf StandardSpecular fullforwardshadows

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		sampler2D _MainTex;
		sampler2D _Normal;
		sampler2D _Specular;
		sampler2D _Occlusion;

		struct Input {
			float2 uv_MainTex;
			float3 viewDir;
		};

		void surf (Input IN, inout SurfaceOutputStandardSpecular o) {
			fixed4 a = tex2D (_MainTex, IN.uv_MainTex);
			fixed4 s = tex2D (_Specular, IN.uv_MainTex);
			fixed4 c = tex2D (_Occlusion, IN.uv_MainTex);
			fixed3 n = UnpackNormal (tex2D (_Normal, IN.uv_MainTex));
			o.Albedo = a.rgb;
			o.Specular = s.rgb;
			o.Normal = n;
			o.Smoothness = s.a;
			o.Occlusion = c.r;
			o.Alpha = 1;
		}
		ENDCG
	} 
	FallBack "Diffuse"
}
