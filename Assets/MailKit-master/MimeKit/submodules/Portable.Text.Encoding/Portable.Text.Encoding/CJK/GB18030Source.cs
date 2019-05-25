//
// GB18030Encoding.cs
//
// Author:
//	Atsushi Enomoto  <atsushi@ximian.com>
//
using System;
using System.Reflection;

namespace Portable.Text
{
	unsafe class GB18030Source
	{
		class GB18030Map
		{
			public readonly int UStart;
			public readonly int UEnd;
			public readonly long GStart;
			public readonly long GEnd;
			public readonly bool Dummy; // This range is actually not usable.

			public GB18030Map (int ustart, int uend, long gstart, long gend, bool dummy)
			{
				this.UStart = ustart;
				this.UEnd = uend;
				this.GStart = gstart;
				this.GEnd = gend;
				this.Dummy = dummy;
			}
		}

		GB18030Source ()
		{
		}

		static readonly byte[] gbx2uni;
		static readonly byte[] uni2gbx;

		static GB18030Source ()
		{
			var assembly = typeof (GB18030Source).GetTypeInfo ().Assembly;

			using (var stream = assembly.GetManifestResourceStream ("gb18030.table")) {
				var buf = new byte[4];
				int size;

				stream.Read (buf, 0, buf.Length);
				size = (buf[0] << 24) + (buf[1] << 16) + (buf[2] << 8) + (buf[3]);

				gbx2uni = new byte[size];
				stream.Read (gbx2uni, 0, size);

				stream.Read (buf, 0, buf.Length);
				size = (buf[0] << 24) + (buf[1] << 16) + (buf[2] << 8) + (buf[3]);

				uni2gbx = new byte[size];
				stream.Read (uni2gbx, 0, size);
			}
		}

		static readonly long gbxBase = FromGBXRaw (0x81, 0x30, 0x81, 0x30, false);
		static readonly long gbxSuppBase = FromGBXRaw (0x90, 0x30, 0x81, 0x30, false);

		// See http://icu.sourceforge.net/docs/papers/gb18030.html
		// and referenced XML mapping table.
		static readonly GB18030Map [] ranges = {
			// rawmap: 0x0080-0x0451
			new GB18030Map (0x0452, 0x200F, FromGBXRaw (0x81, 0x30, 0xD3, 0x30, false), FromGBXRaw (0x81, 0x36, 0xA5, 0x31, false), false),
			// rawmap: 0x2010-0x2642
			new GB18030Map (0x2643, 0x2E80, FromGBXRaw (0x81, 0x37, 0xA8, 0x39, false), FromGBXRaw (0x81, 0x38, 0xFD, 0x38, false), false),
			// rawmap: 0x2E81-0x361A
			new GB18030Map (0x361B, 0x3917, FromGBXRaw (0x82, 0x30, 0xA6, 0x33, false), FromGBXRaw (0x82, 0x30, 0xF2, 0x37, false), false),
			// rawmap: 0x3918-0x3CE0
			new GB18030Map (0x3CE1, 0x4055, FromGBXRaw (0x82, 0x31, 0xD4, 0x38, false), FromGBXRaw (0x82, 0x32, 0xAF, 0x32, false), false),
			// rawmap: 0x4056-0x415F
			new GB18030Map (0x4160, 0x4336, FromGBXRaw (0x82, 0x32, 0xC9, 0x37, false), FromGBXRaw (0x82, 0x32, 0xF8, 0x37, false), false),
			// rawmap: 4337-0x44D6
			new GB18030Map (0x44D7, 0x464B, FromGBXRaw (0x82, 0x33, 0xA3, 0x39, false), FromGBXRaw (0x82, 0x33, 0xC9, 0x31, false), false),
			// rawmap: 0x464C-0x478D
			new GB18030Map (0x478E, 0x4946, FromGBXRaw (0x82, 0x33, 0xE8, 0x38, false), FromGBXRaw (0x82, 0x34, 0x96, 0x38, false), false),
			// rawmap: 0x4947-0x49B7
			new GB18030Map (0x49B8, 0x4C76, FromGBXRaw (0x82, 0x34, 0xA1, 0x31, false), FromGBXRaw (0x82, 0x34, 0xE7, 0x33, false), false),
			// rawmap: 0x4C77-0x4DFF

			// 4E00-9FA5 are all mapped in GB2312
			new GB18030Map (0x4E00, 0x9FA5, 0, 0, true),

			new GB18030Map (0x9FA6, 0xD7FF, FromGBXRaw (0x82, 0x35, 0x8F, 0x33, false), FromGBXRaw (0x83, 0x36, 0xC7, 0x38, false), false),

			// D800-DFFF are ignored (surrogate)
			// E000-E76B are all mapped in GB2312.
			new GB18030Map (0xD800, 0xE76B, 0, 0, true),

			// rawmap: 0xE76C-E884
			new GB18030Map (0xE865, 0xF92B, FromGBXRaw (0x83, 0x36, 0xD0, 0x30, false), FromGBXRaw (0x84, 0x30, 0x85, 0x34, false), false),
			// rawmap: 0xF92C-FA29
			new GB18030Map (0xFA2A, 0xFE2F, FromGBXRaw (0x84, 0x30, 0x9C, 0x38, false), FromGBXRaw (0x84, 0x31, 0x85, 0x37, false), false),
			// rawmap: 0xFE30-FFE5
			new GB18030Map (0xFFE6, 0xFFFF, FromGBXRaw (0x84, 0x31, 0xA2, 0x34, false), FromGBXRaw (0x84, 0x31, 0xA4, 0x39, false), false),
		};

		public static void Unlinear (byte [] bytes, int start, long gbx)
		{
			fixed (byte* bptr = bytes) {
				Unlinear (bptr + start, gbx);
			}
		}

		public static unsafe void Unlinear (byte* bytes, long gbx)
		{
			bytes [3] = (byte) (gbx % 10 + 0x30);
			gbx /= 10;
			bytes [2] = (byte) (gbx % 126 + 0x81);
			gbx /= 126;
			bytes [1] = (byte) (gbx % 10 + 0x30);
			gbx /= 10;
			bytes [0] = (byte) (gbx + 0x81);
		}

		// negative (invalid) or positive (valid)
		public static long FromGBX (byte [] bytes, int start)
		{
			byte b1 = bytes [start];
			byte b2 = bytes [start + 1];
			byte b3 = bytes [start + 2];
			byte b4 = bytes [start + 3];

			if (b1 < 0x81 || b1 == 0xFF)
				return -1;
			if (b2 < 0x30 || b2 > 0x39)
				return -2;
			if (b3 < 0x81 || b3 == 0xFF)
				return -3;
			if (b4 < 0x30 || b4 > 0x39)
				return -4;

			if (b1 >= 0x90)
				return FromGBXRaw (b1, b2, b3, b4, true);

			long linear = FromGBXRaw (b1, b2, b3, b4, false);

			long rawOffset = 0;
			long startIgnore = 0;

			foreach (var range in ranges) {
				if (linear < range.GStart)
					return ToUcsRaw ((int)(linear - startIgnore + rawOffset));

				if (linear <= range.GEnd)
					return linear - gbxBase - range.GStart + range.UStart;

				if (range.GStart != 0) {
					rawOffset += range.GStart - startIgnore;
					startIgnore = range.GEnd + 1;
				}
			}

//			return ToUcsRaw ((int) (linear - gbxBase));
			throw new Exception (string.Format ("GB18030 INTERNAL ERROR (should not happen): GBX {0:x02} {1:x02} {2:x02} {3:x02}", b1, b2, b3, b4));
		}

		public static long FromUCSSurrogate (int cp)
		{
			return cp + gbxSuppBase;
		}

		public static long FromUCS (int cp)
		{
			long startIgnore = 0x80;
			long rawOffset = 0;

			foreach (var range in ranges) {
				if (cp < range.UStart)
					return ToGbxRaw ((int) (cp - startIgnore + rawOffset));

				if (cp <= range.UEnd)
					return cp - range.UStart + range.GStart;

				if (range.GStart != 0) {
					rawOffset += range.UStart - startIgnore;
					startIgnore = range.UEnd + 1;
				}
			}

			throw new Exception (String.Format ("GB18030 INTERNAL ERROR (should not happen): UCS {0:x06}", cp));
		}

		static long FromGBXRaw (byte b1, byte b2, byte b3, byte b4, bool supp)
		{
			// 126 = 0xFE - 0x80
			return (((b1 - (supp ? 0x90 : 0x81)) * 10 +
				(b2 - 0x30)) * 126 +
				(b3 - 0x81)) * 10 +
				b4 - 0x30 + (supp ? 0x10000 : 0);
		}

		static int ToUcsRaw (int idx)
		{
			return gbx2uni[idx * 2] * 0x100 + gbx2uni[idx * 2 + 1];
		}

		static long ToGbxRaw (int idx)
		{
			if (idx < 0 || idx * 2 + 1 >= uni2gbx.Length)
				return -1;

			return gbxBase + uni2gbx[idx * 2] * 0x100 + uni2gbx[idx * 2 + 1];
		}
	}
}
