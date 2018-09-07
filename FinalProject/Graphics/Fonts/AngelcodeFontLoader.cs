using System;
using System.IO;

namespace FinalProject
{
	public class AngelcodeGlyph
	{
		public int charID;
		public int x;
		public int y;
		public int width;
		public int height;
		public int xoffset;
		public int yoffset;
		public int xadvance;
		public int page;
		public int channel;
	}

	public class AngelcodeKerning
	{
		public int first;
		public int second;
		public int amount;
	}

	public class AngelcodePage
	{
		#region pageInfo
		public int id;
		public string file;
		#endregion pageInfo

		public int charsCount;
		public int kerningsCount;

		public AngelcodeGlyph[] glyphs;
		public AngelcodeKerning[] kernings;
	}

	public class AngelcodeFont
	{
		#region info
		public string face;
		public int size;
		public bool bold;
		public bool italic;
		public string charset;
		public bool unicode;
		public int stretchH;
		public bool smooth;
		public int aa;
		public int[] padding = new int[4];
		public int[] spacing = new int[2];
		#endregion info

		#region common
		public int lineHeight;
		public int @base;
		public int scaleW;
		public int scaleH;
		public int numPages;
		public bool packed;
		#endregion

		public AngelcodePage[] pages;
	}

	public static class AngelcodeFontLoader
	{
		public static AngelcodeFont LoadFile(string fontFile)
		{
			StreamReader reader = File.OpenText(fontFile);
			string line;

			AngelcodeFont newFont = new AngelcodeFont();

			while ((line = reader.ReadLine()) != null)
			{
				string[] words = line.Split(' ');

				// switch on first word, to identify the type of data to load
				int pageIndex = 0;
				int glyphIndex = 0;
				int kerningIndex = 0;
				switch (words[0])
				{
					case "info":
						newFont.face = words[1].Split('"')[1];
						newFont.size = int.Parse(words[2].Split('=')[1]);
						newFont.bold = int.Parse(words[3].Split('=')[1]) == 0 ? false : true;
						newFont.italic = int.Parse(words[4].Split('=')[1]) == 0 ? false : true;
						newFont.charset = words[5].Split('"')[1];
						newFont.unicode= int.Parse(words[6].Split('=')[1]) == 0 ? false : true;
						newFont.stretchH = int.Parse(words[7].Split('=')[1]);
						newFont.smooth = int.Parse(words[8].Split('=')[1]) == 0 ? false : true;
						newFont.aa= int.Parse(words[9].Split('=')[1]);
						string padding = words[10].Split('=')[1];
						newFont.padding[0] = int.Parse(padding.Split(',')[0]);
						newFont.padding[1] = int.Parse(padding.Split(',')[1]);
						newFont.padding[2] = int.Parse(padding.Split(',')[2]);
						newFont.padding[3] = int.Parse(padding.Split(',')[3]);
						string spacing = words[11].Split('=')[1];
						newFont.spacing[0] = int.Parse(spacing.Split(',')[0]);
						newFont.spacing[1] = int.Parse(spacing.Split(',')[1]);
						break;
					case "common":
						newFont.lineHeight = int.Parse(words[1].Split('=')[1]);
						newFont.@base = int.Parse(words[2].Split('=')[1]);
						newFont.scaleW = int.Parse(words[3].Split('=')[1]);
						newFont.scaleH = int.Parse(words[4].Split('=')[1]);
						newFont.numPages = int.Parse(words[5].Split('=')[1]);
						newFont.packed = int.Parse(words[6].Split('=')[1]) == 0 ? false : true;

						// create pages
						newFont.pages = new AngelcodePage[newFont.numPages];
						break;
					case "page":
						pageIndex = int.Parse(words[1].Split('=')[1]);
						newFont.pages[pageIndex].file = words[2].Split('"')[1];
						break;
					case "chars":
						newFont.pages[pageIndex].charsCount = int.Parse(words[1].Split('=')[1]);

						// create glyph array
						newFont.pages[pageIndex].glyphs = new AngelcodeGlyph[newFont.pages[pageIndex].charsCount];

						// reset index
						glyphIndex = 0;
						break;
					case "char":
						AngelcodeGlyph glyph = newFont.pages[pageIndex].glyphs[glyphIndex];

						glyph.charID = int.Parse(words[1].Split('=')[1]);
						glyph.x = int.Parse(words[2].Split('=')[1]);
						glyph.y = int.Parse(words[3].Split('=')[1]);
						glyph.width = int.Parse(words[4].Split('=')[1]);
						glyph.height = int.Parse(words[5].Split('=')[1]);
						glyph.xoffset = int.Parse(words[6].Split('=')[1]);
						glyph.yoffset = int.Parse(words[7].Split('=')[1]);
						glyph.xadvance = int.Parse(words[8].Split('=')[1]);
						glyph.page = int.Parse(words[9].Split('=')[1]);
						glyph.channel = int.Parse(words[10].Split('=')[1]);

						glyphIndex++;
						break;
					case "kernings":
						newFont.pages[pageIndex].kerningsCount = int.Parse(words[1].Split('=')[1]);

						// create kernings array
						newFont.pages[pageIndex].kernings = new AngelcodeKerning[newFont.pages[pageIndex].kerningsCount];

						// reset index
						kerningIndex = 0;
						break;
					case "kerning":
						AngelcodeKerning kerning = newFont.pages[pageIndex].kernings[kerningIndex];

						kerning.first = int.Parse(words[1].Split('=')[1]);
						kerning.second = int.Parse(words[2].Split('=')[1]);
						kerning.amount = int.Parse(words[3].Split('=')[1]);

						kerningIndex++;
						break;
				}
			}

			return newFont;
		}
	}
}
