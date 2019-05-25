//
// I18N.CJK.DbcsEncoding
//
// Author:
//   Alan Tam Siu Lung (Tam@SiuLung.com)
//

using System;
using System.Text;

namespace Portable.Text
{
	abstract class DbcsEncoding : MonoEncoding
	{
		protected DbcsEncoding (int codePage) : this (codePage, 0)
		{
		}

		protected DbcsEncoding (int codePage, int windowsCodePage)
			: base (codePage, windowsCodePage)
		{
		}

		internal abstract DbcsConvert GetConvert ();

		// Get the number of bytes needed to encode a character buffer.
		public override int GetByteCount (char[] chars, int index, int count)
		{
			if (chars == null)
				throw new ArgumentNullException ("chars");

			if (index < 0 || index > chars.Length)
				throw new ArgumentOutOfRangeException ("index");

			if (count < 0 || index + count > chars.Length)
				throw new ArgumentOutOfRangeException ("count");

			byte[] buffer = new byte[count * 2];

			return GetBytes(chars, index, count, buffer, 0);
		}
		
		// Get the number of characters needed to decode a byte buffer.
		public override int GetCharCount (byte[] bytes, int index, int count)
		{
			if (bytes == null)
				throw new ArgumentNullException ("bytes");

			if (index < 0 || index > bytes.Length)
				throw new ArgumentOutOfRangeException ("index");

			if (count < 0 || index + count > bytes.Length)
				throw new ArgumentOutOfRangeException ("count");

			char[] buffer = new char[count];

			return GetChars(bytes, index, count, buffer, 0);
		}
		
		// Get the characters that result from decoding a byte buffer.
		public override int GetChars (byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
		{
			if (bytes == null)
				throw new ArgumentNullException ("bytes");

			if (chars == null)
				throw new ArgumentNullException ("chars");

			if (byteIndex < 0 || byteIndex > bytes.Length)
				throw new ArgumentOutOfRangeException ("byteIndex");

			if (byteCount < 0 || byteIndex + byteCount > bytes.Length)
				throw new ArgumentOutOfRangeException ("byteCount");

			if (charIndex < 0 || charIndex > chars.Length)
				throw new ArgumentOutOfRangeException ("charIndex");

			return 0; // For subclasses to implement
		}
		
		// Get the maximum number of bytes needed to encode a
		// specified number of characters.
		public override int GetMaxByteCount (int charCount)
		{
			if (charCount < 0)
				throw new ArgumentOutOfRangeException ("charCount");

			return charCount * 2;
		}
		
		// Get the maximum number of characters needed to decode a
		// specified number of bytes.
		public override int GetMaxCharCount (int byteCount)
		{
			if (byteCount < 0)
				throw new ArgumentOutOfRangeException ("byteCount");

			return byteCount;
		}
		
		// Determine if this encoding can be displayed in a Web browser.
		public override bool IsBrowserDisplay {
			get { return true; }
		}
		
		// Determine if this encoding can be saved from a Web browser.
		public override bool IsBrowserSave {
			get { return true; }
		}
		
		// Determine if this encoding can be displayed in a mail/news agent.
		public override bool IsMailNewsDisplay {
			get { return true; }
		}
		
		// Determine if this encoding can be saved from a mail/news agent.
		public override bool IsMailNewsSave {
			get { return true; }
		}
		
		// Decoder that handles a rolling state.
		internal abstract class DbcsDecoder : Decoder
		{
			protected DbcsConvert convert;
			
			// Constructor.
			public DbcsDecoder (DbcsConvert convert)
			{
				this.convert = convert;
			}
			
			internal void CheckRange (byte[] bytes, int index, int count)
			{
				if (bytes == null)
					throw new ArgumentNullException ("bytes");

				if (index < 0 || index > bytes.Length)
					throw new ArgumentOutOfRangeException ("index");

				if (count < 0 || count > (bytes.Length - index))
					throw new ArgumentOutOfRangeException ("count");
			}

			internal void CheckRange (byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
			{
				if (bytes == null)
					throw new ArgumentNullException ("bytes");

				if (chars == null)
					throw new ArgumentNullException ("chars");

				if (byteIndex < 0 || byteIndex > bytes.Length)
					throw new ArgumentOutOfRangeException ("byteIndex");

				if (byteCount < 0 || byteIndex + byteCount > bytes.Length)
					throw new ArgumentOutOfRangeException ("byteCount");

				if (charIndex < 0 || charIndex > chars.Length)
					throw new ArgumentOutOfRangeException ("charIndex");
			}
		}
	}
}
