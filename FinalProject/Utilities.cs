using System;
namespace FinalProject
{
	public class Utilities
	{
		// Converts a null-terminated string of bytes pointed to by pStr into a C# string
		public unsafe static string BytePointerToString(byte* pStr)
		{
			string newString = "";
			int strIndex = 0;
			char currentChar = (char)*(pStr);
			while (currentChar != 0)
			{
				newString += currentChar;
				strIndex++;
				currentChar = (char)*(pStr + strIndex);
			}
			return newString;
		}

		// Converts a string into a byte array, with a null terminator
		public static byte[] StringToByteArray(string str)
		{
			byte[] pNewString = new byte[str.Length + 1];
			System.Text.Encoding.ASCII.GetBytes(str.ToCharArray()).CopyTo(pNewString, 0);
			pNewString[str.Length] = 0;
			return pNewString;
		}

		// Make a 32 bit unsiged int representing a version number (10 bit major, 10 bit minor, 12 bit patch)
		public static uint MakeVersionNumber(int major, int minor, int patch)
		{
			return (uint)((((major << 10) + minor) << 12) + patch);
		}
	}
}
