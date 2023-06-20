// Methods for parsing barcode labels using the data structure syntax of ISO 15434 and ISO 15418 standards

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TapExtensions.Shared
{
    public static class BarcodeLabelParser
    {
        public static string GetProductCode(byte[] bytes)
        {
            var eofBytes = new byte[] { 0x5B, 0x45, 0x4F, 0x54, 0x5D }; // End of Transmission (as an array or ASCII characters) [EOT]

            if (IndexOf(bytes, eofBytes) >= 0)
                return SectionToString(bytes, EHeader.ProductCode);

            return null;
        }

        public static string GetSerialNumber(byte[] bytes)
        {
            var eofBytes = new byte[] { 0x5B, 0x45, 0x4F, 0x54, 0x5D }; // End of Transmission (as an array or ASCII characters) [EOT]

            if (IndexOf(bytes, eofBytes) >= 0)
                return SectionToString(bytes, EHeader.SerialNumber);

            return null;
        }

        private enum EHeader
        {
            ProductCode,     // 1P
            SerialNumber,    // S
            Company,         // 18V
            Date,            // 10D
            Traceability,    // 1T
            CountryOfOrigin, // 4L
            Quantity         // Q
        }

        private static byte[] GetHeaderIdentifier(EHeader header)
        {
            byte[] identifier;
            switch (header)
            {
                case EHeader.ProductCode:
                    identifier = new byte[] { 0x31, 0x50 }; // 1P = Manufacturer Product Code
                    break;
                case EHeader.SerialNumber:
                    identifier = new byte[] { 0x53 }; // S = Serial Number
                    break;
                case EHeader.Company:
                    identifier = new byte[] { 0x31, 0x38, 0x56 }; // 18V = Company Identification Number (CIN)
                    break;
                case EHeader.Date:
                    identifier = new byte[] { 0x31, 0x30, 0x44 }; // 10D = Date (Format YYWW)
                    break;
                case EHeader.Traceability:
                    identifier = new byte[] { 0x31, 0x54 }; // 1T = Traceability Number (Lot/Batch Number)
                    break;
                case EHeader.CountryOfOrigin:
                    identifier = new byte[] { 0x34, 0x4c }; // 4L = Country of Origin, two-character ISO 3166 country code
                    break;
                case EHeader.Quantity:
                    identifier = new byte[] { 0x51 }; // Q = Quantity, Number of Pieces, or Amount (numeric only)
                    break;
                default:
                    throw new ArgumentException(
                        $"Case not found for {nameof(header)}={header}");
            }

            return identifier;
        }

        private static string SectionToString(byte[] rawBytes, EHeader header)
        {
            if (rawBytes == null || rawBytes.Length == 0)
                throw new InvalidOperationException($"{nameof(rawBytes)} is null or empty");

            var headerIdentifier = GetHeaderIdentifier(header);
            var sectionBytes = GetSection(rawBytes, headerIdentifier);
            var section = AsciiBytesToString(sectionBytes);
            return section;
        }

        private static string AsciiBytesToString(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
                return null;

            var msg = new StringBuilder();
            foreach (var b in bytes)
            {
                if (b >= 0x20 && b <= 0x7E)
                    msg.Append((char)b);
                else
                    msg.Append("{" + b.ToString("X2") + "}");
            }
            return msg.ToString();
        }

        private static byte[] GetSection(byte[] source, byte[] header)
        {
            var gsBytes = new byte[] { 0x5B, 0x47, 0x53, 0x5D }; // Group Separator (as an array or ASCII characters) [GS]
            var gsByte = new byte[] { 0x1D }; // Group Separator (as a single byte)
            var rsBytes = new byte[] { 0x5B, 0x52, 0x53, 0x5D }; // Record Separator (as an array or ASCII characters) [RS]
            var rsByte = new byte[] { 0x1E }; // Record Separator (as a single byte)
            var eofBytes = new byte[] { 0x5B, 0x45, 0x4F, 0x54, 0x5D }; // End of Transmission (as an array or ASCII characters) [EOT]
            var eofByte = new byte[] { 0x04 }; // End of Transmission (as a single byte)

            var temp1 = Replace(source, eofBytes, eofByte);
            var temp2 = Replace(temp1, gsBytes, gsByte);
            var temp3 = Replace(temp2, rsBytes, rsByte);

            const byte gs = 0x1D; // Group Separator (as a single byte)
            const byte rs = 0x1E; // Record Separator (as a single byte)
            const byte eof = 0x04; // End of Transmission (as a single byte)

            var delimiters = new List<byte> { gs, rs, eof };
            var sections = Split(temp3, delimiters);

            foreach (var section in sections)
            {
                var beginningSection = section.Take(header.Length).ToArray();
                if (beginningSection.SequenceEqual(header))
                {
                    var sectionWithoutHeader = new byte[section.Length - header.Length];
                    Array.Copy(section, header.Length, sectionWithoutHeader, 0, section.Length - header.Length);
                    return sectionWithoutHeader;
                }
            }

            throw new InvalidOperationException(
                $"Cannot find section with header of '{AsciiBytesToString(header)}'");
        }

        private static int IndexOf(byte[] source, byte[] find)
        {
            if (source == null || find == null || source.Length == 0 || find.Length == 0 || find.Length > source.Length)
                return -1;

            for (var i = 0; i < source.Length - find.Length + 1; i++)
            {
                if (source[i] != find[0]) // compare only first byte
                    continue;

                // found a match on first byte, now try to match rest of the pattern
                for (var j = find.Length - 1; j >= 1; j--)
                {
                    if (source[i + j] != find[j]) break;
                    if (j == 1) return i;
                }
            }

            return -1;
        }

        private static byte[] Replace(byte[] source, byte[] find, byte[] replace)
        {
            var destination = source;
            byte[] temp = null;
            var index = IndexOf(source, find);
            while (index >= 0)
            {
                if (temp == null)
                    temp = source;
                else
                    temp = destination;

                destination = new byte[temp.Length - find.Length + replace.Length];

                // before found array
                Buffer.BlockCopy(temp, 0, destination, 0, index);

                // replace copy
                Buffer.BlockCopy(replace, 0, destination, index, replace.Length);

                // rest of source array
                Buffer.BlockCopy(
                    temp,
                    index + find.Length,
                    destination,
                    index + replace.Length,
                    temp.Length - (index + find.Length));

                index = IndexOf(destination, find);
            }

            return destination;
        }

        private static List<byte[]> Split(byte[] source, List<byte> delimiters)
        {
            var parts = new List<byte[]>();
            var startIndex = 0;
            byte[] part;

            // Find delimiters
            for (var i = 0; i < source.Length; i++)
            {
                if (Equals(source[i], delimiters))
                {
                    part = new byte[i - startIndex];
                    Array.Copy(source, startIndex, part, 0, part.Length);
                    parts.Add(part);
                    startIndex = i + 1;
                }
            }

            // Remaining part
            part = new byte[source.Length - startIndex];
            Array.Copy(source, startIndex, part, 0, part.Length);
            parts.Add(part);

            // Remove empty parts
            parts.RemoveAll(b => b.Length == 0);

            return parts;
        }

        private static bool Equals(byte source, List<byte> delimiters)
        {
            foreach (var delimiter in delimiters)
                if (source == delimiter)
                    return true;

            return false;
        }
    }
}