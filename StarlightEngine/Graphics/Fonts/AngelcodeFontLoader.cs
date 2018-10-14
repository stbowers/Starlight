using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using StarlightEngine.Graphics.Math;

namespace StarlightEngine.Graphics.Fonts
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

		public AngelcodeKerning[] kernings;
        public Dictionary<int, AngelcodeGlyph> glyphs = new Dictionary<int, AngelcodeGlyph>();
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

        public AngelcodeGlyph GetGlyph(int charID)
        {
            foreach (AngelcodePage page in pages)
            {
                if (page.glyphs.ContainsKey(charID))
                {
                    return page.glyphs[charID];
                }
            }

            return null;
        }

        public AngelcodeKerning GetKerning(int first, int second)
        {
            foreach (AngelcodePage page in pages)
            {
                foreach (AngelcodeKerning kerning in page.kernings)
                {
                    if (kerning.first == first && kerning.second == second)
                    {
                        return kerning;
                    }
                }
            }

            return null;
        }
	}

    public class TextMesh
    {
        public byte[] meshBufferData;
        public int vboOffset;
        public int vboSize;
        public int iboOffset;
        public int iboSize;
        public int numVertices;
    }

	public static class AngelcodeFontLoader
	{
		public static AngelcodeFont LoadFile(string fontFile)
		{
			StreamReader reader = File.OpenText(fontFile);
			string line;

			AngelcodeFont newFont = new AngelcodeFont();

            int pageIndex = 0;
            int kerningIndex = 0;
			while ((line = reader.ReadLine()) != null)
			{
				string[] words = line.Split(' ');

				// switch on first word, to identify the type of data to load
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
                        newFont.pages[pageIndex] = new AngelcodePage();
						newFont.pages[pageIndex].file = words[2].Split('"')[1];
						break;
					case "chars":
						newFont.pages[pageIndex].charsCount = int.Parse(words[1].Split('=')[1]);
						break;
					case "char":
                        AngelcodeGlyph glyph = new AngelcodeGlyph();

                        for (int i = 0; i < words.Length; i++)
                        {
                            if (words[i] == "")
                            {
                                continue;
                            }

                            string[] parts = words[i].Split('=');
                            switch (parts[0])
                            {
                                case "id":
                                    glyph.charID = int.Parse(parts[1]);
                                    break;
                                case "x":
                                    glyph.x = int.Parse(parts[1]);
                                    break;
                                case "y":
                                    glyph.y = int.Parse(parts[1]);
                                    break;
                                case "width":
                                    glyph.width = int.Parse(parts[1]);
                                    break;
                                case "height":
                                    glyph.height = int.Parse(parts[1]);
                                    break;
                                case "xoffset":
                                    glyph.xoffset = int.Parse(parts[1]);
                                    break;
                                case "yoffset":
                                    glyph.yoffset = int.Parse(parts[1]);
                                    break;
                                case "xadvance":
                                    glyph.xadvance = int.Parse(parts[1]);
                                    break;
                                case "page":
                                    glyph.page = int.Parse(parts[1]);
                                    break;
                                case "channel":
                                    glyph.channel = int.Parse(parts[1]);
                                    break;
                            }
                        }
                        newFont.pages[glyph.page].glyphs[glyph.charID] = glyph;

						break;
					case "kernings":
						newFont.pages[pageIndex].kerningsCount = int.Parse(words[1].Split('=')[1]);

						// create kernings array
						newFont.pages[pageIndex].kernings = new AngelcodeKerning[newFont.pages[pageIndex].kerningsCount];

						// reset index
						kerningIndex = 0;
						break;
					case "kerning":
						AngelcodeKerning kerning = (newFont.pages[pageIndex].kernings[kerningIndex] = new AngelcodeKerning());

						kerning.first = int.Parse(words[1].Split('=')[1]);
						kerning.second = int.Parse(words[2].Split('=')[1]);
						kerning.amount = int.Parse(words[3].Split('=')[1]);

						kerningIndex++;
						break;
				}
			}

			return newFont;
		}

        // Make a mesh out of a given string of text
        // The resulting mesh will start at (0, 0), expanding down and to the right
        public static TextMesh CreateTextMesh(AngelcodeFont font, int size, string text, FVec2 offset, float width)
        {
            TextMesh newTextMesh = new TextMesh();

            // list of vertices for the mesh (posX, posY, textureX, textureY)
            List<FVec4> verts = new List<FVec4>();
            List<int> indices = new List<int>();

            // calculate scaling factors
            float scaleX = 1 / ((float)font.size / (float)size);
            float scaleY = 1 / ((float)font.size / (float)size);

            // padding constants
            float yPadding = scaleY * (float)font.padding[0];
            float xPadding = scaleX * (float)font.padding[1];

            // keep track of where to put new characters
            FVec2 cursor = new FVec2(offset.X() + xPadding, offset.Y() + (scaleY * font.@base) - yPadding);

            // loop through words, adding them to the mesh one at a time
            int lineWordCount = 0;
            foreach (string word in text.Split(' '))
            {
                // If this is the first word of the line add it without checking the length
                if (lineWordCount == 0)
                {
                    AddWordToMesh(font, scaleX, scaleY, word, ref cursor, ref verts, ref indices);
                    lineWordCount++;
                    continue;
                }

                // Else check if there is enough space for the word, and if not move the cursor to the next line
                float wordWidth = GetWidthOfString(font, size, word);
                if (cursor.X() + wordWidth > width)
                {
                    // move cursor to next line by resting x and adding lineHeight to it
                    cursor.SetX(xPadding);
                    cursor.SetY(cursor.Y() + (scaleY * font.lineHeight));
                    cursor.SetY(cursor.Y() - yPadding);

                    // reset word count
                    lineWordCount = 0;
                }

                // if this is not the first word of the line add a space
                if (lineWordCount > 0)
                {
                    cursor.SetX(cursor.X() + (scaleX * font.GetGlyph(Encoding.ASCII.GetBytes(" ")[0]).xadvance));
                }

                // add the word
                AddWordToMesh(font, scaleX, scaleY, word, ref cursor, ref verts, ref indices);
                lineWordCount++;
            }

            // create byte array of vertex data
            newTextMesh.meshBufferData = new byte[(verts.Count * verts[0].PrimativeSizeOf) + (indices.Count * 4)];
            newTextMesh.vboOffset = 0;
            for (int i = 0; i < verts.Count; i++)
            {
                System.Buffer.BlockCopy(verts[i].Bytes, 0, newTextMesh.meshBufferData, (int)(newTextMesh.vboOffset + (i * (verts[0].PrimativeSizeOf))), (int)verts[0].PrimativeSizeOf);
                newTextMesh.vboSize += (int)verts[0].PrimativeSizeOf;
                newTextMesh.iboOffset += (int)verts[0].PrimativeSizeOf;
            }
            for (int i = 0; i < indices.Count; i++)
            {
                System.Buffer.BlockCopy(new[] { indices[i] }, 0, newTextMesh.meshBufferData, (int)(newTextMesh.iboOffset + (i * (4))), 4);
                newTextMesh.iboSize += 4;
            }
            newTextMesh.numVertices = indices.Count;

            return newTextMesh;
        }

        // get the width of the string
        public static float GetWidthOfString(AngelcodeFont font, int size, string text)
        {
            float width = 0;

			// calculate scaling factors
			float scaleX = 1 / ((float)font.size / (float)size);
			float scaleY = 1 / ((float)font.size / (float)size);

			// padding constants
			float yPadding = scaleY * (float)font.padding[0];
			float xPadding = scaleX * (float)font.padding[1];

			byte[] bytes = Encoding.ASCII.GetBytes(text);
			for (int i = 0; i < bytes.Length; i++)
			{
				byte ch = bytes[i];
				AngelcodeGlyph glyph = font.GetGlyph(ch);
				AngelcodeKerning kerning = (i == 0) ? null : font.GetKerning((int)bytes[i - 1], (int)ch);
				float paddingOffset = scaleX * (float)font.padding[0];
				float kerningOffset = scaleX * (float)((kerning == null) ? 0 : kerning.amount);

				// calculate width of char
				float charWidth = scaleX * font.GetGlyph((int)ch).width;

				// move cursor
				width += scaleX * font.GetGlyph((int)ch).xadvance;
				width -= paddingOffset - kerningOffset;
			}

			return width;
        }

		// get height of string
		public static float GetHeightOfString(AngelcodeFont font, int size, string text, float width)
		{
			// calculate scaling factors
			float scaleX = 1 / ((float)font.size / (float)size);
			float scaleY = 1 / ((float)font.size / (float)size);

			// padding constants
			float yPadding = scaleY * (float)font.padding[0];
			float xPadding = scaleX * (float)font.padding[1];

			// keep track of where to put new characters
			FVec2 cursor = new FVec2(xPadding, (scaleY * font.@base) - yPadding);

			// loop through words, adding them to the mesh one at a time
			int lineWordCount = 0;
			foreach (string word in text.Split(' '))
			{
				// If this is the first word of the line add it without checking the length
				if (lineWordCount == 0)
				{
					cursor.SetX(cursor.X() + GetWidthOfString(font, size, word));
					lineWordCount++;
					continue;
				}

				// Else check if there is enough space for the word, and if not move the cursor to the next line
				float wordWidth = GetWidthOfString(font, size, word);
				if (cursor.X() + wordWidth > width)
				{
					// move cursor to next line by resting x and adding lineHeight to it
					cursor.SetX(xPadding);
					cursor.SetY(cursor.Y() + (scaleY * font.lineHeight));
					cursor.SetY(cursor.Y() - yPadding);

					// reset word count
					lineWordCount = 0;
				}

				// if this is not the first word of the line add a space
				if (lineWordCount > 0)
				{
					cursor.SetX(cursor.X() + (scaleX * font.GetGlyph(Encoding.ASCII.GetBytes(" ")[0]).xadvance));
				}

				// add the word
				lineWordCount++;
			}

			//return cursor.Y();
			return (scaleY * font.@base) - yPadding;
		}

        private static void AddWordToMesh(AngelcodeFont font, float scaleX, float scaleY, string word, ref FVec2 cursor, ref List<FVec4> verts, ref List<int> indices)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(word);
            for (int i = 0; i < bytes.Length; i++)
            {
                byte ch = bytes[i];
                AngelcodeGlyph glyph = font.GetGlyph(ch);
                AngelcodeKerning kerning = (i == 0)? null : font.GetKerning((int)bytes[i-1], (int)ch);
                float paddingOffset = scaleX * (float)font.padding[0];
                float kerningOffset = scaleX * (float) ((kerning == null) ? 0 : kerning.amount);
                float xOffset = (scaleX * glyph.xoffset) - paddingOffset + kerningOffset;
                float yOffset = -(scaleY * (font.@base - (glyph.yoffset + glyph.height)));

                // calculate width and height of char
                float charWidth = scaleX * font.GetGlyph((int)ch).width;
                float charHeight = scaleY * font.GetGlyph((int)ch).height;

                // calculate texture positions of char
                float textureX = (float)font.GetGlyph((int)ch).x / (float)font.scaleW;
                float textureY = (float)font.GetGlyph((int)ch).y / (float)font.scaleH;
                float textureWidth = (float)font.GetGlyph((int)ch).width / (float)font.scaleW;
                float textureHeight = (float)font.GetGlyph((int)ch).height / (float)font.scaleH;

                // Calculate verticies for char
                int topLeftIndex = verts.Count;
                FVec4 topLeft = new FVec4(cursor.X() + xOffset, cursor.Y() + yOffset - charHeight, textureX, textureY);
                FVec4 topRight = new FVec4(cursor.X() + charWidth + xOffset, cursor.Y() + yOffset - charHeight, textureX + textureWidth, textureY);
                FVec4 bottomLeft = new FVec4(cursor.X() + xOffset, cursor.Y() + yOffset, textureX, textureY + textureHeight);
                FVec4 bottomRight = new FVec4(cursor.X() + charWidth + xOffset, cursor.Y() + yOffset, textureX + textureWidth, textureY + textureHeight);
                verts.Add(topLeft);
                verts.Add(topRight);
                verts.Add(bottomLeft);
                verts.Add(bottomRight);

                // add indices to list
                indices.AddRange(new[] { topLeftIndex + 0, topLeftIndex + 1, topLeftIndex + 3, topLeftIndex + 3, topLeftIndex + 2, topLeftIndex + 0 });

                // move cursor
                cursor.SetX(cursor.X() + (scaleX * font.GetGlyph((int)ch).xadvance));
                cursor.SetX(cursor.X() - (paddingOffset - kerningOffset));
            }
        }
	}
}
