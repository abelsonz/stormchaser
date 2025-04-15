Shader "Custom/DoubleSidedUnlit"
{
  Properties
  {
    _Color("Color", Color) = (1,1,1,1)
    _MainTex ("Albedo (RGB)", 2D) = "white" {}
    _Glossiness ("Smoothness", Range(0,1)) = 0.5
    _Metallic ("Metallic", Range(0,1)) = 0.0
  }
  SubShader
  {
    Tags { "RenderType"="Opaque" }
    Cull Off           // ← draw both front & back faces
    Lighting Off       // ← ignore all lights
    ZWrite On

    Pass
    {
      CGPROGRAM
      #pragma vertex vert
      #pragma fragment frag
      #include "UnityCG.cginc"

      fixed4 _Color;

      struct appdata { float4 vertex : POSITION; };
      struct v2f { float4 pos : SV_POSITION; };

      v2f vert(appdata v)
      {
        v2f o;
        o.pos = UnityObjectToClipPos(v.vertex);
        return o;
      }

      fixed4 frag(v2f i) : SV_Target
      {
        return _Color;
      }
      ENDCG
    }
  }
}
