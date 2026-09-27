// post-2.0 BCL members as extension methods. namespaces match the real
// types so existing usings pick them up.

using System.Globalization;

namespace System
{
    public static class EnumExtensions
    {
        // Enum.HasFlag (4.0)
        public static bool HasFlag(this Enum value, Enum flag)
        {
            if (flag == null)
                throw new ArgumentNullException("flag");
            if (value.GetType() != flag.GetType())
                throw new ArgumentException("Enum types don't match.", "flag");
            ulong bits = Convert.ToUInt64(flag, CultureInfo.InvariantCulture);
            return (Convert.ToUInt64(value, CultureInfo.InvariantCulture) & bits) == bits;
        }
    }

    // statics can't be extensions, call sites use these under #if NET20
    public static class Net20
    {
        // string.IsNullOrWhiteSpace (4.0)
        public static bool IsNullOrWhiteSpace(string value)
        {
            if (value == null)
                return true;
            for (int i = 0; i < value.Length; i++)
                if (!char.IsWhiteSpace(value[i]))
                    return false;
            return true;
        }

        // Enum.TryParse<TEnum>(string, bool, out TEnum) (4.0)
        public static bool EnumTryParse<TEnum>(string value, bool ignoreCase, out TEnum result) where TEnum : struct
        {
            result = default(TEnum);
            if (value == null)
                return false;
            value = value.Trim();
            if (value.Length == 0)
                return false;
            try
            {
                result = (TEnum)Enum.Parse(typeof(TEnum), value, ignoreCase);
                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
            catch (OverflowException)
            {
                return false;
            }
        }

        // Environment.Is64BitOperatingSystem (4.0)
        public static bool Is64BitOperatingSystem
        {
            get
            {
                return IntPtr.Size == 8 ||
                       !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("PROCESSOR_ARCHITEW6432"));
            }
        }
    }
}

namespace System.Text
{
    public static class StringBuilderExtensions
    {
        // StringBuilder.Clear (4.0)
        public static StringBuilder Clear(this StringBuilder builder)
        {
            builder.Length = 0;
            return builder;
        }
    }
}

namespace System.Reflection
{
    public static class PropertyInfoExtensions
    {
        // PropertyInfo.GetValue(object) / SetValue(object, object) (4.5)
        public static object GetValue(this PropertyInfo property, object obj)
        {
            return property.GetValue(obj, null);
        }

        public static void SetValue(this PropertyInfo property, object obj, object value)
        {
            property.SetValue(obj, value, null);
        }
    }
}
