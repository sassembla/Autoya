/*
 * UTF7Encoding.cs - Implementation of the
 *		"System.Text.UTF7Encoding" class.
 *
 * Copyright (c) 2002  Southern Storm Software, Pty Ltd
 * Copyright (c) 2003, 2004, Novell, Inc.
 *
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the "Software"),
 * to deal in the Software without restriction, including without limitation
 * the rights to use, copy, modify, merge, publish, distribute, sublicense,
 * and/or sell copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included
 * in all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS
 * OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL
 * THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR
 * OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE,
 * ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
 * OTHER DEALINGS IN THE SOFTWARE.
 */

using System;

namespace Portable.Text
{
	public class UTF7Encoding : Encoding
	{
		// Magic number used by Windows for UTF-7.
		internal const int UTF7_CODE_PAGE = 65000;

		// Internal state.
		readonly bool allowOptionals;

		// Encoding rule table for 0x00-0x7F.
		// 0 - full encode, 1 - direct, 2 - optional, 3 - encode plus.
		static readonly byte[] encodingRules = {
			0, 0, 0, 0, 0, 0, 0, 0,   0, 1, 1, 0, 0, 1, 0, 0,	// 00
			0, 0, 0, 0, 0, 0, 0, 0,   0, 0, 0, 0, 0, 0, 0, 0,	// 10
			1, 2, 2, 2, 2, 2, 2, 1,   1, 1, 2, 3, 1, 1, 1, 1,	// 20
			1, 1, 1, 1, 1, 1, 1, 1,   1, 1, 1, 2, 2, 2, 2, 1,	// 30

			2, 1, 1, 1, 1, 1, 1, 1,   1, 1, 1, 1, 1, 1, 1, 1,	// 40
			1, 1, 1, 1, 1, 1, 1, 1,   1, 1, 1, 2, 0, 2, 2, 2,	// 50
			2, 1, 1, 1, 1, 1, 1, 1,   1, 1, 1, 1, 1, 1, 1, 1,	// 60
			1, 1, 1, 1, 1, 1, 1, 1,   1, 1, 1, 2, 2, 2, 0, 0,	// 70
		};

		// Characters to use to encode 6-bit values in base64.
		const string base64Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/";

		// Map bytes in base64 to 6-bit values.
		static readonly sbyte[] base64Values = {
			-1, -1, -1, -1, -1, -1, -1, -1,   -1, -1, -1, -1, -1, -1, -1, -1, // 00
			-1, -1, -1, -1, -1, -1, -1, -1,   -1, -1, -1, -1, -1, -1, -1, -1, // 10
			-1, -1, -1, -1, -1, -1, -1, -1,   -1, -1, -1, 62, -1, -1, -1, 63, // 20
			52, 53, 54, 55, 56, 57, 58, 59,   60, 61, -1, -1, -1, -1, -1, -1, // 30

			-1,  0,  1,  2,  3,  4,  5,  6,    7,  8,  9, 10, 11, 12, 13, 14, // 40
			15, 16, 17, 18, 19, 20, 21, 22,   23, 24, 25, -1, -1, -1, -1, -1, // 50
			-1, 26, 27, 28, 29, 30, 31, 32,   33, 34, 35, 36, 37, 38, 39, 40, // 60
			41, 42, 43, 44, 45, 46, 47, 48,   49, 50, 51, -1, -1, -1, -1, -1, // 70

			-1, -1, -1, -1, -1, -1, -1, -1,   -1, -1, -1, -1, -1, -1, -1, -1, // 80
			-1, -1, -1, -1, -1, -1, -1, -1,   -1, -1, -1, -1, -1, -1, -1, -1, // 90
			-1, -1, -1, -1, -1, -1, -1, -1,   -1, -1, -1, -1, -1, -1, -1, -1, // A0
			-1, -1, -1, -1, -1, -1, -1, -1,   -1, -1, -1, -1, -1, -1, -1, -1, // B0

			-1, -1, -1, -1, -1, -1, -1, -1,   -1, -1, -1, -1, -1, -1, -1, -1, // C0
			-1, -1, -1, -1, -1, -1, -1, -1,   -1, -1, -1, -1, -1, -1, -1, -1, // D0
			-1, -1, -1, -1, -1, -1, -1, -1,   -1, -1, -1, -1, -1, -1, -1, -1, // E0
			-1, -1, -1, -1, -1, -1, -1, -1,   -1, -1, -1, -1, -1, -1, -1, -1, // F0
		};

		// Constructors.
		public UTF7Encoding () : this (false)
		{
		}
	
		public UTF7Encoding (bool allowOptionals) : base (UTF7_CODE_PAGE)
		{
			this.allowOptionals = allowOptionals;

			body_name = "utf-7";
			encoding_name = "Unicode (UTF-7)";
			header_name = "utf-7";
			is_mail_news_display = true;
			is_mail_news_save = true;
			web_name = "utf-7";
			windows_code_page = UnicodeEncoding.UNICODE_CODE_PAGE;
		}

		public override int GetHashCode ()
		{
			int basis = base.GetHashCode ();
			return allowOptionals ? -basis : basis;
		}

		public override bool Equals (object obj)
		{
			var encoding = obj as UTF7Encoding;

			return encoding != null && allowOptionals == encoding.allowOptionals && EncoderFallback.Equals (encoding.EncoderFallback) && DecoderFallback.Equals (encoding.DecoderFallback);
		}

		// Internal version of "GetByteCount" that can handle
		// a rolling state between calls.
		static int InternalGetByteCount (char[] chars, int index, int count, bool flush, int leftOver, bool isInShifted, bool allowOptionals)
		{
			// Validate the parameters.
			if (chars == null)
				throw new ArgumentNullException ("chars");

			if (index < 0 || index > chars.Length)
				throw new ArgumentOutOfRangeException ("index");

			if (count < 0 || count > (chars.Length - index))
				throw new ArgumentOutOfRangeException ("count");

			// Determine the length of the output.
			int leftOverSize = (leftOver >> 8);
			var rules = encodingRules;
			int length = 0;
			int ch, rule;

			while (count > 0) {
				ch = (int)chars[index++];
				--count;

				rule = ch < 0x0080 ? rules[ch] : 0;

				switch (rule) {
				case 0:
					// Handle characters that must be fully encoded.
					if (!isInShifted) {
						++length;
						leftOverSize = 0;
						isInShifted = true;
					}

					leftOverSize += 16;
					while (leftOverSize >= 6) {
						++length;
						leftOverSize -= 6;
					}
					break;
				case 1:
					// The character is encoded as itself.
					if (isInShifted) {
						if (leftOverSize != 0) {
							// Flush the previous encoded sequence.
							++length;
							leftOverSize = 0;
						}
						// Count the "-" (sequence terminator)
						++length;
						isInShifted = false;
					}
					++length;
					break;
				case 2:
					// The character may need to be encoded.
					if (allowOptionals)
						goto case 1;

					goto case 0;
				case 3:
					// Encode the plus sign as "+-".
					if (isInShifted) {
						if (leftOverSize != 0) {
							// Flush the previous encoded sequence.
							++length;
							leftOverSize = 0;
						}
						// Count the "-" (sequence terminator)
						++length;
						isInShifted = false;
					}
					length += 2;
					break;
				}
			}

			if (isInShifted && flush) {
				if (leftOverSize != 0) {
					// Flush the previous encoded sequence.
					++length;
				}

				// Count the "-" (sequence terminator)
				++length;
			}

			// Return the length to the caller.
			return length;
		}

		// Get the number of bytes needed to encode a character buffer.
		public override int GetByteCount (char[] chars, int index, int count)
		{
			return InternalGetByteCount (chars, index, count, true, 0, false, allowOptionals);
		}

		// Internal version of "GetBytes" that can handle a
		// rolling state between calls.
		static int InternalGetBytes (char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex, bool flush, ref int leftOver, ref bool isInShifted, bool allowOptionals)
		{
			// Validate the parameters.
			if (chars == null)
				throw new ArgumentNullException ("chars");

			if (bytes == null)
				throw new ArgumentNullException ("bytes");

			if (charIndex < 0 || charIndex > chars.Length)
				throw new ArgumentOutOfRangeException ("charIndex");

			if (charCount < 0 || charCount > (chars.Length - charIndex))
				throw new ArgumentOutOfRangeException ("charCount");

			if (byteIndex < 0 || byteIndex > bytes.Length)
				throw new ArgumentOutOfRangeException ("byteIndex");

			// Convert the characters.
			int leftOverBits = (leftOver & 0xFF);
			int leftOverSize = (leftOver >> 8);
			int byteLength = bytes.Length;
			var rules = encodingRules;
			var base64 = base64Chars;
			int posn = byteIndex;
			int ch, rule;

			while (charCount > 0) {
				ch = (int) chars[charIndex++];
				--charCount;

				rule = ch < 0x0080 ? rules[ch] : 0;

				switch (rule) {
				case 0:
					// Handle characters that must be fully encoded.
					if (!isInShifted) {
						if (posn >= byteLength)
							throw new ArgumentException ("Insufficient space available.", "bytes");

						// Start the sequence
						bytes[posn++] = (byte)'+';
						isInShifted = true;
						leftOverSize = 0;
					}

					leftOverBits = ((leftOverBits << 16) | ch);
					leftOverSize += 16;

					while (leftOverSize >= 6) {
						if (posn >= byteLength)
							throw new ArgumentException ("Insufficient space available.", "bytes");

						leftOverSize -= 6;
						bytes[posn++] = (byte) base64 [leftOverBits >> leftOverSize];
						leftOverBits &= ((1 << leftOverSize) - 1);
					}
					break;
				case 1:
					// The character is encoded as itself.
					if (isInShifted) {
						if (leftOverSize != 0) {
							// Flush the previous encoded sequence.
							if ((posn + 1) > byteLength)
								throw new ArgumentException ("Insufficient space available.", "bytes");

							bytes[posn++] = (byte) base64 [leftOverBits << (6 - leftOverSize)];
						}

						if ((posn + 1) > byteLength)
							throw new ArgumentException ("Insufficient space available.", "bytes");

						// Terminate the sequence
						bytes[posn++] = (byte)'-';
						isInShifted = false;
						leftOverSize = 0;
						leftOverBits = 0;
					}

					if (posn >= byteLength)
						throw new ArgumentException ("Insufficient space available.", "bytes");

					bytes[posn++] = (byte) ch;
					break;
				case 2:
					// The character may need to be encoded.
					if (allowOptionals)
						goto case 1;

					goto case 0;
				case 3:
					// Encode the plus sign as "+-".
					if (isInShifted) {
						if (leftOverSize != 0) {
							// Flush the previous encoded sequence.
							if ((posn + 1) > byteLength)
								throw new ArgumentException ("Insufficient space available.", "bytes");

							bytes[posn++] = (byte) base64 [leftOverBits << (6 - leftOverSize)];
						}

						if ((posn + 1) > byteLength)
							throw new ArgumentException ("Insufficient space available.", "bytes");

						// Terminate the sequence
						bytes[posn++] = (byte) '-';
						isInShifted = false;
						leftOverSize = 0;
						leftOverBits = 0;
					}

					if ((posn + 2) > byteLength)
						throw new ArgumentException ("Insufficient space available.", "bytes");

					bytes[posn++] = (byte) '+';
					bytes[posn++] = (byte) '-';
					break;
				}
			}

			if (isInShifted && flush) {
				// Flush the previous encoded sequence.
				if (leftOverSize != 0) {
					if ((posn + 1) > byteLength)
						throw new ArgumentException ("Insufficient space available.", "bytes");

					bytes[posn++] = (byte) base64 [leftOverBits << (6 - leftOverSize)];
				}

				// Terminate the sequence
				bytes[posn++] = (byte) '-';
				isInShifted = false;
				leftOverSize = 0;
				leftOverBits = 0;
			}

			leftOver = (leftOverSize << 8) | leftOverBits;

			// Return the length to the caller.
			return posn - byteIndex;
		}

		// Get the bytes that result from encoding a character buffer.
		public override int GetBytes (char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex)
		{
			bool isInShifted = false;
			int leftOver = 0;

			return InternalGetBytes (chars, charIndex, charCount, bytes, byteIndex, true, ref leftOver, ref isInShifted, allowOptionals);
		}

		// Internal version of "GetCharCount" that can handle
		// a rolling state between call.s
		static int InternalGetCharCount (byte[] bytes, int index, int count, int leftOver)
		{
			// Validate the parameters.
			if (bytes == null)
				throw new ArgumentNullException ("bytes");

			if (index < 0 || index > bytes.Length)
				throw new ArgumentOutOfRangeException ("index");

			if (count < 0 || count > (bytes.Length - index))
				throw new ArgumentOutOfRangeException ("count");

			// Determine the length of the result.
			bool prevIsPlus = ((leftOver & 0x02000000) != 0);
			int leftOverSize = ((leftOver >> 16) & 0xFF);
			bool normal = ((leftOver & 0x01000000) == 0);
			sbyte[] base64 = base64Values;
			int length = 0;
			int byteval;

			while (count > 0) {
				byteval = (int) bytes[index++];
				--count;

				if (normal) {
					if (byteval != '+') {
						// Directly-encoded character.
						++length;
					} else {
						// Start of a base64-encoded character.
						normal = false;
						prevIsPlus = true;
					}
				} else {
					// Process the next byte in a base64 sequence.
					if (byteval == (int)'-') {
						// End of a base64 sequence.
						if (prevIsPlus)
							++length;

						leftOverSize = 0;
						normal = true;
					} else if (base64 [byteval] != -1) {
						// Extra character in a base64 sequence.
						leftOverSize += 6;
						if (leftOverSize >= 16) {
							++length;
							leftOverSize -= 16;
						}
					} else {
						++length;
						normal = true;
						leftOverSize = 0;
					}

					prevIsPlus = false;
				}
			}

			// Return the final length to the caller.
			return length;
		}

		// Get the number of characters needed to decode a byte buffer.
		public override int GetCharCount (byte[] bytes, int index, int count)
		{
			return InternalGetCharCount (bytes, index, count, 0);
		}

		// Internal version of "GetChars" that can handle a
		// rolling state between calls.
		static int InternalGetChars (byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex, ref int leftOver)
		{
			// Validate the parameters.
			if (bytes == null)
				throw new ArgumentNullException ("bytes");

			if (chars == null)
				throw new ArgumentNullException ("chars");

			if (byteIndex < 0 || byteIndex > bytes.Length)
				throw new ArgumentOutOfRangeException ("byteIndex");

			if (byteCount < 0 || byteCount > (bytes.Length - byteIndex))
				throw new ArgumentOutOfRangeException ("byteCount");

			if (charIndex < 0 || charIndex > chars.Length)
				throw new ArgumentOutOfRangeException ("charIndex");

			// Convert the bytes into characters.
			bool afterHighSurrogate = ((leftOver & 0x04000000) != 0);
			bool prevIsPlus = ((leftOver & 0x02000000) != 0);
			bool normal = ((leftOver & 0x01000000) == 0);
			int leftOverSize = ((leftOver >> 16) & 0xFF);
			int leftOverBits = (leftOver & 0xFFFF);
			int charLength = chars.Length;
			sbyte[] base64 = base64Values;
			int byteval, b64value;
			int posn = charIndex;

			while (byteCount > 0) {
				byteval = (int) bytes[byteIndex++];
				--byteCount;

				if (normal) {
					if (byteval != '+') {
						// Directly-encoded character.
						if (posn >= charLength)
							throw new ArgumentException ("Insufficient space available.", "chars");

						if (afterHighSurrogate)
							throw new ArgumentException ("Invalid UTF-7.", "chars");

						chars[posn++] = (char) byteval;
					} else {
						// Start of a base64-encoded character.
						normal = false;
						prevIsPlus = true;
					}
				} else {
					// Process the next byte in a base64 sequence.
					if (byteval == (int)'-') {
						// End of a base64 sequence.
						if (prevIsPlus) {
							if (posn >= charLength)
								throw new ArgumentException ("Insufficient space available.", "chars");

							if (afterHighSurrogate)
								throw new ArgumentException ("Invalid UTF-7.", "chars");

							chars[posn++] = '+';
						}

						// RFC1642 Rule #2
						// When decoding, any bits at the end of the Modified Base64 sequence that 
						// do not constitute a complete 16-bit Unicode character are discarded. 
						// If such discarded bits are non-zero the sequence is ill-formed.
						normal = true;
						leftOverSize = 0;
						leftOverBits = 0;
					} else if ((b64value = base64[byteval]) != -1) {
						// Extra character in a base64 sequence.
						leftOverBits = (leftOverBits << 6) | b64value;
						leftOverSize += 6;

						if (leftOverSize >= 16) {
							if (posn >= charLength)
								throw new ArgumentException ("Insufficient space available.", "chars");

							leftOverSize -= 16;
							char nextChar = (char) (leftOverBits >> leftOverSize);
							if ((nextChar & 0xFC00) == 0xD800) {
								afterHighSurrogate = true;
							} else if ((nextChar & 0xFC00) == 0xDC00) {
								if (!afterHighSurrogate)
									throw new ArgumentException ("Invalid UTF-7.", "chars");

								afterHighSurrogate = false;
							}

							chars[posn++] = nextChar;
							leftOverBits &= ((1 << leftOverSize) - 1);
						}
					} else {
						if (posn >= charLength)
							throw new ArgumentException ("Insufficient space available.", "chars");

						if (afterHighSurrogate)
							throw new ArgumentException ("Invalid UTF-7.", "chars");

						chars[posn++] = (char) byteval;
						leftOverSize = 0;
						leftOverBits = 0;
						normal = true;
					}

					prevIsPlus = false;
				}
			}

			leftOver = (leftOverBits | (leftOverSize << 16) | (normal ? 0 : 0x01000000) | (prevIsPlus ? 0x02000000 : 0) | (afterHighSurrogate ? 0x04000000 : 0));

			// Return the final length to the caller.
			return posn - charIndex;
		}

		// Get the characters that result from decoding a byte buffer.
		public override int GetChars (byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
		{
			int leftOver = 0;

			int amount = InternalGetChars (bytes, byteIndex, byteCount, chars, charIndex, ref leftOver);

			if ((leftOver & 0x04000000) != 0)
				throw new ArgumentException ("Invalid UTF-7.", "chars");

			return amount;
		}

		// Get the maximum number of bytes needed to encode a
		// specified number of characters.
		public override int GetMaxByteCount (int charCount)
		{
			if (charCount < 0)
				throw new ArgumentOutOfRangeException ("charCount");

			if (charCount == 0)
				return 0;

			return 8 * (charCount / 3) + (charCount % 3) * 3 + 2;
		}

		// Get the maximum number of characters needed to decode a
		// specified number of bytes.
		public override int GetMaxCharCount (int byteCount)
		{
			if (byteCount < 0)
				throw new ArgumentOutOfRangeException ("byteCount");

			return byteCount;
		}

		// Get a UTF7-specific decoder that is attached to this instance.
		public override
		#if STANDALONE
		Decoder
		#else
		System.Text.Decoder
		#endif
		GetDecoder ()
		{
			return new UTF7Decoder ();
		}

		// Get a UTF7-specific encoder that is attached to this instance.
		public override
		#if STANDALONE
		Encoder
		#else
		System.Text.Encoder
		#endif
		GetEncoder ()
		{
			return new UTF7Encoder (allowOptionals);
		}

		// UTF-7 decoder implementation.
		sealed class UTF7Decoder : Decoder
		{
			// Internal state.
			int leftOver;

			// Constructor.
			public UTF7Decoder ()
			{
				leftOver = 0;
			}

			// Override inherited methods.
			public override int GetCharCount (byte[] bytes, int index, int count)
			{
				return InternalGetCharCount (bytes, index, count, leftOver);
			}

			public override int GetChars (byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
			{
				return InternalGetChars (bytes, byteIndex, byteCount, chars, charIndex, ref leftOver);
			}
		}

		// UTF-7 encoder implementation.
		sealed class UTF7Encoder : Encoder
		{
			readonly bool allowOptionals;
			bool isInShifted = false;
			int leftOver = 0;

			// Constructor.
			public UTF7Encoder (bool allowOptionals)
			{
				this.allowOptionals = allowOptionals;
			}

			// Override inherited methods.
			public override int GetByteCount (char[] chars, int index, int count, bool flush)
			{
				return InternalGetByteCount (chars, index, count, flush, leftOver, isInShifted, allowOptionals);
			}

			public override int GetBytes (char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex, bool flush)
			{
				return InternalGetBytes (chars, charIndex, charCount, bytes, byteIndex, flush, ref leftOver, ref isInShifted, allowOptionals);
			}
		}
	}
}
