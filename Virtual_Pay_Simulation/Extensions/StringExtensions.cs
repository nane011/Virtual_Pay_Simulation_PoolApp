using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Virtual_Pay_Simulation.Extensions
{
	public static class StringExtensions
	{
		/// <summary>
		/// Kart numarasının sadece son 4 hanesini gösterir
		/// </summary>
		/// <param name="source">Orijinal string</param>
		/// <param name="visibleLength">Gösterilecek karakter sayısı</param>
		/// <returns>Maskelenmiş string</returns>
		public static string Mask(this string source, int visibleLength)
		{
			if (string.IsNullOrEmpty(source))
				return string.Empty;

			if (visibleLength >= source.Length)
				return source;

			var masked = new string('*', source.Length - visibleLength) +
						source.Substring(source.Length - visibleLength);

			// 4'lü gruplara ayırma (1111 1111 1111 1111 formatı için)
			return string.Join(" ", Regex.Matches(masked, ".{4}").Select(m => m.Value));
		}
	}
}