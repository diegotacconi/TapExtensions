// For barcode labels using the data structure syntax of ISO 15434 and ISO 15418 standards

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TapExtensions.Instruments.BarcodeScanner
{
    public class BarcodeUtility
    {
        public static string GetProductCode(byte[] rawBytes)
        {
            var header = new byte[] { 0x31, 0x50 }; // Manufacturer Partnumber Data Identifiers. 0x31 0x50 = 1P
            return SectionToString(rawBytes, header);
        }

        public static string GetSerialNumber(byte[] rawBytes)
        {
            var header = new byte[] { 0x53 }; // Serial Number Data Identifiers. 0x53 = S
            return SectionToString(rawBytes, header);
        }

        public static string SectionToString(byte[] rawBytes, byte[] headerIdentifier)
        {
            if (rawBytes == null || rawBytes.Length == 0)
                throw new InvalidOperationException($"{nameof(rawBytes)} is null or empty");

            var sectionBytes = GetSection(rawBytes, headerIdentifier);
            var section = AsciiBytesToString(sectionBytes);
            return section;
        }

        private static string AsciiBytesToString(byte[] bytes)
        {
            var msg = new StringBuilder();
            if (bytes != null && bytes.Length != 0)
            {
                foreach (var b in bytes)
                {
                    if (b >= 0x20 && b <= 0x7E)
                        msg.Append((char)b);
                    else
                        msg.Append("{" + b.ToString("X2") + "}");
                }
            }
            return msg.ToString();
        }

        private static byte[] GetSection(byte[] source, byte[] header)
        {
            const byte gs = 0x1D; // Group Separator
            const byte rs = 0x1E; // Record Separator
            var delimiters = new List<byte>() { gs, rs };
            var sections = Split(source, delimiters);

            foreach (var section in sections)
            {
                if (FindPattern(section, header) == 0)
                {
                    var sectionWithoutHeader = new byte[section.Length - header.Length];
                    Array.Copy(section, header.Length, sectionWithoutHeader, 0, section.Length - header.Length);
                    return sectionWithoutHeader;
                }
            }

            throw new InvalidOperationException(
                $"Cannot find section with header of '{AsciiBytesToString(header)}'");
        }

        private static int FindPattern(byte[] source, byte[] pattern)
        {
            var j = -1;
            for (var i = 0; i < source.Length; i++)
                if (source.Skip(i).Take(pattern.Length).SequenceEqual(pattern))
                    j = i;

            return j;
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