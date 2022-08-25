// Methods for parsing barcode labels using the data structure syntax of ISO 15434 and ISO 15418 standards

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TapExtensions.Instruments.BarcodeScanner
{
    public class BarcodeLabelUtility
    {
        public static string GetProductCode(byte[] rawBytes)
        {
            return SectionToString(rawBytes, EHeader.ProductCode);
        }

        public static string GetSerialNumber(byte[] rawBytes)
        {
            return SectionToString(rawBytes, EHeader.SerialNumber);
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
            var delimiters = new List<byte> { gs, rs };
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