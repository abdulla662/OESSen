using System.Security.Cryptography;

namespace OES.Helper.General
{
    public static class CustomShuffle
    {
        public static void Shuffle<T>(this List<T> choices) where T : class
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                for (int i = choices.Count - 1; i > 0; i--)
                {
                    byte[] buffer = new byte[4];
                    rng.GetBytes(buffer);
                    int j = BitConverter.ToInt32(buffer, 0) & int.MaxValue % (i + 1);
                    (choices[i], choices[j]) = (choices[j], choices[i]);
                }
            }
        }
    }
}
