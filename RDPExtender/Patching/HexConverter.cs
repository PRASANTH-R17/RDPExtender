using System;
using System.Text;

namespace RDPExtender.Patching;

internal static class HexConverter
{
    public static string BytesToHexString(byte[] bytes)
    {
        if (bytes.Length == 0)
        {
            return string.Empty;
        }

        // Each byte becomes "XX " (3 chars) except the last which is "XX" (2 chars).
        var sb = new StringBuilder(bytes.Length * 3 - 1);
        for (int i = 0; i < bytes.Length; i++)
        {
            if (i > 0)
            {
                sb.Append(' ');
            }
            sb.Append(bytes[i].ToString("X2"));
        }
        return sb.ToString();
    }

    public static byte[] HexStringToBytes(string hex)
    {
        var tokens = hex.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var result = new byte[tokens.Length];
        for (int i = 0; i < tokens.Length; i++)
        {
            result[i] = Convert.ToByte(tokens[i], 16);
        }
        return result;
    }
}
