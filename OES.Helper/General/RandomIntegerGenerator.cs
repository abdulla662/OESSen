using System.Security.Cryptography;
using SharedHelper.General;

namespace OES.Helper.General
{
    public static class RandomIntegerGenerator
    {
        public static long GenerateShortUniqueNumber()
        {
            return ((DateTimeHelper.Now.Ticks % 100000000) * 100) + RandomNumberGenerator.GetInt32(100);
        }
    }
}
