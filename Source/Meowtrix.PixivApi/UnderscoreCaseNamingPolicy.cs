using System;
using System.Text;
using System.Text.Json;

namespace Meowtrix.PixivApi
{
    internal sealed class UnderscoreCaseNamingPolicy : JsonNamingPolicy
    {
        public override string ConvertName(string name)
        {
            var sb = new StringBuilder();
            sb.Append(char.ToLowerInvariant(name[0]));

            foreach (char ch in name.AsSpan(1))
            {
                if (char.IsUpper(ch))
                {
                    sb.Append('_');
                    sb.Append(char.ToLowerInvariant(ch));
                }
                else
                {
                    sb.Append(ch);
                }
            }

            return sb.ToString();
        }
    }
}
