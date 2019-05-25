//
// I18N.CJK.CP949
//
// Author:
//   Hye-Shik Chang (perky@FreeBSD.org)
//   Atsushi Enomoto  <atsushi@ximian.com>
//

using System;

namespace Portable.Text {
	class CP949 : KoreanEncoding
	{
		// Magic number used by Windows for the UHC code page.
		const int UHC_CODE_PAGE = 949;

		// Constructor.
		public CP949 () : base (UHC_CODE_PAGE, true)
		{
		}

		// Get the mail body name for this encoding.
		public override string BodyName {
			get { return "ks_c_5601-1987"; }
		}

		// Get the human-readable name for this encoding.
		public override string EncodingName {
			get { return "Korean (UHC)"; }
		}

		// Get the mail agent header name for this encoding.
		public override string HeaderName {
			get { return "ks_c_5601-1987"; }
		}

		// Get the IANA-preferred Web name for this encoding.
		public override string WebName {
			get { return "ks_c_5601-1987"; }
		}

		/*
        // Get the Windows code page represented by this object.
        public override int WindowsCodePage
        {
            get { return UHC_PAGE; }
        }
        */
	}

	class CP51949 : KoreanEncoding
	{
		// Magic number used by Windows for the euc-kr code page.
		const int EUCKR_CODE_PAGE = 51949;

		// Constructor.
		public CP51949 () : base (EUCKR_CODE_PAGE, false)
		{
		}

		// Get the mail body name for this encoding.
		public override string BodyName {
			get { return "euc-kr"; }
		}

		// Get the human-readable name for this encoding.
		public override string EncodingName {
			get { return "Korean (EUC)"; }
		}

		// Get the mail agent header name for this encoding.
		public override string HeaderName {
			get { return "euc-kr"; }
		}

		// Get the IANA-preferred Web name for this encoding.
		public override string WebName {
			get { return "euc-kr"; }
		}

		/*
        // Get the Windows code page represented by this object.
        public override int WindowsCodePage
        {
            get { return UHC_PAGE; }
        }
        */
	}

	class KoreanEncoding : DbcsEncoding
	{
		readonly bool useUHC;

		// Constructor.
		public KoreanEncoding (int codepage, bool useUHC) : base (codepage, 949)
		{
			this.useUHC = useUHC;
		}

		internal override DbcsConvert GetConvert ()
		{
			return DbcsConvert.KS;
		}
		// Get the bytes that result from encoding a character buffer.
		public unsafe override int GetByteCountImpl (char* chars, int charCount)
		{
			DbcsConvert convert = GetConvert ();
			int end = charCount;
			int length = 0;

			// 00 00 - FF FF
			for (int i = 0; i < end; i++, charCount--) {
				char c = chars [i];
				if (c <= 0x80 || c == 0xFF) { // ASCII
					length++;
					continue;
				}
				byte b1 = convert.u2n [((int)c) * 2];
				byte b2 = convert.u2n [((int)c) * 2 + 1];
				if (b1 == 0 && b2 == 0) {
					// FIXME: handle fallback for GetByteCountImpl().
					length++;
				} else {
					length += 2;
				}
			}
			return length;
		}

		// Get the bytes that result from encoding a character buffer.
		public unsafe override int GetBytesImpl (char* chars, int charCount, byte* bytes, int byteCount)
		{
			EncoderFallbackBuffer buffer = null;
			DbcsConvert convert = GetConvert ();
			int end = charCount;
			int charIndex = 0;
			int byteIndex = 0;

			// 00 00 - FF FF
			int origIndex = byteIndex;
			for (int i = charIndex; i < end; i++, charCount--) {
				char c = chars [i];
				if (c <= 0x80 || c == 0xFF) { // ASCII
					bytes [byteIndex++] = (byte)c;
					continue;
				}

				byte b1 = convert.u2n [((int)c) * 2];
				byte b2 = convert.u2n [((int)c) * 2 + 1];

				if (b1 == 0 && b2 == 0) {
					HandleFallback (ref buffer, chars, ref i, ref charCount, bytes, ref byteIndex, ref byteCount, null);
				} else {
					bytes [byteIndex++] = b1;
					bytes [byteIndex++] = b2;
				}
			}

			return byteIndex - origIndex;
		}

		// Get the characters that result from decoding a byte buffer.
		public override int GetCharCount (byte[] bytes, int index, int count)
		{
			return GetDecoder ().GetCharCount (bytes, index, count);
		}

		// Get the characters that result from decoding a byte buffer.
		public override int GetChars (byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
		{
			return GetDecoder ().GetChars (bytes, byteIndex, byteCount, chars, charIndex);
		}

		// Get a decoder that handles a rolling UHC state.
		public override
		#if STANDALONE
		Decoder
		#else
		System.Text.Decoder
		#endif
		GetDecoder ()
		{
			return new KoreanDecoder (GetConvert (), useUHC);
		}

		// Decoder that handles a rolling UHC state.
		sealed class KoreanDecoder : DbcsDecoder
		{
			int last_byte_count, last_byte_conv;
			readonly bool useUHC;

			// Constructor.
			public KoreanDecoder (DbcsConvert convert, bool useUHC) : base (convert)
			{
				this.useUHC = useUHC;
			}

			public override int GetCharCount (byte[] bytes, int index, int count)
			{
				return GetCharCount (bytes, index, count, false);
			}

			public override int GetCharCount (byte[] bytes, int index, int count, bool flush)
			{
				CheckRange (bytes, index, count);

				int lastByte = last_byte_count;
				last_byte_count = 0;
				int length = 0;

				while (count-- > 0) {
					int b = bytes [index++];
					if (lastByte == 0) {
						if (b <= 0x80 || b == 0xFF) { // ASCII
							length++;
							continue;
						} else {
							lastByte = b;
							continue;
						}
					}

					char c1;
					if (useUHC && lastByte < 0xa1) { // UHC Level 1
						int ord = 8836 + (lastByte - 0x81) * 178;

						if (b >= 0x41 && b <= 0x5A)
							ord += b - 0x41;
						else if (b >= 0x61 && b <= 0x7A)
							ord += b - 0x61 + 26;
						else if (b >= 0x81 && b <= 0xFE)
							ord += b - 0x81 + 52;
						else
							ord = -1;

						if (ord >= 0 && ord * 2 <= convert.n2u.Length)
							c1 = (char)(convert.n2u [ord * 2] +
							convert.n2u [ord * 2 + 1] * 256);
						else
							c1 = (char)0;
					} else if (useUHC && lastByte <= 0xC6 && b < 0xA1) { // UHC Level 2
						int ord = 14532 + (lastByte - 0xA1) * 84;

						if (b >= 0x41 && b <= 0x5A)
							ord += b - 0x41;
						else if (b >= 0x61 && b <= 0x7A)
							ord += b - 0x61 + 26;
						else if (b >= 0x81 && b <= 0xA0)
							ord += b - 0x81 + 52;
						else
							ord = -1;

						if (ord >= 0 && ord * 2 <= convert.n2u.Length)
							c1 = (char)(convert.n2u [ord * 2] +
							convert.n2u [ord * 2 + 1] * 256);
						else
							c1 = (char)0;
					} else if (b >= 0xA1 && b <= 0xFE) { // KS X 1001
						int ord = ((lastByte - 0xA1) * 94 + b - 0xA1) * 2;

						c1 = ord < 0 || ord >= convert.n2u.Length ?
                            '\0' : (char)(convert.n2u [ord] +
						convert.n2u [ord + 1] * 256);
					} else
						c1 = (char)0;

					if (c1 == 0)
                        // FIXME: fallback
                        length++;
					else
						length++;
					lastByte = 0;
				}

				if (lastByte != 0) {
					if (flush) {
						// FIXME: fallback
						length++;
						last_byte_count = 0;
					} else
						last_byte_count = lastByte;
				}
				return length;
			}

			public override int GetChars (byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
			{
				return GetChars (bytes, byteIndex, byteCount, chars, charIndex, false);
			}

			public override int GetChars (byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex, bool flush)
			{
				CheckRange (bytes, byteIndex, byteCount, chars, charIndex);

				int origIndex = charIndex;
				int lastByte = last_byte_conv;
				last_byte_conv = 0;
                
				while (byteCount-- > 0) {
					int b = bytes [byteIndex++];
					if (lastByte == 0) {
						if (b <= 0x80 || b == 0xFF) { // ASCII
							chars [charIndex++] = (char)b;
							continue;
						} else {
							lastByte = b;
							continue;
						}
					}

					char c1;
					if (useUHC && lastByte < 0xa1) { // UHC Level 1
						int ord = 8836 + (lastByte - 0x81) * 178;

						if (b >= 0x41 && b <= 0x5A)
							ord += b - 0x41;
						else if (b >= 0x61 && b <= 0x7A)
							ord += b - 0x61 + 26;
						else if (b >= 0x81 && b <= 0xFE)
							ord += b - 0x81 + 52;
						else
							ord = -1;

						if (ord >= 0 && ord * 2 <= convert.n2u.Length)
							c1 = (char)(convert.n2u [ord * 2] +
							convert.n2u [ord * 2 + 1] * 256);
						else
							c1 = (char)0;
					} else if (useUHC && lastByte <= 0xC6 && b < 0xA1) { // UHC Level 2
						int ord = 14532 + (lastByte - 0xA1) * 84;

						if (b >= 0x41 && b <= 0x5A)
							ord += b - 0x41;
						else if (b >= 0x61 && b <= 0x7A)
							ord += b - 0x61 + 26;
						else if (b >= 0x81 && b <= 0xA0)
							ord += b - 0x81 + 52;
						else
							ord = -1;

						if (ord >= 0 && ord * 2 <= convert.n2u.Length)
							c1 = (char)(convert.n2u [ord * 2] +
							convert.n2u [ord * 2 + 1] * 256);
						else
							c1 = (char)0;
					} else if (b >= 0xA1 && b <= 0xFE) { // KS X 1001
						int ord = ((lastByte - 0xA1) * 94 + b - 0xA1) * 2;

						c1 = ord < 0 || ord >= convert.n2u.Length ?
                            '\0' : (char)(convert.n2u [ord] +
						convert.n2u [ord + 1] * 256);
					} else
						c1 = (char)0;

					if (c1 == 0)
						chars [charIndex++] = '?';
					else
						chars [charIndex++] = c1;
					lastByte = 0;
				}

				if (lastByte != 0) {
					if (flush) {
						chars [charIndex++] = '?';
						last_byte_conv = 0;
					} else
						last_byte_conv = lastByte;
				}
				return charIndex - origIndex;
			}
		}
	}

	class ENCuhc : CP949
	{
	}

	class ENCeuc_kr: CP51949
	{
	}
}
