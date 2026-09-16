namespace OES.Helper.General
{
    public static class VerificationCodeHelper
    {
        public static string GenerateVerificationCode(string candidateCode, string registrationNumber)
        {
            var str = candidateCode + registrationNumber;

            int hash = 0;

            for (int i = 0; i < str.Length; i++)
            {
                hash = (hash * 31 + str[i]) & 0xFFFFFF;
            }

            var absHash = Math.Abs(hash);

            var paddedHash = absHash.ToString().PadLeft(6, '0');

            var finalLength = paddedHash.Length;

            var startIndex = Math.Max(0, finalLength - 6);

            return paddedHash.Substring(startIndex);
        }
    }
}
