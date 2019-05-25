/*
 * CodeTable.cs - Implementation of the "System.Text.CodeTable" class.
 *
 * Copyright (c) 2002  Southern Storm Software, Pty Ltd
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
using System.IO;
using System.Reflection;

namespace Portable.Text {
	// This class assists encoding classes for the large CJK character
	// sets by providing pointer access to table data in the resource
	// section of the current assembly.
	//
	// Code tables are named by their resource (e.g. "jis.table") and
	// contain one or more sections.  Each section has an 8-byte header,
	// consisting of a 32-bit section number and a 32-bit section length.
	// The alignment of the data in the table is not guaranteed.
	sealed class CodeTable : IDisposable
	{
		// Internal state.
		Stream stream;

		// Load a code table from the resource section of this assembly.
		public CodeTable (string name)
		{
			stream = typeof (CodeTable).GetTypeInfo ().Assembly.GetManifestResourceStream (name);

			if (stream == null)
				throw new NotSupportedException ("Encoding not supported.");
		}

		// Implement the IDisposable interface.
		public void Dispose ()
		{
			if (stream != null) {
				stream.Dispose ();
				stream = null;
			}
		}

		public byte[] GetSection (int num)
		{
			// If the table has been disposed, then bail out.
			if (stream == null)
				return null;

			// Scan through the stream looking for the section.
			byte[] header = new byte [8];
			long length = stream.Length;
			int sectNum, sectLen;
			long posn = 0;

			while ((posn + 8) <= length) {
				// Read the next header block.
				stream.Position = posn;
				if (stream.Read (header, 0, 8) != 8)
					break;

				// Decode the fields in the header block.
				sectNum = ((int)(header [0])) |
					(((int)(header [1])) << 8) |
					(((int)(header [2])) << 16) |
					(((int)(header [3])) << 24);
				sectLen = ((int)(header [4])) |
					(((int)(header [5])) << 8) |
					(((int)(header [6])) << 16) |
					(((int)(header [7])) << 24);

				// Is this the section we are looking for?
				if (sectNum == num) {
					byte[] buf = new byte [sectLen];
					if (stream.Read (buf, 0, sectLen) != sectLen)
						break;

					return buf;
				}

				// Advance to the next section.
				posn += 8 + sectLen;
			}

			// We were unable to find the requested section.
			return null;
		}
	}
}

