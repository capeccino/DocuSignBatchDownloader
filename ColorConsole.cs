using System.Text;

namespace DSBatchDownloader
{
  public static class ColorConsole
  {
    //Supported colors
    private static Dictionary<string, ConsoleColor> colorMap = new Dictionary<string, ConsoleColor>()
    {
      {"blue", ConsoleColor.Blue },
      {"darkcyan", ConsoleColor.DarkCyan},
      {"cyan", ConsoleColor.Cyan},
      {"green", ConsoleColor.Green},
      {"red", ConsoleColor.Red},
      {"yellow", ConsoleColor.Yellow}
    };

    public static void WriteMarkedUpString(string message, bool addNewLineAfter = true)
    {
      Console.ResetColor();

      var colorStrings = Parse(message);

      foreach (var colorString in colorStrings)
      {
        if(colorString.Color == null)
          Console.Write(colorString.Value);
        else
        {
          Console.ResetColor();
          Console.ForegroundColor = (ConsoleColor)colorString.Color;
          Console.Write(colorString.Value);
          Console.ResetColor();
        }
      }

      if (addNewLineAfter)
        Console.WriteLine();
    }

    private static List<ColorString> Parse(string message)
    {
      var colorStrings = new List<ColorString>();

      var workingSb = new StringBuilder();

      for(int i = 0; i < message.Length; i++)
      {
        var c = message[i];
        if(c != '<') workingSb.Append(c);
        else
        {
          i++;
          colorStrings.Add(new ColorString(null, workingSb.ToString()));
          workingSb.Clear();

          var colorSb = new StringBuilder();
          while (message[i] != '>')
          {
            if (i + 1 >= message.Length)
              throw new Exception("Invalid color string format");

            colorSb.Append(message[i]);
            i++;
          }
          i++;

          var strColor = colorSb.ToString();
          if (!colorMap.ContainsKey(strColor))
            throw new Exception($"Unsupported color: {strColor}");

          var closerIndex = message.IndexOf($"</{strColor}>", i);
          if(closerIndex < i)
            throw new Exception($"No closing tag for color: {strColor}");

          var phraseSb = new StringBuilder();
          while (i < closerIndex)
          {
            phraseSb.Append(message[i]);
            i++;
          }
          colorStrings.Add(new ColorString(colorMap[strColor], phraseSb.ToString()));

          i = closerIndex + 2 + strColor.Length;
        }
      }

      if (workingSb.Length > 0)
        colorStrings.Add(new ColorString(null, workingSb.ToString()));

      return colorStrings;
    }

    private class ColorString
    {
      public ConsoleColor? Color { get; set; }
      public string Value { get; set; }
      public ColorString(ConsoleColor? color, string value)
      {
        Color = color;
        Value = value;
      }
    }
  }
}
