using Newtonsoft.Json;
using System;
using System.Text.RegularExpressions;

namespace DAL
{

    public class Record
    {
        public int Id { get; set; }

        virtual public bool IsValid()
        {
            return true;
        }
        public static bool IsAlpha(string input)
        {
            return !string.IsNullOrEmpty(input) &&
                   Regex.IsMatch(input, @"^[a-zA-Z\- 'ààâäæáãåāèéêëęėēîïīįíìôōøõóòöœùûüūúÿçćčńñÀÂÄÆÁÃÅĀÈÉÊËĘĖĒÎÏĪĮÍÌÔŌØÕÓÒÖŒÙÛÜŪÚŸÇĆČŃÑ]*$");
        }
        public bool HasRequiredLength(string input, int length)
        {
            return !string.IsNullOrEmpty(input) && input.Length >= length;
        }
        public static bool IsEmail(string input)
        {
            return !string.IsNullOrEmpty(input) &&
                   Regex.IsMatch(input, @"^[^@\s]+@[^@\s]+\.[^@\s]+$$");
        }
        public static bool IsURL(string input)
        {
            return !string.IsNullOrEmpty(input) &&
                   Regex.IsMatch(input, @"(http|https):\/\/(\w+:{0,1}\w*)?(\S+)(:[0-9]+)?(\/|\/([\w#!:.?+=&%!\-\/]))?$");
        }
        public static bool IsPhone(string input)
        {
            return !string.IsNullOrEmpty(input) &&
                   Regex.IsMatch(input, @"^$$?([0-9]{3})$$?[-. ]?([0-9]{3})[-. ]?([0-9]{4})$");
        }

        // Todo Add more syntax validators
    }
    public static class JsonUtilities
    {

        public static T Copy<T>(this T source)
        {
            if (Object.ReferenceEquals(source, null))
            {
                return default(T);
            }
            return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(source));
        }
    }
}
