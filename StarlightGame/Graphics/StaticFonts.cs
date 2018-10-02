using System;
using StarlightEngine.Graphics.Fonts;

namespace StarlightGame.Graphics{
    public class StaticFonts{
        // Typeface: Arial
        static AngelcodeFont m_font_arial;
        public static AngelcodeFont Font_Arial{
            get{
                if (m_font_arial == null){
                    CreateArialFont();
                }
                return m_font_arial;
            }
        }
        private static void CreateArialFont(){
            m_font_arial = AngelcodeFontLoader.LoadFile("./assets/Arial.fnt");
        }
    }
}