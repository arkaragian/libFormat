using System.Reflection;

namespace libFormat;

/// <summary>
/// A class that provides width for the given list of files
/// </summary>
public class TextWidthProvider {

    public static int GetWidestText(List<string> values) {
        int width = 0;
        foreach (string s in values) {
            if (s.Length > width) {
                width = s.Length;
            }
        }
        return width;
    }


    /// <summary>
    /// Retrieves the width of the widest text containeed in the <paramref name="propertyName"/>
    /// property for the object insances contained in the <paramref name="objects"/> argument.
    /// </summary>
    public static int GetWidestText<T>(List<T>? objects, string propertyName) {

        if (objects is null || objects.Count is 0) {
            return 0;
        }

        List<string> strings = [];

        foreach (T obj in objects) {
            if (obj is null) {
                continue;
            }

            PropertyInfo[] props = obj.GetType().GetProperties();
            foreach (PropertyInfo prop in props) {
                if (prop.Name == propertyName) {
                    string? s = (string?)prop.GetValue(obj);
                    if (s is not null) {
                        strings.Add(s!);
                    }
                }
            }
        }

        if (strings.Count is 0) {
            return 0;
        }


        return GetWidestText(strings);
    }
}
