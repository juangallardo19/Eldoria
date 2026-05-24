Shader "Eldoria/ScreenColorEffect"
{
    Properties
    {
        _MainTex   ("Texture",    2D)    = "white" {}
        _Brightness("Brightness", Float) = 0
        _Contrast  ("Contrast",   Float) = 0
        _Saturation("Saturation", Float) = 0
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float     _Brightness;
            float     _Contrast;
            float     _Saturation;

            // Luminancia percibida (Rec.709)
            static const fixed3 LUM = fixed3(0.2126, 0.7152, 0.0722);

            fixed4 frag(v2f_img i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);

                // Brillo: desplaza el rango completo  (-0.5 → 0 → +0.5)
                col.rgb = col.rgb + _Brightness;

                // Contraste: escala alrededor del punto medio
                col.rgb = (col.rgb - 0.5) * (_Contrast + 1.0) + 0.5;

                // Saturación: interpola entre gris luminante y color original
                float lum = dot(col.rgb, LUM);
                col.rgb = lerp(fixed3(lum, lum, lum), col.rgb, _Saturation + 1.0);

                col.rgb = saturate(col.rgb);
                return col;
            }
            ENDCG
        }
    }
}
