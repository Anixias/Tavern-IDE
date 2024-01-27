using System;
using System.IO;
using System.Text;

namespace Tavern;

public static class EncodingManager
{
    public enum EndOfLine
    {
        Unknown = -1,
        LF,
        CRLF,
        CR,
        Mixed
    }

    public enum Tab
    {
        Unknown = -1,
        Tab,
        Space,
        Mixed
    }
    
    public static EndOfLine DetectEol(string content)
    {
        const string crlf = "\r\n";
        const string lf = "\n";
        const string cr = "\r";

        var containsCRLF = content.Contains(crlf);
        var contentWithoutCRLF = content.Replace(crlf, "");
        var containsLF = contentWithoutCRLF.Contains(lf);
        var containsCR = contentWithoutCRLF.Contains(cr);

        switch (containsCRLF)
        {
            case true when containsLF && containsCR:
                return EndOfLine.Mixed;
            case true:
                return EndOfLine.CRLF;
        }

        if (containsLF)
            return EndOfLine.LF;
        
        if (containsCR)
            return EndOfLine.CR;

        #if GODOT_WINDOWS
        return EndOfLine.CRLF;
        #elif GODOT_LINUX
        return EndOfLine.LF;
        #elif GODOT_MACOS
        return EndOfLine.CR;
        #else
        return EndOfLine.Unknown;
        #endif
    }
    
	// Function to detect the encoding for UTF-7, UTF-8/16/32 (bom, no bom, little
    // & big endian), and local default codepage, and potentially other codepages.
    // 'taster' = number of bytes to check of the file (to save processing). Higher
    // value is slower, but more reliable (especially UTF-8 with special characters
    // later on may appear to be ASCII initially). If taster = 0, then taster
    // becomes the length of the file (for maximum reliability). 'text' is simply
    // the string with the discovered encoding applied to the file.
    public static Encoding DetectTextEncoding(string filename, out string text, int sampleSize = 1000)
    {
        var bytes = File.ReadAllBytes(filename);

        switch (bytes.Length)
        {
            //////////////// First check the low hanging fruit by checking if a
            //////////////// BOM/signature exists (sourced from http://www.unicode.org/faq/utf_bom.html#bom4)
            case >= 4 when bytes[0] == 0x00 && bytes[1] == 0x00 && bytes[2] == 0xFE && bytes[3] == 0xFF:
                text = Encoding.GetEncoding("utf-32BE").GetString(bytes, 4, bytes.Length - 4);
                return Encoding.GetEncoding("utf-32BE"); // UTF-32, big-endian 
            case >= 4 when bytes[0] == 0xFF && bytes[1] == 0xFE && bytes[2] == 0x00 && bytes[3] == 0x00:
                text = Encoding.UTF32.GetString(bytes, 4, bytes.Length - 4);
                return Encoding.UTF32; // UTF-32, little-endian
            case >= 2 when bytes[0] == 0xFE && bytes[1] == 0xFF:
                text = Encoding.BigEndianUnicode.GetString(bytes, 2, bytes.Length - 2);
                return Encoding.BigEndianUnicode; // UTF-16, big-endian
            case >= 2 when bytes[0] == 0xFF && bytes[1] == 0xFE:
                text = Encoding.Unicode.GetString(bytes, 2, bytes.Length - 2);
                return Encoding.Unicode; // UTF-16, little-endian
            case >= 3 when bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF:
                text = Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);
                return Encoding.UTF8; // UTF-8
            case >= 3 when bytes[0] == 0x2b && bytes[1] == 0x2f && bytes[2] == 0x76:
                text = Encoding.UTF7.GetString(bytes,3,bytes.Length-3);
                return Encoding.UTF7; // UTF-7
        }

            
        //////////// If the code reaches here, no BOM/signature was found, so now
        //////////// we need to 'taste' the file to see if can manually discover
        //////////// the encoding. A high taster value is desired for UTF-8
        if (sampleSize == 0 || sampleSize > bytes.Length)
            sampleSize = bytes.Length;    // Taster size can't be bigger than the filesize obviously.


        // Some text files are encoded in UTF8, but have no BOM/signature. Hence
        // the below manually checks for a UTF8 pattern. This code is based off
        // the top answer at: https://stackoverflow.com/questions/6555015/check-for-invalid-utf8
        // For our purposes, an unnecessarily strict (and terser/slower)
        // implementation is shown at: https://stackoverflow.com/questions/1031645/how-to-detect-utf-8-in-plain-c
        // For the below, false positives should be exceedingly rare (and would
        // be either slightly malformed UTF-8 (which would suit our purposes
        // anyway) or 8-bit extended ASCII/UTF-16/32 at a vanishingly long shot).
        var i = 0;
        var utf8 = false;
        while (i < sampleSize - 4)
        {
            switch (bytes[i])
            {
                case <= 0x7F:
                    // If all characters are below 0x80, then it is valid UTF8, but UTF8 is not 'required'
                    // (and therefore the text is more desirable to be treated as the default codepage of the computer).
                    // Hence, there's no "utf8 = true;" code unlike the next three checks.
                    i += 1;
                    continue;
                case >= 0xC2 and < 0xE0 when bytes[i + 1] >= 0x80 && bytes[i + 1] < 0xC0:
                    i += 2;
                    utf8 = true;
                    continue;
            }

            if (bytes[i] >= 0xE0 && bytes[i] < 0xF0 && bytes[i + 1] >= 0x80 && bytes[i + 1] < 0xC0 &&
                bytes[i + 2] >= 0x80 && bytes[i + 2] < 0xC0)
            {
                i += 3;
                utf8 = true;
                continue;
            };

            if (bytes[i] >= 0xF0 && bytes[i] < 0xF5 && bytes[i + 1] >= 0x80 && bytes[i + 1] < 0xC0 &&
                bytes[i + 2] >= 0x80 && bytes[i + 2] < 0xC0 && bytes[i + 3] >= 0x80 && bytes[i + 3] < 0xC0)
            {
                i += 4;
                utf8 = true;
                continue;
            }
            
            utf8 = false;
            break;
        }
        
        if (utf8)
        {
            text = Encoding.UTF8.GetString(bytes);
            return Encoding.UTF8;
        }


        // The next check is a heuristic attempt to detect UTF-16 without a BOM.
        // We simply look for zeroes in odd or even byte places, and if a certain
        // threshold is reached, the code is 'probably' UF-16.          
        const double threshold = 0.1; // proportion of chars step 2 which must be zeroed to be diagnosed as utf-16. 0.1 = 10%
        var count = 0;
        for (var n = 0; n < sampleSize; n += 2)
            if (bytes[n] == 0)
                count++;

        if ((double)count / sampleSize > threshold)
        {
            // (big-endian)
            text = Encoding.BigEndianUnicode.GetString(bytes);
            return Encoding.BigEndianUnicode;
        }
        
        count = 0;
        for (var n = 1; n < sampleSize; n += 2)
            if (bytes[n] == 0)
                count++;

        if ((double)count / sampleSize > threshold)
        {
            // (little-endian)
            text = Encoding.Unicode.GetString(bytes);
            return Encoding.Unicode;
        }


        // Finally, a long shot - let's see if we can find "charset=xyz" or
        // "encoding=xyz" to identify the encoding:
        for (var n = 0; n < sampleSize-9; n++)
        {
            if (((bytes[n + 0] != 'c' && bytes[n + 0] != 'C') || (bytes[n + 1] != 'h' && bytes[n + 1] != 'H') ||
                 (bytes[n + 2] != 'a' && bytes[n + 2] != 'A') || (bytes[n + 3] != 'r' && bytes[n + 3] != 'R') ||
                 (bytes[n + 4] != 's' && bytes[n + 4] != 'S') || (bytes[n + 5] != 'e' && bytes[n + 5] != 'E') ||
                 (bytes[n + 6] != 't' && bytes[n + 6] != 'T') || (bytes[n + 7] != '=')) &&
                ((bytes[n + 0] != 'e' && bytes[n + 0] != 'E') || (bytes[n + 1] != 'n' && bytes[n + 1] != 'N') ||
                 (bytes[n + 2] != 'c' && bytes[n + 2] != 'C') || (bytes[n + 3] != 'o' && bytes[n + 3] != 'O') ||
                 (bytes[n + 4] != 'd' && bytes[n + 4] != 'D') || (bytes[n + 5] != 'i' && bytes[n + 5] != 'I') ||
                 (bytes[n + 6] != 'n' && bytes[n + 6] != 'N') || (bytes[n + 7] != 'g' && bytes[n + 7] != 'G') ||
                 bytes[n + 8] != '='))
                continue;
            
            if (bytes[n + 0] == 'c' || bytes[n + 0] == 'C')
                n += 8;
            else
                n += 9;
            
            if (bytes[n] == '"' || bytes[n] == '\'')
                n++;
            
            var oldN = n;
            while (n < sampleSize && (bytes[n] == '_' || bytes[n] == '-' || (bytes[n] >= '0' && bytes[n] <= '9') ||
                                      (bytes[n] >= 'a' && bytes[n] <= 'z') || (bytes[n] >= 'A' && bytes[n] <= 'Z')))
            {
                n++;
            }
            
            var nb = new byte[n - oldN];
            Array.Copy(bytes, oldN, nb, 0, n - oldN);

            try
            {
                var internalEncoding = Encoding.ASCII.GetString(nb);
                text = Encoding.GetEncoding(internalEncoding).GetString(bytes);
                return Encoding.GetEncoding(internalEncoding);
            }
            catch
            {
                // If C# doesn't recognize the name of the encoding, break.
                break;
            }
        }


        // If all else fails, the encoding is probably (though certainly not
        // definitely) the user's local codepage! One might present to the user a
        // list of alternative encodings as shown here: https://stackoverflow.com/questions/8509339/what-is-the-most-common-encoding-of-each-language
        // A full list can be found using Encoding.GetEncodings();
        text = Encoding.Default.GetString(bytes);
        return Encoding.Default;
    }

    public static string ConvertEol(string text, EndOfLine endOfLine)
    {
        // Normalize line endings to \n
        var normalized = text.Replace("\r\n", "\n").Replace("\r", "\n");

        // Convert line endings to the desired format
        return endOfLine switch
        {
            EndOfLine.CRLF => normalized.Replace("\n", "\r\n"),
            EndOfLine.CR   => normalized.Replace("\n", "\r"),
            EndOfLine.LF   => normalized,
            _              => text
        };
    }

    public static Tab DetectTab(string content)
    {
        var lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

        var tabCount = 0;
        var spaceCount = 0;
        
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            switch (line[0])
            {
                case '\t':
                    tabCount++;
                    break;
                case ' ':
                    spaceCount++;
                    break;
            }
        }

        if (tabCount > 0 && spaceCount > 0)
            return Tab.Mixed;
        
        if (spaceCount > 0)
            return Tab.Space;
        
        return Tab.Tab;
    }
}