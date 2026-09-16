using SharedHelper.General;

namespace OES.Helper.General
{
    public static class RandomGenerator
    {
        private static Random Random = new Random();

        public static string GenerateString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

            return new string(Enumerable
                .Repeat(chars, length)
                .Select(x => x[Random.Next(x.Length)])
                .ToArray());
        }

        public static int GenerateNumber(int min, int max)
        {
            return Random.Next(min, max);
        }

        public static string GenerateItemBankSignatureCode(string ItembankName)
        {
            if (string.IsNullOrEmpty(ItembankName))
            {
                throw new ArgumentException("Domain Name Cannot Be Null Or Empty...", nameof(ItembankName));
            }

            long datePart = DateTimeHelper.Now.Ticks;

            return $"IB-{ItembankName}-{datePart}";
        }

        public static string GenerateIloSignatureCode(string iloName)
        {
            if (string.IsNullOrWhiteSpace(iloName))
            {
                throw new ArgumentException("Ilo Name Cannot Be Null Or Empty...", nameof(iloName));
            }

            long datePart = DateTimeHelper.Now.Ticks;

            return $"ILO-{iloName}-{datePart}";
        }

        public static string GenerateOrganizationStructureRootNodeSignature(string rootNodeName)
        {
            if (string.IsNullOrWhiteSpace(rootNodeName))
            {
                throw new ArgumentException("Organization structure root node name cannot be null or empty", nameof(rootNodeName));
            }

            long currentDateTimeTicks = DateTimeHelper.Now.Ticks;

            return $"OS-{rootNodeName}-{currentDateTimeTicks}";
        }
    }
}
